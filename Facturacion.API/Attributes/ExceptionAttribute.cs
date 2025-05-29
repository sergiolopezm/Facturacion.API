using Facturacion.API.Domain.Contracts;
using Facturacion.API.Shared.GeneralDTO;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using ILoggerFactory = Facturacion.API.Domain.Contracts.ILoggerFactory;

namespace Facturacion.API.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ExceptionAttribute : Attribute, IExceptionFilter
    {
        private readonly ILogRepository _logRepository;
        private readonly ILogger<ExceptionAttribute> _logger;
        private readonly ILoggerFactory _loggerFactory;

        public ExceptionAttribute(
            ILogRepository logRepository,
            ILogger<ExceptionAttribute> logger,
            ILoggerFactory loggerFactory)
        {
            _logRepository = logRepository;
            _logger = logger;
            _loggerFactory = loggerFactory;
        }

        public void OnException(ExceptionContext context)
        {
            var userId = ObtenerUserId(context.HttpContext);
            var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString();
            var method = context.HttpContext.Request.Method;
            var path = context.HttpContext.Request.Path.Value;
            var actionContext = $"{method} {path}";

            // Crear logger extendido para este contexto
            var extendedLogger = _loggerFactory.CreateLogger(userId, ip, actionContext);

            _logger.LogError(context.Exception, "Error no controlado: {Message}", context.Exception.Message);

            // Registrar el error en archivo y base de datos
            var errorMessage = $"Exception in {actionContext}: {context.Exception.Message}";
            var errorTask = Task.Run(async () =>
            {
                await extendedLogger.ErrorAsync(errorMessage, context.Exception);
            });

            // Registrar el error en el log de la aplicación
            try
            {
                string? idUsuario = null;
                var userClaim = context.HttpContext.User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userClaim != null && Guid.TryParse(userClaim.Value, out var userIdGuid))
                {
                    idUsuario = userIdGuid.ToString();
                }

                // Asegurarse de que la tarea de logging asíncrono se ejecute
                errorTask.Wait(TimeSpan.FromSeconds(5));

                _logRepository.ErrorAsync(
                    idUsuario != null ? Guid.Parse(idUsuario) : null,
                    context.HttpContext.Connection.RemoteIpAddress?.ToString(),
                    $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}",
                    context.Exception.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar excepción en log de base de datos");

                // Intentar registrar este error también
                Task.Run(async () =>
                {
                    await extendedLogger.ErrorAsync("Error al registrar excepción en log de base de datos", ex);
                });
            }

            context.Result = new ObjectResult(RespuestaDto.ErrorInterno())
            {
                StatusCode = 500,
            };

            context.ExceptionHandled = true;
        }

        private string? ObtenerUserId(HttpContext context)
        {
            var userClaim = context.User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
            return userClaim?.Value;
        }
    }
}
