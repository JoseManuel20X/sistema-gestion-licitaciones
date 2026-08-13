using Licitaciones.Domain.Common;
using Licitaciones.Domain.Licitaciones;
using Licitaciones.UnitTests.Common;

namespace Licitaciones.UnitTests.Licitaciones;

/// <summary>Ciclo de estados, vencimiento y reglas de presupuesto (enunciado §8.1, §8.2 y §8.5).</summary>
public sealed class LicitacionTests
{
    private readonly RelojFalso _reloj = RelojFalso.EnInstanteBase();

    [Fact]
    public void Crear_NaceEnBorrador()
    {
        var licitacion = ConstructorLicitacion.EnBorrador(_reloj);

        Assert.Equal(EstadoLicitacion.Borrador, licitacion.Estado);
    }

    [Theory]
    [InlineData("lic-2026-001")]
    [InlineData("  LIC-2026-001  ")]
    [InlineData("Lic-2026-001")]
    public void Crear_NormalizaElCodigoIgnorandoEspaciosYMayusculas(string codigo)
    {
        var licitacion = ConstructorLicitacion.EnBorrador(_reloj, codigo: codigo);

        Assert.Equal("LIC-2026-001", licitacion.CodigoNormalizado);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-1000.50)]
    public void Crear_RechazaPresupuestoNoPositivo(decimal presupuesto)
    {
        var error = Assert.Throws<ExcepcionDominio>(
            () => ConstructorLicitacion.EnBorrador(_reloj, presupuestoCRC: presupuesto));

        Assert.Equal(CodigosError.PresupuestoNoPositivo, error.Codigo);
    }

    [Fact]
    public void Crear_RechazaCodigoVacio()
    {
        var error = Assert.Throws<ExcepcionDominio>(
            () => ConstructorLicitacion.EnBorrador(_reloj, codigo: "   "));

        Assert.Equal(CodigosError.CodigoLicitacionVacio, error.Codigo);
    }

    // --- Transiciones de estado ---

    [Fact]
    public void Publicar_DesdeBorradorConDatosCompletos_CambiaAPublicada()
    {
        var licitacion = ConstructorLicitacion.EnBorrador(_reloj);

        licitacion.Publicar(_reloj);

        Assert.Equal(EstadoLicitacion.Publicada, licitacion.Estado);
    }

    [Fact]
    public void Publicar_ConFechaDeCierreNoFutura_EsRechazado()
    {
        var licitacion = ConstructorLicitacion.EnBorrador(_reloj, plazo: TimeSpan.FromHours(1));
        _reloj.Avanzar(TimeSpan.FromHours(2));

        var error = Assert.Throws<ExcepcionDominio>(() => licitacion.Publicar(_reloj));

        Assert.Equal(CodigosError.FechaCierreNoFutura, error.Codigo);
        Assert.Equal(EstadoLicitacion.Borrador, licitacion.Estado);
    }

    [Fact]
    public void Publicar_UnaLicitacionYaPublicada_EsRechazado()
    {
        var licitacion = ConstructorLicitacion.Publicada(_reloj);

        var error = Assert.Throws<ExcepcionDominio>(() => licitacion.Publicar(_reloj));

        Assert.Equal(CodigosError.TransicionEstadoInvalida, error.Codigo);
    }

    [Fact]
    public void Cerrar_DesdeBorrador_SeAdmiteComoCancelacion()
    {
        var licitacion = ConstructorLicitacion.EnBorrador(_reloj);

        licitacion.Cerrar(_reloj);

        Assert.Equal(EstadoLicitacion.Cerrada, licitacion.Estado);
    }

    [Fact]
    public void Cerrar_DesdePublicada_CambiaACerrada()
    {
        var licitacion = ConstructorLicitacion.Publicada(_reloj);

        licitacion.Cerrar(_reloj);

        Assert.Equal(EstadoLicitacion.Cerrada, licitacion.Estado);
    }

    [Fact]
    public void Cerrar_UnaLicitacionYaCerrada_EsRechazado()
    {
        var licitacion = ConstructorLicitacion.Publicada(_reloj);
        licitacion.Cerrar(_reloj);

        var error = Assert.Throws<ExcepcionDominio>(() => licitacion.Cerrar(_reloj));

        Assert.Equal(CodigosError.TransicionEstadoInvalida, error.Codigo);
    }

    [Fact]
    public void Publicar_DesdeCerrada_EsRechazado()
    {
        var licitacion = ConstructorLicitacion.Publicada(_reloj);
        licitacion.Cerrar(_reloj);

        var error = Assert.Throws<ExcepcionDominio>(() => licitacion.Publicar(_reloj));

        Assert.Equal(CodigosError.TransicionEstadoInvalida, error.Codigo);
    }

    // --- Vencimiento ---

    [Fact]
    public void EstadoEfectivo_PublicadaConFechaAlcanzada_EsCerrada()
    {
        var licitacion = ConstructorLicitacion.Publicada(_reloj, plazo: TimeSpan.FromDays(1));

        _reloj.Avanzar(TimeSpan.FromDays(1));

        // El campo persistido sigue diciendo Publicada...
        Assert.Equal(EstadoLicitacion.Publicada, licitacion.Estado);
        // ...pero funcionalmente la licitación está cerrada (enunciado §8.1).
        Assert.Equal(EstadoLicitacion.Cerrada, licitacion.EstadoEfectivo(_reloj));
        Assert.False(licitacion.AceptaOfertas(_reloj));
    }

    [Fact]
    public void EstaVencida_EnElInstanteExactoDelCierre_EsVerdadero()
    {
        var licitacion = ConstructorLicitacion.Publicada(_reloj, plazo: TimeSpan.FromDays(1));

        _reloj.Situar(licitacion.FechaCierre);

        // El enunciado §8.2 rechaza la oferta cuando la hora actual es igual o
        // posterior al cierre: el instante exacto ya está vencido.
        Assert.True(licitacion.EstaVencida(_reloj));
    }

    [Fact]
    public void EstaVencida_UnInstanteAntesDelCierre_EsFalso()
    {
        var licitacion = ConstructorLicitacion.Publicada(_reloj, plazo: TimeSpan.FromDays(1));

        _reloj.Situar(licitacion.FechaCierre.AddTicks(-1));

        Assert.False(licitacion.EstaVencida(_reloj));
        Assert.True(licitacion.AceptaOfertas(_reloj));
    }

    [Fact]
    public void AceptaOfertas_EnBorrador_EsFalso()
    {
        var licitacion = ConstructorLicitacion.EnBorrador(_reloj);

        Assert.False(licitacion.AceptaOfertas(_reloj));
    }

    // --- Presupuesto ---

    [Fact]
    public void ActualizarDatos_NoPermiteBajarElPresupuestoPorDebajoDeUnaOfertaExistente()
    {
        var licitacion = ConstructorLicitacion.Publicada(_reloj, presupuestoCRC: 1_000_000m);

        var error = Assert.Throws<ExcepcionDominio>(() => licitacion.ActualizarDatos(
            licitacion.Codigo,
            licitacion.Titulo,
            presupuestoEstimadoCRC: 700_000m,
            licitacion.FechaCierre,
            mayorOfertaRegistradaCRC: 800_000m,
            _reloj));

        Assert.Equal(CodigosError.PresupuestoMenorQueOferta, error.Codigo);
        Assert.Equal(1_000_000m, licitacion.PresupuestoEstimadoCRC);
    }

    [Fact]
    public void ActualizarDatos_PermiteBajarElPresupuestoHastaLaMayorOferta()
    {
        var licitacion = ConstructorLicitacion.Publicada(_reloj, presupuestoCRC: 1_000_000m);

        licitacion.ActualizarDatos(
            licitacion.Codigo,
            licitacion.Titulo,
            presupuestoEstimadoCRC: 800_000m,
            licitacion.FechaCierre,
            mayorOfertaRegistradaCRC: 800_000m,
            _reloj);

        Assert.Equal(800_000m, licitacion.PresupuestoEstimadoCRC);
    }

    [Fact]
    public void ActualizarDatos_SobreLicitacionCerrada_EsRechazado()
    {
        var licitacion = ConstructorLicitacion.Publicada(_reloj);
        licitacion.Cerrar(_reloj);

        var error = Assert.Throws<ExcepcionDominio>(() => licitacion.ActualizarDatos(
            licitacion.Codigo,
            "Otro título",
            licitacion.PresupuestoEstimadoCRC,
            licitacion.FechaCierre,
            mayorOfertaRegistradaCRC: null,
            _reloj));

        Assert.Equal(CodigosError.LicitacionCerradaNoModificable, error.Codigo);
    }

    [Fact]
    public void ActualizarDatos_SobreLicitacionVencida_EsRechazado()
    {
        var licitacion = ConstructorLicitacion.Publicada(_reloj, plazo: TimeSpan.FromDays(1));
        _reloj.Avanzar(TimeSpan.FromDays(2));

        var error = Assert.Throws<ExcepcionDominio>(() => licitacion.ActualizarDatos(
            licitacion.Codigo,
            "Otro título",
            licitacion.PresupuestoEstimadoCRC,
            licitacion.FechaCierre,
            mayorOfertaRegistradaCRC: null,
            _reloj));

        Assert.Equal(CodigosError.LicitacionCerradaNoModificable, error.Codigo);
    }

    [Fact]
    public void Crear_RedondeaElPresupuestoADosDecimales()
    {
        var licitacion = ConstructorLicitacion.EnBorrador(_reloj, presupuestoCRC: 1_000_000.005m);

        Assert.Equal(1_000_000.01m, licitacion.PresupuestoEstimadoCRC);
    }
}
