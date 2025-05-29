using Facturacion.API.Attributes;
using Facturacion.API.Domain.Contracts;
using Facturacion.API.Shared.GeneralDTO;
using Facturacion.API.Shared.InDTO;
using Microsoft.AspNetCore.Mvc;
using ILoggerFactory = Facturacion.API.Domain.Contracts.ILoggerFactory;

namespace Facturacion.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ServiceFilter(typeof(LogAttribute))]
    [ServiceFilter(typeof(ExceptionAttribute))]
    [ProducesResponseType(typeof(RespuestaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespuestaDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RespuestaDto), StatusCodes.Status500InternalServerError)]
    public class AuthController : ControllerBase
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly ITokenRepository _tokenRepository;
        private readonly ILogRepository _logRepository;
        private readonly IAccesoRepository _accesoRepository;
        private readonly ILoggerFactory _loggerFactory;

        public AuthController(
            IUsuarioRepository usuarioRepository,
            ITokenRepository tokenRepository,
            ILogRepository logRepository,
            IAccesoRepository accesoRepository,
            ILoggerFactory loggerFactory)
        {
            _usuarioRepository = usuarioRepository;
            _tokenRepository = tokenRepository;
            _logRepository = logRepository;
            _accesoRepository = accesoRepository;
            _loggerFactory = loggerFactory;
        }

        /// <summary>
        /// Autentica un usuario en el sistema
        /// </summary>
        [HttpPost("login")]
        [ServiceFilter(typeof(AccesoAttribute))]
        public async Task<IActionResult> Login([FromBody] UsuarioLoginDto loginDto)
        {
            var logger = _loggerFactory.CreateLogger(null, HttpContext.Connection.RemoteIpAddress?.ToString(), "Login");

            // Validar acceso a la API
            string sitio = Request.Headers["Sitio"].FirstOrDefault() ?? string.Empty;
            string clave = Request.Headers["Clave"].FirstOrDefault() ?? string.Empty;

            await logger.InfoAsync($"Intento de login para usuario: {loginDto.NombreUsuario}");

            if (!await _accesoRepository.ValidarAccesoAsync(sitio, clave))
            {
                await _logRepository.ErrorAsync(null, HttpContext.Connection.RemoteIpAddress?.ToString(),
                    "Login - Acceso Inválido", "Credenciales de acceso inválidas");

                await logger.ErrorAsync($"Acceso inválido - Sitio: {sitio}");

                return Unauthorized(RespuestaDto.ParametrosIncorrectos(
                    "Acceso inválido",
                    "Las credenciales de acceso son inválidas"));
            }

            try
            {
                loginDto.Ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                var resultado = await _usuarioRepository.AutenticarUsuarioAsync(loginDto);

                if (resultado.Exito)
                {
                    await _logRepository.AccionAsync(
                        null,
                        loginDto.Ip,
                        "Login",
                        $"Login exitoso para usuario {loginDto.NombreUsuario}");

                    await logger.ActionAsync($"Login exitoso para usuario: {loginDto.NombreUsuario}");

                    return Ok(resultado);
                }
                else
                {
                    await _logRepository.InfoAsync(
                        null,
                        loginDto.Ip,
                        "Login",
                        $"Login fallido para usuario {loginDto.NombreUsuario}: {resultado.Detalle}");

                    await logger.InfoAsync($"Login fallido para usuario: {loginDto.NombreUsuario} - {resultado.Detalle}");

                    return BadRequest(resultado);
                }
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    null,
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    "Login - Error",
                    ex.Message);

                await logger.ErrorAsync($"Error en login para usuario: {loginDto.NombreUsuario}", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno(ex.Message));
            }
        }

        /// <summary>
        /// Registra un nuevo usuario en el sistema
        /// </summary>
        [HttpPost("registro")]
        [ServiceFilter(typeof(AccesoAttribute))]
        [ServiceFilter(typeof(ValidarModeloAttribute))]
        [ServiceFilter(typeof(JwtAuthorizationAttribute))]
        public async Task<IActionResult> Registro([FromBody] UsuarioRegistroDto registroDto)
        {
            var usuarioId = GetUsuarioId();
            var logger = _loggerFactory.CreateLogger(usuarioId.ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), "Registro");

            try
            {
                await logger.InfoAsync($"Iniciando registro para usuario: {registroDto.NombreUsuario}");

                var resultado = await _usuarioRepository.RegistrarUsuarioAsync(registroDto);

                if (resultado.Exito)
                {
                    await _logRepository.AccionAsync(
                        usuarioId,
                        HttpContext.Connection.RemoteIpAddress?.ToString(),
                        "Registro",
                        $"Registro exitoso para usuario {registroDto.NombreUsuario}");

                    await logger.ActionAsync($"Registro exitoso para usuario: {registroDto.NombreUsuario}");

                    return Ok(resultado);
                }
                else
                {
                    await _logRepository.InfoAsync(
                        usuarioId,
                        HttpContext.Connection.RemoteIpAddress?.ToString(),
                        "Registro",
                        $"Registro fallido para usuario {registroDto.NombreUsuario}: {resultado.Detalle}");

                    await logger.WarningAsync($"Registro fallido para usuario: {registroDto.NombreUsuario} - {resultado.Detalle}");

                    return BadRequest(resultado);
                }
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    usuarioId,
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    "Registro",
                    ex.Message);

                await logger.ErrorAsync($"Error en registro para usuario: {registroDto.NombreUsuario}", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno(ex.Message));
            }
        }

        /// <summary>
        /// Obtiene el perfil del usuario actual
        /// </summary>
        [HttpGet("perfil")]
        [JwtAuthorization]
        public async Task<IActionResult> ObtenerPerfil()
        {
            var usuarioId = GetUsuarioId();
            var logger = _loggerFactory.CreateLogger(usuarioId.ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), "ObtenerPerfil");

            try
            {
                await logger.InfoAsync($"Obteniendo perfil para usuario: {usuarioId}");

                var perfil = await _usuarioRepository.ObtenerUsuarioPorIdAsync(usuarioId);

                if (perfil == null)
                {
                    await logger.WarningAsync($"Usuario no encontrado: {usuarioId}");
                    return NotFound(RespuestaDto.NoEncontrado("Usuario"));
                }

                await logger.ActionAsync($"Perfil obtenido exitosamente para usuario: {usuarioId}");

                return Ok(RespuestaDto.Exitoso(
                    "Perfil obtenido",
                    "Perfil de usuario obtenido correctamente",
                    perfil));
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    usuarioId,
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    "ObtenerPerfil",
                    ex.Message);

                await logger.ErrorAsync($"Error obteniendo perfil para usuario: {usuarioId}", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Cierra la sesión del usuario actual
        /// </summary>
        [HttpPost("logout")]
        [JwtAuthorization]
        public async Task<IActionResult> Logout()
        {
            var usuarioId = GetUsuarioId();
            var logger = _loggerFactory.CreateLogger(usuarioId.ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), "Logout");

            try
            {
                var token = Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

                if (string.IsNullOrEmpty(token))
                {
                    await logger.WarningAsync("Token no proporcionado en logout");
                    return BadRequest(RespuestaDto.ParametrosIncorrectos(
                        "Logout fallido",
                        "Token no proporcionado"));
                }

                var resultado = await _tokenRepository.CancelarTokenAsync(token);

                if (resultado)
                {
                    await _logRepository.AccionAsync(
                        usuarioId,
                        HttpContext.Connection.RemoteIpAddress?.ToString(),
                        "Logout",
                        "Logout exitoso");

                    await logger.ActionAsync($"Logout exitoso para usuario: {usuarioId}");

                    return Ok(RespuestaDto.Exitoso(
                        "Logout exitoso",
                        "Sesión cerrada correctamente",
                        null));
                }
                else
                {
                    await logger.WarningAsync($"No se pudo cerrar la sesión para usuario: {usuarioId}");
                    return BadRequest(RespuestaDto.ParametrosIncorrectos(
                        "Logout fallido",
                        "No se pudo cerrar la sesión"));
                }
            }
            catch (Exception ex)
            {
                await _logRepository.ErrorAsync(
                    usuarioId,
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    "Logout",
                    ex.Message);

                await logger.ErrorAsync($"Error en logout para usuario: {usuarioId}", ex);

                return StatusCode(500, RespuestaDto.ErrorInterno());
            }
        }

        /// <summary>
        /// Obtiene el ID del usuario actual desde el token JWT
        /// </summary>
        private Guid GetUsuarioId()
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
            return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
        }
    }
}
