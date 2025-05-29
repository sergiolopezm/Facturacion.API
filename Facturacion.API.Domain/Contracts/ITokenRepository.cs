using Facturacion.API.Infrastructure;
using Facturacion.API.Shared.GeneralDTO;

namespace Facturacion.API.Domain.Contracts
{
    /// <summary>
    /// Interfaz para la gestión de tokens de autenticación
    /// </summary>
    public interface ITokenRepository
    {
        Task<string> GenerarTokenAsync(Usuario usuario, string ip);
        Task<bool> CancelarTokenAsync(string token);
        Task<object> ObtenerInformacionTokenAsync(string token);
        Task<ValidoDto> EsValidoAsync(string idToken, Guid idUsuario, string ip);
        Task AumentarTiempoExpiracionAsync(string token);
    }
}
