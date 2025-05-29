using Facturacion.API.Domain.Contracts;
using Facturacion.API.Shared.GeneralDTO;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Facturacion.API.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class JwtAuthorizationAttribute : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Obtener token y usuario desde los headers
            string token = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last() ?? string.Empty;
            string usuarioId = context.HttpContext.Request.Headers["UsuarioId"].FirstOrDefault() ?? string.Empty;
            string ip = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(usuarioId))
            {
                context.Result = new ObjectResult(RespuestaDto.ParametrosIncorrectos(
                    "Autenticación fallida",
                    "No se han proporcionado credenciales de autenticación"))
                {
                    StatusCode = 401
                };
                return;
            }

            // Validar token
            var tokenRepository = context.HttpContext.RequestServices.GetRequiredService<ITokenRepository>();
            var validoDto = await tokenRepository.EsValidoAsync(token, Guid.Parse(usuarioId), ip);

            if (!validoDto.EsValido)
            {
                context.Result = new ObjectResult(RespuestaDto.ParametrosIncorrectos(
                    "Sesión inválida",
                    validoDto.Detalle ?? "La sesión no es válida"))
                {
                    StatusCode = 401
                };
                return;
            }

            // Aumentar tiempo de expiración del token
            await tokenRepository.AumentarTiempoExpiracionAsync(token);

            // Continuar con la ejecución
            await next();
        }
    }
}
