using System.ComponentModel.DataAnnotations;

namespace Facturacion.API.Shared.InDTO.FacturacionInDto
{
    public class FacturaDetalleDto
    {
        public int Id { get; set; }
        public int FacturaId { get; set; }

        [Required(ErrorMessage = "El artículo es requerido")]
        public int ArticuloId { get; set; }

        // Datos del artículo al momento de la factura
        public string ArticuloCodigo { get; set; } = null!;
        public string ArticuloNombre { get; set; } = null!;
        public string? ArticuloDescripcion { get; set; }

        [Required(ErrorMessage = "La cantidad es requerida")]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public int Cantidad { get; set; }

        [Required(ErrorMessage = "El precio unitario es requerido")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal PrecioUnitario { get; set; }

        public decimal Subtotal { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; }

        // Propiedades de navegación para UI
        public string? NumeroFactura { get; set; }
        public DateTime? FechaFactura { get; set; }
        public int? StockActual { get; set; }
        public string? CategoriaArticulo { get; set; }

        // Propiedades formateadas para mostrar
        public string? PrecioUnitarioFormateado { get; set; }
        public string? SubtotalFormateado { get; set; }
    }

}
