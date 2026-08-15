using Licitaciones.Domain.Common;

namespace Licitaciones.Infrastructure.Tiempo;

/// <summary>Reloj real del sistema, siempre en UTC.</summary>
/// <remarks>
/// Las comparaciones internas se hacen en UTC; la conversión a
/// <c>America/Costa_Rica</c> ocurre solo al presentar (enunciado §8.2).
/// </remarks>
public sealed class RelojSistema : IReloj
{
    public DateTimeOffset AhoraUtc => DateTimeOffset.UtcNow;
}

/// <summary>Zona horaria de presentación del sistema.</summary>
public static class ZonaHoraria
{
    /// <summary>Identificador IANA de la zona de Costa Rica.</summary>
    public const string CostaRica = "America/Costa_Rica";

    /// <summary>
    /// Zona horaria de Costa Rica, resuelta de forma portable.
    /// </summary>
    /// <remarks>
    /// .NET 8 y posteriores aceptan identificadores IANA también en Windows, pero
    /// el respaldo explícito evita que la aplicación falle si faltan los datos
    /// de zona horaria en una imagen de contenedor mínima.
    /// </remarks>
    public static TimeZoneInfo Resolver()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(CostaRica);
        }
        catch (TimeZoneNotFoundException)
        {
            // Costa Rica no aplica horario de verano: el desfase es fijo.
            return TimeZoneInfo.CreateCustomTimeZone(CostaRica, TimeSpan.FromHours(-6), CostaRica, CostaRica);
        }
    }
}
