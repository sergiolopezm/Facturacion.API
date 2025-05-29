using Facturacion.API.Shared.GeneralDTO;
using Facturacion.API.Shared.InDTO.FacturacionInDto;

namespace Facturacion.API.Domain.Contracts.FacturacionRepository
{
    public interface IFacturaRepository
    {
        Task<List<FacturaDto>> ObtenerTodosAsync();
        Task<FacturaDto?> ObtenerPorIdAsync(int id);
        Task<FacturaDto?> ObtenerPorNumeroAsync(string numeroFactura);
        Task<RespuestaDto> CrearAsync(CrearFacturaDto crearFacturaDto, Guid usuarioId);
        Task<RespuestaDto> ActualizarAsync(int id, FacturaDto facturaDto, Guid usuarioId);
        Task<RespuestaDto> AnularAsync(int id, string motivo, Guid usuarioId);
        Task<bool> ExisteAsync(int id);
        Task<PaginacionDto<FacturaResumenDto>> ObtenerPaginadoAsync(
            int pagina,
            int elementosPorPagina,
            string? busqueda = null,
            DateTime? fechaInicio = null,
            DateTime? fechaFin = null,
            string? estado = null,
            int? clienteId = null);

        Task<List<FacturaDto>> ObtenerPorClienteAsync(int clienteId);
        Task<List<FacturaDto>> ObtenerPorFechaAsync(DateTime fechaInicio, DateTime fechaFin);
        Task<ValidacionFacturaDto> ValidarFacturaAsync(CrearFacturaDto crearFacturaDto);
        Task<FacturaCalculoDto> CalcularTotalesAsync(List<CrearFacturaDetalleDto> detalles);
        Task<ReporteVentasDto> GenerarReporteVentasAsync(DateTime fechaInicio, DateTime fechaFin);
    }
}
