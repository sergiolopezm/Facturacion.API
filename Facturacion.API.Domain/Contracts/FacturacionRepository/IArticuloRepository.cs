using Facturacion.API.Shared.GeneralDTO;
using Facturacion.API.Shared.InDTO.ArticulosInDto;

namespace Facturacion.API.Domain.Contracts.FacturacionRepository
{
    public interface IArticuloRepository
    {
        Task<List<ArticuloDto>> ObtenerTodosAsync();
        Task<ArticuloDto?> ObtenerPorIdAsync(int id);
        Task<ArticuloDto?> ObtenerPorCodigoAsync(string codigo);
        Task<RespuestaDto> CrearAsync(ArticuloDto articuloDto, Guid usuarioId);
        Task<RespuestaDto> ActualizarAsync(int id, ArticuloDto articuloDto, Guid usuarioId);
        Task<RespuestaDto> EliminarAsync(int id);
        Task<RespuestaDto> ActualizarStockAsync(int id, int nuevoStock, Guid usuarioId);
        Task<bool> ExisteAsync(int id);
        Task<bool> ExistePorCodigoAsync(string codigo);
        Task<bool> TieneStockSuficienteAsync(int id, int cantidadRequerida);
        Task<PaginacionDto<ArticuloDto>> ObtenerPaginadoAsync(int pagina, int elementosPorPagina, string? busqueda = null, int? categoriaId = null);
        Task<List<ArticuloDto>> ObtenerPorStockBajoAsync();
        Task<List<ArticuloVendidoDto>> ObtenerMasVendidosAsync(DateTime? fechaInicio = null, DateTime? fechaFin = null, int top = 10);
        Task<List<ArticuloDto>> ObtenerPorCategoriaAsync(int categoriaId);
    }
}
