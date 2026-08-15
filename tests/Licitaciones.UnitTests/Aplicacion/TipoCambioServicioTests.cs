using Licitaciones.Application.Common;
using Licitaciones.Application.TiposCambio;
using Licitaciones.Domain.Common;
using Licitaciones.UnitTests.Common;

namespace Licitaciones.UnitTests.Aplicacion;

/// <summary>Casos de uso del tipo de cambio (HU-09 y HU-10).</summary>
public sealed class TipoCambioServicioTests
{
    private readonly RelojFalso _reloj = RelojFalso.EnInstanteBase();
    private readonly AlmacenFalso _almacen = new();

    private TipoCambioServicio CrearServicio() =>
        new(new TipoCambioRepositorioFalso(_almacen), new UnidadDeTrabajoFalsa(_almacen), _reloj);

    private static TipoCambioEntrada Entrada(decimal valor = 520m, DateTimeOffset? vigencia = null) =>
        new(valor, vigencia ?? RelojFalso.InstanteBase);

    [Fact]
    public async Task CrearAsync_ConValorValido_RegistraElTipoDeCambio()
    {
        var servicio = CrearServicio();

        var resultado = await servicio.CrearAsync(Entrada(535.75m));

        Assert.True(resultado.EsExitoso);
        Assert.Equal(535.75m, resultado.Valor!.CRCporUSD);
        Assert.Equal(1, _almacen.Confirmaciones);
    }

    [Fact]
    public async Task CrearAsync_ElPrimerRegistroQuedaActivo()
    {
        var servicio = CrearServicio();

        var resultado = await servicio.CrearAsync(Entrada());

        // Sin un tipo de cambio activo la conversión a USD no puede calcularse,
        // así que el primero se activa solo en lugar de dejar el sistema inútil.
        Assert.True(resultado.Valor!.Activo);
    }

    [Fact]
    public async Task CrearAsync_LosSiguientesNoSeActivanSolos()
    {
        var servicio = CrearServicio();
        await servicio.CrearAsync(Entrada(520m));

        var segundo = await servicio.CrearAsync(Entrada(535m));

        Assert.False(segundo.Valor!.Activo);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CrearAsync_RechazaValorNoPositivo(decimal valor)
    {
        var servicio = CrearServicio();

        var resultado = await servicio.CrearAsync(Entrada(valor));

        Assert.False(resultado.EsExitoso);
        Assert.Equal(CodigosError.TipoCambioNoPositivo, resultado.Error!.Codigo);
        Assert.Equal(TipoError.Validacion, resultado.Error.Tipo);
    }

    [Fact]
    public async Task ActivarAsync_DesactivaElAnteriorEnLaMismaTransaccion()
    {
        var servicio = CrearServicio();
        var primero = await servicio.CrearAsync(Entrada(520m));
        var segundo = await servicio.CrearAsync(Entrada(535m));

        var resultado = await servicio.ActivarAsync(segundo.Valor!.Id);

        Assert.True(resultado.EsExitoso);
        Assert.Single(_almacen.TiposCambio, t => t.Activo);
        Assert.Equal(segundo.Valor.Id, _almacen.TiposCambio.Single(t => t.Activo).Id);
        Assert.False(_almacen.TiposCambio.Single(t => t.Id == primero.Valor!.Id).Activo);

        // Debe ocurrir dentro de una transacción: entre desactivar el anterior y
        // activar el nuevo, el índice único parcial de PostgreSQL no admite dos
        // filas activas ni ninguna operación a medias.
        Assert.Equal(1, _almacen.Transacciones);
    }

    [Fact]
    public async Task ActivarAsync_ElYaActivo_NoRompeNiDuplica()
    {
        var servicio = CrearServicio();
        var unico = await servicio.CrearAsync(Entrada());

        var resultado = await servicio.ActivarAsync(unico.Valor!.Id);

        Assert.True(resultado.EsExitoso);
        Assert.Single(_almacen.TiposCambio, t => t.Activo);
    }

    [Fact]
    public async Task ActivarAsync_ConIdInexistente_DevuelveNoEncontrado()
    {
        var servicio = CrearServicio();

        var resultado = await servicio.ActivarAsync(Guid.CreateVersion7());

        Assert.False(resultado.EsExitoso);
        Assert.Equal(TipoError.NoEncontrado, resultado.Error!.Tipo);
    }

    [Fact]
    public async Task ObtenerActivoAsync_SinNinguno_DevuelveErrorControlado()
    {
        var servicio = CrearServicio();

        var resultado = await servicio.ObtenerActivoAsync();

        Assert.False(resultado.EsExitoso);
        Assert.Equal(CodigosError.SinTipoCambioActivo, resultado.Error!.Codigo);
    }

    [Fact]
    public async Task ConvertirAsync_UsaElTipoDeCambioActivoYDevuelveSuFecha()
    {
        var servicio = CrearServicio();
        var vigencia = RelojFalso.InstanteBase.AddDays(-3);
        await servicio.CrearAsync(Entrada(520m, vigencia));

        var resultado = await servicio.ConvertirAsync(1_040_000m);

        Assert.True(resultado.EsExitoso);
        Assert.Equal(2_000m, resultado.Valor!.MontoUSD);
        Assert.Equal(1_040_000m, resultado.Valor.MontoCRC);
        Assert.Equal(520m, resultado.Valor.CRCporUSD);
        // El enunciado §8.8 exige mostrar la fecha del tipo de cambio utilizado.
        Assert.Equal(vigencia, resultado.Valor.FechaVigencia);
    }

    [Fact]
    public async Task EliminarAsync_ElActivo_EsRechazado()
    {
        var servicio = CrearServicio();
        var unico = await servicio.CrearAsync(Entrada());

        var resultado = await servicio.EliminarAsync(unico.Valor!.Id);

        // Borrar el activo dejaría la aplicación sin poder convertir a USD.
        Assert.False(resultado.EsExitoso);
        Assert.Equal(TipoError.ReglaNegocio, resultado.Error!.Tipo);
        Assert.Single(_almacen.TiposCambio);
    }

    [Fact]
    public async Task EliminarAsync_UnoInactivo_LoElimina()
    {
        var servicio = CrearServicio();
        await servicio.CrearAsync(Entrada(520m));
        var segundo = await servicio.CrearAsync(Entrada(535m));

        var resultado = await servicio.EliminarAsync(segundo.Valor!.Id);

        Assert.True(resultado.EsExitoso);
        Assert.Single(_almacen.TiposCambio);
    }

    [Fact]
    public async Task ListarAsync_DevuelveLosMasVigentesPrimero()
    {
        var servicio = CrearServicio();
        await servicio.CrearAsync(Entrada(500m, RelojFalso.InstanteBase.AddDays(-10)));
        await servicio.CrearAsync(Entrada(520m, RelojFalso.InstanteBase));

        var lista = await servicio.ListarAsync();

        Assert.Equal(2, lista.Count);
        Assert.Equal(520m, lista[0].CRCporUSD);
    }
}
