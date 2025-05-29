namespace Facturacion.API.Shared.InDTO
{
    /// <summary>
    /// DTO para token de autenticación
    /// </summary>
    public class TokenDto
    {
        public string Token { get; set; } = null!;
        public DateTime Expiracion { get; set; }
        public UsuarioPerfilDto Usuario { get; set; } = null!;
    }
}
