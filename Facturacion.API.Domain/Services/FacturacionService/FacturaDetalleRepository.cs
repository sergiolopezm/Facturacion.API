using Facturacion.API.Domain.Contracts.FacturacionRepository;
using Facturacion.API.Infrastructure;
using Facturacion.API.Shared.GeneralDTO;
using Facturacion.API.Shared.InDTO.FacturacionInDto;
using Facturacion.API.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Facturacion.API.Domain.Services.FacturacionService
{
    public class FacturaDetalleRepository : IFacturaDetalleRepository
    {
        private readonly DBContext _context;
        private readonly ILogger<FacturaDetalleRepository> _logger;

        public FacturaDetalleRepository(DBContext context, ILogger<FacturaDetalleRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<FacturaDetalleDto>> ObtenerPorFacturaAsync(int facturaId)
        {
            _logger.LogInformation("Obteniendo detalles de factura con ID: {Id}", facturaId);

            var detalles = await _context.FacturaDetalles
                .Where(d => d.FacturaId == facturaId && d.Activo)
                .Include(d => d.Articulo)
                .Include(d => d.Factura)
                .AsNoTracking()
                .OrderBy(d => d.Id)
                .ToListAsync();

            var detallesDto = Mapping.ConvertirLista<FacturaDetalle, FacturaDetalleDto>(detalles);

            // Completar datos adicionales
            for (int i = 0; i < detalles.Count; i++)
            {
                CompletarDatosDetalleDto(detalles[i], detallesDto[i]);
            }

            return detallesDto;
        }

        public async Task<FacturaDetalleDto?> ObtenerPorIdAsync(int id)
        {
            _logger.LogInformation("Obteniendo detalle de factura con ID: {Id}", id);

            var detalle = await _context.FacturaDetalles
                .Where(d => d.Id == id)
                .Include(d => d.Articulo)
                .Include(d => d.Factura)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (detalle == null)
                return null;

            var detalleDto = Mapping.Convertir<FacturaDetalle, FacturaDetalleDto>(detalle);
            CompletarDatosDetalleDto(detalle, detalleDto);

            return detalleDto;
        }

        public async Task<RespuestaDto> CrearAsync(FacturaDetalleDto detalleDto, Guid usuarioId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _logger.LogInformation("Creando detalle para factura ID: {FacturaId}, Artículo: {ArticuloId}",
                    detalleDto.FacturaId, detalleDto.ArticuloId);

                // Validar que la factura exista
                var factura = await _context.Facturas
                    .FirstOrDefaultAsync(f => f.Id == detalleDto.FacturaId && f.Activo);

                if (factura == null)
                {
                    return RespuestaDto.ParametrosIncorrectos(
                        "Factura no encontrada",
                        "La factura especificada no existe o está inactiva");
                }

                // Validar que no esté anulada
                if (factura.Estado == "Anulada")
                {
                    return RespuestaDto.ParametrosIncorrectos(
                        "Factura anulada",
                        "No se pueden agregar detalles a una factura anulada");
                }

                // Validar que el artículo exista
                var articulo = await _context.Articulos
                    .FirstOrDefaultAsync(a => a.Id == detalleDto.ArticuloId && a.Activo);

                if (articulo == null)
                {
                    return RespuestaDto.ParametrosIncorrectos(
                        "Artículo no encontrado",
                        "El artículo especificado no existe o está inactivo");
                }

                // Validar stock suficiente
                if (articulo.Stock < detalleDto.Cantidad)
                {
                    return RespuestaDto.ParametrosIncorrectos(
                        "Stock insuficiente",
                        $"No hay suficiente stock del artículo '{articulo.Nombre}'. Stock disponible: {articulo.Stock}");
                }

                // Validar que los campos numéricos sean correctos
                if (detalleDto.Cantidad <= 0)
                {
                    return RespuestaDto.ParametrosIncorrectos(
                        "Cantidad inválida",
                        "La cantidad debe ser mayor a 0");
                }

                if (detalleDto.PrecioUnitario <= 0)
                {
                    return RespuestaDto.ParametrosIncorrectos(
                        "Precio inválido",
                        "El precio unitario debe ser mayor a 0");
                }

                // Calcular subtotal
                decimal subtotal = detalleDto.Cantidad * detalleDto.PrecioUnitario;

                // Crear detalle
                var detalle = new FacturaDetalle
                {
                    FacturaId = detalleDto.FacturaId,
                    ArticuloId = detalleDto.ArticuloId,
                    ArticuloCodigo = articulo.Codigo,
                    ArticuloNombre = articulo.Nombre,
                    ArticuloDescripcion = articulo.Descripcion,
                    Cantidad = detalleDto.Cantidad,
                    PrecioUnitario = detalleDto.PrecioUnitario,
                    Subtotal = subtotal,
                    Activo = true,
                    FechaCreacion = DateTime.Now
                };

                await _context.FacturaDetalles.AddAsync(detalle);

                // Actualizar stock
                articulo.Stock -= detalleDto.Cantidad;
                articulo.FechaModificacion = DateTime.Now;

                // Actualizar totales de la factura
                await RecalcularTotalesFacturaAsync(factura.Id);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Obtener el detalle completo
                var detalleCreado = await ObtenerPorIdAsync(detalle.Id);

                return RespuestaDto.Exitoso(
                    "Detalle agregado",
                    $"Se agregó correctamente {detalleDto.Cantidad} unidad(es) del artículo '{articulo.Nombre}' a la factura",
                    detalleCreado);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al crear detalle para factura {FacturaId}", detalleDto.FacturaId);
                return RespuestaDto.ErrorInterno(ex.Message);
            }
        }

        public async Task<RespuestaDto> ActualizarAsync(int id, FacturaDetalleDto detalleDto, Guid usuarioId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _logger.LogInformation("Actualizando detalle de factura con ID: {Id}", id);

                // Validar que el detalle exista
                var detalle = await _context.FacturaDetalles
                    .Include(d => d.Factura)
                    .Include(d => d.Articulo)
                    .FirstOrDefaultAsync(d => d.Id == id && d.Activo);

                if (detalle == null)
                {
                    return RespuestaDto.NoEncontrado("Detalle de factura");
                }

                // Validar que la factura no esté anulada
                if (detalle.Factura.Estado == "Anulada")
                {
                    return RespuestaDto.ParametrosIncorrectos(
                        "Factura anulada",
                        "No se pueden modificar detalles de una factura anulada");
                }

                // Calcular diferencia de stock
                int cantidadAnterior = detalle.Cantidad;
                int diferenciaCantidad = detalleDto.Cantidad - cantidadAnterior;

                // Validar stock suficiente si se aumenta la cantidad
                if (diferenciaCantidad > 0 && detalle.Articulo.Stock < diferenciaCantidad)
                {
                    return RespuestaDto.ParametrosIncorrectos(
                        "Stock insuficiente",
                        $"No hay suficiente stock del artículo '{detalle.Articulo.Nombre}'. Stock disponible: {detalle.Articulo.Stock}");
                }

                // Validar que los campos numéricos sean correctos
                if (detalleDto.Cantidad <= 0)
                {
                    return RespuestaDto.ParametrosIncorrectos(
                        "Cantidad inválida",
                        "La cantidad debe ser mayor a 0");
                }

                if (detalleDto.PrecioUnitario <= 0)
                {
                    return RespuestaDto.ParametrosIncorrectos(
                        "Precio inválido",
                        "El precio unitario debe ser mayor a 0");
                }

                // Actualizar detalle
                detalle.Cantidad = detalleDto.Cantidad;
                detalle.PrecioUnitario = detalleDto.PrecioUnitario;
                detalle.Subtotal = detalleDto.Cantidad * detalleDto.PrecioUnitario;
                detalle.FechaModificacion = DateTime.Now;

                // Actualizar stock del artículo
                detalle.Articulo.Stock -= diferenciaCantidad;
                detalle.Articulo.FechaModificacion = DateTime.Now;

                // Actualizar totales de la factura
                await RecalcularTotalesFacturaAsync(detalle.FacturaId);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Obtener el detalle actualizado
                var detalleActualizado = await ObtenerPorIdAsync(id);

                return RespuestaDto.Exitoso(
                    "Detalle actualizado",
                    $"Se actualizó correctamente el detalle del artículo '{detalle.ArticuloNombre}' en la factura",
                    detalleActualizado);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al actualizar detalle con ID {Id}", id);
                return RespuestaDto.ErrorInterno(ex.Message);
            }
        }

        public async Task<RespuestaDto> EliminarAsync(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _logger.LogInformation("Eliminando detalle de factura con ID: {Id}", id);

                // Validar que el detalle exista
                var detalle = await _context.FacturaDetalles
                    .Include(d => d.Factura)
                    .Include(d => d.Articulo)
                    .FirstOrDefaultAsync(d => d.Id == id && d.Activo);

                if (detalle == null)
                {
                    return RespuestaDto.NoEncontrado("Detalle de factura");
                }

                // Validar que la factura no esté anulada
                if (detalle.Factura.Estado == "Anulada")
                {
                    return RespuestaDto.ParametrosIncorrectos(
                        "Factura anulada",
                        "No se pueden eliminar detalles de una factura anulada");
                }

                // Guardar información para mensaje
                string articuloNombre = detalle.ArticuloNombre;
                int cantidad = detalle.Cantidad;

                // Restaurar stock
                detalle.Articulo.Stock += detalle.Cantidad;
                detalle.Articulo.FechaModificacion = DateTime.Now;

                // Marcar como inactivo (eliminación lógica)
                detalle.Activo = false;
                detalle.FechaModificacion = DateTime.Now;

                // Actualizar totales de la factura
                await RecalcularTotalesFacturaAsync(detalle.FacturaId);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return RespuestaDto.Exitoso(
                    "Detalle eliminado",
                    $"Se eliminó correctamente {cantidad} unidad(es) del artículo '{articuloNombre}' de la factura",
                    null);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al eliminar detalle con ID {Id}", id);
                return RespuestaDto.ErrorInterno(ex.Message);
            }
        }

        public async Task<bool> ExisteAsync(int id)
        {
            return await _context.FacturaDetalles.AnyAsync(d => d.Id == id && d.Activo);
        }

        public async Task<List<FacturaDetalleDto>> ObtenerPorArticuloAsync(int articuloId)
        {
            _logger.LogInformation("Obteniendo detalles por artículo ID: {Id}", articuloId);

            var detalles = await _context.FacturaDetalles
                .Where(d => d.ArticuloId == articuloId && d.Activo)
                .Include(d => d.Articulo)
                .Include(d => d.Factura)
                .AsNoTracking()
                .OrderByDescending(d => d.Factura.Fecha)
                .ToListAsync();

            var detallesDto = Mapping.ConvertirLista<FacturaDetalle, FacturaDetalleDto>(detalles);

            // Completar datos adicionales
            for (int i = 0; i < detalles.Count; i++)
            {
                CompletarDatosDetalleDto(detalles[i], detallesDto[i]);
            }

            return detallesDto;
        }

        public async Task<PaginacionDto<FacturaDetalleDto>> ObtenerPaginadoAsync(
            int pagina,
            int elementosPorPagina,
            int? facturaId = null,
            int? articuloId = null,
            DateTime? fechaInicio = null,
            DateTime? fechaFin = null)
        {
            _logger.LogInformation(
                "Obteniendo detalles paginados. Página: {Pagina}, Elementos: {Elementos}, FacturaId: {FacturaId}, ArticuloId: {ArticuloId}",
                pagina, elementosPorPagina, facturaId, articuloId);

            IQueryable<FacturaDetalle> query = _context.FacturaDetalles
                .Include(d => d.Articulo)
                .Include(d => d.Factura)
                .Where(d => d.Activo);

            // Aplicar filtros
            if (facturaId.HasValue)
            {
                query = query.Where(d => d.FacturaId == facturaId.Value);
            }

            if (articuloId.HasValue)
            {
                query = query.Where(d => d.ArticuloId == articuloId.Value);
            }

            if (fechaInicio.HasValue)
            {
                query = query.Where(d => d.Factura.Fecha >= fechaInicio.Value);
            }

            if (fechaFin.HasValue)
            {
                query = query.Where(d => d.Factura.Fecha <= fechaFin.Value);
            }

            int totalRegistros = await query.CountAsync();
            int totalPaginas = (int)Math.Ceiling((double)totalRegistros / elementosPorPagina);

            var detalles = await query
                .OrderByDescending(d => d.Factura.Fecha)
                .Skip((pagina - 1) * elementosPorPagina)
                .Take(elementosPorPagina)
                .AsNoTracking()
                .ToListAsync();

            var detallesDto = Mapping.ConvertirLista<FacturaDetalle, FacturaDetalleDto>(detalles);

            for (int i = 0; i < detalles.Count; i++)
            {
                CompletarDatosDetalleDto(detalles[i], detallesDto[i]);
            }

            return new PaginacionDto<FacturaDetalleDto>
            {
                Pagina = pagina,
                ElementosPorPagina = elementosPorPagina,
                TotalPaginas = totalPaginas,
                TotalRegistros = totalRegistros,
                Lista = detallesDto
            };
        }

        // Método privado para completar datos del DTO
        private void CompletarDatosDetalleDto(FacturaDetalle entidad, FacturaDetalleDto dto)
        {
            // Datos de navegación para UI
            dto.NumeroFactura = entidad.Factura?.NumeroFactura;
            dto.FechaFactura = entidad.Factura?.Fecha;
            dto.StockActual = entidad.Articulo?.Stock;
            dto.CategoriaArticulo = entidad.Articulo?.Categoria?.Nombre;

            // Datos formateados para mostrar
            dto.PrecioUnitarioFormateado = CurrencyHelper.FormatCurrency(entidad.PrecioUnitario);
            dto.SubtotalFormateado = CurrencyHelper.FormatCurrency(entidad.Subtotal);
        }

        // Método privado para recalcular totales de una factura
        private async Task RecalcularTotalesFacturaAsync(int facturaId)
        {
            var factura = await _context.Facturas.FindAsync(facturaId);
            if (factura == null)
                return;

            // Obtener todos los detalles activos
            var detalles = await _context.FacturaDetalles
                .Where(d => d.FacturaId == facturaId && d.Activo)
                .ToListAsync();

            // Calcular subtotal
            decimal subtotal = detalles.Sum(d => d.Subtotal);

            // Calcular descuento (si aplica)
            decimal porcentajeDescuento = 5m; // Podría venir de configuración
            decimal montoMinimoDescuento = 500000m; // Podría venir de configuración
            decimal valorDescuento = 0m;

            if (subtotal >= montoMinimoDescuento)
            {
                valorDescuento = Math.Round(subtotal * (porcentajeDescuento / 100m), 2);
            }
            else
            {
                porcentajeDescuento = 0m;
            }

            // Calcular base imponible
            decimal baseImpuestos = subtotal - valorDescuento;

            // Calcular IVA
            decimal porcentajeIva = 19m; // Podría venir de configuración
            decimal valorIva = Math.Round(baseImpuestos * (porcentajeIva / 100m), 2);

            // Calcular total
            decimal total = baseImpuestos + valorIva;

            // Actualizar factura
            factura.SubTotal = subtotal;
            factura.PorcentajeDescuento = porcentajeDescuento;
            factura.ValorDescuento = valorDescuento;
            factura.BaseImpuestos = baseImpuestos;
            factura.PorcentajeIva = porcentajeIva;
            factura.ValorIva = valorIva;
            factura.Total = total;
            factura.FechaModificacion = DateTime.Now;
        }
    }
}
