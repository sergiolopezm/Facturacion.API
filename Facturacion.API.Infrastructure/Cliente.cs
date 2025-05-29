using System;
using System.Collections.Generic;

namespace Facturacion.API.Infrastructure;

public partial class Cliente
{
    public int Id { get; set; }

    public string NumeroDocumento { get; set; } = null!;

    public string Nombres { get; set; } = null!;

    public string Apellidos { get; set; } = null!;

    public string Direccion { get; set; } = null!;

    public string Telefono { get; set; } = null!;

    public string? Email { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public Guid? CreadoPorId { get; set; }

    public Guid? ModificadoPorId { get; set; }

    public bool Activo { get; set; }

    public virtual Usuario? CreadoPor { get; set; }

    public virtual ICollection<Factura> Facturas { get; set; } = new List<Factura>();

    public virtual Usuario? ModificadoPor { get; set; }
}
