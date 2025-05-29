using Facturacion.API.Shared.GeneralDTO;
using Facturacion.API.Shared.InDTO;

namespace Facturacion.API.Domain.Contracts
{
    /// <summary>
    /// Interfaz para la gestión de usuarios en el sistema
    /// </summary>
    public interface IUsuarioRepository
    {
        Task<RespuestaDto> AutenticarUsuarioAsync(UsuarioLoginDto usuario);
        Task<RespuestaDto> RegistrarUsuarioAsync(UsuarioRegistroDto usuario);
        Task<UsuarioPerfilDto?> ObtenerUsuarioPorIdAsync(Guid id);
        Task<bool> ExisteUsuarioAsync(string nombreUsuario);
        Task<bool> ExisteEmailAsync(string email);
        Task<List<UsuarioPerfilDto>> ObtenerTodosAsync();
        Task<RespuestaDto> ActualizarUsuarioAsync(Guid id, UsuarioRegistroDto usuario);
        Task<RespuestaDto> CambiarEstadoUsuarioAsync(Guid id, bool estado);
        Task<RespuestaDto> CambiarContraseñaAsync(Guid id, string nuevaContraseña);
    }
}
