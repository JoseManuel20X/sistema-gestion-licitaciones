using Licitaciones.Domain.Ofertas;
using Licitaciones.UnitTests.Common;

namespace Licitaciones.UnitTests.Ofertas;

/// <summary>Mejor oferta, desempate y clasificación del ahorro (enunciado §8.6).</summary>
public sealed class EvaluadorOfertasTests
{
    private const decimal Presupuesto = 1_000_000m;

    private readonly RelojFalso _reloj = RelojFalso.EnInstanteBase();

    [Fact]
    public void Evaluar_SinOfertas_DevuelveSinOfertasValidas()
    {
        var evaluacion = EvaluadorOfertas.Evaluar([], Presupuesto);

        Assert.Null(evaluacion.MejorOferta);
        Assert.Null(evaluacion.PorcentajeAhorro);
        Assert.Equal(ClasificacionAhorro.SinOfertasValidas, evaluacion.Clasificacion);
        Assert.Equal("Sin ofertas válidas", evaluacion.Clasificacion.Descripcion());
    }

    [Fact]
    public void Evaluar_EligeLaOfertaDeMenorMonto()
    {
        var licitacion = ConstructorLicitacion.Publicada(_reloj, Presupuesto);
        var cara = Oferta.Registrar(licitacion, Guid.CreateVersion7(), 950_000m, _reloj);
        var barata = Oferta.Registrar(licitacion, Guid.CreateVersion7(), 800_000m, _reloj);
        var media = Oferta.Registrar(licitacion, Guid.CreateVersion7(), 900_000m, _reloj);

        var evaluacion = EvaluadorOfertas.Evaluar([cara, barata, media], Presupuesto);

        Assert.Same(barata, evaluacion.MejorOferta);
    }

    [Fact]
    public void Evaluar_EnEmpateDeMonto_GanaLaRegistradaPrimero()
    {
        var licitacion = ConstructorLicitacion.Publicada(_reloj, Presupuesto);

        var primera = Oferta.Registrar(licitacion, Guid.CreateVersion7(), 900_000m, _reloj);
        _reloj.Avanzar(TimeSpan.FromHours(1));
        var segunda = Oferta.Registrar(licitacion, Guid.CreateVersion7(), 900_000m, _reloj);

        // Se pasa la lista en orden inverso para comprobar que gana por fecha de
        // registro y no por el orden en que llegan a la evaluación.
        var evaluacion = EvaluadorOfertas.Evaluar([segunda, primera], Presupuesto);

        Assert.Same(primera, evaluacion.MejorOferta);
    }

    [Theory]
    // Ahorro ≥ 10 %
    [InlineData(900_000, 10.0, ClasificacionAhorro.OfertaConveniente)]
    [InlineData(500_000, 50.0, ClasificacionAhorro.OfertaConveniente)]
    // Ahorro > 0 % y < 10 %
    [InlineData(950_000, 5.0, ClasificacionAhorro.OfertaAceptable)]
    [InlineData(999_999, 0.0001, ClasificacionAhorro.OfertaAceptable)]
    // Sin ahorro
    [InlineData(1_000_000, 0.0, ClasificacionAhorro.OfertaValidaSinAhorro)]
    public void Evaluar_ClasificaSegunElPorcentajeDeAhorro(
        decimal montoOferta,
        decimal ahorroEsperado,
        ClasificacionAhorro clasificacionEsperada)
    {
        var licitacion = ConstructorLicitacion.Publicada(_reloj, Presupuesto);
        var oferta = Oferta.Registrar(licitacion, Guid.CreateVersion7(), montoOferta, _reloj);

        var evaluacion = EvaluadorOfertas.Evaluar([oferta], Presupuesto);

        Assert.Equal(ahorroEsperado, evaluacion.PorcentajeAhorro);
        Assert.Equal(clasificacionEsperada, evaluacion.Clasificacion);
    }

    [Fact]
    public void Evaluar_EnElUmbralExactoDel10Porciento_EsOfertaConveniente()
    {
        var licitacion = ConstructorLicitacion.Publicada(_reloj, Presupuesto);
        var oferta = Oferta.Registrar(licitacion, Guid.CreateVersion7(), 900_000m, _reloj);

        var evaluacion = EvaluadorOfertas.Evaluar([oferta], Presupuesto);

        // El enunciado define "igual o superior al 10 %" como conveniente.
        Assert.Equal(ClasificacionAhorro.OfertaConveniente, evaluacion.Clasificacion);
    }

    [Fact]
    public void Evaluar_JustoPorDebajoDelUmbral_EsOfertaAceptable()
    {
        var licitacion = ConstructorLicitacion.Publicada(_reloj, Presupuesto);
        var oferta = Oferta.Registrar(licitacion, Guid.CreateVersion7(), 900_000.01m, _reloj);

        var evaluacion = EvaluadorOfertas.Evaluar([oferta], Presupuesto);

        Assert.Equal(ClasificacionAhorro.OfertaAceptable, evaluacion.Clasificacion);
    }

    [Theory]
    [InlineData(ClasificacionAhorro.OfertaConveniente, "Oferta conveniente")]
    [InlineData(ClasificacionAhorro.OfertaAceptable, "Oferta aceptable")]
    [InlineData(ClasificacionAhorro.OfertaValidaSinAhorro, "Oferta válida sin ahorro")]
    [InlineData(ClasificacionAhorro.SinOfertasValidas, "Sin ofertas válidas")]
    public void Descripcion_DevuelveElTextoExigidoPorElEnunciado(
        ClasificacionAhorro clasificacion,
        string textoEsperado)
    {
        Assert.Equal(textoEsperado, clasificacion.Descripcion());
    }
}
