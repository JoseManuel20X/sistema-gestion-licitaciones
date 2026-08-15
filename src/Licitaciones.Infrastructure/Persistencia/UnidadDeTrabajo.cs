using Licitaciones.Application.Abstracciones;
using Licitaciones.Application.Common;
using Licitaciones.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Licitaciones.Infrastructure.Persistencia;

/// <summary>
/// Confirma los cambios y traduce los fallos de PostgreSQL a excepciones propias
/// de la aplicación.
/// </summary>
/// <remarks>
/// Es el único punto donde la solución conoce los códigos de error de
/// PostgreSQL: gracias a eso la capa de aplicación puede distinguir un
/// duplicado de un error genérico sin depender de Entity Framework Core ni del
/// motor (enunciado §11).
/// </remarks>
public sealed class UnidadDeTrabajo : IUnidadDeTrabajo
{
    /// <summary>Violación de restricción única (SQLSTATE 23505).</summary>
    private const string ViolacionUnicidad = "23505";

    /// <summary>Violación de clave foránea (SQLSTATE 23503).</summary>
    private const string ViolacionClaveForanea = "23503";

    /// <summary>Violación de restricción CHECK (SQLSTATE 23514).</summary>
    private const string ViolacionCheck = "23514";

    private readonly LicitacionesDbContext _contexto;

    public UnidadDeTrabajo(LicitacionesDbContext contexto) => _contexto = contexto;

    public async Task<int> GuardarCambiosAsync(CancellationToken cancelacion = default)
    {
        try
        {
            return await _contexto.SaveChangesAsync(cancelacion);
        }
        catch (DbUpdateConcurrencyException excepcion)
        {
            throw new ExcepcionConcurrencia(
                "El registro fue modificado por otro usuario mientras se editaba.",
                excepcion);
        }
        catch (DbUpdateException excepcion) when (excepcion.InnerException is PostgresException postgres)
        {
            throw TraducirErrorPostgres(postgres, excepcion);
        }
    }

    public async Task<T> EnTransaccionAsync<T>(
        Func<CancellationToken, Task<T>> operacion,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(operacion);

        var estrategia = _contexto.Database.CreateExecutionStrategy();

        // La estrategia de reintentos exige envolver la transacción completa para
        // poder repetirla entera si falla por un error transitorio.
        return await estrategia.ExecuteAsync(async () =>
        {
            await using var transaccion = await _contexto.Database.BeginTransactionAsync(cancelacion);

            var resultado = await operacion(cancelacion);

            await transaccion.CommitAsync(cancelacion);
            return resultado;
        });
    }

    private static Exception TraducirErrorPostgres(PostgresException postgres, Exception original) =>
        postgres.SqlState switch
        {
            ViolacionUnicidad => new ExcepcionConflictoPersistencia(
                DeducirCodigoDeDuplicado(postgres.ConstraintName),
                MensajeDeDuplicado(postgres.ConstraintName),
                original),

            ViolacionClaveForanea => new ExcepcionConflictoPersistencia(
                CodigosError.ViolacionIntegridad,
                "La operación no se puede completar porque existen registros relacionados.",
                original),

            ViolacionCheck => new ExcepcionConflictoPersistencia(
                CodigosError.ViolacionIntegridad,
                "Los datos infringen una restricción de integridad de la base de datos.",
                original),

            _ => original,
        };

    private static string DeducirCodigoDeDuplicado(string? restriccion) => restriccion switch
    {
        "ix_proveedores_nombre_normalizado" => CodigosError.NombreProveedorDuplicado,
        "ix_licitaciones_codigo_normalizado" => CodigosError.CodigoLicitacionDuplicado,
        "ix_ofertas_licitacion_proveedor" => CodigosError.OfertaDuplicada,
        "ix_niveles_aprobacion_minimo" => CodigosError.RangoAprobacionTraslapado,
        _ => CodigosError.ViolacionIntegridad,
    };

    private static string MensajeDeDuplicado(string? restriccion) => restriccion switch
    {
        "ix_proveedores_nombre_normalizado" => "Ya existe un proveedor con ese nombre.",
        "ix_licitaciones_codigo_normalizado" => "Ya existe una licitación con ese código.",
        "ix_ofertas_licitacion_proveedor" => "El proveedor ya registró una oferta en esta licitación.",
        "ix_niveles_aprobacion_minimo" => "Ya existe un nivel de aprobación que inicia en ese monto.",
        "ix_tipos_cambio_activo_unico" => "Ya existe un tipo de cambio activo.",
        // No se expone el nombre de la restricción al cliente (enunciado §10.2).
        _ => "La operación infringe una restricción de unicidad.",
    };
}
