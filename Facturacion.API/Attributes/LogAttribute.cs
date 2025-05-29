using Facturacion.API.Domain.Contracts;
using Microsoft.AspNetCore.Mvc.Filters;
using ILoggerFactory = Facturacion.API.Domain.Contracts.ILoggerFactory;

namespace Facturacion.API.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class LogAttribute : ActionFilterAttribute
    {
        private readonly ILogRepository _logRepository;
        private readonly ILoggerFactory _loggerFactory;

        public LogAttribute(ILogRepository logRepository, ILoggerFactory loggerFactory)
        {
            _logRepository = logRepository;
            _loggerFactory = loggerFactory;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var userId = ObtenerUserId(context.HttpContext);
            var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString();
            var method = context.HttpContext.Request.Method;
            var path = context.HttpContext.Request.Path.Value;
            var actionContext = $"{method} {path}";

            // Crear logger extendido para este contexto
            var extendedLogger = _loggerFactory.CreateLogger(userId, ip, actionContext);

            await extendedLogger.InfoAsync($"Ejecutando action: {context.ActionDescriptor.DisplayName}");

            // Ejecutar la acción
            var resultContext = await next();

            // Registrar la acción después de ejecutarla
            string idUsuario = context.HttpContext.Request.Headers["IdUsuario"].FirstOrDefault()?.Split(" ").Last();
            string accion = context.HttpContext.Request.Path.Value ?? string.Empty;
            string tipo = resultContext.HttpContext.Response.StatusCode.ToString();
            string detalle = string.Empty;

            if (resultContext.Result is Microsoft.AspNetCore.Mvc.ObjectResult objectResult &&
                objectResult.Value is Facturacion.API.Shared.GeneralDTO.RespuestaDto respuesta)
            {
                detalle = respuesta.Detalle ?? respuesta.Mensaje ?? string.Empty;
            }

            // Log en archivo y base de datos
            await extendedLogger.ActionAsync($"Action completed: {accion} - Status: {tipo} - {detalle}");

            await _logRepository.LogAsync(
                idUsuario != null ? Guid.Parse(idUsuario) : null,
                ip,
                accion,
                detalle,
                tipo);
        }

        private string? ObtenerUserId(HttpContext context)
        {
            var userClaim = context.User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
            return userClaim?.Value;
        }
    }
}
