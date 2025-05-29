using Facturacion.API.Domain.Contracts;
using Facturacion.API.Infrastructure;
using Facturacion.API.Util;
using Microsoft.Extensions.Logging;

namespace Facturacion.API.Domain.Services
{
    /// <summary>
    /// Implementación del repositorio de logs para registro de actividades
    /// </summary>
    public class LogRepository : ILogRepository
    {
        private readonly DBContext _context;
        private readonly ILogger<LogRepository> _logger;

        public LogRepository(DBContext context, ILogger<LogRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Registra una acción exitosa (código 200)
        /// </summary>
        public async Task AccionAsync(Guid? usuarioId, string? ip, string? accion, string? detalle)
        {
            await LogAsync(usuarioId, ip, accion, detalle, "200");
        }

        /// <summary>
        /// Registra información (código 400)
        /// </summary>
        public async Task InfoAsync(Guid? usuarioId, string? ip, string? accion, string? detalle)
        {
            await LogAsync(usuarioId, ip, accion, detalle, "400");
        }

        /// <summary>
        /// Registra un error (código 500)
        /// </summary>
        public async Task ErrorAsync(Guid? usuarioId, string? ip, string? accion, string? error)
        {
            await LogAsync(usuarioId, ip, accion, error, "500");
        }

        /// <summary>
        /// Registra un log con un tipo específico
        /// </summary>
        public async Task LogAsync(Guid? usuarioId, string? ip, string? accion, string? detalle, string tipo)
        {
            try
            {
                // Validar IP si se proporciona
                if (!string.IsNullOrEmpty(ip) && !ValidationHelper.EsIpValida(ip))
                {
                    _logger.LogWarning("Intento de registro con IP inválida: {Ip}", ip);
                    ip = "0.0.0.0"; // Usar valor predeterminado si es inválida
                }

                var log = new Log
                {
                    Fecha = DateTime.Now,
                    UsuarioId = usuarioId,
                    Ip = ip,
                    Accion = accion,
                    Tipo = tipo,
                    Detalle = detalle
                };

                // Desactivar tracking para evitar problemas con otras entidades
                _context.ChangeTracker.Clear();
                await _context.Logs.AddAsync(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Si falla el log, registramos en el sistema de logs de aplicación
                _logger.LogError(ex, "Error al guardar log: {DetalleLogs}", detalle);
            }
        }
    }
}
