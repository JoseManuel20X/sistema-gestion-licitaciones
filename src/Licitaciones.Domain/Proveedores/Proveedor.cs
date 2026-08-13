using Licitaciones.Domain.Common;

namespace Licitaciones.Domain.Proveedores;

/// <summary>
/// Empresa o persona habilitada para presentar ofertas en una licitación.
/// </summary>
public sealed class Proveedor : EntidadAuditable
{
    // Constructor sin parámetros requerido por Entity Framework Core.
    private Proveedor()
    {
        Nombre = string.Empty;
        NombreNormalizado = string.Empty;
    }

    /// <summary>Nombre tal como lo escribió el usuario, con espacios ya colapsados.</summary>
    public string Nombre { get; private set; }

    /// <summary>
    /// Nombre normalizado usado para comparar unicidad. Tiene índice único en
    /// PostgreSQL (enunciado §8.3).
    /// </summary>
    public string NombreNormalizado { get; private set; }

    /// <summary>Instante del borrado lógico; <c>null</c> mientras el proveedor esté vigente.</summary>
    public DateTimeOffset? DeletedAt { get; private set; }

    /// <summary>Token de concurrencia optimista respaldado por la columna <c>xmin</c> de PostgreSQL.</summary>
    public uint Version { get; private set; }

    /// <summary>Indica si el proveedor fue dado de baja lógicamente.</summary>
    public bool EstaEliminado => DeletedAt is not null;

    /// <summary>Registra un proveedor nuevo validando el nombre.</summary>
    /// <exception cref="ExcepcionDominio">Si el nombre está vacío o tiene caracteres no permitidos.</exception>
    public static Proveedor Crear(string nombre, IReloj reloj)
    {
        var proveedor = new Proveedor();
        proveedor.AsignarNombre(nombre);
        proveedor.MarcarCreacion(reloj);
        return proveedor;
    }

    /// <summary>Cambia el nombre aplicando las mismas validaciones del registro.</summary>
    public void Renombrar(string nombre, IReloj reloj)
    {
        AsignarNombre(nombre);
        MarcarActualizacion(reloj);
    }

    /// <summary>
    /// Aplica el borrado lógico. Se prefiere sobre el borrado físico porque las
    /// ofertas ya presentadas deben conservarse como evidencia (enunciado §8.9).
    /// </summary>
    public void Eliminar(IReloj reloj)
    {
        ArgumentNullException.ThrowIfNull(reloj);

        if (EstaEliminado)
        {
            return;
        }

        DeletedAt = reloj.AhoraUtc;
        MarcarActualizacion(reloj);
    }

    private void AsignarNombre(string nombre)
    {
        ExcepcionDominio.SiCumple(
            string.IsNullOrWhiteSpace(nombre),
            CodigosError.NombreProveedorVacio,
            "El nombre del proveedor es obligatorio.");

        ExcepcionDominio.SiCumple(
            !Normalizador.EsNombreProveedorValido(nombre),
            CodigosError.NombreProveedorCaracteresInvalidos,
            "El nombre del proveedor solo admite letras, números, espacios, punto, coma y paréntesis.");

        Nombre = Normalizador.LimpiarEspacios(nombre);
        NombreNormalizado = Normalizador.NormalizarNombreProveedor(nombre);
    }
}
