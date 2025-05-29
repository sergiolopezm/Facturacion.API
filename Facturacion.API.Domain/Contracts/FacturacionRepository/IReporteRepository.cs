using Facturacion.API.Shared.InDTO.ArticulosInDto;
using Facturacion.API.Shared.InDTO.ClienteInDto;
using Facturacion.API.Shared.InDTO.FacturacionInDto;

namespace Facturacion.API.Domain.Contracts.FacturacionRepository
{
    public interface IReporteRepository
    {
        Task<ReporteVentasDto> GenerarReporteVentasPorPeriodoAsync(DateTime fechaInicio, DateTime fechaFin);
        Task<List<ArticuloVendidoDto>> ObtenerArticulosMasVendidosAsync(DateTime fechaInicio, DateTime fechaFin, int top = 10);
        Task<List<ClienteFrecuenteDto>> ObtenerClientesFrecuentesAsync(DateTime fechaInicio, DateTime fechaFin, int top = 10);
        Task<Dictionary<string, decimal>> ObtenerVentasPorMesAsync(int año);
        Task<Dictionary<string, int>> ObtenerFacturasPorEstadoAsync();
        Task<Dictionary<string, decimal>> ObtenerVentasPorCategoriaAsync(DateTime fechaInicio, DateTime fechaFin);
        Task<decimal> ObtenerTotalVentasDelDiaAsync(DateTime fecha);
        Task<int> ObtenerTotalFacturasDelDiaAsync(DateTime fecha);
        Task<decimal> ObtenerPromedioVentaDiariaAsync(DateTime fechaInicio, DateTime fechaFin);
        Task<List<ArticuloDto>> ObtenerArticulosConStockBajoAsync();
    }
}
