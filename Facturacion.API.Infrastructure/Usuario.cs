using System;
using System.Collections.Generic;

namespace Facturacion.API.Infrastructure;

public partial class Usuario
{
    public Guid Id { get; set; }

    public string NombreUsuario { get; set; } = null!;

    public string Contraseña { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string Apellido { get; set; } = null!;

    public string Email { get; set; } = null!;

    public int RolId { get; set; }

    public bool Activo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public DateTime? FechaUltimoAcceso { get; set; }

    public virtual ICollection<Articulo> ArticuloCreadoPors { get; set; } = new List<Articulo>();

    public virtual ICollection<Articulo> ArticuloModificadoPors { get; set; } = new List<Articulo>();

    public virtual ICollection<CategoriasArticulo> CategoriasArticuloCreadoPors { get; set; } = new List<CategoriasArticulo>();

    public virtual ICollection<CategoriasArticulo> CategoriasArticuloModificadoPors { get; set; } = new List<CategoriasArticulo>();

    public virtual ICollection<Cliente> ClienteCreadoPors { get; set; } = new List<Cliente>();

    public virtual ICollection<Cliente> ClienteModificadoPors { get; set; } = new List<Cliente>();

    public virtual ICollection<Factura> FacturaCreadoPors { get; set; } = new List<Factura>();

    public virtual ICollection<Factura> FacturaModificadoPors { get; set; } = new List<Factura>();

    public virtual ICollection<Log> Logs { get; set; } = new List<Log>();

    public virtual Role Rol { get; set; } = null!;

    public virtual ICollection<Token> Tokens { get; set; } = new List<Token>();

    public virtual ICollection<TokensExpirado> TokensExpirados { get; set; } = new List<TokensExpirado>();
}
