using Licitaciones.Application.Abstracciones;
using Licitaciones.Application.Common;
using Licitaciones.Domain.Common;
using Licitaciones.Domain.TiposCambio;

namespace Licitaciones.Application.TiposCambio;

/// <summary>Datos necesarios para crear o editar un tipo de cambio.</summary>
public sealed record TipoCambioEntrada(decimal CRCporUSD, DateTimeOffset FechaVigencia);

/// <summary>Representación de un tipo de cambio hacia el exterior.</summary>
public sealed record TipoCambioDto(
    Guid Id,
    decimal CRCporUSD,
    DateTimeOffset FechaVigencia,
    bool Activo,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    /// <summary>Proyecta la entidad al DTO.</summary>
    public static TipoCambioDto Desde(TipoCambio tipoCambio)
    {
        ArgumentNullException.ThrowIfNull(tipoCambio);

        return new TipoCambioDto(
            tipoCambio.Id,
            tipoCambio.CRCporUSD,
            tipoCambio.FechaVigencia,
            tipoCambio.Activo,
            tipoCambio.CreatedAt,
            tipoCambio.UpdatedAt);
    }
}

/// <summary>
/// Monto expresado en ambas monedas, con la referencia del tipo de cambio usado.
/// </summary>
/// <param name="MontoCRC">Valor oficial, el único persistido.</param>
/// <param name="MontoUSD">Representación calculada, nunca almacenada.</param>
/// <param name="CRCporUSD">Tipo de cambio aplicado.</param>
/// <param name="FechaVigencia">Fecha del tipo de cambio, que el enunciado §8.8 exige mostrar.</param>
public sealed record MontoConvertidoDto(
    decimal MontoCRC,
    decimal MontoUSD,
    decimal CRCporUSD,
    DateTimeOffset FechaVigencia);

/// <summary>Casos de uso del tipo de cambio (HU-09 y HU-10).</summary>
public sealed class TipoCambioServicio
{
    private readonly ITipoCambioRepositorio _repositorio;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IReloj _reloj;

    public TipoCambioServicio(
        ITipoCambioRepositorio repositorio,
        IUnidadDeTrabajo unidadDeTrabajo,
        IReloj reloj)
    {
        _repositorio = repositorio;
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
    }

    /// <summary>
    /// Registra un tipo de cambio. El primero queda activo automáticamente.
    /// </summary>
    /// <remarks>
    /// Sin ningún registro activo la conversión a dólares no puede calcularse, así
    /// que activar el primero evita dejar la aplicación en un estado inútil que
    /// obligaría a un segundo paso manual.
    /// </remarks>
    public async Task<Resultado<TipoCambioDto>> CrearAsync(
        TipoCambioEntrada entrada,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        try
        {
            var hayActivo = await _repositorio.ObtenerActivoAsync(cancelacion) is not null;

            var tipoCambio = TipoCambio.Crear(
                entrada.CRCporUSD,
                entrada.FechaVigencia,
                activo: !hayActivo,
                _reloj);

            _repositorio.Agregar(tipoCambio);
            await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

            return Resultado<TipoCambioDto>.Exito(TipoCambioDto.Desde(tipoCambio));
        }
        catch (ExcepcionDominio excepcion)
        {
            return Resultado<TipoCambioDto>.Fallo(TraductorErrores.Traducir(excepcion));
        }
    }

    /// <summary>Actualiza el valor y la fecha de vigencia.</summary>
    public async Task<Resultado<TipoCambioDto>> ActualizarAsync(
        Guid id,
        TipoCambioEntrada entrada,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        var tipoCambio = await _repositorio.ObtenerPorIdAsync(id, cancelacion);
        if (tipoCambio is null)
        {
            return NoEncontrado(id);
        }

        try
        {
            tipoCambio.Actualizar(entrada.CRCporUSD, entrada.FechaVigencia, _reloj);
            await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

            return Resultado<TipoCambioDto>.Exito(TipoCambioDto.Desde(tipoCambio));
        }
        catch (ExcepcionDominio excepcion)
        {
            return Resultado<TipoCambioDto>.Fallo(TraductorErrores.Traducir(excepcion));
        }
        catch (ExcepcionConcurrencia)
        {
            return Resultado<TipoCambioDto>.Fallo(
                CodigosError.ConflictoConcurrencia,
                "El tipo de cambio fue modificado por otro usuario. Vuelva a cargarlo e intente de nuevo.",
                TipoError.Concurrencia);
        }
    }

    /// <summary>
    /// Marca un tipo de cambio como el activo y desactiva el anterior.
    /// </summary>
    /// <remarks>
    /// Ambos cambios van en una sola transacción: PostgreSQL tiene un índice único
    /// parcial que solo admite una fila con <c>Activo = true</c>, de modo que una
    /// operación a medias dejaría la tabla sin activo o rechazaría la escritura.
    /// </remarks>
    public async Task<Resultado<TipoCambioDto>> ActivarAsync(Guid id, CancellationToken cancelacion = default)
    {
        var tipoCambio = await _repositorio.ObtenerPorIdAsync(id, cancelacion);
        if (tipoCambio is null)
        {
            return NoEncontrado(id);
        }

        try
        {
            var dto = await _unidadDeTrabajo.EnTransaccionAsync(
                async token =>
                {
                    var activo = await _repositorio.ObtenerActivoAsync(token);

                    if (activo is not null && activo.Id != tipoCambio.Id)
                    {
                        // Se desactiva y se confirma antes de activar el nuevo:
                        // el índice único no admite dos filas activas ni siquiera
                        // de forma momentánea dentro de la transacción.
                        activo.Desactivar(_reloj);
                        await _unidadDeTrabajo.GuardarCambiosAsync(token);
                    }

                    tipoCambio.Activar(_reloj);
                    await _unidadDeTrabajo.GuardarCambiosAsync(token);

                    return TipoCambioDto.Desde(tipoCambio);
                },
                cancelacion);

            return Resultado<TipoCambioDto>.Exito(dto);
        }
        catch (ExcepcionConcurrencia)
        {
            return Resultado<TipoCambioDto>.Fallo(
                CodigosError.ConflictoConcurrencia,
                "Otro usuario cambió el tipo de cambio activo. Vuelva a cargar la lista e intente de nuevo.",
                TipoError.Concurrencia);
        }
        catch (ExcepcionConflictoPersistencia excepcion)
        {
            return Resultado<TipoCambioDto>.Fallo(
                CodigosError.ViolacionIntegridad,
                excepcion.Message,
                TipoError.Conflicto);
        }
    }

    /// <summary>Consulta un tipo de cambio por su identificador.</summary>
    public async Task<Resultado<TipoCambioDto>> ObtenerAsync(Guid id, CancellationToken cancelacion = default)
    {
        var tipoCambio = await _repositorio.ObtenerPorIdAsync(id, cancelacion);

        return tipoCambio is null
            ? NoEncontrado(id)
            : Resultado<TipoCambioDto>.Exito(TipoCambioDto.Desde(tipoCambio));
    }

    /// <summary>Devuelve el tipo de cambio vigente para la operación ordinaria.</summary>
    public async Task<Resultado<TipoCambioDto>> ObtenerActivoAsync(CancellationToken cancelacion = default)
    {
        var activo = await _repositorio.ObtenerActivoAsync(cancelacion);

        return activo is null
            ? Resultado<TipoCambioDto>.Fallo(
                CodigosError.SinTipoCambioActivo,
                "No hay un tipo de cambio activo. Registre uno para poder mostrar montos en dólares.",
                TipoError.ReglaNegocio)
            : Resultado<TipoCambioDto>.Exito(TipoCambioDto.Desde(activo));
    }

    /// <summary>Lista los tipos de cambio, del más vigente al más antiguo.</summary>
    public async Task<IReadOnlyList<TipoCambioDto>> ListarAsync(CancellationToken cancelacion = default)
    {
        var tipos = await _repositorio.ListarTodosAsync(cancelacion);

        return [.. tipos.Select(TipoCambioDto.Desde)];
    }

    /// <summary>
    /// Convierte un monto en colones a dólares con el tipo de cambio activo
    /// (HU-10).
    /// </summary>
    public async Task<Resultado<MontoConvertidoDto>> ConvertirAsync(
        decimal montoCRC,
        CancellationToken cancelacion = default)
    {
        var activo = await _repositorio.ObtenerActivoAsync(cancelacion);

        if (activo is null)
        {
            return Resultado<MontoConvertidoDto>.Fallo(
                CodigosError.SinTipoCambioActivo,
                "No hay un tipo de cambio activo. Registre uno para poder mostrar montos en dólares.",
                TipoError.ReglaNegocio);
        }

        return Resultado<MontoConvertidoDto>.Exito(new MontoConvertidoDto(
            montoCRC,
            activo.ConvertirCrcAUsd(montoCRC),
            activo.CRCporUSD,
            activo.FechaVigencia));
    }

    /// <summary>Elimina un tipo de cambio que no esté activo.</summary>
    public async Task<Resultado> EliminarAsync(Guid id, CancellationToken cancelacion = default)
    {
        var tipoCambio = await _repositorio.ObtenerPorIdAsync(id, cancelacion);
        if (tipoCambio is null)
        {
            return Resultado.Fallo(
                CodigosError.TipoCambioNoEncontrado,
                $"No existe un tipo de cambio con el identificador {id}.",
                TipoError.NoEncontrado);
        }

        if (tipoCambio.Activo)
        {
            // Sin activo la aplicación no puede convertir a dólares; primero hay
            // que activar otro.
            return Resultado.Fallo(
                CodigosError.TipoCambioActivoNoEliminable,
                "No se puede eliminar el tipo de cambio activo. Active otro antes de eliminarlo.",
                TipoError.ReglaNegocio);
        }

        _repositorio.Eliminar(tipoCambio);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        return Resultado.Exito();
    }

    private static Resultado<TipoCambioDto> NoEncontrado(Guid id) =>
        Resultado<TipoCambioDto>.Fallo(
            CodigosError.TipoCambioNoEncontrado,
            $"No existe un tipo de cambio con el identificador {id}.",
            TipoError.NoEncontrado);
}
