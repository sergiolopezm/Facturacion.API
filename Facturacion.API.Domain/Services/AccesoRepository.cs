using Facturacion.API.Domain.Contracts;
using Facturacion.API.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Facturacion.API.Domain.Services
{
    /// <summary>
    /// Implementación del repositorio de acceso para validación de APIs
    /// </summary>
    public class AccesoRepository : IAccesoRepository
    {
        private readonly DBContext _context;
        private readonly ILogger<AccesoRepository> _logger;

        public AccesoRepository(DBContext context, ILogger<AccesoRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Valida si la combinación de sitio y contraseña es válida
        /// </summary>
        public async Task<bool> ValidarAccesoAsync(string sitio, string contraseña)
        {
            if (string.IsNullOrEmpty(sitio) || string.IsNullOrEmpty(contraseña))
            {
                _logger.LogWarning("Intento de acceso inválido: Sitio o contraseña vacío");
                return false;
            }

            try
            {
                // Nota: En un entorno de producción, la contraseña debería estar hasheada
                return await _context.Accesos
                    .AnyAsync(a => a.Sitio == sitio &&
                                   a.Contraseña == contraseña &&
                                   a.Activo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar acceso para {Sitio}", sitio);
                return false;
            }
        }
    }
}
