namespace Licitaciones.Domain.Licitaciones;

/// <summary>Estados del ciclo de vida de una licitación (enunciado §8.1).</summary>
public enum EstadoLicitacion
{
    /// <summary>En preparación; todavía no recibe ofertas.</summary>
    Borrador = 0,

    /// <summary>Convocada y aceptando ofertas hasta la fecha de cierre.</summary>
    Publicada = 1,

    /// <summary>Finalizada; no admite ofertas nuevas ni modificaciones.</summary>
    Cerrada = 2,
}
