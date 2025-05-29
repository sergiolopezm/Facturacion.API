using Facturacion.API.Shared.GeneralDTO;
using Facturacion.API.Shared.InDTO.FacturacionInDto;

namespace Facturacion.API.Domain.Contracts.FacturacionRepository
{
    public interface IFacturaDetalleRepository
    {
        Task<List<FacturaDetalleDto>> ObtenerPorFacturaAsync(int facturaId);
        Task<FacturaDetalleDto?> ObtenerPorIdAsync(int id);
        Task<RespuestaDto> CrearAsync(FacturaDetalleDto detalleDto, Guid usuarioId);
        Task<RespuestaDto> ActualizarAsync(int id, FacturaDetalleDto detalleDto, Guid usuarioId);
        Task<RespuestaDto> EliminarAsync(int id);
        Task<bool> ExisteAsync(int id);
        Task<List<FacturaDetalleDto>> ObtenerPorArticuloAsync(int articuloId);
        Task<PaginacionDto<FacturaDetalleDto>> ObtenerPaginadoAsync(
            int pagina,
            int elementosPorPagina,
            int? facturaId = null,
            int? articuloId = null,
            DateTime? fechaInicio = null,
            DateTime? fechaFin = null);
    }
}
