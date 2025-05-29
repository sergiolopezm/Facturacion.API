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
    public class CategoriaArticuloController : ControllerBase
    {
        private readonly ICategoriaArticuloRepository _categoriaRepository;
        private readonly IArticuloRepository _articuloRepository;
        private readonly ILogRepository _logRepository;
        private readonly ILoggerFactory _loggerFactory;

        public CategoriaArticuloController(
            ICategoriaArticuloRepository categoriaRepository,
            IArticuloRepository articuloRepository,
            ILogRepository logRepository,
            ILoggerFactory loggerFactory)
        {
            _categoriaRepository = categoriaRepository;
            _articuloRepository = articuloRepository;
            _logRepository = logRepository;
            _loggerFactory = loggerFactory;
        }

        /// <summary>
        /// Obtiene todas las categorías de artículos
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ObtenerTodas()
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), "ObtenerTodasCategorias");

            try
            {
                await logger.InfoAsync("Iniciando búsqueda de todas las categorías de artículos");

                var categorias = await _categoriaRepository.ObtenerTodosAsync();

                await logger.InfoAsync($"Se encontraron {categorias.Count} categorías", logToDb: true);

                return Ok(RespuestaDto.Exitoso(
                    "Categorías obtenidas",
                    $"Se han obtenido {categorias.Count} categorías de artículos",
                    categorias));
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    "ObtenerTodasCategorias",
                    ex.Message);

                await logger.ErrorAsync("Error al obtener todas las categorías", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Obtiene una categoría por su ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), $"ObtenerCategoriaPorId-{id}");

            try
            {
                await logger.InfoAsync($"Buscando categoría con ID: {id}");

                var categoria = await _categoriaRepository.ObtenerPorIdAsync(id);

                if (categoria == null)
                {
                    await logger.WarningAsync($"Categoría no encontrada con ID: {id}");
                    return NotFound(RespuestaDto.NoEncontrado("Categoría"));
                }

                await logger.InfoAsync($"Categoría encontrada: {categoria.Nombre}", logToDb: true);

                return Ok(RespuestaDto.Exitoso(
                    "Categoría obtenida",
                    $"Se ha obtenido la categoría '{categoria.Nombre}'",
                    categoria));
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    $"ObtenerCategoriaPorId: {id}",
                    ex.Message);

                await logger.ErrorAsync($"Error al obtener categoría con ID: {id}", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Obtiene una lista paginada de categorías
        /// </summary>
        [HttpGet("paginado")]
        public async Task<IActionResult> ObtenerPaginado(
            [FromQuery] int pagina = 1,
            [FromQuery] int elementosPorPagina = 10,
            [FromQuery] string? busqueda = null)
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), "ObtenerCategoriasPaginado");

            try
            {
                await logger.InfoAsync($"Búsqueda paginada - Página: {pagina}, Elementos: {elementosPorPagina}, Búsqueda: '{busqueda}'");

                var categorias = await _categoriaRepository.ObtenerPaginadoAsync(
                    pagina, elementosPorPagina, busqueda);

                await logger.InfoAsync($"Resultado paginado - {categorias.Lista?.Count ?? 0} categorías de {categorias.TotalRegistros} total", logToDb: true);

                return Ok(RespuestaDto.Exitoso(
                    "Categorías obtenidas",
                    $"Se han obtenido {categorias.Lista?.Count ?? 0} categorías de un total de {categorias.TotalRegistros}",
                    categorias));
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    "ObtenerCategoriasPaginado",
                    ex.Message);

                await logger.ErrorAsync("Error en búsqueda paginada de categorías", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Crea una nueva categoría de artículos
        /// </summary>
        [HttpPost]
        [ServiceFilter(typeof(ValidarModeloAttribute))]
        public async Task<IActionResult> Crear([FromBody] CategoriaArticuloDto categoriaDto)
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), "CrearCategoria");

            try
            {
                await logger.InfoAsync($"Iniciando creación de categoría - Nombre: {categoriaDto.Nombre}");

                var resultado = await _categoriaRepository.CrearAsync(categoriaDto, GetUsuarioId());

                if (resultado.Exito)
                {
                    await _logRepository.AccionAsync(
                        GetUsuarioId(),
                        HttpContext.Connection.RemoteIpAddress?.ToString(),
                        "CrearCategoria",
                        $"Se ha creado la categoría '{categoriaDto.Nombre}'");

                    await logger.ActionAsync($"Categoría creada exitosamente - Nombre: {categoriaDto.Nombre}");

                    return CreatedAtAction(nameof(ObtenerPorId), new { id = ((CategoriaArticuloDto)resultado.Resultado!).Id }, resultado);
                }
                else
                {
                    await logger.WarningAsync($"Fallo en creación de categoría: {resultado.Detalle}");
                    return BadRequest(resultado);
                }
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    "CrearCategoria",
                    ex.Message);

                await logger.ErrorAsync($"Error al crear categoría - Nombre: {categoriaDto.Nombre}", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Actualiza una categoría existente
        /// </summary>
        [HttpPut("{id}")]
        [ServiceFilter(typeof(ValidarModeloAttribute))]
        public async Task<IActionResult> Actualizar(int id, [FromBody] CategoriaArticuloDto categoriaDto)
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), $"ActualizarCategoria-{id}");

            try
            {
                if (categoriaDto.Id != 0 && categoriaDto.Id != id)
                {
                    await logger.WarningAsync($"Discrepancia de ID - URL: {id}, DTO: {categoriaDto.Id}");
                    return BadRequest(RespuestaDto.ParametrosIncorrectos(
                        "Actualización fallida",
                        "El ID de la categoría no coincide con el ID de la URL"));
                }

                await logger.InfoAsync($"Iniciando actualización de categoría - ID: {id}, Nombre: {categoriaDto.Nombre}");

                var existe = await _categoriaRepository.ExisteAsync(id);
                if (!existe)
                {
                    await logger.WarningAsync($"Categoría no existe - ID: {id}");
                    return NotFound(RespuestaDto.NoEncontrado("Categoría"));
                }

                var resultado = await _categoriaRepository.ActualizarAsync(id, categoriaDto, GetUsuarioId());

                if (resultado.Exito)
                {
                    await _logRepository.AccionAsync(
                        GetUsuarioId(),
                        HttpContext.Connection.RemoteIpAddress?.ToString(),
                        "ActualizarCategoria",
                        $"Se ha actualizado la categoría '{categoriaDto.Nombre}'");

                    await logger.ActionAsync($"Categoría actualizada exitosamente - ID: {id}");

                    return Ok(resultado);
                }
                else
                {
                    await logger.WarningAsync($"Fallo en actualización de categoría: {resultado.Detalle}");
                    return BadRequest(resultado);
                }
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    $"ActualizarCategoria: {id}",
                    ex.Message);

                await logger.ErrorAsync($"Error al actualizar categoría - ID: {id}", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Elimina una categoría
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), $"EliminarCategoria-{id}");

            try
            {
                await logger.InfoAsync($"Iniciando eliminación de categoría - ID: {id}");

                var existe = await _categoriaRepository.ExisteAsync(id);
                if (!existe)
                {
                    await logger.WarningAsync($"Categoría no existe - ID: {id}");
                    return NotFound(RespuestaDto.NoEncontrado("Categoría"));
                }

                // Verificar si la categoría tiene artículos asociados
                var articulosCategoria = await _articuloRepository.ObtenerPorCategoriaAsync(id);
                if (articulosCategoria.Count > 0)
                {
                    await logger.WarningAsync($"Categoría con artículos asociados - ID: {id}, Artículos: {articulosCategoria.Count}");
                    return BadRequest(RespuestaDto.ParametrosIncorrectos(
                        "Eliminación fallida",
                        $"No se puede eliminar la categoría porque tiene {articulosCategoria.Count} artículos asociados"));
                }

                var resultado = await _categoriaRepository.EliminarAsync(id);

                if (resultado.Exito)
                {
                    await _logRepository.AccionAsync(
                        GetUsuarioId(),
                        HttpContext.Connection.RemoteIpAddress?.ToString(),
                        "EliminarCategoria",
                        $"Se ha eliminado la categoría con ID '{id}'");

                    await logger.ActionAsync($"Categoría eliminada exitosamente - ID: {id}");

                    return Ok(resultado);
                }
                else
                {
                    await logger.WarningAsync($"No se pudo eliminar la categoría: {resultado.Detalle}");
                    return BadRequest(resultado);
                }
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    $"EliminarCategoria: {id}",
                    ex.Message);

                await logger.ErrorAsync($"Error al eliminar categoría - ID: {id}", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Obtiene los artículos de una categoría específica
        /// </summary>
        [HttpGet("{id}/articulos")]
        public async Task<IActionResult> ObtenerArticulosCategoria(int id)
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), $"ObtenerArticulosCategoria-{id}");

            try
            {
                await logger.InfoAsync($"Buscando artículos de la categoría - ID: {id}");

                var existe = await _categoriaRepository.ExisteAsync(id);
                if (!existe)
                {
                    await logger.WarningAsync($"Categoría no existe - ID: {id}");
                    return NotFound(RespuestaDto.NoEncontrado("Categoría"));
                }

                var articulos = await _articuloRepository.ObtenerPorCategoriaAsync(id);

                await logger.InfoAsync($"Se encontraron {articulos.Count} artículos en la categoría ID: {id}", logToDb: true);

                return Ok(RespuestaDto.Exitoso(
                    "Artículos obtenidos",
                    $"Se han obtenido {articulos.Count} artículos de la categoría",
                    articulos));
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    $"ObtenerArticulosCategoria: {id}",
                    ex.Message);

                await logger.ErrorAsync($"Error al obtener artículos de la categoría - ID: {id}", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Verifica si existe una categoría con el nombre especificado
        /// </summary>
        [HttpGet("existe-nombre/{nombre}")]
        public async Task<IActionResult> ExistePorNombre(string nombre)
        {
            var logger = _loggerFactory.CreateLogger(GetUsuarioId().ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), $"ExisteCategoriaConNombre-{nombre}");

            try
            {
                await logger.InfoAsync($"Verificando existencia de categoría con nombre: {nombre}");

                var existe = await _categoriaRepository.ExistePorNombreAsync(nombre);

                await logger.InfoAsync($"Resultado de verificación para nombre '{nombre}': {(existe ? "Existe" : "No existe")}", logToDb: true);

                return Ok(RespuestaDto.Exitoso(
                    "Verificación completada",
                    $"La categoría con nombre '{nombre}' {(existe ? "existe" : "no existe")}",
                    new { Existe = existe }));
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    GetUsuarioId(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    $"ExisteCategoriaConNombre: {nombre}",
                    ex.Message);

                await logger.ErrorAsync($"Error al verificar existencia de categoría con nombre: {nombre}", ex);

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
