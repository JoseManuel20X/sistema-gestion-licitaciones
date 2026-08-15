using Licitaciones.Application.Abstracciones;
using Licitaciones.Application.Common;
using Licitaciones.Domain.Common;
using Licitaciones.Domain.Ofertas;

namespace Licitaciones.Application.Ofertas;

/// <summary>Casos de uso de ofertas (HU-05 y HU-06).</summary>
public sealed class OfertaServicio
{
    private readonly IOfertaRepositorio _repositorio;
    private readonly ILicitacionRepositorio _licitaciones;
    private readonly IProveedorRepositorio _proveedores;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IReloj _reloj;

    public OfertaServicio(
        IOfertaRepositorio repositorio,
        ILicitacionRepositorio licitaciones,
        IProveedorRepositorio proveedores,
        IUnidadDeTrabajo unidadDeTrabajo,
        IReloj reloj)
    {
        _repositorio = repositorio;
        _licitaciones = licitaciones;
        _proveedores = proveedores;
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
    }

    /// <summary>
    /// Registra una oferta en una licitación publicada y vigente.
    /// </summary>
    /// <remarks>
    /// Rechaza la oferta duplicada del mismo proveedor, la que supera el
    /// presupuesto, la de una licitación no publicada y la posterior al cierre.
    /// </remarks>
    public async Task<Resultado<OfertaDto>> RegistrarAsync(
        Guid licitacionId,
        OfertaEntrada entrada,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        var licitacion = await _licitaciones.ObtenerPorIdAsync(licitacionId, cancelacion);
        if (licitacion is null)
        {
            return Resultado<OfertaDto>.Fallo(
                CodigosError.LicitacionNoEncontrada,
                $"No existe una licitación con el identificador {licitacionId}.",
                TipoError.NoEncontrado);
        }

        var proveedor = await _proveedores.ObtenerPorIdAsync(entrada.ProveedorId, cancelacion);
        if (proveedor is null)
        {
            return Resultado<OfertaDto>.Fallo(
                CodigosError.ProveedorNoEncontrado,
                $"No existe un proveedor con el identificador {entrada.ProveedorId}.",
                TipoError.NoEncontrado);
        }

        try
        {
            var yaOferto = await _repositorio.ExisteOfertaDelProveedorAsync(
                licitacionId,
                entrada.ProveedorId,
                null,
                cancelacion);

            if (yaOferto)
            {
                return OfertaDuplicada();
            }

            var oferta = Oferta.Registrar(licitacion, entrada.ProveedorId, entrada.MontoOfertadoCRC, _reloj);

            _repositorio.Agregar(oferta);
            await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

            return Resultado<OfertaDto>.Exito(OfertaDto.Desde(oferta));
        }
        catch (ExcepcionDominio excepcion)
        {
            return Resultado<OfertaDto>.Fallo(TraductorErrores.Traducir(excepcion));
        }
        catch (ExcepcionConflictoPersistencia)
        {
            // El índice único compuesto (LicitacionId, ProveedorId) ganó la carrera.
            return OfertaDuplicada();
        }
    }

    /// <summary>Modifica el monto de una oferta mientras la licitación siga vigente.</summary>
    public async Task<Resultado<OfertaDto>> ActualizarAsync(
        Guid id,
        OfertaActualizacion entrada,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        var oferta = await _repositorio.ObtenerPorIdAsync(id, cancelacion);
        if (oferta is null)
        {
            return NoEncontrada(id);
        }

        var licitacion = await _licitaciones.ObtenerPorIdAsync(oferta.LicitacionId, cancelacion);
        if (licitacion is null)
        {
            return Resultado<OfertaDto>.Fallo(
                CodigosError.LicitacionNoEncontrada,
                "La licitación asociada con la oferta no existe.",
                TipoError.NoEncontrado);
        }

        try
        {
            oferta.CambiarMonto(entrada.MontoOfertadoCRC, licitacion, _reloj);
            await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

            return Resultado<OfertaDto>.Exito(OfertaDto.Desde(oferta));
        }
        catch (ExcepcionDominio excepcion)
        {
            return Resultado<OfertaDto>.Fallo(TraductorErrores.Traducir(excepcion));
        }
        catch (ExcepcionConcurrencia)
        {
            return Resultado<OfertaDto>.Fallo(
                CodigosError.ConflictoConcurrencia,
                "La oferta fue modificada por otro usuario. Vuelva a cargarla e intente de nuevo.",
                TipoError.Concurrencia);
        }
    }

    /// <summary>Consulta una oferta por su identificador.</summary>
    public async Task<Resultado<OfertaDto>> ObtenerAsync(Guid id, CancellationToken cancelacion = default)
    {
        var oferta = await _repositorio.ObtenerPorIdAsync(id, cancelacion);

        return oferta is null ? NoEncontrada(id) : Resultado<OfertaDto>.Exito(OfertaDto.Desde(oferta));
    }

    /// <summary>Lista ofertas con paginación y filtro por licitación y proveedor.</summary>
    public async Task<PaginaResultado<OfertaDto>> ListarAsync(
        ParametrosConsulta consulta,
        Guid? licitacionId = null,
        Guid? proveedorId = null,
        CancellationToken cancelacion = default)
    {
        var pagina = await _repositorio.ListarAsync(consulta, licitacionId, proveedorId, cancelacion);

        return pagina.Proyectar(OfertaDto.Desde);
    }

    /// <summary>
    /// Elimina una oferta. Solo procede mientras la licitación siga publicada y
    /// vigente: las ofertas de licitaciones cerradas se conservan como evidencia
    /// (enunciado §8.9).
    /// </summary>
    public async Task<Resultado> EliminarAsync(Guid id, CancellationToken cancelacion = default)
    {
        var oferta = await _repositorio.ObtenerPorIdAsync(id, cancelacion);
        if (oferta is null)
        {
            return Resultado.Fallo(
                CodigosError.OfertaNoEncontrada,
                $"No existe una oferta con el identificador {id}.",
                TipoError.NoEncontrado);
        }

        var licitacion = await _licitaciones.ObtenerPorIdAsync(oferta.LicitacionId, cancelacion);
        if (licitacion is null)
        {
            return Resultado.Fallo(
                CodigosError.LicitacionNoEncontrada,
                "La licitación asociada con la oferta no existe.",
                TipoError.NoEncontrado);
        }

        try
        {
            licitacion.GarantizarQueAceptaOfertas(_reloj);

            _repositorio.Eliminar(oferta);
            await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

            return Resultado.Exito();
        }
        catch (ExcepcionDominio excepcion)
        {
            return Resultado.Fallo(TraductorErrores.Traducir(excepcion));
        }
        catch (ExcepcionConcurrencia)
        {
            return Resultado.Fallo(
                CodigosError.ConflictoConcurrencia,
                "La oferta fue modificada por otro usuario. Vuelva a cargarla e intente de nuevo.",
                TipoError.Concurrencia);
        }
    }

    private static Resultado<OfertaDto> NoEncontrada(Guid id) =>
        Resultado<OfertaDto>.Fallo(
            CodigosError.OfertaNoEncontrada,
            $"No existe una oferta con el identificador {id}.",
            TipoError.NoEncontrado);

    private static Resultado<OfertaDto> OfertaDuplicada() =>
        Resultado<OfertaDto>.Fallo(
            CodigosError.OfertaDuplicada,
            "El proveedor ya registró una oferta en esta licitación.",
            TipoError.Conflicto);
}
