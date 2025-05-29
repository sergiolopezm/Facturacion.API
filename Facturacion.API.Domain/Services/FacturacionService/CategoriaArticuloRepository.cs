using Facturacion.API.Domain.Contracts.FacturacionRepository;
using Facturacion.API.Infrastructure;
using Facturacion.API.Shared.GeneralDTO;
using Facturacion.API.Shared.InDTO.ArticulosInDto;
using Facturacion.API.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Facturacion.API.Domain.Services.FacturacionService
{
    public class CategoriaArticuloRepository : ICategoriaArticuloRepository
    {
        private readonly DBContext _context;
        private readonly ILogger<CategoriaArticuloRepository> _logger;

        public CategoriaArticuloRepository(DBContext context, ILogger<CategoriaArticuloRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<CategoriaArticuloDto>> ObtenerTodosAsync()
        {
            _logger.LogInformation("Obteniendo todas las categorías de artículos");

            var categorias = await _context.CategoriasArticulos
                .Where(c => c.Activo)
                .Include(c => c.CreadoPor)
                .Include(c => c.ModificadoPor)
                .Include(c => c.Articulos.Where(a => a.Activo))
                .AsNoTracking()
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            var categoriasDto = Mapping.ConvertirLista<CategoriasArticulo, CategoriaArticuloDto>(categorias);

            for (int i = 0; i < categorias.Count; i++)
            {
                CompletarDatosCategoriaDto(categorias[i], categoriasDto[i]);
            }

            return categoriasDto;
        }

        public async Task<CategoriaArticuloDto?> ObtenerPorIdAsync(int id)
        {
            _logger.LogInformation("Obteniendo categoría de artículo con ID: {Id}", id);

            var categoria = await _context.CategoriasArticulos
                .Where(c => c.Id == id)
                .Include(c => c.CreadoPor)
                .Include(c => c.ModificadoPor)
                .Include(c => c.Articulos.Where(a => a.Activo))
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (categoria == null)
                return null;

            var categoriaDto = Mapping.Convertir<CategoriasArticulo, CategoriaArticuloDto>(categoria);
            CompletarDatosCategoriaDto(categoria, categoriaDto);

            return categoriaDto;
        }

        public async Task<RespuestaDto> CrearAsync(CategoriaArticuloDto categoriaDto, Guid usuarioId)
        {
            try
            {
                _logger.LogInformation("Creando nueva categoría de artículo: {Nombre}", categoriaDto.Nombre);

                // Validar que no exista otra categoría con el mismo nombre
                if (await ExistePorNombreAsync(categoriaDto.Nombre))
                {
                    return RespuestaDto.ParametrosIncorrectos(
                        "Nombre ya existe",
                        $"El nombre '{categoriaDto.Nombre}' ya está asociado a otra categoría");
                }

                var categoria = new CategoriasArticulo
                {
                    Nombre = categoriaDto.Nombre,
                    Descripcion = categoriaDto.Descripcion,
                    CreadoPorId = usuarioId,
                    Activo = true,
                    FechaCreacion = DateTime.Now
                };

                await _context.CategoriasArticulos.AddAsync(categoria);
                await _context.SaveChangesAsync();

                var nuevaCategoriaDto = await ObtenerPorIdAsync(categoria.Id);

                return RespuestaDto.Exitoso(
                    "Categoría creada",
                    $"La categoría '{categoria.Nombre}' ha sido creada correctamente",
                    nuevaCategoriaDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear categoría de artículo: {Nombre}", categoriaDto.Nombre);
                return RespuestaDto.ErrorInterno(ex.Message);
            }
        }

        public async Task<RespuestaDto> ActualizarAsync(int id, CategoriaArticuloDto categoriaDto, Guid usuarioId)
        {
            try
            {
                _logger.LogInformation("Actualizando categoría de artículo con ID: {Id}", id);

                var categoria = await _context.CategoriasArticulos.FindAsync(id);
                if (categoria == null)
                {
                    return RespuestaDto.NoEncontrado("Categoría");
                }

                // Validar que no exista otra categoría con el mismo nombre (excepto esta misma)
                if (categoria.Nombre != categoriaDto.Nombre && await _context.CategoriasArticulos.AnyAsync(c => c.Nombre == categoriaDto.Nombre && c.Id != id && c.Activo))
                {
                    return RespuestaDto.ParametrosIncorrectos(
                        "Nombre ya existe",
                        $"El nombre '{categoriaDto.Nombre}' ya está asociado a otra categoría");
                }

                // Actualizar propiedades
                categoria.Nombre = categoriaDto.Nombre;
                categoria.Descripcion = categoriaDto.Descripcion;
                categoria.ModificadoPorId = usuarioId;
                categoria.FechaModificacion = DateTime.Now;

                await _context.SaveChangesAsync();

                var categoriaActualizada = await ObtenerPorIdAsync(id);

                return RespuestaDto.Exitoso(
                    "Categoría actualizada",
                    $"La categoría '{categoria.Nombre}' ha sido actualizada correctamente",
                    categoriaActualizada);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar categoría de artículo con ID {Id}", id);
                return RespuestaDto.ErrorInterno(ex.Message);
            }
        }

        public async Task<RespuestaDto> EliminarAsync(int id)
        {
            try
            {
                _logger.LogInformation("Eliminando categoría de artículo con ID: {Id}", id);

                var categoria = await _context.CategoriasArticulos
                    .Include(c => c.Articulos.Where(a => a.Activo))
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (categoria == null)
                {
                    return RespuestaDto.NoEncontrado("Categoría");
                }

                // Verificar si la categoría está en uso en artículos
                if (categoria.Articulos.Any())
                {
                    return RespuestaDto.ParametrosIncorrectos(
                        "No se puede eliminar",
                        "La categoría no puede ser eliminada porque está siendo utilizada en artículos");
                }

                // Marcar como inactiva en lugar de eliminar físicamente
                categoria.Activo = false;
                categoria.FechaModificacion = DateTime.Now;

                await _context.SaveChangesAsync();

                return RespuestaDto.Exitoso(
                    "Categoría eliminada",
                    $"La categoría '{categoria.Nombre}' ha sido eliminada correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar categoría de artículo con ID {Id}", id);
                return RespuestaDto.ErrorInterno(ex.Message);
            }
        }

        public async Task<bool> ExisteAsync(int id)
        {
            return await _context.CategoriasArticulos.AnyAsync(c => c.Id == id && c.Activo);
        }

        public async Task<bool> ExistePorNombreAsync(string nombre)
        {
            return await _context.CategoriasArticulos.AnyAsync(c => c.Nombre == nombre && c.Activo);
        }

        public async Task<PaginacionDto<CategoriaArticuloDto>> ObtenerPaginadoAsync(int pagina, int elementosPorPagina, string? busqueda = null)
        {
            _logger.LogInformation(
                "Obteniendo categorías de artículos paginados. Página: {Pagina}, Elementos: {Elementos}, Búsqueda: {Busqueda}",
                pagina, elementosPorPagina, busqueda);

            IQueryable<CategoriasArticulo> query = _context.CategoriasArticulos
                .Include(c => c.CreadoPor)
                .Include(c => c.ModificadoPor)
                .Include(c => c.Articulos.Where(a => a.Activo))
                .Where(c => c.Activo);

            // Aplicar filtro de búsqueda
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                busqueda = busqueda.ToLower();
                query = query.Where(c =>
                    c.Nombre.ToLower().Contains(busqueda) ||
                    c.Descripcion != null && c.Descripcion.ToLower().Contains(busqueda));
            }

            int totalRegistros = await query.CountAsync();
            int totalPaginas = (int)Math.Ceiling((double)totalRegistros / elementosPorPagina);

            var categorias = await query
                .OrderBy(c => c.Nombre)
                .Skip((pagina - 1) * elementosPorPagina)
                .Take(elementosPorPagina)
                .AsNoTracking()
                .ToListAsync();

            var categoriasDto = Mapping.ConvertirLista<CategoriasArticulo, CategoriaArticuloDto>(categorias);

            for (int i = 0; i < categorias.Count; i++)
            {
                CompletarDatosCategoriaDto(categorias[i], categoriasDto[i]);
            }

            return new PaginacionDto<CategoriaArticuloDto>
            {
                Pagina = pagina,
                ElementosPorPagina = elementosPorPagina,
                TotalPaginas = totalPaginas,
                TotalRegistros = totalRegistros,
                Lista = categoriasDto
            };
        }

        // Método privado para completar datos del DTO
        private void CompletarDatosCategoriaDto(CategoriasArticulo entidad, CategoriaArticuloDto dto)
        {
            dto.CreadoPor = entidad.CreadoPor != null ? $"{entidad.CreadoPor.Nombre} {entidad.CreadoPor.Apellido}" : null;
            dto.ModificadoPor = entidad.ModificadoPor != null ? $"{entidad.ModificadoPor.Nombre} {entidad.ModificadoPor.Apellido}" : null;
            dto.TotalArticulos = entidad.Articulos?.Count(a => a.Activo) ?? 0;
        }
    }
}
