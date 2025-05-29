using Facturacion.API.Attributes;
using Facturacion.API.Domain.Contracts;
using Facturacion.API.Domain.Contracts.FacturacionRepository;
using Facturacion.API.Shared.GeneralDTO;
using Facturacion.API.Shared.InDTO.ArticulosInDto;
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
    public class ArticuloController : ControllerBase
    {
        private readonly IArticuloRepository _articuloRepository;
        private readonly ILogRepository _logRepository;
        private readonly ILoggerFactory _loggerFactory;

        public ArticuloController(
            IArticuloRepository articuloRepository,
            ILogRepository logRepository,
            ILoggerFactory loggerFactory)
        {
            _articuloRepository = articuloRepository;
            _logRepository = logRepository;
            _loggerFactory = loggerFactory;
        }

        /// <summary>
        /// Obtiene todos los artículos
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ObtenerTodos()
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), "ObtenerTodosArticulos");

            try
            {
                await logger.InfoAsync("Iniciando búsqueda de todos los artículos");

                var articulos = await _articuloRepository.ObtenerTodosAsync();

                await logger.InfoAsync($"Se encontraron {articulos.Count} artículos", logToDb: true);

                return Ok(RespuestaDto.Exitoso(
                    "Artículos obtenidos",
                    $"Se han obtenido {articulos.Count} artículos",
                    articulos));
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    "ObtenerTodosArticulos",
                    ex.Message);

                await logger.ErrorAsync("Error al obtener todos los artículos", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Obtiene un artículo por su ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), $"ObtenerArticuloPorId-{id}");

            try
            {
                await logger.InfoAsync($"Buscando artículo con ID: {id}");

                var articulo = await _articuloRepository.ObtenerPorIdAsync(id);

                if (articulo == null)
                {
                    await logger.WarningAsync($"Artículo no encontrado con ID: {id}");
                    return NotFound(RespuestaDto.NoEncontrado("Artículo"));
                }

                await logger.InfoAsync($"Artículo encontrado: {articulo.Nombre}", logToDb: true);

                return Ok(RespuestaDto.Exitoso(
                    "Artículo obtenido",
                    $"Se ha obtenido el artículo '{articulo.Nombre}'",
                    articulo));
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    $"ObtenerArticuloPorId: {id}",
                    ex.Message);

                await logger.ErrorAsync($"Error al obtener artículo con ID: {id}", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Obtiene un artículo por su código
        /// </summary>
        [HttpGet("codigo/{codigo}")]
        public async Task<IActionResult> ObtenerPorCodigo(string codigo)
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), $"ObtenerArticuloPorCodigo-{codigo}");

            try
            {
                await logger.InfoAsync($"Buscando artículo con código: {codigo}");

                var articulo = await _articuloRepository.ObtenerPorCodigoAsync(codigo);

                if (articulo == null)
                {
                    await logger.WarningAsync($"Artículo no encontrado con código: {codigo}");
                    return NotFound(RespuestaDto.NoEncontrado("Artículo"));
                }

                await logger.InfoAsync($"Artículo encontrado: {articulo.Nombre}", logToDb: true);

                return Ok(RespuestaDto.Exitoso(
                    "Artículo obtenido",
                    $"Se ha obtenido el artículo '{articulo.Nombre}'",
                    articulo));
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    $"ObtenerArticuloPorCodigo: {codigo}",
                    ex.Message);

                await logger.ErrorAsync($"Error al obtener artículo con código: {codigo}", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Obtiene una lista paginada de artículos
        /// </summary>
        [HttpGet("paginado")]
        public async Task<IActionResult> ObtenerPaginado(
            [FromQuery] int pagina = 1,
            [FromQuery] int elementosPorPagina = 10,
            [FromQuery] string? busqueda = null,
            [FromQuery] int? categoriaId = null)
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), "ObtenerArticulosPaginado");

            try
            {
                await logger.InfoAsync($"Búsqueda paginada - Página: {pagina}, Elementos: {elementosPorPagina}, Búsqueda: '{busqueda}', CategoriaId: {categoriaId}");

                var articulos = await _articuloRepository.ObtenerPaginadoAsync(
                    pagina, elementosPorPagina, busqueda, categoriaId);

                await logger.InfoAsync($"Resultado paginado - {articulos.Lista?.Count ?? 0} artículos de {articulos.TotalRegistros} total", logToDb: true);

                return Ok(RespuestaDto.Exitoso(
                    "Artículos obtenidos",
                    $"Se han obtenido {articulos.Lista?.Count ?? 0} artículos de un total de {articulos.TotalRegistros}",
                    articulos));
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    "ObtenerArticulosPaginado",
                    ex.Message);

                await logger.ErrorAsync("Error en búsqueda paginada de artículos", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Crea un nuevo artículo
        /// </summary>
        [HttpPost]
        [ServiceFilter(typeof(ValidarModeloAttribute))]
        public async Task<IActionResult> Crear([FromBody] ArticuloDto articuloDto)
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), "CrearArticulo");

            try
            {
                await logger.InfoAsync($"Iniciando creación de artículo - Código: {articuloDto.Codigo}");

                var resultado = await _articuloRepository.CrearAsync(articuloDto, GetUsuarioId());

                if (resultado.Exito)
                {
                    await _logRepository.AccionAsync(
                        GetUsuarioId(),
                        HttpContext.Connection.RemoteIpAddress?.ToString(),
                        "CrearArticulo",
                        $"Se ha creado el artículo con código '{articuloDto.Codigo}'");

                    await logger.ActionAsync($"Artículo creado exitosamente - Código: {articuloDto.Codigo}");

                    return CreatedAtAction(nameof(ObtenerPorId), new { id = ((ArticuloDto)resultado.Resultado!).Id }, resultado);
                }
                else
                {
                    await logger.WarningAsync($"Fallo en creación de artículo: {resultado.Detalle}");
                    return BadRequest(resultado);
                }
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    "CrearArticulo",
                    ex.Message);

                await logger.ErrorAsync($"Error al crear artículo - Código: {articuloDto.Codigo}", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Actualiza un artículo existente
        /// </summary>
        [HttpPut("{id}")]
        [ServiceFilter(typeof(ValidarModeloAttribute))]
        public async Task<IActionResult> Actualizar(int id, [FromBody] ArticuloDto articuloDto)
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), $"ActualizarArticulo-{id}");

            try
            {
                if (articuloDto.Id != 0 && articuloDto.Id != id)
                {
                    await logger.WarningAsync($"Discrepancia de ID - URL: {id}, DTO: {articuloDto.Id}");
                    return BadRequest(RespuestaDto.ParametrosIncorrectos(
                        "Actualización fallida",
                        "El ID del artículo no coincide con el ID de la URL"));
                }

                await logger.InfoAsync($"Iniciando actualización de artículo - ID: {id}, Código: {articuloDto.Codigo}");

                var existe = await _articuloRepository.ExisteAsync(id);
                if (!existe)
                {
                    await logger.WarningAsync($"Artículo no existe - ID: {id}");
                    return NotFound(RespuestaDto.NoEncontrado("Artículo"));
                }

                var resultado = await _articuloRepository.ActualizarAsync(id, articuloDto, GetUsuarioId());

                if (resultado.Exito)
                {
                    await _logRepository.AccionAsync(
                        GetUsuarioId(),
                        HttpContext.Connection.RemoteIpAddress?.ToString(),
                        "ActualizarArticulo",
                        $"Se ha actualizado el artículo con código '{articuloDto.Codigo}'");

                    await logger.ActionAsync($"Artículo actualizado exitosamente - ID: {id}");

                    return Ok(resultado);
                }
                else
                {
                    await logger.WarningAsync($"Fallo en actualización de artículo: {resultado.Detalle}");
                    return BadRequest(resultado);
                }
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    $"ActualizarArticulo: {id}",
                    ex.Message);

                await logger.ErrorAsync($"Error al actualizar artículo - ID: {id}", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Actualiza el stock de un artículo
        /// </summary>
        [HttpPatch("{id}/stock")]
        public async Task<IActionResult> ActualizarStock(int id, [FromBody] int nuevoStock)
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), $"ActualizarStockArticulo-{id}");

            try
            {
                await logger.InfoAsync($"Iniciando actualización de stock - ID: {id}, Nuevo stock: {nuevoStock}");

                var existe = await _articuloRepository.ExisteAsync(id);
                if (!existe)
                {
                    await logger.WarningAsync($"Artículo no existe - ID: {id}");
                    return NotFound(RespuestaDto.NoEncontrado("Artículo"));
                }

                if (nuevoStock < 0)
                {
                    await logger.WarningAsync($"Stock inválido - ID: {id}, Stock: {nuevoStock}");
                    return BadRequest(RespuestaDto.ParametrosIncorrectos(
                        "Actualización fallida",
                        "El stock no puede ser negativo"));
                }

                var resultado = await _articuloRepository.ActualizarStockAsync(id, nuevoStock, GetUsuarioId());

                if (resultado.Exito)
                {
                    await _logRepository.AccionAsync(
                        GetUsuarioId(),
                        HttpContext.Connection.RemoteIpAddress?.ToString(),
                        "ActualizarStockArticulo",
                        $"Se ha actualizado el stock del artículo ID '{id}' a {nuevoStock}");

                    await logger.ActionAsync($"Stock actualizado exitosamente - ID: {id}, Nuevo stock: {nuevoStock}");

                    return Ok(resultado);
                }
                else
                {
                    await logger.WarningAsync($"Fallo en actualización de stock: {resultado.Detalle}");
                    return BadRequest(resultado);
                }
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    $"ActualizarStockArticulo: {id}",
                    ex.Message);

                await logger.ErrorAsync($"Error al actualizar stock - ID: {id}", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Elimina un artículo
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), $"EliminarArticulo-{id}");

            try
            {
                await logger.InfoAsync($"Iniciando eliminación de artículo - ID: {id}");

                var existe = await _articuloRepository.ExisteAsync(id);
                if (!existe)
                {
                    await logger.WarningAsync($"Artículo no existe - ID: {id}");
                    return NotFound(RespuestaDto.NoEncontrado("Artículo"));
                }

                var resultado = await _articuloRepository.EliminarAsync(id);

                if (resultado.Exito)
                {
                    await _logRepository.AccionAsync(
                        GetUsuarioId(),
                        HttpContext.Connection.RemoteIpAddress?.ToString(),
                        "EliminarArticulo",
                        $"Se ha eliminado el artículo con ID '{id}'");

                    await logger.ActionAsync($"Artículo eliminado exitosamente - ID: {id}");

                    return Ok(resultado);
                }
                else
                {
                    await logger.WarningAsync($"No se pudo eliminar el artículo: {resultado.Detalle}");
                    return BadRequest(resultado);
                }
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    $"EliminarArticulo: {id}",
                    ex.Message);

                await logger.ErrorAsync($"Error al eliminar artículo - ID: {id}", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Obtiene los artículos con stock bajo
        /// </summary>
        [HttpGet("stock-bajo")]
        public async Task<IActionResult> ObtenerArticulosConStockBajo()
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), "ObtenerArticulosConStockBajo");

            try
            {
                await logger.InfoAsync("Iniciando búsqueda de artículos con stock bajo");

                var articulos = await _articuloRepository.ObtenerPorStockBajoAsync();

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
        /// Obtiene los artículos más vendidos
        /// </summary>
        [HttpGet("mas-vendidos")]
        public async Task<IActionResult> ObtenerArticulosMasVendidos(
            [FromQuery] DateTime? fechaInicio = null,
            [FromQuery] DateTime? fechaFin = null,
            [FromQuery] int top = 10)
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), "ObtenerArticulosMasVendidos");

            try
            {
                await logger.InfoAsync($"Obteniendo los {top} artículos más vendidos. Período: {fechaInicio} - {fechaFin}");

                var articulos = await _articuloRepository.ObtenerMasVendidosAsync(fechaInicio, fechaFin, top);

                await logger.InfoAsync($"Se encontraron {articulos.Count} artículos más vendidos", logToDb: true);

                return Ok(RespuestaDto.Exitoso(
                    "Artículos más vendidos obtenidos",
                    $"Se han obtenido los {articulos.Count} artículos más vendidos",
                    articulos));
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    "ObtenerArticulosMasVendidos",
                    ex.Message);

                await logger.ErrorAsync("Error al obtener artículos más vendidos", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Obtiene los artículos por categoría
        /// </summary>
        [HttpGet("categoria/{categoriaId}")]
        public async Task<IActionResult> ObtenerPorCategoria(int categoriaId)
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), $"ObtenerArticulosPorCategoria-{categoriaId}");

            try
            {
                await logger.InfoAsync($"Buscando artículos de la categoría: {categoriaId}");

                var articulos = await _articuloRepository.ObtenerPorCategoriaAsync(categoriaId);

                await logger.InfoAsync($"Se encontraron {articulos.Count} artículos en la categoría {categoriaId}", logToDb: true);

                return Ok(RespuestaDto.Exitoso(
                    "Artículos por categoría obtenidos",
                    $"Se han obtenido {articulos.Count} artículos en la categoría",
                    articulos));
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    $"ObtenerArticulosPorCategoria: {categoriaId}",
                    ex.Message);

                await logger.ErrorAsync($"Error al obtener artículos por categoría: {categoriaId}", ex);

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
