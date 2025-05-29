using Facturacion.API.Shared.GeneralDTO;
using Facturacion.API.Shared.InDTO.ClienteInDto;

namespace Facturacion.API.Domain.Contracts.FacturacionRepository
{
    public interface IClienteRepository
    {
        Task<List<ClienteDto>> ObtenerTodosAsync();
        Task<ClienteDto?> ObtenerPorIdAsync(int id);
        Task<ClienteDto?> ObtenerPorDocumentoAsync(string numeroDocumento);
        Task<RespuestaDto> CrearAsync(ClienteDto clienteDto, Guid usuarioId);
        Task<RespuestaDto> ActualizarAsync(int id, ClienteDto clienteDto, Guid usuarioId);
        Task<RespuestaDto> EliminarAsync(int id);
        Task<bool> ExisteAsync(int id);
        Task<bool> ExistePorDocumentoAsync(string numeroDocumento);
        Task<PaginacionDto<ClienteDto>> ObtenerPaginadoAsync(int pagina, int elementosPorPagina, string? busqueda = null);
        Task<List<ClienteFrecuenteDto>> ObtenerClientesFrecuentesAsync(int top = 10);
    }
}
