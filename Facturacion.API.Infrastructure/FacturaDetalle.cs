using System;
using System.Collections.Generic;

namespace Facturacion.API.Infrastructure;

public partial class FacturaDetalle
{
    public int Id { get; set; }

    public int FacturaId { get; set; }

    public int ArticuloId { get; set; }

    public string ArticuloCodigo { get; set; } = null!;

    public string ArticuloNombre { get; set; } = null!;

    public string? ArticuloDescripcion { get; set; }

    public int Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal Subtotal { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public bool Activo { get; set; }

    public virtual Articulo Articulo { get; set; } = null!;

    public virtual Factura Factura { get; set; } = null!;
}
