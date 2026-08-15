using Licitaciones.Application.Abstracciones;
using Licitaciones.Application.Common;
using Licitaciones.Domain.Common;
using Licitaciones.Domain.Proveedores;

namespace Licitaciones.Application.Proveedores;

/// <summary>Casos de uso de proveedores (HU-01 y HU-02).</summary>
public sealed class ProveedorServicio
{
    private readonly IProveedorRepositorio _repositorio;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IReloj _reloj;

    public ProveedorServicio(IProveedorRepositorio repositorio, IUnidadDeTrabajo unidadDeTrabajo, IReloj reloj)
    {
        _repositorio = repositorio;
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
    }

    /// <summary>Registra un proveedor con nombre único normalizado.</summary>
    public async Task<Resultado<ProveedorDto>> CrearAsync(
        ProveedorEntrada entrada,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        try
        {
            var proveedor = Proveedor.Crear(entrada.Nombre, _reloj);

            if (await _repositorio.ExisteNombreAsync(proveedor.NombreNormalizado, null, cancelacion))
            {
                return Resultado<ProveedorDto>.Fallo(
                    CodigosError.NombreProveedorDuplicado,
                    $"Ya existe un proveedor registrado con el nombre «{proveedor.Nombre}».",
                    TipoError.Conflicto);
            }

            _repositorio.Agregar(proveedor);
            await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

            return Resultado<ProveedorDto>.Exito(ProveedorDto.Desde(proveedor));
        }
        catch (ExcepcionDominio excepcion)
        {
            return Resultado<ProveedorDto>.Fallo(TraductorErrores.Traducir(excepcion));
        }
        catch (ExcepcionConflictoPersistencia excepcion)
        {
            // El índice único de PostgreSQL rechazó el nombre: otra transacción
            // lo insertó entre la comprobación anterior y este guardado.
            return Resultado<ProveedorDto>.Fallo(
                CodigosError.NombreProveedorDuplicado,
                excepcion.Message,
                TipoError.Conflicto);
        }
    }

    /// <summary>Cambia el nombre de un proveedor existente.</summary>
    public async Task<Resultado<ProveedorDto>> ActualizarAsync(
        Guid id,
        ProveedorEntrada entrada,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        var proveedor = await _repositorio.ObtenerPorIdAsync(id, cancelacion);
        if (proveedor is null)
        {
            return NoEncontrado(id);
        }

        try
        {
            proveedor.Renombrar(entrada.Nombre, _reloj);

            if (await _repositorio.ExisteNombreAsync(proveedor.NombreNormalizado, id, cancelacion))
            {
                return Resultado<ProveedorDto>.Fallo(
                    CodigosError.NombreProveedorDuplicado,
                    $"Ya existe otro proveedor registrado con el nombre «{proveedor.Nombre}».",
                    TipoError.Conflicto);
            }

            await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

            return Resultado<ProveedorDto>.Exito(ProveedorDto.Desde(proveedor));
        }
        catch (ExcepcionDominio excepcion)
        {
            return Resultado<ProveedorDto>.Fallo(TraductorErrores.Traducir(excepcion));
        }
        catch (ExcepcionConcurrencia)
        {
            return Resultado<ProveedorDto>.Fallo(
                CodigosError.ConflictoConcurrencia,
                "El proveedor fue modificado por otro usuario. Vuelva a cargarlo e intente de nuevo.",
                TipoError.Concurrencia);
        }
        catch (ExcepcionConflictoPersistencia excepcion)
        {
            return Resultado<ProveedorDto>.Fallo(
                CodigosError.NombreProveedorDuplicado,
                excepcion.Message,
                TipoError.Conflicto);
        }
    }

    /// <summary>Consulta un proveedor por su identificador.</summary>
    public async Task<Resultado<ProveedorDto>> ObtenerAsync(Guid id, CancellationToken cancelacion = default)
    {
        var proveedor = await _repositorio.ObtenerPorIdAsync(id, cancelacion);

        return proveedor is null
            ? NoEncontrado(id)
            : Resultado<ProveedorDto>.Exito(ProveedorDto.Desde(proveedor));
    }

    /// <summary>Lista proveedores con paginación, filtro por nombre y ordenamiento.</summary>
    public async Task<PaginaResultado<ProveedorDto>> ListarAsync(
        ParametrosConsulta consulta,
        CancellationToken cancelacion = default)
    {
        var pagina = await _repositorio.ListarAsync(consulta, cancelacion);

        return pagina.Proyectar(ProveedorDto.Desde);
    }

    /// <summary>
    /// Elimina un proveedor. Si tiene ofertas se aplica borrado lógico para
    /// conservarlas como evidencia (enunciado §8.9).
    /// </summary>
    public async Task<Resultado> EliminarAsync(Guid id, CancellationToken cancelacion = default)
    {
        var proveedor = await _repositorio.ObtenerPorIdAsync(id, cancelacion);
        if (proveedor is null)
        {
            return Resultado.Fallo(
                CodigosError.ProveedorNoEncontrado,
                $"No existe un proveedor con el identificador {id}.",
                TipoError.NoEncontrado);
        }

        try
        {
            if (await _repositorio.TieneOfertasAsync(id, cancelacion))
            {
                proveedor.Eliminar(_reloj);
            }
            else
            {
                _repositorio.Eliminar(proveedor);
            }

            await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

            return Resultado.Exito();
        }
        catch (ExcepcionConcurrencia)
        {
            return Resultado.Fallo(
                CodigosError.ConflictoConcurrencia,
                "El proveedor fue modificado por otro usuario. Vuelva a cargarlo e intente de nuevo.",
                TipoError.Concurrencia);
        }
        catch (ExcepcionConflictoPersistencia excepcion)
        {
            return Resultado.Fallo(
                CodigosError.ViolacionIntegridad,
                excepcion.Message,
                TipoError.Conflicto);
        }
    }

    private static Resultado<ProveedorDto> NoEncontrado(Guid id) =>
        Resultado<ProveedorDto>.Fallo(
            CodigosError.ProveedorNoEncontrado,
            $"No existe un proveedor con el identificador {id}.",
            TipoError.NoEncontrado);
}
