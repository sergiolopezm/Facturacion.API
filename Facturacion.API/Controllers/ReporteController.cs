using Facturacion.API.Attributes;
using Facturacion.API.Domain.Contracts;
using Facturacion.API.Domain.Contracts.FacturacionRepository;
using Facturacion.API.Shared.GeneralDTO;
using Microsoft.AspNetCore.Mvc;
using ILoggerFactory = Facturacion.API.Domain.Contracts.ILoggerFactory;

namespace Facturacion.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [JwtAuthorization]
    [ServiceFilter(typeof(LogAttribute))]
    [ServiceFilter(typeof(ExceptionAttribute))]
    [ProducesResponseType(typeof(RespuestaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespuestaDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RespuestaDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespuestaDto), StatusCodes.Status500InternalServerError)]
    public class ReporteController : ControllerBase
    {
        private readonly IReporteRepository _reporteRepository;
        private readonly ILogRepository _logRepository;
        private readonly ILoggerFactory _loggerFactory;

        public ReporteController(
            IReporteRepository reporteRepository,
            ILogRepository logRepository,
            ILoggerFactory loggerFactory)
        {
            _reporteRepository = reporteRepository;
            _logRepository = logRepository;
            _loggerFactory = loggerFactory;
        }

        /// <summary>
        /// Genera un reporte de ventas para un período específico
        /// </summary>
        [HttpGet("ventas")]
        public async Task<IActionResult> GenerarReporteVentas(
            [FromQuery] DateTime fechaInicio,
            [FromQuery] DateTime fechaFin)
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), "GenerarReporteVentas");

            try
            {
                // Validar rango de fechas
                if (fechaFin < fechaInicio)
                {
                    await logger.WarningAsync($"Rango de fechas inválido: {fechaInicio} - {fechaFin}");
                    return BadRequest(RespuestaDto.ParametrosIncorrectos(
                        "Rango inválido",
                        "La fecha final no puede ser anterior a la fecha inicial"));
                }

                await logger.InfoAsync($"Generando reporte de ventas para período: {fechaInicio:yyyy-MM-dd} - {fechaFin:yyyy-MM-dd}");

                var reporte = await _reporteRepository.GenerarReporteVentasPorPeriodoAsync(fechaInicio, fechaFin);

                await logger.InfoAsync($"Reporte generado. Total facturas: {reporte.TotalFacturas}, Total ventas: {reporte.TotalVentasFormateado}", logToDb: true);

                return Ok(RespuestaDto.Exitoso(
                    "Reporte generado",
                    $"Se ha generado el reporte de ventas para el período {fechaInicio:dd/MM/yyyy} - {fechaFin:dd/MM/yyyy}",
                    reporte));
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    $"GenerarReporteVentas: {fechaInicio:yyyy-MM-dd} - {fechaFin:yyyy-MM-dd}",
                    ex.Message);

                await logger.ErrorAsync($"Error al generar reporte de ventas", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Obtiene los artículos más vendidos en un período
        /// </summary>
        [HttpGet("articulos-mas-vendidos")]
        public async Task<IActionResult> ObtenerArticulosMasVendidos(
            [FromQuery] DateTime fechaInicio,
            [FromQuery] DateTime fechaFin,
            [FromQuery] int top = 10)
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), "ObtenerArticulosMasVendidos");

            try
            {
                // Validar rango de fechas
                if (fechaFin < fechaInicio)
                {
                    await logger.WarningAsync($"Rango de fechas inválido: {fechaInicio} - {fechaFin}");
                    return BadRequest(RespuestaDto.ParametrosIncorrectos(
                        "Rango inválido",
                        "La fecha final no puede ser anterior a la fecha inicial"));
                }

                await logger.InfoAsync($"Obteniendo los {top} artículos más vendidos. Período: {fechaInicio:yyyy-MM-dd} - {fechaFin:yyyy-MM-dd}");

                var articulos = await _reporteRepository.ObtenerArticulosMasVendidosAsync(fechaInicio, fechaFin, top);

                await logger.InfoAsync($"Se encontraron {articulos.Count} artículos más vendidos", logToDb: true);

                return Ok(RespuestaDto.Exitoso(
                    "Artículos más vendidos obtenidos",
                    $"Se han obtenido los {articulos.Count} artículos más vendidos en el período",
                    articulos));
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    $"ObtenerArticulosMasVendidos: {fechaInicio:yyyy-MM-dd} - {fechaFin:yyyy-MM-dd}",
                    ex.Message);

                await logger.ErrorAsync($"Error al obtener artículos más vendidos", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Obtiene los clientes frecuentes en un período
        /// </summary>
        [HttpGet("clientes-frecuentes")]
        public async Task<IActionResult> ObtenerClientesFrecuentes(
            [FromQuery] DateTime fechaInicio,
            [FromQuery] DateTime fechaFin,
            [FromQuery] int top = 10)
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), "ObtenerClientesFrecuentes");

            try
            {
                // Validar rango de fechas
                if (fechaFin < fechaInicio)
                {
                    await logger.WarningAsync($"Rango de fechas inválido: {fechaInicio} - {fechaFin}");
                    return BadRequest(RespuestaDto.ParametrosIncorrectos(
                        "Rango inválido",
                        "La fecha final no puede ser anterior a la fecha inicial"));
                }

                await logger.InfoAsync($"Obteniendo los {top} clientes frecuentes. Período: {fechaInicio:yyyy-MM-dd} - {fechaFin:yyyy-MM-dd}");

                var clientes = await _reporteRepository.ObtenerClientesFrecuentesAsync(fechaInicio, fechaFin, top);

                await logger.InfoAsync($"Se encontraron {clientes.Count} clientes frecuentes", logToDb: true);

                return Ok(RespuestaDto.Exitoso(
                    "Clientes frecuentes obtenidos",
                    $"Se han obtenido los {clientes.Count} clientes frecuentes en el período",
                    clientes));
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    $"ObtenerClientesFrecuentes: {fechaInicio:yyyy-MM-dd} - {fechaFin:yyyy-MM-dd}",
                    ex.Message);

                await logger.ErrorAsync($"Error al obtener clientes frecuentes", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Obtiene las ventas por mes para un año específico
        /// </summary>
        [HttpGet("ventas-por-mes/{año}")]
        public async Task<IActionResult> ObtenerVentasPorMes(int año)
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), $"ObtenerVentasPorMes-{año}");

            try
            {
                // Validar que el año sea válido
                if (año < 2000 || año > 2100)
                {
                    await logger.WarningAsync($"Año inválido: {año}");
                    return BadRequest(RespuestaDto.ParametrosIncorrectos(
                        "Año inválido",
                        "El año debe estar entre 2000 y 2100"));
                }

                await logger.InfoAsync($"Obteniendo ventas por mes para el año: {año}");

                var ventasPorMes = await _reporteRepository.ObtenerVentasPorMesAsync(año);

                await logger.InfoAsync($"Se obtuvieron ventas para {ventasPorMes.Count} meses del año {año}", logToDb: true);

                return Ok(RespuestaDto.Exitoso(
                    "Ventas por mes obtenidas",
                    $"Se han obtenido las ventas por mes para el año {año}",
                    ventasPorMes));
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    $"ObtenerVentasPorMes: {año}",
                    ex.Message);

                await logger.ErrorAsync($"Error al obtener ventas por mes para el año {año}", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Obtiene la cantidad de facturas por estado
        /// </summary>
        [HttpGet("facturas-por-estado")]
        public async Task<IActionResult> ObtenerFacturasPorEstado()
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), "ObtenerFacturasPorEstado");

            try
            {
                await logger.InfoAsync("Obteniendo cantidad de facturas por estado");

                var facturasPorEstado = await _reporteRepository.ObtenerFacturasPorEstadoAsync();

                await logger.InfoAsync($"Se obtuvieron datos para {facturasPorEstado.Count} estados de factura", logToDb: true);

                return Ok(RespuestaDto.Exitoso(
                    "Facturas por estado obtenidas",
                    "Se han obtenido las cantidades de facturas por estado",
                    facturasPorEstado));
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    "ObtenerFacturasPorEstado",
                    ex.Message);

                await logger.ErrorAsync("Error al obtener facturas por estado", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Obtiene las ventas por categoría de artículo en un período
        /// </summary>
        [HttpGet("ventas-por-categoria")]
        public async Task<IActionResult> ObtenerVentasPorCategoria(
            [FromQuery] DateTime fechaInicio,
            [FromQuery] DateTime fechaFin)
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), "ObtenerVentasPorCategoria");

            try
            {
                // Validar rango de fechas
                if (fechaFin < fechaInicio)
                {
                    await logger.WarningAsync($"Rango de fechas inválido: {fechaInicio} - {fechaFin}");
                    return BadRequest(RespuestaDto.ParametrosIncorrectos(
                        "Rango inválido",
                        "La fecha final no puede ser anterior a la fecha inicial"));
                }

                await logger.InfoAsync($"Obteniendo ventas por categoría. Período: {fechaInicio:yyyy-MM-dd} - {fechaFin:yyyy-MM-dd}");

                var ventasPorCategoria = await _reporteRepository.ObtenerVentasPorCategoriaAsync(fechaInicio, fechaFin);

                await logger.InfoAsync($"Se obtuvieron datos para {ventasPorCategoria.Count} categorías", logToDb: true);

                return Ok(RespuestaDto.Exitoso(
                    "Ventas por categoría obtenidas",
                    $"Se han obtenido las ventas por categoría para el período {fechaInicio:dd/MM/yyyy} - {fechaFin:dd/MM/yyyy}",
                    ventasPorCategoria));
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    $"ObtenerVentasPorCategoria: {fechaInicio:yyyy-MM-dd} - {fechaFin:yyyy-MM-dd}",
                    ex.Message);

                await logger.ErrorAsync("Error al obtener ventas por categoría", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Obtiene el total de ventas del día especificado
        /// </summary>
        [HttpGet("ventas-del-dia")]
        public async Task<IActionResult> ObtenerVentasDelDia([FromQuery] DateTime? fecha = null)
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), "ObtenerVentasDelDia");

            try
            {
                // Si no se proporciona fecha, usar la fecha actual
                fecha ??= DateTime.Now.Date;

                await logger.InfoAsync($"Obteniendo total de ventas para la fecha: {fecha:yyyy-MM-dd}");

                var totalVentas = await _reporteRepository.ObtenerTotalVentasDelDiaAsync(fecha.Value);
                var totalFacturas = await _reporteRepository.ObtenerTotalFacturasDelDiaAsync(fecha.Value);

                await logger.InfoAsync($"Total ventas del día {fecha:yyyy-MM-dd}: {totalVentas}, Total facturas: {totalFacturas}", logToDb: true);

                return Ok(RespuestaDto.Exitoso(
                    "Ventas del día obtenidas",
                    $"Se han obtenido las ventas del día {fecha:dd/MM/yyyy}",
                    new
                    {
                        Fecha = fecha,
                        TotalVentas = totalVentas,
                        TotalFacturas = totalFacturas,
                        TotalVentasFormateado = Util.CurrencyHelper.FormatCurrency(totalVentas)
                    }));
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    $"ObtenerVentasDelDia: {fecha:yyyy-MM-dd}",
                    ex.Message);

                await logger.ErrorAsync($"Error al obtener ventas del día {fecha:yyyy-MM-dd}", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Obtiene el promedio de ventas diarias en un período
        /// </summary>
        [HttpGet("promedio-ventas-diarias")]
        public async Task<IActionResult> ObtenerPromedioVentasDiarias(
            [FromQuery] DateTime fechaInicio,
            [FromQuery] DateTime fechaFin)
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), "ObtenerPromedioVentasDiarias");

            try
            {
                // Validar rango de fechas
                if (fechaFin < fechaInicio)
                {
                    await logger.WarningAsync($"Rango de fechas inválido: {fechaInicio} - {fechaFin}");
                    return BadRequest(RespuestaDto.ParametrosIncorrectos(
                        "Rango inválido",
                        "La fecha final no puede ser anterior a la fecha inicial"));
                }

                await logger.InfoAsync($"Calculando promedio de ventas diarias. Período: {fechaInicio:yyyy-MM-dd} - {fechaFin:yyyy-MM-dd}");

                var promedio = await _reporteRepository.ObtenerPromedioVentaDiariaAsync(fechaInicio, fechaFin);

                await logger.InfoAsync($"Promedio de ventas diarias calculado: {promedio}", logToDb: true);

                return Ok(RespuestaDto.Exitoso(
                    "Promedio calculado",
                    $"Se ha calculado el promedio de ventas diarias para el período {fechaInicio:dd/MM/yyyy} - {fechaFin:dd/MM/yyyy}",
                    new
                    {
                        FechaInicio = fechaInicio,
                        FechaFin = fechaFin,
                        Promedio = promedio,
                        PromedioFormateado = Util.CurrencyHelper.FormatCurrency(promedio)
                    }));
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    $"ObtenerPromedioVentasDiarias: {fechaInicio:yyyy-MM-dd} - {fechaFin:yyyy-MM-dd}",
                    ex.Message);

                await logger.ErrorAsync("Error al calcular promedio de ventas diarias", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Obtiene los artículos con stock bajo según umbral configurado
        /// </summary>
        [HttpGet("articulos-stock-bajo")]
        public async Task<IActionResult> ObtenerArticulosConStockBajo()
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), "ObtenerArticulosConStockBajo");

            try
            {
                await logger.InfoAsync("Obteniendo artículos con stock bajo");

                var articulos = await _reporteRepository.ObtenerArticulosConStockBajoAsync();

                await logger.InfoAsync($"Se encontraron {articulos.Count} artículos con stock bajo", logToDb: true);

                return Ok(RespuestaDto.Exitoso(
                    "Artículos con stock bajo obtenidos",
                    $"Se han obtenido {articulos.Count} artículos con stock bajo",
                    articulos));
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    "ObtenerArticulosConStockBajo",
                    ex.Message);

                await logger.ErrorAsync("Error al obtener artículos con stock bajo", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Obtiene un resumen del dashboard con métricas principales
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<IActionResult> ObtenerDashboard()
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), "ObtenerDashboard");

            try
            {
                await logger.InfoAsync("Generando datos para dashboard");

                // Definir fechas para los reportes
                var hoy = DateTime.Now.Date;
                var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
                var finMes = inicioMes.AddMonths(1).AddDays(-1);
                var inicioAño = new DateTime(hoy.Year, 1, 1);
                var finAño = new DateTime(hoy.Year, 12, 31);

                // Obtener datos para el dashboard
                var ventasHoy = await _reporteRepository.ObtenerTotalVentasDelDiaAsync(hoy);
                var facturasHoy = await _reporteRepository.ObtenerTotalFacturasDelDiaAsync(hoy);
                var reporteMes = await _reporteRepository.GenerarReporteVentasPorPeriodoAsync(inicioMes, finMes);
                var ventasPorCategoria = await _reporteRepository.ObtenerVentasPorCategoriaAsync(inicioMes, finMes);
                var articulosMasVendidos = await _reporteRepository.ObtenerArticulosMasVendidosAsync(inicioMes, finMes, 5);
                var clientesFrecuentes = await _reporteRepository.ObtenerClientesFrecuentesAsync(inicioMes, finMes, 5);
                var articulosStockBajo = await _reporteRepository.ObtenerArticulosConStockBajoAsync();
                var facturasPorEstado = await _reporteRepository.ObtenerFacturasPorEstadoAsync();
                var ventasPorMes = await _reporteRepository.ObtenerVentasPorMesAsync(hoy.Year);

                // Crear objeto de respuesta
                var dashboard = new
                {
                    FechaGeneracion = DateTime.Now,
                    ResumenHoy = new
                    {
                        Fecha = hoy,
                        TotalVentas = ventasHoy,
                        TotalVentasFormateado = Util.CurrencyHelper.FormatCurrency(ventasHoy),
                        TotalFacturas = facturasHoy
                    },
                    ResumenMes = new
                    {
                        FechaInicio = inicioMes,
                        FechaFin = finMes,
                        TotalVentas = reporteMes.TotalVentas,
                        TotalVentasFormateado = reporteMes.TotalVentasFormateado,
                        TotalFacturas = reporteMes.TotalFacturas,
                        TotalIVA = reporteMes.TotalIVA,
                        TotalIVAFormateado = reporteMes.TotalIVAFormateado,
                        TotalDescuentos = reporteMes.TotalDescuentos,
                        TotalDescuentosFormateado = reporteMes.TotalDescuentosFormateado
                    },
                    VentasPorCategoria = ventasPorCategoria,
                    ArticulosMasVendidos = articulosMasVendidos,
                    ClientesFrecuentes = clientesFrecuentes,
                    ArticulosStockBajo = articulosStockBajo.Count,
                    FacturasPorEstado = facturasPorEstado,
                    VentasPorMes = ventasPorMes
                };

                await logger.InfoAsync("Dashboard generado exitosamente", logToDb: true);

                return Ok(RespuestaDto.Exitoso(
                    "Dashboard generado",
                    "Se ha generado el dashboard con los indicadores principales",
                    dashboard));
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    "ObtenerDashboard",
                    ex.Message);

                await logger.ErrorAsync("Error al generar dashboard", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        private Guid GetUsuarioId()
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
            return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
        }
    }
}
