namespace Licitaciones.Domain.Common;

/// <summary>
/// Códigos estables de error del dominio. La capa de API los expone en
/// <c>ProblemDetails</c> para que un cliente pueda reaccionar sin depender del
/// texto del mensaje, que sí puede cambiar o traducirse.
/// </summary>
public static class CodigosError
{
    // Proveedor
    public const string NombreProveedorVacio = "PROVEEDOR_NOMBRE_VACIO";
    public const string NombreProveedorCaracteresInvalidos = "PROVEEDOR_NOMBRE_CARACTERES_INVALIDOS";
    public const string NombreProveedorDuplicado = "PROVEEDOR_NOMBRE_DUPLICADO";
    public const string ProveedorNoEncontrado = "PROVEEDOR_NO_ENCONTRADO";

    // Licitación
    public const string CodigoLicitacionVacio = "LICITACION_CODIGO_VACIO";
    public const string TituloLicitacionVacio = "LICITACION_TITULO_VACIO";
    public const string CodigoLicitacionDuplicado = "LICITACION_CODIGO_DUPLICADO";
    public const string PresupuestoNoPositivo = "LICITACION_PRESUPUESTO_NO_POSITIVO";
    public const string PresupuestoMenorQueOferta = "LICITACION_PRESUPUESTO_MENOR_QUE_OFERTA";
    public const string FechaCierreNoFutura = "LICITACION_FECHA_CIERRE_NO_FUTURA";
    public const string TransicionEstadoInvalida = "LICITACION_TRANSICION_INVALIDA";
    public const string LicitacionCerradaNoModificable = "LICITACION_CERRADA_NO_MODIFICABLE";
    public const string LicitacionNoEncontrada = "LICITACION_NO_ENCONTRADA";
    public const string LicitacionConOfertasNoEliminable = "LICITACION_CON_OFERTAS_NO_ELIMINABLE";

    // Oferta
    public const string MontoOfertaNoPositivo = "OFERTA_MONTO_NO_POSITIVO";
    public const string OfertaSuperaPresupuesto = "OFERTA_SUPERA_PRESUPUESTO";
    public const string OfertaLicitacionNoPublicada = "OFERTA_LICITACION_NO_PUBLICADA";
    public const string OfertaLicitacionVencida = "OFERTA_LICITACION_VENCIDA";
    public const string OfertaDuplicada = "OFERTA_DUPLICADA";
    public const string OfertaNoEncontrada = "OFERTA_NO_ENCONTRADA";

    // Nivel de aprobación
    public const string RangoAprobacionInvalido = "APROBACION_RANGO_INVALIDO";
    public const string RangoAprobacionTraslapado = "APROBACION_RANGO_TRASLAPADO";
    public const string RangoAbiertoDuplicado = "APROBACION_RANGO_ABIERTO_DUPLICADO";
    public const string AprobadorVacio = "APROBACION_APROBADOR_VACIO";
    public const string NivelAprobacionNoEncontrado = "APROBACION_NIVEL_NO_ENCONTRADO";
    public const string SinNivelAprobacionAplicable = "APROBACION_SIN_NIVEL_APLICABLE";

    // Tipo de cambio
    public const string TipoCambioNoPositivo = "TIPO_CAMBIO_NO_POSITIVO";
    public const string TipoCambioNoEncontrado = "TIPO_CAMBIO_NO_ENCONTRADO";
    public const string SinTipoCambioActivo = "TIPO_CAMBIO_SIN_ACTIVO";

    // Concurrencia e integridad
    public const string ConflictoConcurrencia = "CONFLICTO_CONCURRENCIA";
    public const string ViolacionIntegridad = "VIOLACION_INTEGRIDAD";
}
