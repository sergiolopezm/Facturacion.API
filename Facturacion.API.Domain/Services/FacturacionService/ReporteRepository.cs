using Facturacion.API.Domain.Contracts.FacturacionRepository;
using Facturacion.API.Infrastructure;
using Facturacion.API.Shared.InDTO.ArticulosInDto;
using Facturacion.API.Shared.InDTO.ClienteInDto;
using Facturacion.API.Shared.InDTO.FacturacionInDto;
using Facturacion.API.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Facturacion.API.Domain.Services.FacturacionService
{
    /// <summary>
    /// Implementación del repositorio de reportes para el sistema de facturación
    /// </summary>
    public class ReporteRepository : IReporteRepository
    {
        private readonly DBContext _context;
        private readonly ILogger<ReporteRepository> _logger;
        private readonly IConfiguration _configuration;

        public ReporteRepository(
            DBContext context,
            ILogger<ReporteRepository> logger,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Genera un reporte de ventas para un período específico
        /// </summary>
        public async Task<ReporteVentasDto> GenerarReporteVentasPorPeriodoAsync(DateTime fechaInicio, DateTime fechaFin)
        {
            _logger.LogInformation("Generando reporte de ventas para período: {FechaInicio:yyyy-MM-dd} - {FechaFin:yyyy-MM-dd}",
                fechaInicio, fechaFin);

            // Asegurar que las fechas tienen el formato correcto
            DateTime inicio = fechaInicio.Date;
            DateTime fin = fechaFin.Date.AddDays(1).AddTicks(-1); // Hasta el final del día

            // Obtener todas las facturas activas del período
            var facturas = await _context.Facturas
                .Where(f => f.Fecha >= inicio && f.Fecha <= fin && f.Estado == "Activa" && f.Activo)
                .Include(f => f.FacturaDetalles.Where(fd => fd.Activo))
                .AsNoTracking()
                .ToListAsync();

            // Calcular totales
            decimal totalVentas = facturas.Sum(f => f.Total);
            decimal totalIVA = facturas.Sum(f => f.ValorIva);
            decimal totalDescuentos = facturas.Sum(f => f.ValorDescuento);

            // Crear DTO de respuesta
            var reporte = new ReporteVentasDto
            {
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                TotalFacturas = facturas.Count,
                TotalVentas = totalVentas,
                TotalVentasFormateado = CurrencyHelper.FormatCurrency(totalVentas),
                TotalIVA = totalIVA,
                TotalIVAFormateado = CurrencyHelper.FormatCurrency(totalIVA),
                TotalDescuentos = totalDescuentos,
                TotalDescuentosFormateado = CurrencyHelper.FormatCurrency(totalDescuentos)
            };

            // Complementar con artículos más vendidos y clientes frecuentes
            reporte.ArticulosMasVendidos = await ObtenerArticulosMasVendidosAsync(fechaInicio, fechaFin);
            reporte.ClientesFrecuentes = await ObtenerClientesFrecuentesAsync(fechaInicio, fechaFin);

            return reporte;
        }

        /// <summary>
        /// Obtiene los artículos más vendidos en un período específico
        /// </summary>
        public async Task<List<ArticuloVendidoDto>> ObtenerArticulosMasVendidosAsync(DateTime fechaInicio, DateTime fechaFin, int top = 10)
        {
            _logger.LogInformation("Obteniendo top {Top} artículos más vendidos: {FechaInicio:yyyy-MM-dd} - {FechaFin:yyyy-MM-dd}",
                top, fechaInicio, fechaFin);

            DateTime inicio = fechaInicio.Date;
            DateTime fin = fechaFin.Date.AddDays(1).AddTicks(-1);

            // Obtener y agrupar por artículo para calcular cantidades y montos
            var articulosMasVendidos = await _context.FacturaDetalles
                .Where(fd => fd.Activo && fd.Factura.Activo && fd.Factura.Estado == "Activa" &&
                             fd.Factura.Fecha >= inicio && fd.Factura.Fecha <= fin)
                .GroupBy(fd => new { fd.ArticuloId, fd.ArticuloCodigo, fd.ArticuloNombre })
                .Select(g => new
                {
                    ArticuloId = g.Key.ArticuloId,
                    Codigo = g.Key.ArticuloCodigo,
                    Nombre = g.Key.ArticuloNombre,
                    CantidadVendida = g.Sum(fd => fd.Cantidad),
                    MontoVendido = g.Sum(fd => fd.Subtotal),
                    VecesVendido = g.Count()
                })
                .OrderByDescending(a => a.MontoVendido)
                .Take(top)
                .ToListAsync();

            // Convertir a DTOs
            return articulosMasVendidos.Select(a => new ArticuloVendidoDto
            {
                ArticuloId = a.ArticuloId,
                Codigo = a.Codigo,
                Nombre = a.Nombre,
                CantidadVendida = a.CantidadVendida,
                MontoVendido = a.MontoVendido,
                MontoVendidoFormateado = CurrencyHelper.FormatCurrency(a.MontoVendido),
                VecesVendido = a.VecesVendido
            }).ToList();
        }

        /// <summary>
        /// Obtiene los clientes frecuentes en un período específico
        /// </summary>
        public async Task<List<ClienteFrecuenteDto>> ObtenerClientesFrecuentesAsync(DateTime fechaInicio, DateTime fechaFin, int top = 10)
        {
            _logger.LogInformation("Obteniendo top {Top} clientes frecuentes: {FechaInicio:yyyy-MM-dd} - {FechaFin:yyyy-MM-dd}",
                top, fechaInicio, fechaFin);

            DateTime inicio = fechaInicio.Date;
            DateTime fin = fechaFin.Date.AddDays(1).AddTicks(-1);

            // Obtener y agrupar por cliente para calcular totales
            var clientesFrecuentes = await _context.Facturas
                .Where(f => f.Activo && f.Estado == "Activa" && f.Fecha >= inicio && f.Fecha <= fin)
                .GroupBy(f => new { f.ClienteId, f.ClienteNombres, f.ClienteApellidos, f.ClienteNumeroDocumento })
                .Select(g => new
                {
                    ClienteId = g.Key.ClienteId,
                    NombreCompleto = $"{g.Key.ClienteNombres} {g.Key.ClienteApellidos}",
                    NumeroDocumento = g.Key.ClienteNumeroDocumento,
                    TotalFacturas = g.Count(),
                    MontoTotalCompras = g.Sum(f => f.Total),
                    UltimaCompra = g.Max(f => f.Fecha)
                })
                .OrderByDescending(c => c.MontoTotalCompras)
                .Take(top)
                .ToListAsync();

            // Convertir a DTOs
            return clientesFrecuentes.Select(c => new ClienteFrecuenteDto
            {
                ClienteId = c.ClienteId,
                NombreCompleto = c.NombreCompleto,
                NumeroDocumento = c.NumeroDocumento,
                TotalFacturas = c.TotalFacturas,
                MontoTotalCompras = c.MontoTotalCompras,
                MontoTotalComprasFormateado = CurrencyHelper.FormatCurrency(c.MontoTotalCompras),
                UltimaCompra = c.UltimaCompra
            }).ToList();
        }

        /// <summary>
        /// Obtiene las ventas por mes para un año específico
        /// </summary>
        public async Task<Dictionary<string, decimal>> ObtenerVentasPorMesAsync(int año)
        {
            _logger.LogInformation("Obteniendo ventas por mes para el año {Año}", año);

            var fechaInicio = new DateTime(año, 1, 1);
            var fechaFin = new DateTime(año, 12, 31).AddDays(1).AddTicks(-1);

            // Obtener facturas del año
            var facturas = await _context.Facturas
                .Where(f => f.Activo && f.Estado == "Activa" && f.Fecha >= fechaInicio && f.Fecha <= fechaFin)
                .ToListAsync();

            // Inicializar diccionario con todos los meses
            var ventasPorMes = new Dictionary<string, decimal>();
            for (int i = 1; i <= 12; i++)
            {
                string nombreMes = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(i);
                ventasPorMes.Add(nombreMes, 0);
            }

            // Calcular ventas por mes
            foreach (var factura in facturas)
            {
                string mes = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(factura.Fecha.Month);
                ventasPorMes[mes] += factura.Total;
            }

            return ventasPorMes;
        }

        /// <summary>
        /// Obtiene la cantidad de facturas por estado
        /// </summary>
        public async Task<Dictionary<string, int>> ObtenerFacturasPorEstadoAsync()
        {
            _logger.LogInformation("Obteniendo cantidades de facturas por estado");

            // Obtener facturas agrupadas por estado
            var facturasAgrupadas = await _context.Facturas
                .Where(f => f.Activo)
                .GroupBy(f => f.Estado)
                .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
                .ToListAsync();

            // Convertir a diccionario
            var facturasEstado = facturasAgrupadas
                .ToDictionary(f => f.Estado, f => f.Cantidad);

            return facturasEstado;
        }

        /// <summary>
        /// Obtiene las ventas por categoría de artículo en un período específico
        /// </summary>
        public async Task<Dictionary<string, decimal>> ObtenerVentasPorCategoriaAsync(DateTime fechaInicio, DateTime fechaFin)
        {
            _logger.LogInformation("Obteniendo ventas por categoría: {FechaInicio:yyyy-MM-dd} - {FechaFin:yyyy-MM-dd}",
                fechaInicio, fechaFin);

            DateTime inicio = fechaInicio.Date;
            DateTime fin = fechaFin.Date.AddDays(1).AddTicks(-1);

            // Obtener detalles de facturas con sus artículos y categorías
            var detalles = await _context.FacturaDetalles
                .Where(fd => fd.Activo && fd.Factura.Activo && fd.Factura.Estado == "Activa" &&
                             fd.Factura.Fecha >= inicio && fd.Factura.Fecha <= fin)
                .Include(fd => fd.Articulo)
                .ThenInclude(a => a.Categoria)
                .ToListAsync();

            // Agrupar por categoría
            var ventasPorCategoria = detalles
                .GroupBy(fd => fd.Articulo.Categoria?.Nombre ?? "Sin Categoría")
                .ToDictionary(g => g.Key, g => g.Sum(fd => fd.Subtotal));

            return ventasPorCategoria;
        }

        /// <summary>
        /// Obtiene el total de ventas del día especificado
        /// </summary>
        public async Task<decimal> ObtenerTotalVentasDelDiaAsync(DateTime fecha)
        {
            _logger.LogInformation("Obteniendo total de ventas para la fecha: {Fecha:yyyy-MM-dd}", fecha);

            DateTime inicio = fecha.Date;
            DateTime fin = fecha.Date.AddDays(1).AddTicks(-1);

            // Obtener suma de totales de facturas activas del día
            var totalVentas = await _context.Facturas
                .Where(f => f.Activo && f.Estado == "Activa" && f.Fecha >= inicio && f.Fecha <= fin)
                .SumAsync(f => f.Total);

            return totalVentas;
        }

        /// <summary>
        /// Obtiene el total de facturas emitidas en el día especificado
        /// </summary>
        public async Task<int> ObtenerTotalFacturasDelDiaAsync(DateTime fecha)
        {
            _logger.LogInformation("Obteniendo total de facturas para la fecha: {Fecha:yyyy-MM-dd}", fecha);

            DateTime inicio = fecha.Date;
            DateTime fin = fecha.Date.AddDays(1).AddTicks(-1);

            // Contar facturas activas del día
            var totalFacturas = await _context.Facturas
                .Where(f => f.Activo && f.Fecha >= inicio && f.Fecha <= fin)
                .CountAsync();

            return totalFacturas;
        }

        /// <summary>
        /// Obtiene el promedio de ventas diarias en un período específico
        /// </summary>
        public async Task<decimal> ObtenerPromedioVentaDiariaAsync(DateTime fechaInicio, DateTime fechaFin)
        {
            _logger.LogInformation("Calculando promedio de ventas diarias: {FechaInicio:yyyy-MM-dd} - {FechaFin:yyyy-MM-dd}",
                fechaInicio, fechaFin);

            DateTime inicio = fechaInicio.Date;
            DateTime fin = fechaFin.Date.AddDays(1).AddTicks(-1);

            // Obtener facturas del período
            var facturas = await _context.Facturas
                .Where(f => f.Activo && f.Estado == "Activa" && f.Fecha >= inicio && f.Fecha <= fin)
                .ToListAsync();

            // Si no hay facturas, retornar 0
            if (!facturas.Any())
                return 0;

            // Agrupar por día y calcular venta por día
            var ventasPorDia = facturas
                .GroupBy(f => f.Fecha.Date)
                .ToDictionary(g => g.Key, g => g.Sum(f => f.Total));

            // Calcular promedio
            decimal promedio = ventasPorDia.Values.Average();

            return promedio;
        }

        /// <summary>
        /// Obtiene los artículos con stock bajo según umbral configurado
        /// </summary>
        public async Task<List<ArticuloDto>> ObtenerArticulosConStockBajoAsync()
        {
            _logger.LogInformation("Obteniendo artículos con stock bajo");

            // Obtener artículos activos donde stock esté por debajo del mínimo
            var articulos = await _context.Articulos
                .Where(a => a.Activo && a.Stock <= a.StockMinimo)
                .Include(a => a.Categoria)
                .AsNoTracking()
                .OrderBy(a => a.Stock)
                .ToListAsync();

            // Convertir a DTOs
            var articulosDto = Mapping.ConvertirLista<Articulo, ArticuloDto>(articulos);

            // Completar información adicional
            for (int i = 0; i < articulos.Count; i++)
            {
                var articulo = articulos[i];
                var dto = articulosDto[i];

                dto.Categoria = articulo.Categoria?.Nombre;
                dto.PrecioUnitarioFormateado = CurrencyHelper.FormatCurrency(articulo.PrecioUnitario);
                dto.StockBajo = true;

                // Calcular veces vendido 
                dto.VecesVendido = await _context.FacturaDetalles
                    .Where(fd => fd.Activo && fd.ArticuloId == articulo.Id)
                    .CountAsync();
            }

            return articulosDto;
        }
    }
}
