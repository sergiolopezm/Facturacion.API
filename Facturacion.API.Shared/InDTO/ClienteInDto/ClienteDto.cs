using System.ComponentModel.DataAnnotations;

namespace Facturacion.API.Shared.InDTO.ClienteInDto
{
    public class ClienteDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El número de documento es requerido")]
        [StringLength(15, ErrorMessage = "El número de documento no puede exceder los 15 caracteres")]
        public string NumeroDocumento { get; set; } = null!;

        [Required(ErrorMessage = "Los nombres son requeridos")]
        [StringLength(100, ErrorMessage = "Los nombres no pueden exceder los 100 caracteres")]
        public string Nombres { get; set; } = null!;

        [Required(ErrorMessage = "Los apellidos son requeridos")]
        [StringLength(100, ErrorMessage = "Los apellidos no pueden exceder los 100 caracteres")]
        public string Apellidos { get; set; } = null!;

        [Required(ErrorMessage = "La dirección es requerida")]
        [StringLength(250, ErrorMessage = "La dirección no puede exceder los 250 caracteres")]
        public string Direccion { get; set; } = null!;

        [Required(ErrorMessage = "El teléfono es requerido")]
        [StringLength(20, ErrorMessage = "El teléfono no puede exceder los 20 caracteres")]
        [Phone(ErrorMessage = "El formato del teléfono no es válido")]
        public string Telefono { get; set; } = null!;

        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        [StringLength(100, ErrorMessage = "El email no puede exceder los 100 caracteres")]
        public string? Email { get; set; }

        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }

        // Propiedades calculadas para UI
        public string? NombreCompleto { get; set; }
        public string? CreadoPor { get; set; }
        public string? ModificadoPor { get; set; }
        public int TotalFacturas { get; set; }
        public decimal MontoTotalCompras { get; set; }
        public string? MontoTotalComprasFormateado { get; set; }
    }
}
