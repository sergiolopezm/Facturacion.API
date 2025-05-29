using System.ComponentModel.DataAnnotations;

namespace Facturacion.API.Shared.InDTO.ArticulosInDto
{
    public class ArticuloDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El código del artículo es requerido")]
        [StringLength(20, ErrorMessage = "El código no puede exceder los 20 caracteres")]
        public string Codigo { get; set; } = null!;

        [Required(ErrorMessage = "El nombre del artículo es requerido")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres")]
        public string Nombre { get; set; } = null!;

        [StringLength(250, ErrorMessage = "La descripción no puede exceder los 250 caracteres")]
        public string? Descripcion { get; set; }

        [Required(ErrorMessage = "El precio unitario es requerido")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal PrecioUnitario { get; set; }

        [Required(ErrorMessage = "El stock es requerido")]
        [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo")]
        public int Stock { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "El stock mínimo no puede ser negativo")]
        public int StockMinimo { get; set; }

        public int? CategoriaId { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }

        // Propiedades de navegación para UI
        public string? Categoria { get; set; }
        public string? CreadoPor { get; set; }
        public string? ModificadoPor { get; set; }
        public string? PrecioUnitarioFormateado { get; set; }
        public bool StockBajo { get; set; }
        public int VecesVendido { get; set; }
    }
}
