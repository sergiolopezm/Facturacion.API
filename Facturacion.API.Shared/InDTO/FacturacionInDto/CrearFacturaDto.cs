using System.ComponentModel.DataAnnotations;

namespace Facturacion.API.Shared.InDTO.FacturacionInDto
{
    public class CrearFacturaDto
    {
        [Required(ErrorMessage = "El cliente es requerido")]
        public int ClienteId { get; set; }

        [StringLength(500, ErrorMessage = "Las observaciones no pueden exceder los 500 caracteres")]
        public string? Observaciones { get; set; }

        [Required(ErrorMessage = "Debe incluir al menos un artículo")]
        [MinLength(1, ErrorMessage = "Debe incluir al menos un artículo")]
        public List<CrearFacturaDetalleDto> Detalles { get; set; } = new List<CrearFacturaDetalleDto>();
    }
}
