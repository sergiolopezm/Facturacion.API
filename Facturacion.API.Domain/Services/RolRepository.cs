using Facturacion.API.Domain.Contracts;
using Facturacion.API.Infrastructure;
using Facturacion.API.Shared.InDTO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Facturacion.API.Domain.Services
{
    /// <summary>
    /// Implementación del repositorio de roles
    /// </summary>
    public class RolRepository : IRolRepository
    {
        private readonly DBContext _context;
        private readonly ILogger<RolRepository> _logger;

        public RolRepository(DBContext context, ILogger<RolRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene todos los roles
        /// </summary>
        public async Task<List<RolDto>> ObtenerTodosAsync()
        {
            _logger.LogInformation("Obteniendo todos los roles");

            return await _context.Roles
                .Select(r => new RolDto
                {
                    Id = r.Id,
                    Nombre = r.Nombre,
                    Descripcion = r.Descripcion,
                    Activo = r.Activo
                })
                .OrderBy(r => r.Nombre)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene un rol por su ID
        /// </summary>
        public async Task<RolDto?> ObtenerPorIdAsync(int id)
        {
            _logger.LogInformation("Obteniendo rol con ID: {Id}", id);

            return await _context.Roles
                .Where(r => r.Id == id)
                .Select(r => new RolDto
                {
                    Id = r.Id,
                    Nombre = r.Nombre,
                    Descripcion = r.Descripcion,
                    Activo = r.Activo
                })
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Obtiene un rol por su nombre
        /// </summary>
        public async Task<RolDto?> ObtenerPorNombreAsync(string nombre)
        {
            _logger.LogInformation("Obteniendo rol con nombre: {Nombre}", nombre);

            return await _context.Roles
                .Where(r => r.Nombre.ToLower() == nombre.ToLower())
                .Select(r => new RolDto
                {
                    Id = r.Id,
                    Nombre = r.Nombre,
                    Descripcion = r.Descripcion,
                    Activo = r.Activo
                })
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Verifica si existe un rol con el ID especificado
        /// </summary>
        public async Task<bool> ExisteAsync(int id)
        {
            return await _context.Roles.AnyAsync(r => r.Id == id);
        }

        /// <summary>
        /// Verifica si existe un rol con el nombre especificado
        /// </summary>
        public async Task<bool> ExistePorNombreAsync(string nombre)
        {
            return await _context.Roles.AnyAsync(r => r.Nombre.ToLower() == nombre.ToLower());
        }
    }
}
