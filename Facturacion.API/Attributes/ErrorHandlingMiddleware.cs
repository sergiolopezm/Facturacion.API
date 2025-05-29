using Facturacion.API.Domain.Contracts;
using Facturacion.API.Shared.GeneralDTO;
using System.Text.Json;
using ILoggerFactory = Facturacion.API.Domain.Contracts.ILoggerFactory;

namespace Facturacion.API.Attributes
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;
        private readonly ILoggerFactory _loggerFactory;

        public ErrorHandlingMiddleware(
            RequestDelegate next,
            ILogger<ErrorHandlingMiddleware> logger,
            ILoggerFactory loggerFactory)
        {
            _next = next;
            _logger = logger;
            _loggerFactory = loggerFactory;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var userId = ObtenerUserId(context);
                var ip = context.Connection.RemoteIpAddress?.ToString();
                var method = context.Request.Method;
                var path = context.Request.Path.Value;
                var actionContext = $"{method} {path}";

                // Crear logger extendido para este contexto
                var extendedLogger = _loggerFactory.CreateLogger(userId, ip, actionContext);

                _logger.LogError(ex, "Error no manejado");
                await extendedLogger.ErrorAsync($"Error no manejado en {actionContext}", ex);

                await HandleExceptionAsync(context, ex);

                // Registrar en log de base de datos si está disponible
                try
                {
                    if (context.RequestServices.GetService<ILogRepository>() is ILogRepository logRepository)
                    {
                        string? idUsuario = null;
                        var userClaim = context.User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
                        if (userClaim != null && Guid.TryParse(userClaim.Value, out var userIdGuid))
                        {
                            idUsuario = userIdGuid.ToString();
                        }

                        await logRepository.ErrorAsync(
                            idUsuario != null ? Guid.Parse(idUsuario) : null,
                            context.Connection.RemoteIpAddress?.ToString(),
                            $"{context.Request.Method} {context.Request.Path}",
                            ex.Message);

                        // También registrar via el logger extendido
                        await extendedLogger.ErrorAsync($"DB Error logged: {ex.Message}");
                    }
                }
                catch (Exception logEx)
                {
                    _logger.LogError(logEx, "Error al registrar en log de base de datos");
                    await extendedLogger.ErrorAsync("Error al registrar en log de base de datos", logEx);
                }
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            var respuesta = RespuestaDto.ErrorInterno();
            var json = JsonSerializer.Serialize(respuesta);
            await context.Response.WriteAsync(json);
        }

        private string? ObtenerUserId(HttpContext context)
        {
            var userClaim = context.User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
            return userClaim?.Value;
        }
    }
}
