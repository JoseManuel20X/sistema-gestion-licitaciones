using Licitaciones.Domain.Aprobaciones;
using Licitaciones.Domain.Common;
using Licitaciones.UnitTests.Common;

namespace Licitaciones.UnitTests.Aprobaciones;

/// <summary>Rangos parametrizables de aprobación (enunciado §8.7).</summary>
public sealed class NivelAprobacionTests
{
    private readonly RelojFalso _reloj = RelojFalso.EnInstanteBase();

    /// <summary>Tabla de referencia del enunciado, usada también como dato semilla.</summary>
    private List<NivelAprobacion> TablaDeReferencia() =>
    [
        NivelAprobacion.Crear(0.01m, 999_999.99m, "Encargado de área", _reloj),
        NivelAprobacion.Crear(1_000_000m, 9_999_999.99m, "Gerencia", _reloj),
        NivelAprobacion.Crear(10_000_000m, null, "Junta Directiva", _reloj),
    ];

    [Theory]
    [InlineData(0.01, "Encargado de área")]
    [InlineData(500_000, "Encargado de área")]
    [InlineData(999_999.99, "Encargado de área")]
    [InlineData(1_000_000, "Gerencia")]
    [InlineData(5_000_000, "Gerencia")]
    [InlineData(9_999_999.99, "Gerencia")]
    [InlineData(10_000_000, "Junta Directiva")]
    [InlineData(999_999_999, "Junta Directiva")]
    public void ResolverNivel_DevuelveElAprobadorDeLaTabla(decimal monto, string aprobadorEsperado)
    {
        var nivel = TablaNivelesAprobacion.ResolverNivel(TablaDeReferencia(), monto);

        Assert.NotNull(nivel);
        Assert.Equal(aprobadorEsperado, nivel.Aprobador);
    }

    [Fact]
    public void ResolverNivel_ConMontoFueraDeTodoRango_DevuelveNulo()
    {
        // La tabla de referencia empieza en 0,01: un monto menor no tiene aprobador.
        var nivel = TablaNivelesAprobacion.ResolverNivel(TablaDeReferencia(), 0.001m);

        Assert.Null(nivel);
    }

    [Fact]
    public void Contiene_IncluyeAmbosExtremosDelRango()
    {
        var nivel = NivelAprobacion.Crear(1_000m, 2_000m, "Encargado de área", _reloj);

        Assert.True(nivel.Contiene(1_000m));
        Assert.True(nivel.Contiene(2_000m));
        Assert.False(nivel.Contiene(999.99m));
        Assert.False(nivel.Contiene(2_000.01m));
    }

    [Fact]
    public void Contiene_EnRangoAbierto_NoTieneLimiteSuperior()
    {
        var nivel = NivelAprobacion.Crear(10_000_000m, null, "Junta Directiva", _reloj);

        Assert.True(nivel.EsRangoAbierto);
        Assert.True(nivel.Contiene(decimal.MaxValue));
    }

    [Fact]
    public void GarantizarConsistencia_ConLaTablaDeReferencia_NoLanza()
    {
        TablaNivelesAprobacion.GarantizarConsistencia(TablaDeReferencia());
    }

    [Fact]
    public void GarantizarConsistencia_ConRangosTraslapados_EsRechazado()
    {
        List<NivelAprobacion> niveles =
        [
            NivelAprobacion.Crear(0.01m, 1_000_000m, "Encargado de área", _reloj),
            NivelAprobacion.Crear(900_000m, 5_000_000m, "Gerencia", _reloj),
        ];

        var error = Assert.Throws<ExcepcionDominio>(
            () => TablaNivelesAprobacion.GarantizarConsistencia(niveles));

        Assert.Equal(CodigosError.RangoAprobacionTraslapado, error.Codigo);
    }

    [Fact]
    public void GarantizarConsistencia_ConDosRangosAbiertos_EsRechazado()
    {
        List<NivelAprobacion> niveles =
        [
            NivelAprobacion.Crear(1_000_000m, null, "Gerencia", _reloj),
            NivelAprobacion.Crear(10_000_000m, null, "Junta Directiva", _reloj),
        ];

        var error = Assert.Throws<ExcepcionDominio>(
            () => TablaNivelesAprobacion.GarantizarConsistencia(niveles));

        Assert.Equal(CodigosError.RangoAbiertoDuplicado, error.Codigo);
    }

    [Fact]
    public void GarantizarConsistencia_AdmiteHuecosEntreRangos()
    {
        // El enunciado prohíbe el traslape, no exige que los rangos sean contiguos.
        List<NivelAprobacion> niveles =
        [
            NivelAprobacion.Crear(0.01m, 1_000m, "Encargado de área", _reloj),
            NivelAprobacion.Crear(5_000m, 10_000m, "Gerencia", _reloj),
        ];

        TablaNivelesAprobacion.GarantizarConsistencia(niveles);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Crear_RechazaMontoMinimoNoPositivo(decimal minimo)
    {
        var error = Assert.Throws<ExcepcionDominio>(
            () => NivelAprobacion.Crear(minimo, 1_000m, "Encargado de área", _reloj));

        Assert.Equal(CodigosError.RangoAprobacionInvalido, error.Codigo);
    }

    [Fact]
    public void Crear_RechazaMaximoMenorOIgualQueElMinimo()
    {
        var error = Assert.Throws<ExcepcionDominio>(
            () => NivelAprobacion.Crear(1_000m, 1_000m, "Encargado de área", _reloj));

        Assert.Equal(CodigosError.RangoAprobacionInvalido, error.Codigo);
    }

    [Fact]
    public void Crear_RechazaAprobadorVacio()
    {
        var error = Assert.Throws<ExcepcionDominio>(
            () => NivelAprobacion.Crear(1_000m, 2_000m, "  ", _reloj));

        Assert.Equal(CodigosError.AprobadorVacio, error.Codigo);
    }
}
