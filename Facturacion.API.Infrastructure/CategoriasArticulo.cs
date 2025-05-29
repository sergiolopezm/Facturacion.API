using System;
using System.Collections.Generic;

namespace Facturacion.API.Infrastructure;

public partial class CategoriasArticulo
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public Guid? CreadoPorId { get; set; }

    public Guid? ModificadoPorId { get; set; }

    public bool Activo { get; set; }

    public virtual ICollection<Articulo> Articulos { get; set; } = new List<Articulo>();

    public virtual Usuario? CreadoPor { get; set; }

    public virtual Usuario? ModificadoPor { get; set; }
}
