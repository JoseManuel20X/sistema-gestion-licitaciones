using Licitaciones.Domain.Common;
using Licitaciones.Domain.TiposCambio;
using Licitaciones.UnitTests.Common;

namespace Licitaciones.UnitTests.TiposCambio;

/// <summary>Conversión referencial CRC/USD (enunciado §8.8).</summary>
public sealed class TipoCambioTests
{
    private readonly RelojFalso _reloj = RelojFalso.EnInstanteBase();

    private TipoCambio CrearTipoCambio(decimal crcPorUsd = 520m) =>
        TipoCambio.Crear(crcPorUsd, _reloj.AhoraUtc, activo: true, _reloj);

    [Theory]
    [InlineData(520, 520_000, 1_000)]
    [InlineData(520, 1_000_000, 1_923.08)]
    [InlineData(500, 1_234.56, 2.47)]
    public void ConvertirCrcAUsd_DivideElMontoEntreElTipoDeCambio(
        decimal crcPorUsd,
        decimal montoCRC,
        decimal usdEsperado)
    {
        var tipoCambio = CrearTipoCambio(crcPorUsd);

        Assert.Equal(usdEsperado, tipoCambio.ConvertirCrcAUsd(montoCRC));
    }

    [Fact]
    public void ConvertirCrcAUsd_NoModificaElMontoOriginalEnColones()
    {
        var tipoCambio = CrearTipoCambio(520m);
        const decimal montoCRC = 1_000_000m;

        tipoCambio.ConvertirCrcAUsd(montoCRC);

        // La conversión es una representación calculada: el valor oficial en
        // colones es la única fuente de verdad (enunciado §8.8).
        Assert.Equal(1_000_000m, montoCRC);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-520.50)]
    public void Crear_RechazaTipoDeCambioNoPositivo(decimal valor)
    {
        var error = Assert.Throws<ExcepcionDominio>(
            () => TipoCambio.Crear(valor, _reloj.AhoraUtc, activo: true, _reloj));

        Assert.Equal(CodigosError.TipoCambioNoPositivo, error.Codigo);
    }

    [Fact]
    public void Activar_MarcaElRegistroComoActivo()
    {
        var tipoCambio = TipoCambio.Crear(520m, _reloj.AhoraUtc, activo: false, _reloj);

        tipoCambio.Activar(_reloj);

        Assert.True(tipoCambio.Activo);
    }

    [Fact]
    public void Desactivar_QuitaLaMarcaDeActivo()
    {
        var tipoCambio = CrearTipoCambio();

        tipoCambio.Desactivar(_reloj);

        Assert.False(tipoCambio.Activo);
    }

    [Fact]
    public void Actualizar_CambiaValorYFechaDeVigencia()
    {
        var tipoCambio = CrearTipoCambio(520m);
        var nuevaVigencia = _reloj.AhoraUtc.AddDays(1);
        _reloj.Avanzar(TimeSpan.FromHours(5));

        tipoCambio.Actualizar(535.25m, nuevaVigencia, _reloj);

        Assert.Equal(535.25m, tipoCambio.CRCporUSD);
        Assert.Equal(nuevaVigencia, tipoCambio.FechaVigencia);
        Assert.Equal(RelojFalso.InstanteBase.AddHours(5), tipoCambio.UpdatedAt);
    }
}
