using System.ComponentModel.DataAnnotations;
using Licitaciones.Application.Aprobaciones;
using Licitaciones.Application.Licitaciones;
using Licitaciones.Application.Ofertas;
using Licitaciones.Application.Proveedores;
using Licitaciones.Application.TiposCambio;

namespace Licitaciones.Web.Models;

/*
  Modelos de formulario con anotaciones de validación.

  Duplican deliberadamente algunas reglas del dominio para poder validarlas en
  el navegador antes de enviar (enunciado §8.5). No son la autoridad: el dominio
  vuelve a comprobarlas en el servidor y PostgreSQL las respalda con
  restricciones. Si alguna vez discrepan, manda el dominio; estas solo evitan un
  viaje al servidor para errores evidentes.
*/

/// <summary>Alta y edición de un proveedor (HU-01 y HU-02).</summary>
public sealed class ProveedorFormulario
{
    public Guid? Id { get; set; }

    [Display(Name = "Nombre")]
    [Required(ErrorMessage = "El nombre del proveedor es obligatorio.")]
    [StringLength(200, ErrorMessage = "El nombre no puede superar los 200 caracteres.")]
    [RegularExpression(
        @"^[\p{L}\p{N} .,\(\)]+$",
        ErrorMessage = "El nombre solo admite letras, números, espacios, punto, coma y paréntesis.")]
    public string Nombre { get; set; } = string.Empty;

    public bool EsEdicion => Id is not null;

    public ProveedorEntrada AEntrada() => new(Nombre);

    public static ProveedorFormulario Desde(ProveedorDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new ProveedorFormulario { Id = dto.Id, Nombre = dto.Nombre };
    }
}

/// <summary>Alta y edición de una licitación (HU-03).</summary>
public sealed class LicitacionFormulario
{
    public Guid? Id { get; set; }

    [Display(Name = "Código")]
    [Required(ErrorMessage = "El código es obligatorio.")]
    [StringLength(50, ErrorMessage = "El código no puede superar los 50 caracteres.")]
    public string Codigo { get; set; } = string.Empty;

    [Display(Name = "Título")]
    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(300, ErrorMessage = "El título no puede superar los 300 caracteres.")]
    public string Titulo { get; set; } = string.Empty;

    [Display(Name = "Presupuesto estimado (CRC)")]
    [Required(ErrorMessage = "El presupuesto es obligatorio.")]
    [Range(0.01, 9_999_999_999_999.99, ErrorMessage = "El presupuesto debe ser mayor que cero.")]
    public decimal PresupuestoEstimadoCRC { get; set; }

    /// <summary>
    /// Fecha y hora de cierre, seleccionada con un control de calendario y hora
    /// (enunciado §8.2), no escrita a mano como texto libre.
    /// </summary>
    [Display(Name = "Fecha y hora de cierre")]
    [Required(ErrorMessage = "La fecha de cierre es obligatoria.")]
    [DataType(DataType.DateTime)]
    public DateTime FechaCierre { get; set; } = DateTime.Now.AddDays(30);

    public bool EsEdicion => Id is not null;

    /// <summary>
    /// Convierte la fecha local que escribió la persona a un instante en UTC.
    /// </summary>
    /// <remarks>
    /// La conversión a UTC no es opcional: Npgsql solo admite desplazamiento cero
    /// al escribir en <c>timestamp with time zone</c>, y enviar -06:00 hace fallar
    /// la inserción. Además el §8.2 exige comparar en UTC y presentar en la zona
    /// de Costa Rica, que es justo lo que hace esta conversión.
    /// </remarks>
    public LicitacionEntrada AEntrada() =>
        new(Codigo, Titulo, PresupuestoEstimadoCRC, AInstanteUtc(FechaCierre));

    /// <summary>Interpreta una fecha escrita en hora de Costa Rica y la lleva a UTC.</summary>
    internal static DateTimeOffset AInstanteUtc(DateTime fechaLocal) =>
        new DateTimeOffset(DateTime.SpecifyKind(fechaLocal, DateTimeKind.Unspecified), ZonaCostaRica).ToUniversalTime();

    /// <summary>Costa Rica no aplica horario de verano: el desfase es fijo.</summary>
    internal static readonly TimeSpan ZonaCostaRica = TimeSpan.FromHours(-6);

    public static LicitacionFormulario Desde(LicitacionDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new LicitacionFormulario
        {
            Id = dto.Id,
            Codigo = dto.Codigo,
            Titulo = dto.Titulo,
            PresupuestoEstimadoCRC = dto.PresupuestoEstimadoCRC,
            FechaCierre = dto.FechaCierre.ToOffset(ZonaCostaRica).DateTime,
        };
    }
}

/// <summary>Registro de una oferta en una licitación (HU-05).</summary>
public sealed class OfertaFormulario
{
    public Guid? Id { get; set; }

    public Guid LicitacionId { get; set; }

    public string? CodigoLicitacion { get; set; }

    /// <summary>Presupuesto de la licitación, para mostrarlo como referencia al ofertar.</summary>
    public decimal PresupuestoCRC { get; set; }

    [Display(Name = "Proveedor")]
    [Required(ErrorMessage = "Debe seleccionar un proveedor.")]
    public Guid ProveedorId { get; set; }

    [Display(Name = "Monto ofertado (CRC)")]
    [Required(ErrorMessage = "El monto es obligatorio.")]
    [Range(0.01, 9_999_999_999_999.99, ErrorMessage = "El monto debe ser mayor que cero.")]
    public decimal MontoOfertadoCRC { get; set; }

    public bool EsEdicion => Id is not null;

    public OfertaEntrada AEntrada() => new(ProveedorId, MontoOfertadoCRC);

    public OfertaActualizacion AActualizacion() => new(MontoOfertadoCRC);
}

/// <summary>Alta y edición de un nivel de aprobación (HU-08).</summary>
public sealed class NivelAprobacionFormulario
{
    public Guid? Id { get; set; }

    [Display(Name = "Monto mínimo (CRC)")]
    [Required(ErrorMessage = "El monto mínimo es obligatorio.")]
    [Range(0.01, 9_999_999_999_999.99, ErrorMessage = "El monto mínimo debe ser mayor que cero.")]
    public decimal MontoMinimoCRC { get; set; }

    /// <summary>Vacío significa rango abierto, sin límite superior.</summary>
    [Display(Name = "Monto máximo (CRC)")]
    [Range(0.01, 9_999_999_999_999.99, ErrorMessage = "El monto máximo debe ser mayor que cero.")]
    public decimal? MontoMaximoCRC { get; set; }

    [Display(Name = "Aprobador")]
    [Required(ErrorMessage = "El aprobador es obligatorio.")]
    [StringLength(150, ErrorMessage = "El aprobador no puede superar los 150 caracteres.")]
    public string Aprobador { get; set; } = string.Empty;

    public bool EsEdicion => Id is not null;

    public NivelAprobacionEntrada AEntrada() => new(MontoMinimoCRC, MontoMaximoCRC, Aprobador);

    public static NivelAprobacionFormulario Desde(NivelAprobacionDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new NivelAprobacionFormulario
        {
            Id = dto.Id,
            MontoMinimoCRC = dto.MontoMinimoCRC,
            MontoMaximoCRC = dto.MontoMaximoCRC,
            Aprobador = dto.Aprobador,
        };
    }
}

/// <summary>Alta y edición de un tipo de cambio (HU-09).</summary>
public sealed class TipoCambioFormulario
{
    public Guid? Id { get; set; }

    [Display(Name = "Colones por dólar")]
    [Required(ErrorMessage = "El tipo de cambio es obligatorio.")]
    [Range(0.0001, 9_999_999.9999, ErrorMessage = "El tipo de cambio debe ser mayor que cero.")]
    public decimal CRCporUSD { get; set; }

    [Display(Name = "Fecha de vigencia")]
    [Required(ErrorMessage = "La fecha de vigencia es obligatoria.")]
    [DataType(DataType.Date)]
    public DateTime FechaVigencia { get; set; } = DateTime.Now;

    public bool EsEdicion => Id is not null;

    public TipoCambioEntrada AEntrada() =>
        new(CRCporUSD, LicitacionFormulario.AInstanteUtc(FechaVigencia));

    public static TipoCambioFormulario Desde(TipoCambioDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new TipoCambioFormulario
        {
            Id = dto.Id,
            CRCporUSD = dto.CRCporUSD,
            FechaVigencia = dto.FechaVigencia.ToOffset(LicitacionFormulario.ZonaCostaRica).DateTime,
        };
    }
}
