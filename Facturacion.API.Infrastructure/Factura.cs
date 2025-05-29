using System;
using System.Collections.Generic;

namespace Facturacion.API.Infrastructure;

public partial class Factura
{
    public int Id { get; set; }

    public string? NumeroFactura { get; set; }

    public DateTime Fecha { get; set; }

    public int ClienteId { get; set; }

    public string ClienteNumeroDocumento { get; set; } = null!;

    public string ClienteNombres { get; set; } = null!;

    public string ClienteApellidos { get; set; } = null!;

    public string ClienteDireccion { get; set; } = null!;

    public string ClienteTelefono { get; set; } = null!;

    public decimal SubTotal { get; set; }

    public decimal PorcentajeDescuento { get; set; }

    public decimal ValorDescuento { get; set; }

    public decimal BaseImpuestos { get; set; }

    public decimal PorcentajeIva { get; set; }

    public decimal ValorIva { get; set; }

    public decimal Total { get; set; }

    public string? Observaciones { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public Guid CreadoPorId { get; set; }

    public Guid? ModificadoPorId { get; set; }

    public bool Activo { get; set; }

    public virtual Cliente Cliente { get; set; } = null!;

    public virtual Usuario CreadoPor { get; set; } = null!;

    public virtual ICollection<FacturaDetalle> FacturaDetalles { get; set; } = new List<FacturaDetalle>();

    public virtual Usuario? ModificadoPor { get; set; }
}
