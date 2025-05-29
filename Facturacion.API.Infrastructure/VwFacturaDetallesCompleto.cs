using System;
using System.Collections.Generic;

namespace Facturacion.API.Infrastructure;

public partial class VwFacturaDetallesCompleto
{
    public int Id { get; set; }

    public int FacturaId { get; set; }

    public string? NumeroFactura { get; set; }

    public DateTime FechaFactura { get; set; }

    public int ArticuloId { get; set; }

    public string ArticuloCodigo { get; set; } = null!;

    public string ArticuloNombre { get; set; } = null!;

    public string? ArticuloDescripcion { get; set; }

    public int Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal Subtotal { get; set; }

    public int StockActual { get; set; }

    public string? CategoriaArticulo { get; set; }
}
