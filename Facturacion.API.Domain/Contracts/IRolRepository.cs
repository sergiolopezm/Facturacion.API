using Facturacion.API.Shared.InDTO;

namespace Facturacion.API.Domain.Contracts
{
    /// <summary>
    /// Interfaz para la gestión de roles en el sistema
    /// </summary>
    public interface IRolRepository
    {
        Task<List<RolDto>> ObtenerTodosAsync();
        Task<RolDto?> ObtenerPorIdAsync(int id);
        Task<RolDto?> ObtenerPorNombreAsync(string nombre);
        Task<bool> ExisteAsync(int id);
        Task<bool> ExistePorNombreAsync(string nombre);
    }
}
