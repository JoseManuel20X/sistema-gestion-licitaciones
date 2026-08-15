using Licitaciones.Application.Abstracciones;
using Licitaciones.Application.Common;
using Licitaciones.Domain.Aprobaciones;
using Licitaciones.Domain.Common;

namespace Licitaciones.Application.Aprobaciones;

/// <summary>Datos necesarios para crear o editar un nivel de aprobación.</summary>
public sealed record NivelAprobacionEntrada(decimal MontoMinimoCRC, decimal? MontoMaximoCRC, string Aprobador);

/// <summary>Representación de un nivel de aprobación hacia el exterior.</summary>
public sealed record NivelAprobacionDto(
    Guid Id,
    decimal MontoMinimoCRC,
    decimal? MontoMaximoCRC,
    string Aprobador,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    /// <summary>Proyecta la entidad al DTO.</summary>
    public static NivelAprobacionDto Desde(NivelAprobacion nivel)
    {
        ArgumentNullException.ThrowIfNull(nivel);

        return new NivelAprobacionDto(
            nivel.Id,
            nivel.MontoMinimoCRC,
            nivel.MontoMaximoCRC,
            nivel.Aprobador,
            nivel.CreatedAt,
            nivel.UpdatedAt);
    }
}

/// <summary>Casos de uso de los niveles de aprobación (HU-08).</summary>
public sealed class NivelAprobacionServicio
{
    private readonly INivelAprobacionRepositorio _repositorio;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IReloj _reloj;

    public NivelAprobacionServicio(
        INivelAprobacionRepositorio repositorio,
        IUnidadDeTrabajo unidadDeTrabajo,
        IReloj reloj)
    {
        _repositorio = repositorio;
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
    }

    /// <summary>Crea un nivel comprobando que no se traslape con los existentes.</summary>
    public async Task<Resultado<NivelAprobacionDto>> CrearAsync(
        NivelAprobacionEntrada entrada,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        try
        {
            var nivel = NivelAprobacion.Crear(
                entrada.MontoMinimoCRC,
                entrada.MontoMaximoCRC,
                entrada.Aprobador,
                _reloj);

            var existentes = await _repositorio.ListarTodosAsync(cancelacion);
            TablaNivelesAprobacion.GarantizarConsistencia([.. existentes, nivel]);

            _repositorio.Agregar(nivel);
            await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

            return Resultado<NivelAprobacionDto>.Exito(NivelAprobacionDto.Desde(nivel));
        }
        catch (ExcepcionDominio excepcion)
        {
            return Resultado<NivelAprobacionDto>.Fallo(TraductorErrores.Traducir(excepcion));
        }
    }

    /// <summary>Actualiza un nivel comprobando que el conjunto resultante siga siendo consistente.</summary>
    public async Task<Resultado<NivelAprobacionDto>> ActualizarAsync(
        Guid id,
        NivelAprobacionEntrada entrada,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        var nivel = await _repositorio.ObtenerPorIdAsync(id, cancelacion);
        if (nivel is null)
        {
            return NoEncontrado(id);
        }

        try
        {
            nivel.Actualizar(entrada.MontoMinimoCRC, entrada.MontoMaximoCRC, entrada.Aprobador, _reloj);

            var existentes = await _repositorio.ListarTodosAsync(cancelacion);
            TablaNivelesAprobacion.GarantizarConsistencia(existentes.Where(n => n.Id != id).Append(nivel));

            await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

            return Resultado<NivelAprobacionDto>.Exito(NivelAprobacionDto.Desde(nivel));
        }
        catch (ExcepcionDominio excepcion)
        {
            return Resultado<NivelAprobacionDto>.Fallo(TraductorErrores.Traducir(excepcion));
        }
        catch (ExcepcionConcurrencia)
        {
            return Resultado<NivelAprobacionDto>.Fallo(
                CodigosError.ConflictoConcurrencia,
                "El nivel fue modificado por otro usuario. Vuelva a cargarlo e intente de nuevo.",
                TipoError.Concurrencia);
        }
    }

    /// <summary>Consulta un nivel por su identificador.</summary>
    public async Task<Resultado<NivelAprobacionDto>> ObtenerAsync(Guid id, CancellationToken cancelacion = default)
    {
        var nivel = await _repositorio.ObtenerPorIdAsync(id, cancelacion);

        return nivel is null
            ? NoEncontrado(id)
            : Resultado<NivelAprobacionDto>.Exito(NivelAprobacionDto.Desde(nivel));
    }

    /// <summary>Lista todos los niveles ordenados por monto mínimo.</summary>
    public async Task<IReadOnlyList<NivelAprobacionDto>> ListarAsync(CancellationToken cancelacion = default)
    {
        var niveles = await _repositorio.ListarTodosAsync(cancelacion);

        return [.. niveles.Select(NivelAprobacionDto.Desde)];
    }

    /// <summary>Resuelve el aprobador que corresponde a un monto.</summary>
    public async Task<Resultado<NivelAprobacionDto>> ResolverAprobadorAsync(
        decimal montoCRC,
        CancellationToken cancelacion = default)
    {
        var niveles = await _repositorio.ListarTodosAsync(cancelacion);
        var nivel = TablaNivelesAprobacion.ResolverNivel(niveles, montoCRC);

        return nivel is null
            ? Resultado<NivelAprobacionDto>.Fallo(
                CodigosError.SinNivelAprobacionAplicable,
                $"Ningún nivel de aprobación cubre el monto {montoCRC}.",
                TipoError.ReglaNegocio)
            : Resultado<NivelAprobacionDto>.Exito(NivelAprobacionDto.Desde(nivel));
    }

    /// <summary>Elimina un nivel de aprobación.</summary>
    public async Task<Resultado> EliminarAsync(Guid id, CancellationToken cancelacion = default)
    {
        var nivel = await _repositorio.ObtenerPorIdAsync(id, cancelacion);
        if (nivel is null)
        {
            return Resultado.Fallo(
                CodigosError.NivelAprobacionNoEncontrado,
                $"No existe un nivel de aprobación con el identificador {id}.",
                TipoError.NoEncontrado);
        }

        _repositorio.Eliminar(nivel);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        return Resultado.Exito();
    }

    private static Resultado<NivelAprobacionDto> NoEncontrado(Guid id) =>
        Resultado<NivelAprobacionDto>.Fallo(
            CodigosError.NivelAprobacionNoEncontrado,
            $"No existe un nivel de aprobación con el identificador {id}.",
            TipoError.NoEncontrado);
}
