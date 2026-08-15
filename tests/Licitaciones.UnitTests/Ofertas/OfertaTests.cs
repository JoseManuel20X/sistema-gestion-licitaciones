using Licitaciones.Domain.Common;
using Licitaciones.Domain.Ofertas;
using Licitaciones.UnitTests.Common;

namespace Licitaciones.UnitTests.Ofertas;

/// <summary>Validaciones de registro y edición de ofertas (enunciado §8.2 y §8.5).</summary>
public sealed class OfertaTests
{
    private readonly RelojFalso _reloj = RelojFalso.EnInstanteBase();
    private readonly Guid _proveedorId = Guid.CreateVersion7();

    [Fact]
    public void Registrar_SobreLicitacionPublicadaVigente_CreaLaOferta()
    {
        var licitacion = ConstructorLicitacion.Publicada(_reloj);

        var oferta = Oferta.Registrar(licitacion, _proveedorId, 900_000m, _reloj);

        Assert.Equal(900_000m, oferta.MontoOfertadoCRC);
        Assert.Equal(licitacion.Id, oferta.LicitacionId);
        Assert.Equal(_proveedorId, oferta.ProveedorId);
        Assert.Equal(RelojFalso.InstanteBase, oferta.FechaRegistro);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50000)]
    public void Registrar_RechazaMontoNoPositivo(decimal monto)
    {
        var licitacion = ConstructorLicitacion.Publicada(_reloj);

        var error = Assert.Throws<ExcepcionDominio>(
            () => Oferta.Registrar(licitacion, _proveedorId, monto, _reloj));

        Assert.Equal(CodigosError.MontoOfertaNoPositivo, error.Codigo);
    }

    [Fact]
    public void Registrar_RechazaOfertaSuperiorAlPresupuesto()
    {
        var licitacion = ConstructorLicitacion.Publicada(_reloj, presupuestoCRC: 1_000_000m);

        var error = Assert.Throws<ExcepcionDominio>(
            () => Oferta.Registrar(licitacion, _proveedorId, 1_000_000.01m, _reloj));

        Assert.Equal(CodigosError.OfertaSuperaPresupuesto, error.Codigo);
    }

    [Fact]
    public void Registrar_AdmiteOfertaIgualAlPresupuesto()
    {
        var licitacion = ConstructorLicitacion.Publicada(_reloj, presupuestoCRC: 1_000_000m);

        var oferta = Oferta.Registrar(licitacion, _proveedorId, 1_000_000m, _reloj);

        Assert.Equal(1_000_000m, oferta.MontoOfertadoCRC);
    }

    [Fact]
    public void Registrar_SobreLicitacionEnBorrador_EsRechazado()
    {
        var licitacion = ConstructorLicitacion.EnBorrador(_reloj);

        var error = Assert.Throws<ExcepcionDominio>(
            () => Oferta.Registrar(licitacion, _proveedorId, 900_000m, _reloj));

        Assert.Equal(CodigosError.OfertaLicitacionNoPublicada, error.Codigo);
    }

    [Fact]
    public void Registrar_SobreLicitacionCerrada_EsRechazado()
    {
        var licitacion = ConstructorLicitacion.Publicada(_reloj);
        licitacion.Cerrar(_reloj);

        var error = Assert.Throws<ExcepcionDominio>(
            () => Oferta.Registrar(licitacion, _proveedorId, 900_000m, _reloj));

        Assert.Equal(CodigosError.OfertaLicitacionNoPublicada, error.Codigo);
    }

    [Fact]
    public void Registrar_DespuesDeLaFechaDeCierre_EsRechazado()
    {
        var licitacion = ConstructorLicitacion.Publicada(_reloj, plazo: TimeSpan.FromDays(1));
        _reloj.Avanzar(TimeSpan.FromDays(1));

        var error = Assert.Throws<ExcepcionDominio>(
            () => Oferta.Registrar(licitacion, _proveedorId, 900_000m, _reloj));

        Assert.Equal(CodigosError.OfertaLicitacionVencida, error.Codigo);
    }

    [Fact]
    public void Registrar_UnTickAntesDelCierre_SeAdmite()
    {
        var licitacion = ConstructorLicitacion.Publicada(_reloj, plazo: TimeSpan.FromDays(1));
        _reloj.Situar(licitacion.FechaCierre.AddTicks(-1));

        var oferta = Oferta.Registrar(licitacion, _proveedorId, 900_000m, _reloj);

        Assert.Equal(900_000m, oferta.MontoOfertadoCRC);
    }

    [Fact]
    public void CambiarMonto_SobreLicitacionVigente_ActualizaElMonto()
    {
        var licitacion = ConstructorLicitacion.Publicada(_reloj);
        var oferta = Oferta.Registrar(licitacion, _proveedorId, 900_000m, _reloj);
        _reloj.Avanzar(TimeSpan.FromHours(2));

        oferta.CambiarMonto(850_000m, licitacion, _reloj);

        Assert.Equal(850_000m, oferta.MontoOfertadoCRC);
        Assert.Equal(RelojFalso.InstanteBase, oferta.FechaRegistro);
        Assert.Equal(RelojFalso.InstanteBase.AddHours(2), oferta.UpdatedAt);
    }

    [Fact]
    public void CambiarMonto_SobreLicitacionVencida_EsRechazado()
    {
        var licitacion = ConstructorLicitacion.Publicada(_reloj, plazo: TimeSpan.FromDays(1));
        var oferta = Oferta.Registrar(licitacion, _proveedorId, 900_000m, _reloj);
        _reloj.Avanzar(TimeSpan.FromDays(2));

        var error = Assert.Throws<ExcepcionDominio>(() => oferta.CambiarMonto(850_000m, licitacion, _reloj));

        Assert.Equal(CodigosError.OfertaLicitacionVencida, error.Codigo);
        Assert.Equal(900_000m, oferta.MontoOfertadoCRC);
    }

    [Fact]
    public void CambiarMonto_SobreLicitacionCerrada_EsRechazado()
    {
        var licitacion = ConstructorLicitacion.Publicada(_reloj);
        var oferta = Oferta.Registrar(licitacion, _proveedorId, 900_000m, _reloj);
        licitacion.Cerrar(_reloj);

        var error = Assert.Throws<ExcepcionDominio>(() => oferta.CambiarMonto(850_000m, licitacion, _reloj));

        Assert.Equal(CodigosError.OfertaLicitacionNoPublicada, error.Codigo);
    }
}
