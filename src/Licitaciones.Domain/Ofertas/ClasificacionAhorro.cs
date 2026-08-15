namespace Licitaciones.Domain.Ofertas;

/// <summary>Clasificación de la mejor oferta según el ahorro logrado (enunciado §8.6).</summary>
public enum ClasificacionAhorro
{
    /// <summary>La licitación no tiene ofertas válidas.</summary>
    SinOfertasValidas = 0,

    /// <summary>Ahorro igual o superior al 10 %.</summary>
    OfertaConveniente = 1,

    /// <summary>Ahorro mayor que 0 % y menor que 10 %.</summary>
    OfertaAceptable = 2,

    /// <summary>La mejor oferta es igual al presupuesto: no hay ahorro.</summary>
    OfertaValidaSinAhorro = 3,
}

/// <summary>Texto de presentación de cada clasificación, tal como lo exige el enunciado.</summary>
public static class ClasificacionAhorroExtensiones
{
    /// <summary>Devuelve la etiqueta exacta que debe mostrarse al usuario.</summary>
    public static string Descripcion(this ClasificacionAhorro clasificacion) => clasificacion switch
    {
        ClasificacionAhorro.SinOfertasValidas => "Sin ofertas válidas",
        ClasificacionAhorro.OfertaConveniente => "Oferta conveniente",
        ClasificacionAhorro.OfertaAceptable => "Oferta aceptable",
        ClasificacionAhorro.OfertaValidaSinAhorro => "Oferta válida sin ahorro",
        _ => throw new ArgumentOutOfRangeException(nameof(clasificacion)),
    };
}
