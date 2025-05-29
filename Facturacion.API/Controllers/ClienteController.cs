using Facturacion.API.Attributes;
using Facturacion.API.Domain.Contracts;
using Facturacion.API.Domain.Contracts.FacturacionRepository;
using Facturacion.API.Shared.GeneralDTO;
using Facturacion.API.Shared.InDTO.ClienteInDto;
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
    public class ClienteController : ControllerBase
    {
        private readonly IClienteRepository _clienteRepository;
        private readonly ILogRepository _logRepository;
        private readonly ILoggerFactory _loggerFactory;

        public ClienteController(
            IClienteRepository clienteRepository,
            ILogRepository logRepository,
            ILoggerFactory loggerFactory)
        {
            _clienteRepository = clienteRepository;
            _logRepository = logRepository;
            _loggerFactory = loggerFactory;
        }

        /// <summary>
        /// Obtiene todos los clientes
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ObtenerTodos()
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), "ObtenerTodosClientes");

            try
            {
                await logger.InfoAsync("Iniciando búsqueda de todos los clientes");

                var clientes = await _clienteRepository.ObtenerTodosAsync();

                await logger.InfoAsync($"Se encontraron {clientes.Count} clientes", logToDb: true);

                return Ok(RespuestaDto.Exitoso(
                    "Clientes obtenidos",
                    $"Se han obtenido {clientes.Count} clientes",
                    clientes));
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    "ObtenerTodosClientes",
                    ex.Message);

                await logger.ErrorAsync("Error al obtener todos los clientes", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Obtiene un cliente por su ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), $"ObtenerClientePorId-{id}");

            try
            {
                await logger.InfoAsync($"Buscando cliente con ID: {id}");

                var cliente = await _clienteRepository.ObtenerPorIdAsync(id);

                if (cliente == null)
                {
                    await logger.WarningAsync($"Cliente no encontrado con ID: {id}");
                    return NotFound(RespuestaDto.NoEncontrado("Cliente"));
                }

                await logger.InfoAsync($"Cliente encontrado: {cliente.NombreCompleto}", logToDb: true);

                return Ok(RespuestaDto.Exitoso(
                    "Cliente obtenido",
                    $"Se ha obtenido el cliente '{cliente.NombreCompleto}'",
                    cliente));
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    $"ObtenerClientePorId: {id}",
                    ex.Message);

                await logger.ErrorAsync($"Error al obtener cliente con ID: {id}", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Obtiene un cliente por su número de documento
        /// </summary>
        [HttpGet("documento/{numeroDocumento}")]
        public async Task<IActionResult> ObtenerPorDocumento(string numeroDocumento)
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), $"ObtenerClientePorDocumento-{numeroDocumento}");

            try
            {
                await logger.InfoAsync($"Buscando cliente con documento: {numeroDocumento}");

                var cliente = await _clienteRepository.ObtenerPorDocumentoAsync(numeroDocumento);

                if (cliente == null)
                {
                    await logger.WarningAsync($"Cliente no encontrado con documento: {numeroDocumento}");
                    return NotFound(RespuestaDto.NoEncontrado("Cliente"));
                }

                await logger.InfoAsync($"Cliente encontrado: {cliente.NombreCompleto}", logToDb: true);

                return Ok(RespuestaDto.Exitoso(
                    "Cliente obtenido",
                    $"Se ha obtenido el cliente '{cliente.NombreCompleto}'",
                    cliente));
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    $"ObtenerClientePorDocumento: {numeroDocumento}",
                    ex.Message);

                await logger.ErrorAsync($"Error al obtener cliente con documento: {numeroDocumento}", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Obtiene una lista paginada de clientes
        /// </summary>
        [HttpGet("paginado")]
        public async Task<IActionResult> ObtenerPaginado(
            [FromQuery] int pagina = 1,
            [FromQuery] int elementosPorPagina = 10,
            [FromQuery] string? busqueda = null)
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), "ObtenerClientesPaginado");

            try
            {
                await logger.InfoAsync($"Búsqueda paginada - Página: {pagina}, Elementos: {elementosPorPagina}, Búsqueda: '{busqueda}'");

                var clientes = await _clienteRepository.ObtenerPaginadoAsync(
                    pagina, elementosPorPagina, busqueda);

                await logger.InfoAsync($"Resultado paginado - {clientes.Lista?.Count ?? 0} clientes de {clientes.TotalRegistros} total", logToDb: true);

                return Ok(RespuestaDto.Exitoso(
                    "Clientes obtenidos",
                    $"Se han obtenido {clientes.Lista?.Count ?? 0} clientes de un total de {clientes.TotalRegistros}",
                    clientes));
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    "ObtenerClientesPaginado",
                    ex.Message);

                await logger.ErrorAsync("Error en búsqueda paginada de clientes", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Crea un nuevo cliente
        /// </summary>
        [HttpPost]
        [ServiceFilter(typeof(ValidarModeloAttribute))]
        public async Task<IActionResult> Crear([FromBody] ClienteDto clienteDto)
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), "CrearCliente");

            try
            {
                await logger.InfoAsync($"Iniciando creación de cliente - Documento: {clienteDto.NumeroDocumento}");

                var resultado = await _clienteRepository.CrearAsync(clienteDto, GetUsuarioId());

                if (resultado.Exito)
                {
                    await _logRepository.AccionAsync(
                        GetUsuarioId(),
                        HttpContext.Connection.RemoteIpAddress?.ToString(),
                        "CrearCliente",
                        $"Se ha creado el cliente con documento '{clienteDto.NumeroDocumento}'");

                    await logger.ActionAsync($"Cliente creado exitosamente - Documento: {clienteDto.NumeroDocumento}");

                    return CreatedAtAction(nameof(ObtenerPorId), new { id = ((ClienteDto)resultado.Resultado!).Id }, resultado);
                }
                else
                {
                    await logger.WarningAsync($"Fallo en creación de cliente: {resultado.Detalle}");
                    return BadRequest(resultado);
                }
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    "CrearCliente",
                    ex.Message);

                await logger.ErrorAsync($"Error al crear cliente - Documento: {clienteDto.NumeroDocumento}", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Actualiza un cliente existente
        /// </summary>
        [HttpPut("{id}")]
        [ServiceFilter(typeof(ValidarModeloAttribute))]
        public async Task<IActionResult> Actualizar(int id, [FromBody] ClienteDto clienteDto)
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), $"ActualizarCliente-{id}");

            try
            {
                if (clienteDto.Id != 0 && clienteDto.Id != id)
                {
                    await logger.WarningAsync($"Discrepancia de ID - URL: {id}, DTO: {clienteDto.Id}");
                    return BadRequest(RespuestaDto.ParametrosIncorrectos(
                        "Actualización fallida",
                        "El ID del cliente no coincide con el ID de la URL"));
                }

                await logger.InfoAsync($"Iniciando actualización de cliente - ID: {id}, Documento: {clienteDto.NumeroDocumento}");

                var existe = await _clienteRepository.ExisteAsync(id);
                if (!existe)
                {
                    await logger.WarningAsync($"Cliente no existe - ID: {id}");
                    return NotFound(RespuestaDto.NoEncontrado("Cliente"));
                }

                var resultado = await _clienteRepository.ActualizarAsync(id, clienteDto, GetUsuarioId());

                if (resultado.Exito)
                {
                    await _logRepository.AccionAsync(
                        GetUsuarioId(),
                        HttpContext.Connection.RemoteIpAddress?.ToString(),
                        "ActualizarCliente",
                        $"Se ha actualizado el cliente con documento '{clienteDto.NumeroDocumento}'");

                    await logger.ActionAsync($"Cliente actualizado exitosamente - ID: {id}");

                    return Ok(resultado);
                }
                else
                {
                    await logger.WarningAsync($"Fallo en actualización de cliente: {resultado.Detalle}");
                    return BadRequest(resultado);
                }
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    $"ActualizarCliente: {id}",
                    ex.Message);

                await logger.ErrorAsync($"Error al actualizar cliente - ID: {id}", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Elimina un cliente
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), $"EliminarCliente-{id}");

            try
            {
                await logger.InfoAsync($"Iniciando eliminación de cliente - ID: {id}");

                var existe = await _clienteRepository.ExisteAsync(id);
                if (!existe)
                {
                    await logger.WarningAsync($"Cliente no existe - ID: {id}");
                    return NotFound(RespuestaDto.NoEncontrado("Cliente"));
                }

                var resultado = await _clienteRepository.EliminarAsync(id);

                if (resultado.Exito)
                {
                    await _logRepository.AccionAsync(
                        GetUsuarioId(),
                        HttpContext.Connection.RemoteIpAddress?.ToString(),
                        "EliminarCliente",
                        $"Se ha eliminado el cliente con ID '{id}'");

                    await logger.ActionAsync($"Cliente eliminado exitosamente - ID: {id}");

                    return Ok(resultado);
                }
                else
                {
                    await logger.WarningAsync($"No se pudo eliminar el cliente: {resultado.Detalle}");
                    return BadRequest(resultado);
                }
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    $"EliminarCliente: {id}",
                    ex.Message);

                await logger.ErrorAsync($"Error al eliminar cliente - ID: {id}", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Obtiene los clientes frecuentes (los que más compran)
        /// </summary>
        [HttpGet("frecuentes")]
        public async Task<IActionResult> ObtenerClientesFrecuentes([FromQuery] int top = 10)
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), "ObtenerClientesFrecuentes");

            try
            {
                await logger.InfoAsync($"Obteniendo los {top} clientes más frecuentes");

                var clientes = await _clienteRepository.ObtenerClientesFrecuentesAsync(top);

                await logger.InfoAsync($"Se encontraron {clientes.Count} clientes frecuentes", logToDb: true);

                return Ok(RespuestaDto.Exitoso(
                    "Clientes frecuentes obtenidos",
                    $"Se han obtenido {clientes.Count} clientes frecuentes",
                    clientes));
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    "ObtenerClientesFrecuentes",
                    ex.Message);

                await logger.ErrorAsync("Error al obtener clientes frecuentes", ex);

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
