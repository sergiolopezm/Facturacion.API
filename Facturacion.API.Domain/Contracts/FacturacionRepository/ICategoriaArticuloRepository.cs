using Facturacion.API.Shared.GeneralDTO;
using Facturacion.API.Shared.InDTO.ArticulosInDto;

namespace Facturacion.API.Domain.Contracts.FacturacionRepository
{
    public interface ICategoriaArticuloRepository
    {
        Task<List<CategoriaArticuloDto>> ObtenerTodosAsync();
        Task<CategoriaArticuloDto?> ObtenerPorIdAsync(int id);
        Task<RespuestaDto> CrearAsync(CategoriaArticuloDto categoriaDto, Guid usuarioId);
        Task<RespuestaDto> ActualizarAsync(int id, CategoriaArticuloDto categoriaDto, Guid usuarioId);
        Task<RespuestaDto> EliminarAsync(int id);
        Task<bool> ExisteAsync(int id);
        Task<bool> ExistePorNombreAsync(string nombre);
        Task<PaginacionDto<CategoriaArticuloDto>> ObtenerPaginadoAsync(int pagina, int elementosPorPagina, string? busqueda = null);
    }
}
