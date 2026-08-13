using Licitaciones.Domain.Common;
using Licitaciones.Domain.Proveedores;
using Licitaciones.UnitTests.Common;

namespace Licitaciones.UnitTests.Proveedores;

/// <summary>Reglas de nombre, normalización y unicidad del proveedor (enunciado §8.3 y §8.4).</summary>
public sealed class ProveedorTests
{
    private readonly RelojFalso _reloj = RelojFalso.EnInstanteBase();

    [Theory]
    [InlineData("Empresa Central")]
    [InlineData("empresa central")]
    [InlineData("  EMPRESA   CENTRAL  ")]
    public void NombresEquivalentes_ProducenElMismoNombreNormalizado(string nombre)
    {
        var proveedor = Proveedor.Crear(nombre, _reloj);

        Assert.Equal("EMPRESA CENTRAL", proveedor.NombreNormalizado);
    }

    [Fact]
    public void Crear_ConservaElNombreEscritoPeroColapsaEspacios()
    {
        var proveedor = Proveedor.Crear("  Constructora   del   Valle  ", _reloj);

        Assert.Equal("Constructora del Valle", proveedor.Nombre);
    }

    [Theory]
    [InlineData("Servicios S.A.")]
    [InlineData("Grupo Uno, Dos (Costa Rica)")]
    [InlineData("Constructora 2026")]
    [InlineData("Ñandú Ltda.")]
    public void Crear_AdmiteLetrasNumerosEspaciosPuntoComaYParentesis(string nombre)
    {
        var proveedor = Proveedor.Crear(nombre, _reloj);

        Assert.Equal(nombre, proveedor.Nombre);
    }

    [Theory]
    [InlineData("Empresa @ Central")]
    [InlineData("Servicios #1")]
    [InlineData("Grupo <script>")]
    [InlineData("Proveedor/Socio")]
    public void Crear_RechazaCaracteresNoPermitidos(string nombre)
    {
        var error = Assert.Throws<ExcepcionDominio>(() => Proveedor.Crear(nombre, _reloj));

        Assert.Equal(CodigosError.NombreProveedorCaracteresInvalidos, error.Codigo);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_RechazaNombreVacio(string nombre)
    {
        var error = Assert.Throws<ExcepcionDominio>(() => Proveedor.Crear(nombre, _reloj));

        Assert.Equal(CodigosError.NombreProveedorVacio, error.Codigo);
    }

    [Fact]
    public void Crear_RegistraSellosDeAuditoria()
    {
        var proveedor = Proveedor.Crear("Empresa Central", _reloj);

        Assert.Equal(RelojFalso.InstanteBase, proveedor.CreatedAt);
        Assert.Equal(RelojFalso.InstanteBase, proveedor.UpdatedAt);
    }

    [Fact]
    public void Crear_GeneraIdentificadorAutomatico()
    {
        var primero = Proveedor.Crear("Empresa Central", _reloj);
        var segundo = Proveedor.Crear("Otra Empresa", _reloj);

        Assert.NotEqual(Guid.Empty, primero.Id);
        Assert.NotEqual(primero.Id, segundo.Id);
    }

    [Fact]
    public void Renombrar_ActualizaNombreNormalizadoYSelloDeModificacion()
    {
        var proveedor = Proveedor.Crear("Empresa Central", _reloj);
        _reloj.Avanzar(TimeSpan.FromHours(3));

        proveedor.Renombrar("Empresa Central del Sur", _reloj);

        Assert.Equal("EMPRESA CENTRAL DEL SUR", proveedor.NombreNormalizado);
        Assert.Equal(RelojFalso.InstanteBase, proveedor.CreatedAt);
        Assert.Equal(RelojFalso.InstanteBase.AddHours(3), proveedor.UpdatedAt);
    }

    [Fact]
    public void Renombrar_AplicaLasMismasValidacionesDelRegistro()
    {
        var proveedor = Proveedor.Crear("Empresa Central", _reloj);

        var error = Assert.Throws<ExcepcionDominio>(() => proveedor.Renombrar("Empresa @ Central", _reloj));

        Assert.Equal(CodigosError.NombreProveedorCaracteresInvalidos, error.Codigo);
        Assert.Equal("Empresa Central", proveedor.Nombre);
    }

    [Fact]
    public void Eliminar_AplicaBorradoLogicoSinPerderElRegistro()
    {
        var proveedor = Proveedor.Crear("Empresa Central", _reloj);
        _reloj.Avanzar(TimeSpan.FromDays(1));

        proveedor.Eliminar(_reloj);

        Assert.True(proveedor.EstaEliminado);
        Assert.Equal(_reloj.AhoraUtc, proveedor.DeletedAt);
        Assert.Equal("Empresa Central", proveedor.Nombre);
    }
}
