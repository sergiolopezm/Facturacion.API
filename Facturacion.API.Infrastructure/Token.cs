using System;
using System.Collections.Generic;

namespace Facturacion.API.Infrastructure;

public partial class Token
{
    public Guid Id { get; set; }

    public string Token1 { get; set; } = null!;

    public Guid UsuarioId { get; set; }

    public string Ip { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaExpiracion { get; set; }

    public string? Observacion { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;
}
