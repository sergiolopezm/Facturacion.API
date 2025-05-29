using System.ComponentModel.DataAnnotations;

namespace Facturacion.API.Shared.InDTO.FacturacionInDto
{
    public class FacturaDto
    {
        public int Id { get; set; }
        public string? NumeroFactura { get; set; }
        public DateTime Fecha { get; set; }

        [Required(ErrorMessage = "El cliente es requerido")]
        public int ClienteId { get; set; }

        // Datos del cliente al momento de la factura
        [Required(ErrorMessage = "El número de documento del cliente es requerido")]
        public string ClienteNumeroDocumento { get; set; } = null!;

        [Required(ErrorMessage = "Los nombres del cliente son requeridos")]
        public string ClienteNombres { get; set; } = null!;

        [Required(ErrorMessage = "Los apellidos del cliente son requeridos")]
        public string ClienteApellidos { get; set; } = null!;

        [Required(ErrorMessage = "La dirección del cliente es requerida")]
        public string ClienteDireccion { get; set; } = null!;

        [Required(ErrorMessage = "El teléfono del cliente es requerido")]
        public string ClienteTelefono { get; set; } = null!;

        // Totales calculados
        public decimal SubTotal { get; set; }
        public decimal PorcentajeDescuento { get; set; }
        public decimal ValorDescuento { get; set; }
        public decimal BaseImpuestos { get; set; }
        public decimal PorcentajeIVA { get; set; }
        public decimal ValorIVA { get; set; }
        public decimal Total { get; set; }

        [StringLength(500, ErrorMessage = "Las observaciones no pueden exceder los 500 caracteres")]
        public string? Observaciones { get; set; }

        public string Estado { get; set; } = "Activa";
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }

        // Propiedades de navegación para UI
        public string? ClienteNombreCompleto { get; set; }
        public string? CreadoPor { get; set; }
        public string? ModificadoPor { get; set; }
        public List<FacturaDetalleDto>? Detalles { get; set; }
        public int TotalArticulos { get; set; }
        public int TotalCantidad { get; set; }

        // Propiedades formateadas para mostrar
        public string? SubTotalFormateado { get; set; }
        public string? ValorDescuentoFormateado { get; set; }
        public string? ValorIVAFormateado { get; set; }
        public string? TotalFormateado { get; set; }
        public string? FechaFormateada { get; set; }
    }
}
