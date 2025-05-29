using System.ComponentModel.DataAnnotations;

namespace Facturacion.API.Shared.InDTO.ArticulosInDto
{
    public class CategoriaArticuloDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de la categoría es requerido")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres")]
        public string Nombre { get; set; } = null!;

        [StringLength(250, ErrorMessage = "La descripción no puede exceder los 250 caracteres")]
        public string? Descripcion { get; set; }

        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }

        // Propiedades de navegación para UI
        public string? CreadoPor { get; set; }
        public string? ModificadoPor { get; set; }
        public int TotalArticulos { get; set; }
    }
}
