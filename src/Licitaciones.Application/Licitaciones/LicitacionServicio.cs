using Licitaciones.Application.Abstracciones;
using Licitaciones.Application.Common;
using Licitaciones.Domain.Aprobaciones;
using Licitaciones.Domain.Common;
using Licitaciones.Domain.Licitaciones;
using Licitaciones.Domain.Ofertas;

namespace Licitaciones.Application.Licitaciones;

/// <summary>Casos de uso de licitaciones (HU-03, HU-04 y HU-07).</summary>
public sealed class LicitacionServicio
{
    private readonly ILicitacionRepositorio _repositorio;
    private readonly IOfertaRepositorio _ofertasRepositorio;
    private readonly INivelAprobacionRepositorio _nivelesRepositorio;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IReloj _reloj;

    public LicitacionServicio(
        ILicitacionRepositorio repositorio,
        IOfertaRepositorio ofertasRepositorio,
        INivelAprobacionRepositorio nivelesRepositorio,
        IUnidadDeTrabajo unidadDeTrabajo,
        IReloj reloj)
    {
        _repositorio = repositorio;
        _ofertasRepositorio = ofertasRepositorio;
        _nivelesRepositorio = nivelesRepositorio;
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
    }

    /// <summary>Crea una licitación en estado Borrador.</summary>
    public async Task<Resultado<LicitacionDto>> CrearAsync(
        LicitacionEntrada entrada,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        try
        {
            var licitacion = Licitacion.Crear(
                entrada.Codigo,
                entrada.Titulo,
                entrada.PresupuestoEstimadoCRC,
                entrada.FechaCierre,
                _reloj);

            if (await _repositorio.ExisteCodigoAsync(licitacion.CodigoNormalizado, null, cancelacion))
            {
                return CodigoDuplicado(licitacion.Codigo);
            }

            _repositorio.Agregar(licitacion);
            await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

            return Resultado<LicitacionDto>.Exito(LicitacionDto.Desde(licitacion, _reloj));
        }
        catch (ExcepcionDominio excepcion)
        {
            return Resultado<LicitacionDto>.Fallo(TraductorErrores.Traducir(excepcion));
        }
        catch (ExcepcionConflictoPersistencia)
        {
            return CodigoDuplicado(entrada.Codigo);
        }
    }

    /// <summary>Actualiza los datos editables de una licitación no cerrada.</summary>
    public async Task<Resultado<LicitacionDto>> ActualizarAsync(
        Guid id,
        LicitacionEntrada entrada,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        var licitacion = await _repositorio.ObtenerPorIdAsync(id, cancelacion);
        if (licitacion is null)
        {
            return NoEncontrada(id);
        }

        try
        {
            var mayorOferta = await _repositorio.ObtenerMayorOfertaAsync(id, cancelacion);

            licitacion.ActualizarDatos(
                entrada.Codigo,
                entrada.Titulo,
                entrada.PresupuestoEstimadoCRC,
                entrada.FechaCierre,
                mayorOferta,
                _reloj);

            if (await _repositorio.ExisteCodigoAsync(licitacion.CodigoNormalizado, id, cancelacion))
            {
                return CodigoDuplicado(licitacion.Codigo);
            }

            await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

            return Resultado<LicitacionDto>.Exito(LicitacionDto.Desde(licitacion, _reloj));
        }
        catch (ExcepcionDominio excepcion)
        {
            return Resultado<LicitacionDto>.Fallo(TraductorErrores.Traducir(excepcion));
        }
        catch (ExcepcionConcurrencia)
        {
            return Concurrencia();
        }
        catch (ExcepcionConflictoPersistencia)
        {
            return CodigoDuplicado(entrada.Codigo);
        }
    }

    /// <summary>Aplica una transición del ciclo de estados.</summary>
    public async Task<Resultado<LicitacionDto>> CambiarEstadoAsync(
        Guid id,
        TransicionLicitacion transicion,
        CancellationToken cancelacion = default)
    {
        var licitacion = await _repositorio.ObtenerPorIdAsync(id, cancelacion);
        if (licitacion is null)
        {
            return NoEncontrada(id);
        }

        try
        {
            switch (transicion)
            {
                case TransicionLicitacion.Publicar:
                    licitacion.Publicar(_reloj);
                    break;
                case TransicionLicitacion.Cerrar:
                    licitacion.Cerrar(_reloj);
                    break;
                default:
                    return Resultado<LicitacionDto>.Fallo(
                        CodigosError.TransicionEstadoInvalida,
                        $"La transición solicitada no está definida.",
                        TipoError.Validacion);
            }

            await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

            return Resultado<LicitacionDto>.Exito(LicitacionDto.Desde(licitacion, _reloj));
        }
        catch (ExcepcionDominio excepcion)
        {
            return Resultado<LicitacionDto>.Fallo(TraductorErrores.Traducir(excepcion));
        }
        catch (ExcepcionConcurrencia)
        {
            return Concurrencia();
        }
    }

    /// <summary>Consulta una licitación por su identificador.</summary>
    public async Task<Resultado<LicitacionDto>> ObtenerAsync(Guid id, CancellationToken cancelacion = default)
    {
        var licitacion = await _repositorio.ObtenerPorIdAsync(id, cancelacion);

        return licitacion is null
            ? NoEncontrada(id)
            : Resultado<LicitacionDto>.Exito(LicitacionDto.Desde(licitacion, _reloj));
    }

    /// <summary>Lista licitaciones con paginación, filtro y ordenamiento.</summary>
    public async Task<PaginaResultado<LicitacionDto>> ListarAsync(
        ParametrosConsulta consulta,
        CancellationToken cancelacion = default)
    {
        var pagina = await _repositorio.ListarAsync(consulta, cancelacion);

        return pagina.Proyectar(licitacion => LicitacionDto.Desde(licitacion, _reloj));
    }

    /// <summary>
    /// Determina la mejor oferta, su clasificación de ahorro y el aprobador que
    /// corresponde al monto según la tabla de niveles.
    /// </summary>
    public async Task<Resultado<MejorOfertaDto>> ObtenerMejorOfertaAsync(
        Guid id,
        CancellationToken cancelacion = default)
    {
        var licitacion = await _repositorio.ObtenerPorIdAsync(id, cancelacion);
        if (licitacion is null)
        {
            return Resultado<MejorOfertaDto>.Fallo(
                CodigosError.LicitacionNoEncontrada,
                $"No existe una licitación con el identificador {id}.",
                TipoError.NoEncontrado);
        }

        var ofertas = await _ofertasRepositorio.ListarPorLicitacionAsync(id, cancelacion);
        var evaluacion = EvaluadorOfertas.Evaluar(ofertas, licitacion.PresupuestoEstimadoCRC);

        string? aprobador = null;
        if (evaluacion.MejorOferta is { } mejor)
        {
            var niveles = await _nivelesRepositorio.ListarTodosAsync(cancelacion);
            aprobador = TablaNivelesAprobacion.ResolverNivel(niveles, mejor.MontoOfertadoCRC)?.Aprobador;
        }

        return Resultado<MejorOfertaDto>.Exito(MejorOfertaDto.Desde(licitacion, evaluacion, aprobador));
    }

    /// <summary>
    /// Elimina una licitación. Si tiene ofertas se aplica borrado lógico para
    /// conservarlas como evidencia (enunciado §8.9).
    /// </summary>
    public async Task<Resultado> EliminarAsync(Guid id, CancellationToken cancelacion = default)
    {
        var licitacion = await _repositorio.ObtenerPorIdAsync(id, cancelacion);
        if (licitacion is null)
        {
            return Resultado.Fallo(
                CodigosError.LicitacionNoEncontrada,
                $"No existe una licitación con el identificador {id}.",
                TipoError.NoEncontrado);
        }

        try
        {
            if (await _repositorio.TieneOfertasAsync(id, cancelacion))
            {
                licitacion.Eliminar(_reloj);
            }
            else
            {
                _repositorio.Eliminar(licitacion);
            }

            await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

            return Resultado.Exito();
        }
        catch (ExcepcionConcurrencia)
        {
            return Resultado.Fallo(
                CodigosError.ConflictoConcurrencia,
                "La licitación fue modificada por otro usuario. Vuelva a cargarla e intente de nuevo.",
                TipoError.Concurrencia);
        }
        catch (ExcepcionConflictoPersistencia excepcion)
        {
            return Resultado.Fallo(CodigosError.ViolacionIntegridad, excepcion.Message, TipoError.Conflicto);
        }
    }

    private static Resultado<LicitacionDto> NoEncontrada(Guid id) =>
        Resultado<LicitacionDto>.Fallo(
            CodigosError.LicitacionNoEncontrada,
            $"No existe una licitación con el identificador {id}.",
            TipoError.NoEncontrado);

    private static Resultado<LicitacionDto> CodigoDuplicado(string codigo) =>
        Resultado<LicitacionDto>.Fallo(
            CodigosError.CodigoLicitacionDuplicado,
            $"Ya existe una licitación con el código «{codigo}».",
            TipoError.Conflicto);

    private static Resultado<LicitacionDto> Concurrencia() =>
        Resultado<LicitacionDto>.Fallo(
            CodigosError.ConflictoConcurrencia,
            "La licitación fue modificada por otro usuario. Vuelva a cargarla e intente de nuevo.",
            TipoError.Concurrencia);
}
