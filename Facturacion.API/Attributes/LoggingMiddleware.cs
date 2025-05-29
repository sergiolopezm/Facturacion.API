using Facturacion.API.Domain.Contracts;
using Facturacion.API.Util.Logging;
using ILoggerFactory = Facturacion.API.Domain.Contracts.ILoggerFactory;

namespace Facturacion.API.Attributes
{
    public class LoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LoggingMiddleware> _logger;
        private readonly ILoggerFactory _loggerFactory;

        public LoggingMiddleware(
            RequestDelegate next,
            ILogger<LoggingMiddleware> logger,
            ILoggerFactory loggerFactory)
        {
            _next = next;
            _logger = logger;
            _loggerFactory = loggerFactory;
        }

        public async Task Invoke(HttpContext context)
        {
            var userId = ObtenerUserId(context);
            var ip = context.Connection.RemoteIpAddress?.ToString();
            var method = context.Request.Method;
            var path = context.Request.Path.Value;
            var actionContext = $"{method} {path}";

            // Crear logger extendido para este contexto
            var extendedLogger = _loggerFactory.CreateLogger(userId, ip, actionContext);

            // Registrar inicio de solicitud
            _logger.LogInformation("Iniciando solicitud HTTP {Method} {Path}", method, path);
            await extendedLogger.InfoAsync($"Iniciando solicitud HTTP {method} {path}");

            // Capturar la respuesta original
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            // Continuar con la solicitud
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                await _next(context);
                stopwatch.Stop();

                // Registrar solicitud exitosa
                _logger.LogInformation("Solicitud completada HTTP {Method} {Path} - Estado: {StatusCode} - Tiempo: {ElapsedMilliseconds}ms",
                    method, path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);

                await extendedLogger.InfoAsync(
                    $"Solicitud completada - Estado: {context.Response.StatusCode} - Tiempo: {stopwatch.ElapsedMilliseconds}ms");

                // Registrar en log de base de datos si es necesario
                await RegistrarLogAsync(context, stopwatch.ElapsedMilliseconds, extendedLogger);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex, "Error en solicitud HTTP {Method} {Path}", method, path);
                await extendedLogger.ErrorAsync($"Error en solicitud HTTP {method} {path}", ex);

                // La excepción se manejará en el middleware de errores
                throw;
            }
            finally
            {
                // Copiar la respuesta modificada al cuerpo original
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }

        private async Task RegistrarLogAsync(HttpContext context, long tiempoTranscurrido, IExtendedLogger extendedLogger)
        {
            // Solo registrar en BD solicitudes importantes (no archivos estáticos, etc.)
            if (context.Request.Path.StartsWithSegments("/api") && context.Response.StatusCode != 401)
            {
                try
                {
                    var userId = ObtenerUserId(context);
                    string? idUsuarioGuid = null;

                    if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var userIdGuid))
                    {
                        idUsuarioGuid = userIdGuid.ToString();
                    }

                    string tipo = context.Response.StatusCode.ToString();
                    string accion = $"{context.Request.Method} {context.Request.Path}";
                    string detalle = $"Tiempo: {tiempoTranscurrido}ms - Estado: {context.Response.StatusCode}";

                    // Registrar tanto en base de datos como en archivo
                    await extendedLogger.ActionAsync($"DB Log: {accion} - {detalle}");

                    // Log directo a base de datos usando el servicio existente
                    if (context.RequestServices.GetService<ILogRepository>() is ILogRepository logRepository)
                    {
                        await logRepository.LogAsync(
                            idUsuarioGuid != null ? Guid.Parse(idUsuarioGuid) : null,
                            context.Connection.RemoteIpAddress?.ToString(),
                            accion,
                            detalle,
                            tipo);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al registrar log de solicitud");
                    await extendedLogger.ErrorAsync("Error al registrar log de solicitud", ex);
                }
            }
        }

        private string? ObtenerUserId(HttpContext context)
        {
            var userClaim = context.User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
            return userClaim?.Value;
        }
    }
}
