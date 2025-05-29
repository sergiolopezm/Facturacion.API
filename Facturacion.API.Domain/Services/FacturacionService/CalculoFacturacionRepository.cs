using Facturacion.API.Domain.Contracts.FacturacionRepository;
using Facturacion.API.Infrastructure;
using Facturacion.API.Shared.InDTO.FacturacionInDto;
using Facturacion.API.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Facturacion.API.Domain.Services.FacturacionService
{
    public class CalculoFacturacionRepository : ICalculoFacturacionRepository
    {
        private readonly DBContext _context;
        private readonly ILogger<CalculoFacturacionRepository> _logger;

        public CalculoFacturacionRepository(DBContext context, ILogger<CalculoFacturacionRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public FacturaTotalesDto CalcularTotales(decimal subtotal, decimal porcentajeDescuento = 5m, decimal montoMinimoDescuento = 500000m, decimal porcentajeIVA = 19m)
        {
            var descuento = CalcularDescuento(subtotal, porcentajeDescuento, montoMinimoDescuento);
            var baseImponible = subtotal - descuento;
            var iva = CalcularIVA(baseImponible, porcentajeIVA);
            var total = CalcularTotal(subtotal, descuento, iva);

            return new FacturaTotalesDto
            {
                Subtotal = subtotal,
                PorcentajeDescuento = descuento > 0 ? porcentajeDescuento : 0,
                ValorDescuento = descuento,
                BaseImpuestos = baseImponible,
                PorcentajeIVA = porcentajeIVA,
                ValorIVA = iva,
                Total = total
            };
        }

        public decimal CalcularSubtotal(List<CrearFacturaDetalleDto> detalles)
        {
            return detalles.Sum(d => d.Cantidad * d.PrecioUnitario);
        }

        public decimal CalcularDescuento(decimal subtotal, decimal porcentajeDescuento = 5m, decimal montoMinimoDescuento = 500000m)
        {
            if (subtotal >= montoMinimoDescuento)
            {
                return Math.Round(subtotal * (porcentajeDescuento / 100m), 2);
            }
            return 0m;
        }

        public decimal CalcularIVA(decimal baseImponible, decimal porcentajeIVA = 19m)
        {
            return Math.Round(baseImponible * (porcentajeIVA / 100m), 2);
        }

        public decimal CalcularTotal(decimal subtotal, decimal descuento, decimal iva)
        {
            return subtotal - descuento + iva;
        }

        public ValidacionFacturaDto ValidarCalculos(CrearFacturaDto facturaDto)
        {
            var validacion = new ValidacionFacturaDto { EsValida = true };

            try
            {
                if (facturaDto.Detalles == null || !facturaDto.Detalles.Any())
                {
                    validacion.Errores.Add("Debe incluir al menos un artículo en la factura");
                    validacion.EsValida = false;
                    return validacion;
                }

                foreach (var detalle in facturaDto.Detalles)
                {
                    if (detalle.Cantidad <= 0)
                    {
                        validacion.Errores.Add($"La cantidad del artículo ID {detalle.ArticuloId} debe ser mayor a 0");
                        validacion.EsValida = false;
                    }

                    if (detalle.PrecioUnitario <= 0)
                    {
                        validacion.Errores.Add($"El precio del artículo ID {detalle.ArticuloId} debe ser mayor a 0");
                        validacion.EsValida = false;
                    }
                }

                if (validacion.EsValida)
                {
                    var subtotal = CalcularSubtotal(facturaDto.Detalles);
                    validacion.TotalesCalculados = CalcularTotales(subtotal);
                }
            }
            catch (Exception ex)
            {
                validacion.Errores.Add($"Error en cálculos: {ex.Message}");
                validacion.EsValida = false;
            }

            return validacion;
        }

        public bool ValidarStock(List<CrearFacturaDetalleDto> detalles)
        {
            // Validación sincrónica de stock
            try
            {
                if (detalles == null || !detalles.Any())
                    return true;

                var articulosIds = detalles.Select(d => d.ArticuloId).Distinct().ToList();

                // Consultamos los artículos en una sola consulta para mejorar rendimiento
                var articulos = _context.Articulos
                    .Where(a => articulosIds.Contains(a.Id) && a.Activo)
                    .ToDictionary(a => a.Id, a => a);

                // Verificamos si hay suficiente stock para cada artículo
                foreach (var detalle in detalles)
                {
                    if (!articulos.TryGetValue(detalle.ArticuloId, out var articulo))
                        return false; // El artículo no existe o no está activo

                    if (articulo.Stock < detalle.Cantidad)
                        return false; // No hay suficiente stock
                }

                // Si llegamos aquí, todos los artículos tienen stock suficiente
                return true;
            }
            catch (Exception ex)
            {
                // En caso de error, registramos la excepción y devolvemos false
                if (_logger != null)
                    _logger.LogError(ex, "Error al validar stock de artículos");
                return false;
            }
        }

        public async Task<List<string>> ValidarArticulosExistenAsync(List<CrearFacturaDetalleDto> detalles)
        {
            var errores = new List<string>();

            try
            {
                // Si no hay detalles, no hay nada que validar
                if (detalles == null || !detalles.Any())
                    return errores;

                var articulosIds = detalles.Select(d => d.ArticuloId).Distinct().ToList();

                // Consultamos los artículos existentes en una sola consulta
                var articulosExistentes = await _context.Articulos
                    .Where(a => articulosIds.Contains(a.Id))
                    .Select(a => new { a.Id, a.Nombre, a.Activo, a.Stock })
                    .ToDictionaryAsync(a => a.Id);

                // Verificamos cada detalle
                foreach (var detalle in detalles)
                {
                    // Validar existencia
                    if (!articulosExistentes.TryGetValue(detalle.ArticuloId, out var articulo))
                    {
                        errores.Add($"El artículo con ID {detalle.ArticuloId} no existe");
                        continue;
                    }

                    // Validar que esté activo
                    if (!articulo.Activo)
                    {
                        errores.Add($"El artículo '{articulo.Nombre}' (ID: {detalle.ArticuloId}) no está activo");
                    }

                    // Validar stock
                    if (articulo.Stock < detalle.Cantidad)
                    {
                        errores.Add($"No hay suficiente stock del artículo '{articulo.Nombre}'. Stock disponible: {articulo.Stock}, Cantidad solicitada: {detalle.Cantidad}");
                    }
                }

                return errores;
            }
            catch (Exception ex)
            {
                // En caso de error, registramos la excepción y devolvemos un mensaje genérico
                if (_logger != null)
                    _logger.LogError(ex, "Error al validar la existencia de artículos");
                errores.Add("Error al validar la existencia de los artículos");
                return errores;
            }
        }
    }
}
