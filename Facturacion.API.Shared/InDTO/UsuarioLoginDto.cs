using System.ComponentModel.DataAnnotations;

namespace Facturacion.API.Shared.InDTO
{
    /// <summary>
    /// DTO para el login de usuario
    /// </summary>
    public class UsuarioLoginDto
    {
        [Required(ErrorMessage = "El usuario es requerido")]
        public string NombreUsuario { get; set; } = null!;

        [Required(ErrorMessage = "La contraseña es requerida")]
        public string Contraseña { get; set; } = null!;

        public string? Ip { get; set; }
    }
}
