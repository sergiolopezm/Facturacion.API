using System;
using System.Collections.Generic;

namespace Facturacion.API.Infrastructure;

public partial class Articulo
{
    public int Id { get; set; }

    public string Codigo { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public decimal PrecioUnitario { get; set; }

    public int Stock { get; set; }

    public int StockMinimo { get; set; }

    public int? CategoriaId { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public Guid? CreadoPorId { get; set; }

    public Guid? ModificadoPorId { get; set; }

    public bool Activo { get; set; }

    public virtual CategoriasArticulo? Categoria { get; set; }

    public virtual Usuario? CreadoPor { get; set; }

    public virtual ICollection<FacturaDetalle> FacturaDetalles { get; set; } = new List<FacturaDetalle>();

    public virtual Usuario? ModificadoPor { get; set; }
}
