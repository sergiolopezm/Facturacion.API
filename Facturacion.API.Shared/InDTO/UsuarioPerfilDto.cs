namespace Facturacion.API.Shared.InDTO
{
    /// <summary>
    /// DTO para mostrar información de perfil de usuario
    /// </summary>
    public class UsuarioPerfilDto
    {
        public Guid Id { get; set; }
        public string NombreUsuario { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string Apellido { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Rol { get; set; } = null!;
        public int RolId { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaUltimoAcceso { get; set; }
    }
}
