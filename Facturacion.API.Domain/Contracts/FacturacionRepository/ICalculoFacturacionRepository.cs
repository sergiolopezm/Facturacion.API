using Facturacion.API.Shared.InDTO.FacturacionInDto;
using Facturacion.API.Util;

namespace Facturacion.API.Domain.Contracts.FacturacionRepository
{
    public interface ICalculoFacturacionRepository
    {
        FacturaTotalesDto CalcularTotales(decimal subtotal, decimal porcentajeDescuento = 5m, decimal montoMinimoDescuento = 500000m, decimal porcentajeIVA = 19m);
        decimal CalcularSubtotal(List<CrearFacturaDetalleDto> detalles);
        decimal CalcularDescuento(decimal subtotal, decimal porcentajeDescuento = 5m, decimal montoMinimoDescuento = 500000m);
        decimal CalcularIVA(decimal baseImponible, decimal porcentajeIVA = 19m);
        decimal CalcularTotal(decimal subtotal, decimal descuento, decimal iva);
        ValidacionFacturaDto ValidarCalculos(CrearFacturaDto facturaDto);
        bool ValidarStock(List<CrearFacturaDetalleDto> detalles);
        Task<List<string>> ValidarArticulosExistenAsync(List<CrearFacturaDetalleDto> detalles);
    }
}
