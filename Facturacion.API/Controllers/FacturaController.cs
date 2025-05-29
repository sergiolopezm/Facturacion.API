using Facturacion.API.Attributes;
using Facturacion.API.Domain.Contracts;
using Facturacion.API.Domain.Contracts.FacturacionRepository;
using Facturacion.API.Shared.GeneralDTO;
using Facturacion.API.Shared.InDTO.FacturacionInDto;
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
    public class FacturaController : ControllerBase
    {
        private readonly IFacturaRepository _facturaRepository;
        private readonly IFacturaDetalleRepository _facturaDetalleRepository;
        private readonly ICalculoFacturacionRepository _calculoRepository;
        private readonly ILogRepository _logRepository;
        private readonly ILoggerFactory _loggerFactory;

        public FacturaController(
            IFacturaRepository facturaRepository,
            IFacturaDetalleRepository facturaDetalleRepository,
            ICalculoFacturacionRepository calculoRepository,
            ILogRepository logRepository,
            ILoggerFactory loggerFactory)
        {
            _facturaRepository = facturaRepository;
            _facturaDetalleRepository = facturaDetalleRepository;
            _calculoRepository = calculoRepository;
            _logRepository = logRepository;
            _loggerFactory = loggerFactory;
        }

        /// <summary>
        /// Obtiene todas las facturas
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ObtenerTodas()
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), "ObtenerTodasFacturas");

            try
            {
                await logger.InfoAsync("Iniciando búsqueda de todas las facturas");

                var facturas = await _facturaRepository.ObtenerTodosAsync();

                await logger.InfoAsync($"Se encontraron {facturas.Count} facturas", logToDb: true);

                return Ok(RespuestaDto.Exitoso(
                    "Facturas obtenidas",
                    $"Se han obtenido {facturas.Count} facturas",
                    facturas));
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    "ObtenerTodasFacturas",
                    ex.Message);

                await logger.ErrorAsync("Error al obtener todas las facturas", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Obtiene una factura por su ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), $"ObtenerFacturaPorId-{id}");

            try
            {
                await logger.InfoAsync($"Buscando factura con ID: {id}");

                var factura = await _facturaRepository.ObtenerPorIdAsync(id);

                if (factura == null)
                {
                    await logger.WarningAsync($"Factura no encontrada con ID: {id}");
                    return NotFound(RespuestaDto.NoEncontrado("Factura"));
                }

                await logger.InfoAsync($"Factura encontrada: {factura.NumeroFactura}", logToDb: true);

                return Ok(RespuestaDto.Exitoso(
                    "Factura obtenida",
                    $"Se ha obtenido la factura número '{factura.NumeroFactura}'",
                    factura));
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    $"ObtenerFacturaPorId: {id}",
                    ex.Message);

                await logger.ErrorAsync($"Error al obtener factura con ID: {id}", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Obtiene una factura por su número
        /// </summary>
        [HttpGet("numero/{numeroFactura}")]
        public async Task<IActionResult> ObtenerPorNumero(string numeroFactura)
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), $"ObtenerFacturaPorNumero-{numeroFactura}");

            try
            {
                await logger.InfoAsync($"Buscando factura con número: {numeroFactura}");

                var factura = await _facturaRepository.ObtenerPorNumeroAsync(numeroFactura);

                if (factura == null)
                {
                    await logger.WarningAsync($"Factura no encontrada con número: {numeroFactura}");
                    return NotFound(RespuestaDto.NoEncontrado("Factura"));
                }

                await logger.InfoAsync($"Factura encontrada: {factura.NumeroFactura}", logToDb: true);

                return Ok(RespuestaDto.Exitoso(
                    "Factura obtenida",
                    $"Se ha obtenido la factura número '{factura.NumeroFactura}'",
                    factura));
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    $"ObtenerFacturaPorNumero: {numeroFactura}",
                    ex.Message);

                await logger.ErrorAsync($"Error al obtener factura con número: {numeroFactura}", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Obtiene una lista paginada de facturas con filtros opcionales
        /// </summary>
        [HttpGet("paginado")]
        public async Task<IActionResult> ObtenerPaginado(
            [FromQuery] int pagina = 1,
            [FromQuery] int elementosPorPagina = 10,
            [FromQuery] string? busqueda = null,
            [FromQuery] DateTime? fechaInicio = null,
            [FromQuery] DateTime? fechaFin = null,
            [FromQuery] string? estado = null,
            [FromQuery] int? clienteId = null)
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), "ObtenerFacturasPaginado");

            try
            {
                await logger.InfoAsync($"Búsqueda paginada de facturas - Página: {pagina}, Elementos: {elementosPorPagina}, Filtros aplicados: {(busqueda != null ? "Sí" : "No")}");

                var facturas = await _facturaRepository.ObtenerPaginadoAsync(
                    pagina, elementosPorPagina, busqueda, fechaInicio, fechaFin, estado, clienteId);

                await logger.InfoAsync($"Resultado paginado - {facturas.Lista?.Count ?? 0} facturas de {facturas.TotalRegistros} total", logToDb: true);

                return Ok(RespuestaDto.Exitoso(
                    "Facturas obtenidas",
                    $"Se han obtenido {facturas.Lista?.Count ?? 0} facturas de un total de {facturas.TotalRegistros}",
                    facturas));
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    "ObtenerFacturasPaginado",
                    ex.Message);

                await logger.ErrorAsync("Error en búsqueda paginada de facturas", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Crea una nueva factura
        /// </summary>
        [HttpPost]
        [ServiceFilter(typeof(ValidarModeloAttribute))]
        public async Task<IActionResult> Crear([FromBody] CrearFacturaDto facturaDto)
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), "CrearFactura");

            try
            {
                await logger.InfoAsync($"Iniciando creación de factura para cliente ID: {facturaDto.ClienteId}");

                // Validar la factura antes de crear
                var validacion = await _facturaRepository.ValidarFacturaAsync(facturaDto);

                if (!validacion.EsValida)
                {
                    string errores = string.Join(", ", validacion.Errores);
                    await logger.WarningAsync($"Validación fallida: {errores}");

                    return BadRequest(RespuestaDto.ParametrosIncorrectos(
                        "Validación fallida",
                        $"La factura tiene errores: {errores}"));
                }

                // Si hay advertencias, registrarlas pero continuar
                if (validacion.Advertencias.Count > 0)
                {
                    string advertencias = string.Join(", ", validacion.Advertencias);
                    await logger.WarningAsync($"Advertencias en la factura: {advertencias}");
                }

                var resultado = await _facturaRepository.CrearAsync(facturaDto, GetUsuarioId());

                if (resultado.Exito)
                {
                    await _logRepository.AccionAsync(
                        GetUsuarioId(),
                        HttpContext.Connection.RemoteIpAddress?.ToString(),
                        "CrearFactura",
                        $"Se ha creado la factura para el cliente ID {facturaDto.ClienteId}");

                    await logger.ActionAsync($"Factura creada exitosamente - Cliente ID: {facturaDto.ClienteId}");

                    // El ID de la factura está en el resultado
                    var facturaCreada = (FacturaDto)resultado.Resultado!;
                    return CreatedAtAction(nameof(ObtenerPorId), new { id = facturaCreada.Id }, resultado);
                }
                else
                {
                    await logger.WarningAsync($"Fallo en creación de factura: {resultado.Detalle}");
                    return BadRequest(resultado);
                }
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    "CrearFactura",
                    ex.Message);

                await logger.ErrorAsync($"Error al crear factura para cliente ID: {facturaDto.ClienteId}", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Anula una factura existente
        /// </summary>
        [HttpPut("{id}/anular")]
        public async Task<IActionResult> Anular(int id, [FromBody] string motivo)
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), $"AnularFactura-{id}");

            try
            {
                await logger.InfoAsync($"Iniciando anulación de factura - ID: {id}, Motivo: {motivo}");

                var existe = await _facturaRepository.ExisteAsync(id);
                if (!existe)
                {
                    await logger.WarningAsync($"Factura no existe - ID: {id}");
                    return NotFound(RespuestaDto.NoEncontrado("Factura"));
                }

                if (string.IsNullOrWhiteSpace(motivo))
                {
                    await logger.WarningAsync("Motivo de anulación no proporcionado");
                    return BadRequest(RespuestaDto.ParametrosIncorrectos(
                        "Anulación fallida",
                        "Debe proporcionar un motivo para anular la factura"));
                }

                var resultado = await _facturaRepository.AnularAsync(id, motivo, GetUsuarioId());

                if (resultado.Exito)
                {
                    await _logRepository.AccionAsync(
                        GetUsuarioId(),
                        HttpContext.Connection.RemoteIpAddress?.ToString(),
                        "AnularFactura",
                        $"Se ha anulado la factura ID {id}. Motivo: {motivo}");

                    await logger.ActionAsync($"Factura anulada exitosamente - ID: {id}");

                    return Ok(resultado);
                }
                else
                {
                    await logger.WarningAsync($"Fallo en anulación de factura: {resultado.Detalle}");
                    return BadRequest(resultado);
                }
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    $"AnularFactura: {id}",
                    ex.Message);

                await logger.ErrorAsync($"Error al anular factura - ID: {id}", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Calcula los totales para una factura según sus detalles
        /// </summary>
        [HttpPost("calcular-totales")]
        public async Task<IActionResult> CalcularTotales([FromBody] List<CrearFacturaDetalleDto> detalles)
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), "CalcularTotalesFactura");

            try
            {
                await logger.InfoAsync($"Calculando totales para {detalles.Count} detalles de factura");

                if (detalles == null || !detalles.Any())
                {
                    await logger.WarningAsync("No se proporcionaron detalles para el cálculo");
                    return BadRequest(RespuestaDto.ParametrosIncorrectos(
                        "Cálculo fallido",
                        "Debe proporcionar al menos un detalle para calcular los totales"));
                }

                var calculo = await _facturaRepository.CalcularTotalesAsync(detalles);

                await logger.InfoAsync($"Totales calculados: Subtotal={calculo.Totales?.Subtotal}, Total={calculo.Totales?.Total}", logToDb: true);

                return Ok(RespuestaDto.Exitoso(
                    "Totales calculados",
                    "Se han calculado los totales de la factura",
                    calculo));
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    "CalcularTotalesFactura",
                    ex.Message);

                await logger.ErrorAsync("Error al calcular totales de factura", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Obtiene las facturas de un cliente específico
        /// </summary>
        [HttpGet("cliente/{clienteId}")]
        public async Task<IActionResult> ObtenerPorCliente(int clienteId)
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), $"ObtenerFacturasPorCliente-{clienteId}");

            try
            {
                await logger.InfoAsync($"Buscando facturas para cliente ID: {clienteId}");

                var facturas = await _facturaRepository.ObtenerPorClienteAsync(clienteId);

                await logger.InfoAsync($"Se encontraron {facturas.Count} facturas para el cliente {clienteId}", logToDb: true);

                return Ok(RespuestaDto.Exitoso(
                    "Facturas obtenidas",
                    $"Se han obtenido {facturas.Count} facturas para el cliente",
                    facturas));
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    $"ObtenerFacturasPorCliente: {clienteId}",
                    ex.Message);

                await logger.ErrorAsync($"Error al obtener facturas para cliente ID: {clienteId}", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Obtiene las facturas por rango de fechas
        /// </summary>
        [HttpGet("fecha")]
        public async Task<IActionResult> ObtenerPorFecha(
            [FromQuery] DateTime fechaInicio,
            [FromQuery] DateTime fechaFin)
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), "ObtenerFacturasPorFecha");

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

                await logger.InfoAsync($"Buscando facturas por rango de fechas: {fechaInicio:yyyy-MM-dd} - {fechaFin:yyyy-MM-dd}");

                var facturas = await _facturaRepository.ObtenerPorFechaAsync(fechaInicio, fechaFin);

                await logger.InfoAsync($"Se encontraron {facturas.Count} facturas en el rango de fechas", logToDb: true);

                return Ok(RespuestaDto.Exitoso(
                    "Facturas obtenidas",
                    $"Se han obtenido {facturas.Count} facturas para el rango de fechas",
                    facturas));
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    $"ObtenerFacturasPorFecha: {fechaInicio:yyyy-MM-dd} - {fechaFin:yyyy-MM-dd}",
                    ex.Message);

                await logger.ErrorAsync($"Error al obtener facturas por rango de fechas", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Obtiene los detalles de una factura específica
        /// </summary>
        [HttpGet("{id}/detalles")]
        public async Task<IActionResult> ObtenerDetalles(int id)
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), $"ObtenerDetallesFactura-{id}");

            try
            {
                await logger.InfoAsync($"Buscando detalles de factura ID: {id}");

                // Verificar que la factura existe
                var existeFactura = await _facturaRepository.ExisteAsync(id);
                if (!existeFactura)
                {
                    await logger.WarningAsync($"Factura no existe - ID: {id}");
                    return NotFound(RespuestaDto.NoEncontrado("Factura"));
                }

                var detalles = await _facturaDetalleRepository.ObtenerPorFacturaAsync(id);

                await logger.InfoAsync($"Se encontraron {detalles.Count} detalles para la factura {id}", logToDb: true);

                return Ok(RespuestaDto.Exitoso(
                    "Detalles obtenidos",
                    $"Se han obtenido {detalles.Count} detalles de la factura",
                    detalles));
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    $"ObtenerDetallesFactura: {id}",
                    ex.Message);

                await logger.ErrorAsync($"Error al obtener detalles de factura ID: {id}", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Genera un reporte de ventas para un período específico
        /// </summary>
        [HttpGet("reporte-ventas")]
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

                var reporte = await _facturaRepository.GenerarReporteVentasAsync(fechaInicio, fechaFin);

                await logger.InfoAsync($"Reporte generado. Total facturas: {reporte.TotalFacturas}, Total ventas: {reporte.TotalVentas}", logToDb: true);

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

        private Guid GetUsuarioId()
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
            return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
        }
    }
}
