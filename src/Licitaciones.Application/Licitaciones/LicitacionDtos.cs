using Licitaciones.Domain.Licitaciones;
using Licitaciones.Domain.Ofertas;

namespace Licitaciones.Application.Licitaciones;

/// <summary>Datos necesarios para crear o editar una licitación.</summary>
public sealed record LicitacionEntrada(
    string Codigo,
    string Titulo,
    decimal PresupuestoEstimadoCRC,
    DateTimeOffset FechaCierre);

/// <summary>Transición de estado solicitada sobre una licitación (enunciado §8.1).</summary>
public enum TransicionLicitacion
{
    /// <summary>Borrador → Publicada.</summary>
    Publicar,

    /// <summary>Borrador o Publicada → Cerrada.</summary>
    Cerrar,
}

/// <summary>Representación de una licitación hacia el exterior.</summary>
public sealed record LicitacionDto(
    Guid Id,
    string Codigo,
    string Titulo,
    string Estado,
    string EstadoEfectivo,
    DateTimeOffset FechaCierre,
    decimal PresupuestoEstimadoCRC,
    bool Vencida,
    bool AceptaOfertas,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool Eliminada)
{
    /// <summary>Proyecta la entidad al DTO resolviendo el estado efectivo con el reloj.</summary>
    public static LicitacionDto Desde(Licitacion licitacion, Domain.Common.IReloj reloj)
    {
        ArgumentNullException.ThrowIfNull(licitacion);
        ArgumentNullException.ThrowIfNull(reloj);

        return new LicitacionDto(
            licitacion.Id,
            licitacion.Codigo,
            licitacion.Titulo,
            licitacion.Estado.ToString(),
            licitacion.EstadoEfectivo(reloj).ToString(),
            licitacion.FechaCierre,
            licitacion.PresupuestoEstimadoCRC,
            licitacion.EstaVencida(reloj),
            licitacion.AceptaOfertas(reloj),
            licitacion.CreatedAt,
            licitacion.UpdatedAt,
            licitacion.EstaEliminada);
    }
}

/// <summary>Resultado de consultar la mejor oferta de una licitación (HU-07).</summary>
/// <param name="LicitacionId">Licitación evaluada.</param>
/// <param name="PresupuestoEstimadoCRC">Presupuesto contra el que se calculó el ahorro.</param>
/// <param name="MejorOfertaId">Oferta ganadora, o <c>null</c> si no hay ofertas.</param>
/// <param name="MontoMejorOfertaCRC">Monto de la oferta ganadora.</param>
/// <param name="ProveedorId">Proveedor de la oferta ganadora.</param>
/// <param name="NombreProveedor">Nombre del proveedor ganador, si se cargó.</param>
/// <param name="PorcentajeAhorro">Ahorro porcentual sobre el presupuesto.</param>
/// <param name="Clasificacion">Etiqueta de clasificación exigida por el enunciado §8.6.</param>
/// <param name="Aprobador">Aprobador que corresponde al monto, según la tabla de niveles.</param>
public sealed record MejorOfertaDto(
    Guid LicitacionId,
    decimal PresupuestoEstimadoCRC,
    Guid? MejorOfertaId,
    decimal? MontoMejorOfertaCRC,
    Guid? ProveedorId,
    string? NombreProveedor,
    decimal? PorcentajeAhorro,
    string Clasificacion,
    string? Aprobador)
{
    /// <summary>Construye el DTO a partir de la evaluación del dominio.</summary>
    public static MejorOfertaDto Desde(
        Licitacion licitacion,
        EvaluacionOfertas evaluacion,
        string? aprobador)
    {
        ArgumentNullException.ThrowIfNull(licitacion);
        ArgumentNullException.ThrowIfNull(evaluacion);

        return new MejorOfertaDto(
            licitacion.Id,
            licitacion.PresupuestoEstimadoCRC,
            evaluacion.MejorOferta?.Id,
            evaluacion.MejorOferta?.MontoOfertadoCRC,
            evaluacion.MejorOferta?.ProveedorId,
            evaluacion.MejorOferta?.Proveedor?.Nombre,
            evaluacion.PorcentajeAhorro,
            evaluacion.Clasificacion.Descripcion(),
            aprobador);
    }
}
