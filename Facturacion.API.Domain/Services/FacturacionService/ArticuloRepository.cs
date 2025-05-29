using Facturacion.API.Domain.Contracts.FacturacionRepository;
using Facturacion.API.Infrastructure;
using Facturacion.API.Shared.GeneralDTO;
using Facturacion.API.Shared.InDTO.ArticulosInDto;
using Facturacion.API.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Facturacion.API.Domain.Services.FacturacionService
{
    public class ArticuloRepository : IArticuloRepository
    {
        private readonly DBContext _context;
        private readonly ILogger<ArticuloRepository> _logger;

        public ArticuloRepository(DBContext context, ILogger<ArticuloRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<ArticuloDto>> ObtenerTodosAsync()
        {
            _logger.LogInformation("Obteniendo todos los artículos");

            var articulos = await _context.Articulos
                .Where(a => a.Activo)
                .Include(a => a.Categoria)
                .Include(a => a.CreadoPor)
                .Include(a => a.ModificadoPor)
                .AsNoTracking()
                .OrderBy(a => a.Nombre)
                .ToListAsync();

            var articulosDto = Mapping.ConvertirLista<Articulo, ArticuloDto>(articulos);

            for (int i = 0; i < articulos.Count; i++)
            {
                CompletarDatosArticuloDto(articulos[i], articulosDto[i]);
            }

            return articulosDto;
        }

        public async Task<ArticuloDto?> ObtenerPorIdAsync(int id)
        {
            _logger.LogInformation("Obteniendo artículo con ID: {Id}", id);

            var articulo = await _context.Articulos
                .Where(a => a.Id == id)
                .Include(a => a.Categoria)
                .Include(a => a.CreadoPor)
                .Include(a => a.ModificadoPor)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (articulo == null)
                return null;

            var articuloDto = Mapping.Convertir<Articulo, ArticuloDto>(articulo);
            CompletarDatosArticuloDto(articulo, articuloDto);

            return articuloDto;
        }

        public async Task<ArticuloDto?> ObtenerPorCodigoAsync(string codigo)
        {
            _logger.LogInformation("Obteniendo artículo con código: {Codigo}", codigo);

            var articulo = await _context.Articulos
                .Where(a => a.Codigo == codigo)
                .Include(a => a.Categoria)
                .Include(a => a.CreadoPor)
                .Include(a => a.ModificadoPor)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (articulo == null)
                return null;

            var articuloDto = Mapping.Convertir<Articulo, ArticuloDto>(articulo);
            CompletarDatosArticuloDto(articulo, articuloDto);

            return articuloDto;
        }

        public async Task<RespuestaDto> CrearAsync(ArticuloDto articuloDto, Guid usuarioId)
        {
            try
            {
                _logger.LogInformation("Creando nuevo artículo con código: {Codigo}", articuloDto.Codigo);

                // Validar que no exista otro artículo con el mismo código
                if (await ExistePorCodigoAsync(articuloDto.Codigo))
                {
                    return RespuestaDto.ParametrosIncorrectos(
                        "Código ya existe",
                        $"El código '{articuloDto.Codigo}' ya está asociado a otro artículo");
                }

                // Validar que exista la categoría si se especificó
                if (articuloDto.CategoriaId.HasValue)
                {
                    var categoriaExiste = await _context.CategoriasArticulos
                        .AnyAsync(c => c.Id == articuloDto.CategoriaId.Value && c.Activo);
                    if (!categoriaExiste)
                    {
                        return RespuestaDto.ParametrosIncorrectos(
                            "Categoría no existe",
                            "La categoría especificada no existe o está inactiva");
                    }
                }

                var articulo = new Articulo
                {
                    Codigo = articuloDto.Codigo,
                    Nombre = articuloDto.Nombre,
                    Descripcion = articuloDto.Descripcion,
                    PrecioUnitario = articuloDto.PrecioUnitario,
                    Stock = articuloDto.Stock,
                    StockMinimo = articuloDto.StockMinimo,
                    CategoriaId = articuloDto.CategoriaId,
                    CreadoPorId = usuarioId,
                    Activo = true,
                    FechaCreacion = DateTime.Now
                };

                await _context.Articulos.AddAsync(articulo);
                await _context.SaveChangesAsync();

                var nuevoArticuloDto = await ObtenerPorIdAsync(articulo.Id);

                return RespuestaDto.Exitoso(
                    "Artículo creado",
                    $"El artículo '{articulo.Nombre}' ha sido creado correctamente",
                    nuevoArticuloDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear artículo con código {Codigo}", articuloDto.Codigo);
                return RespuestaDto.ErrorInterno(ex.Message);
            }
        }

        public async Task<RespuestaDto> ActualizarAsync(int id, ArticuloDto articuloDto, Guid usuarioId)
        {
            try
            {
                _logger.LogInformation("Actualizando artículo con ID: {Id}", id);

                var articulo = await _context.Articulos.FindAsync(id);
                if (articulo == null)
                {
                    return RespuestaDto.NoEncontrado("Artículo");
                }

                // Validar que no exista otro artículo con el mismo código (excepto este mismo)
                if (articulo.Codigo != articuloDto.Codigo && await _context.Articulos.AnyAsync(a => a.Codigo == articuloDto.Codigo && a.Id != id))
                {
                    return RespuestaDto.ParametrosIncorrectos(
                        "Código ya existe",
                        $"El código '{articuloDto.Codigo}' ya está asociado a otro artículo");
                }

                // Validar que exista la categoría si se especificó
                if (articuloDto.CategoriaId.HasValue)
                {
                    var categoriaExiste = await _context.CategoriasArticulos
                        .AnyAsync(c => c.Id == articuloDto.CategoriaId.Value && c.Activo);
                    if (!categoriaExiste)
                    {
                        return RespuestaDto.ParametrosIncorrectos(
                            "Categoría no existe",
                            "La categoría especificada no existe o está inactiva");
                    }
                }

                // Actualizar propiedades
                articulo.Codigo = articuloDto.Codigo;
                articulo.Nombre = articuloDto.Nombre;
                articulo.Descripcion = articuloDto.Descripcion;
                articulo.PrecioUnitario = articuloDto.PrecioUnitario;
                articulo.StockMinimo = articuloDto.StockMinimo;
                articulo.CategoriaId = articuloDto.CategoriaId;
                articulo.ModificadoPorId = usuarioId;
                articulo.FechaModificacion = DateTime.Now;

                await _context.SaveChangesAsync();

                var articuloActualizado = await ObtenerPorIdAsync(id);

                return RespuestaDto.Exitoso(
                    "Artículo actualizado",
                    $"El artículo '{articulo.Nombre}' ha sido actualizado correctamente",
                    articuloActualizado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar artículo con ID {Id}", id);
                return RespuestaDto.ErrorInterno(ex.Message);
            }
        }

        public async Task<RespuestaDto> ActualizarStockAsync(int id, int nuevoStock, Guid usuarioId)
        {
            try
            {
                _logger.LogInformation("Actualizando stock del artículo con ID: {Id} a {NuevoStock}", id, nuevoStock);

                var articulo = await _context.Articulos.FindAsync(id);
                if (articulo == null)
                {
                    return RespuestaDto.NoEncontrado("Artículo");
                }

                if (nuevoStock < 0)
                {
                    return RespuestaDto.ParametrosIncorrectos(
                        "Stock inválido",
                        "El stock no puede ser un valor negativo");
                }

                int stockAnterior = articulo.Stock;
                articulo.Stock = nuevoStock;
                articulo.ModificadoPorId = usuarioId;
                articulo.FechaModificacion = DateTime.Now;

                await _context.SaveChangesAsync();

                return RespuestaDto.Exitoso(
                    "Stock actualizado",
                    $"El stock del artículo '{articulo.Nombre}' ha sido actualizado de {stockAnterior} a {nuevoStock}",
                    new { Id = articulo.Id, Codigo = articulo.Codigo, Nombre = articulo.Nombre, StockAnterior = stockAnterior, StockActual = nuevoStock });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar stock del artículo con ID {Id}", id);
                return RespuestaDto.ErrorInterno(ex.Message);
            }
        }

        public async Task<RespuestaDto> EliminarAsync(int id)
        {
            try
            {
                _logger.LogInformation("Eliminando artículo con ID: {Id}", id);

                var articulo = await _context.Articulos
                    .Include(a => a.FacturaDetalles.Where(fd => fd.Activo))
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (articulo == null)
                {
                    return RespuestaDto.NoEncontrado("Artículo");
                }

                // Verificar si el artículo está en uso en facturas
                if (articulo.FacturaDetalles.Any())
                {
                    return RespuestaDto.ParametrosIncorrectos(
                        "No se puede eliminar",
                        "El artículo no puede ser eliminado porque está siendo utilizado en facturas");
                }

                // Marcar como inactivo en lugar de eliminar físicamente
                articulo.Activo = false;
                articulo.FechaModificacion = DateTime.Now;

                await _context.SaveChangesAsync();

                return RespuestaDto.Exitoso(
                    "Artículo eliminado",
                    $"El artículo '{articulo.Nombre}' ha sido eliminado correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar artículo con ID {Id}", id);
                return RespuestaDto.ErrorInterno(ex.Message);
            }
        }

        public async Task<bool> ExisteAsync(int id)
        {
            return await _context.Articulos.AnyAsync(a => a.Id == id && a.Activo);
        }

        public async Task<bool> ExistePorCodigoAsync(string codigo)
        {
            return await _context.Articulos.AnyAsync(a => a.Codigo == codigo && a.Activo);
        }

        public async Task<bool> TieneStockSuficienteAsync(int id, int cantidadRequerida)
        {
            var articulo = await _context.Articulos.FindAsync(id);
            return articulo != null && articulo.Activo && articulo.Stock >= cantidadRequerida;
        }

        public async Task<PaginacionDto<ArticuloDto>> ObtenerPaginadoAsync(int pagina, int elementosPorPagina, string? busqueda = null, int? categoriaId = null)
        {
            _logger.LogInformation(
                "Obteniendo artículos paginados. Página: {Pagina}, Elementos: {Elementos}, Búsqueda: {Busqueda}, CategoriaId: {CategoriaId}",
                pagina, elementosPorPagina, busqueda, categoriaId);

            IQueryable<Articulo> query = _context.Articulos
                .Include(a => a.Categoria)
                .Include(a => a.CreadoPor)
                .Include(a => a.ModificadoPor)
                .Where(a => a.Activo);

            // Aplicar filtros
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                busqueda = busqueda.ToLower();
                query = query.Where(a =>
                    a.Codigo.ToLower().Contains(busqueda) ||
                    a.Nombre.ToLower().Contains(busqueda) ||
                    a.Descripcion != null && a.Descripcion.ToLower().Contains(busqueda));
            }

            if (categoriaId.HasValue)
            {
                query = query.Where(a => a.CategoriaId == categoriaId.Value);
            }

            int totalRegistros = await query.CountAsync();
            int totalPaginas = (int)Math.Ceiling((double)totalRegistros / elementosPorPagina);

            var articulos = await query
                .OrderBy(a => a.Nombre)
                .Skip((pagina - 1) * elementosPorPagina)
                .Take(elementosPorPagina)
                .AsNoTracking()
                .ToListAsync();

            var articulosDto = Mapping.ConvertirLista<Articulo, ArticuloDto>(articulos);

            for (int i = 0; i < articulos.Count; i++)
            {
                CompletarDatosArticuloDto(articulos[i], articulosDto[i]);
            }

            return new PaginacionDto<ArticuloDto>
            {
                Pagina = pagina,
                ElementosPorPagina = elementosPorPagina,
                TotalPaginas = totalPaginas,
                TotalRegistros = totalRegistros,
                Lista = articulosDto
            };
        }

        public async Task<List<ArticuloDto>> ObtenerPorStockBajoAsync()
        {
            _logger.LogInformation("Obteniendo artículos con stock bajo");

            var articulos = await _context.Articulos
                .Where(a => a.Activo && a.Stock <= a.StockMinimo)
                .Include(a => a.Categoria)
                .AsNoTracking()
                .OrderBy(a => a.Stock)
                .ToListAsync();

            var articulosDto = Mapping.ConvertirLista<Articulo, ArticuloDto>(articulos);

            for (int i = 0; i < articulos.Count; i++)
            {
                CompletarDatosArticuloDto(articulos[i], articulosDto[i]);
                articulosDto[i].StockBajo = true;
            }

            return articulosDto;
        }

        public async Task<List<ArticuloVendidoDto>> ObtenerMasVendidosAsync(DateTime? fechaInicio = null, DateTime? fechaFin = null, int top = 10)
        {
            _logger.LogInformation(
                "Obteniendo artículos más vendidos. FechaInicio: {FechaInicio}, FechaFin: {FechaFin}, Top: {Top}",
                fechaInicio, fechaFin, top);

            // Consulta base usando la vista
            IQueryable<VwFacturaDetallesCompleto> query = _context.VwFacturaDetallesCompletos;

            // Aplicar filtros de fecha
            if (fechaInicio.HasValue)
            {
                query = query.Where(fd => fd.FechaFactura >= fechaInicio.Value);
            }

            if (fechaFin.HasValue)
            {
                query = query.Where(fd => fd.FechaFactura <= fechaFin.Value);
            }

            // Agrupar y obtener los más vendidos
            var articulosVendidos = await query
                .GroupBy(fd => new { fd.ArticuloId, fd.ArticuloCodigo, fd.ArticuloNombre })
                .Select(g => new
                {
                    ArticuloId = g.Key.ArticuloId,
                    Codigo = g.Key.ArticuloCodigo,
                    Nombre = g.Key.ArticuloNombre,
                    CantidadVendida = g.Sum(fd => fd.Cantidad),
                    MontoVendido = g.Sum(fd => fd.Subtotal),
                    VecesVendido = g.Count()
                })
                .OrderByDescending(a => a.CantidadVendida)
                .Take(top)
                .ToListAsync();

            // Convertir a DTOs
            var resultado = articulosVendidos.Select(a => new ArticuloVendidoDto
            {
                ArticuloId = a.ArticuloId,
                Codigo = a.Codigo,
                Nombre = a.Nombre,
                CantidadVendida = a.CantidadVendida,
                MontoVendido = a.MontoVendido,
                MontoVendidoFormateado = CurrencyHelper.FormatCurrency(a.MontoVendido),
                VecesVendido = a.VecesVendido
            }).ToList();

            return resultado;
        }

        public async Task<List<ArticuloDto>> ObtenerPorCategoriaAsync(int categoriaId)
        {
            _logger.LogInformation("Obteniendo artículos de la categoría: {CategoriaId}", categoriaId);

            var articulos = await _context.Articulos
                .Where(a => a.CategoriaId == categoriaId && a.Activo)
                .Include(a => a.Categoria)
                .AsNoTracking()
                .OrderBy(a => a.Nombre)
                .ToListAsync();

            var articulosDto = Mapping.ConvertirLista<Articulo, ArticuloDto>(articulos);

            for (int i = 0; i < articulos.Count; i++)
            {
                CompletarDatosArticuloDto(articulos[i], articulosDto[i]);
            }

            return articulosDto;
        }

        // Método privado para completar datos del DTO
        private void CompletarDatosArticuloDto(Articulo entidad, ArticuloDto dto)
        {
            dto.Categoria = entidad.Categoria?.Nombre;
            dto.CreadoPor = entidad.CreadoPor != null ? $"{entidad.CreadoPor.Nombre} {entidad.CreadoPor.Apellido}" : null;
            dto.ModificadoPor = entidad.ModificadoPor != null ? $"{entidad.ModificadoPor.Nombre} {entidad.ModificadoPor.Apellido}" : null;
            dto.PrecioUnitarioFormateado = CurrencyHelper.FormatCurrency(entidad.PrecioUnitario);
            dto.StockBajo = entidad.Stock <= entidad.StockMinimo;
        }
    }
}
