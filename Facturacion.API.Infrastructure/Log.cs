using System;
using System.Collections.Generic;

namespace Facturacion.API.Infrastructure;

public partial class Log
{
    public long Id { get; set; }

    public DateTime Fecha { get; set; }

    public string Tipo { get; set; } = null!;

    public Guid? UsuarioId { get; set; }

    public string? Ip { get; set; }

    public string? Accion { get; set; }

    public string? Detalle { get; set; }

    public virtual Usuario? Usuario { get; set; }
}
