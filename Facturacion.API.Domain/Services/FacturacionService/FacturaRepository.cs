using Facturacion.API.Domain.Contracts.FacturacionRepository;
using Facturacion.API.Infrastructure;
using Facturacion.API.Shared.GeneralDTO;
using Facturacion.API.Shared.InDTO.FacturacionInDto;
using Facturacion.API.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Facturacion.API.Domain.Services.FacturacionService
{
    public class FacturaRepository : IFacturaRepository
    {
        private readonly DBContext _context;
        private readonly ILogger<FacturaRepository> _logger;
        private readonly IConfiguration _configuration;
        private readonly ICalculoFacturacionRepository _calculoService;

        // Configuraciones de negocio
        private readonly decimal _porcentajeIVA;
        private readonly decimal _porcentajeDescuento;
        private readonly decimal _montoMinimoDescuento;

        public FacturaRepository(
            DBContext context,
            ILogger<FacturaRepository> logger,
            IConfiguration configuration,
            ICalculoFacturacionRepository calculoService)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _calculoService = calculoService;

            // Cargar configuraciones de negocio
            _porcentajeIVA = _configuration.GetValue<decimal>("BusinessRules:IVA:Porcentaje", 19m);
            _porcentajeDescuento = _configuration.GetValue<decimal>("BusinessRules:Descuento:Porcentaje", 5m);
            _montoMinimoDescuento = _configuration.GetValue<decimal>("BusinessRules:Descuento:MontoMinimo", 500000m);
        }

        public async Task<List<FacturaDto>> ObtenerTodosAsync()
        {
            _logger.LogInformation("Obteniendo todas las facturas");

            var facturas = await _context.Facturas
                .Where(f => f.Activo)
                .Include(f => f.Cliente)
                .Include(f => f.CreadoPor)
                .Include(f => f.ModificadoPor)
                .Include(f => f.FacturaDetalles.Where(fd => fd.Activo))
                .AsNoTracking()
                .OrderByDescending(f => f.FechaCreacion)
                .ToListAsync();

            var facturasDto = Mapping.ConvertirLista<Factura, FacturaDto>(facturas);

            for (int i = 0; i < facturas.Count; i++)
            {
                var entidad = facturas[i];
                var dto = facturasDto[i];

                CompletarDatosFacturaDto(entidad, dto);
            }

            return facturasDto;
        }

        public async Task<FacturaDto?> ObtenerPorIdAsync(int id)
        {
            _logger.LogInformation("Obteniendo factura con ID: {Id}", id);

            var factura = await _context.Facturas
                .Where(f => f.Id == id)
                .Include(f => f.Cliente)
                .Include(f => f.CreadoPor)
                .Include(f => f.ModificadoPor)
                .Include(f => f.FacturaDetalles.Where(fd => fd.Activo))
                .ThenInclude(fd => fd.Articulo)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (factura == null)
                return null;

            var dto = Mapping.Convertir<Factura, FacturaDto>(factura);
            CompletarDatosFacturaDto(factura, dto);

            // Completar detalles
            dto.Detalles = factura.FacturaDetalles
                .Select(fd => {
                    var detalleDto = Mapping.Convertir<FacturaDetalle, FacturaDetalleDto>(fd);
                    detalleDto.NumeroFactura = factura.NumeroFactura;
                    detalleDto.FechaFactura = factura.Fecha;
                    detalleDto.StockActual = fd.Articulo?.Stock;
                    return detalleDto;
                })
                .ToList();

            return dto;
        }

        public async Task<FacturaDto?> ObtenerPorNumeroAsync(string numeroFactura)
        {
            _logger.LogInformation("Obteniendo factura con número: {NumeroFactura}", numeroFactura);

            var factura = await _context.Facturas
                .Where(f => f.NumeroFactura == numeroFactura)
                .Include(f => f.Cliente)
                .Include(f => f.CreadoPor)
                .Include(f => f.ModificadoPor)
                .Include(f => f.FacturaDetalles.Where(fd => fd.Activo))
                .ThenInclude(fd => fd.Articulo)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (factura == null)
                return null;

            var dto = Mapping.Convertir<Factura, FacturaDto>(factura);
            CompletarDatosFacturaDto(factura, dto);

            return dto;
        }

        public async Task<RespuestaDto> CrearAsync(CrearFacturaDto crearFacturaDto, Guid usuarioId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _logger.LogInformation("Creando factura para cliente ID: {ClienteId}", crearFacturaDto.ClienteId);

                // Validar la factura
                var validacion = await ValidarFacturaAsync(crearFacturaDto);
                if (!validacion.EsValida)
                {
                    return RespuestaDto.ParametrosIncorrectos(
                        "Creación fallida",
                        $"La factura tiene errores: {string.Join(", ", validacion.Errores)}");
                }

                // Obtener datos del cliente
                var cliente = await _context.Clientes.FindAsync(crearFacturaDto.ClienteId);
                if (cliente == null)
                {
                    return RespuestaDto.ParametrosIncorrectos(
                        "Creación fallida",
                        "El cliente especificado no existe");
                }

                // Calcular totales
                var calculoTotales = await CalcularTotalesAsync(crearFacturaDto.Detalles);

                // Crear la factura
                var factura = new Factura
                {
                    Fecha = DateTime.Now,
                    ClienteId = crearFacturaDto.ClienteId,
                    ClienteNumeroDocumento = cliente.NumeroDocumento,
                    ClienteNombres = cliente.Nombres,
                    ClienteApellidos = cliente.Apellidos,
                    ClienteDireccion = cliente.Direccion,
                    ClienteTelefono = cliente.Telefono,
                    SubTotal = calculoTotales.Totales!.Subtotal,
                    PorcentajeDescuento = calculoTotales.Totales.PorcentajeDescuento,
                    ValorDescuento = calculoTotales.Totales.ValorDescuento,
                    BaseImpuestos = calculoTotales.Totales.BaseImpuestos,
                    PorcentajeIva = calculoTotales.Totales.PorcentajeIVA,
                    ValorIva = calculoTotales.Totales.ValorIVA,
                    Total = calculoTotales.Totales.Total,
                    Observaciones = crearFacturaDto.Observaciones,
                    Estado = "Activa",
                    CreadoPorId = usuarioId,
                    Activo = true,
                    FechaCreacion = DateTime.Now
                };

                await _context.Facturas.AddAsync(factura);
                await _context.SaveChangesAsync();

                // Crear los detalles
                foreach (var detalleDto in crearFacturaDto.Detalles)
                {
                    var articulo = await _context.Articulos.FindAsync(detalleDto.ArticuloId);
                    if (articulo == null)
                        continue;

                    var detalle = new FacturaDetalle
                    {
                        FacturaId = factura.Id,
                        ArticuloId = detalleDto.ArticuloId,
                        ArticuloCodigo = articulo.Codigo,
                        ArticuloNombre = articulo.Nombre,
                        ArticuloDescripcion = articulo.Descripcion,
                        Cantidad = detalleDto.Cantidad,
                        PrecioUnitario = detalleDto.PrecioUnitario,
                        Subtotal = detalleDto.Cantidad * detalleDto.PrecioUnitario,
                        Activo = true,
                        FechaCreacion = DateTime.Now
                    };

                    await _context.FacturaDetalles.AddAsync(detalle);

                    // Actualizar stock del artículo
                    articulo.Stock -= detalleDto.Cantidad;
                    articulo.FechaModificacion = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Cargar la factura completa para retornar
                var facturaCompleta = await ObtenerPorIdAsync(factura.Id);

                return RespuestaDto.Exitoso(
                    "Factura creada",
                    $"La factura '{facturaCompleta?.NumeroFactura}' ha sido creada correctamente por un valor de {CurrencyHelper.FormatCurrency(facturaCompleta?.Total ?? 0)}",
                    facturaCompleta);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al crear factura para cliente {ClienteId}", crearFacturaDto.ClienteId);
                return RespuestaDto.ErrorInterno(ex.Message);
            }
        }

        public async Task<RespuestaDto> AnularAsync(int id, string motivo, Guid usuarioId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _logger.LogInformation("Anulando factura ID: {Id}", id);

                var factura = await _context.Facturas
                    .Include(f => f.FacturaDetalles.Where(fd => fd.Activo))
                    .FirstOrDefaultAsync(f => f.Id == id);

                if (factura == null)
                {
                    return RespuestaDto.NoEncontrado("Factura");
                }

                if (factura.Estado == "Anulada")
                {
                    return RespuestaDto.ParametrosIncorrectos(
                        "Anulación fallida",
                        "La factura ya se encuentra anulada");
                }

                // Anular la factura
                factura.Estado = "Anulada";
                factura.Observaciones = $"{factura.Observaciones}\n[ANULADA] {DateTime.Now:dd/MM/yyyy HH:mm} - {motivo}";
                factura.ModificadoPorId = usuarioId;
                factura.FechaModificacion = DateTime.Now;

                // Devolver stock de los artículos
                foreach (var detalle in factura.FacturaDetalles)
                {
                    var articulo = await _context.Articulos.FindAsync(detalle.ArticuloId);
                    if (articulo != null)
                    {
                        articulo.Stock += detalle.Cantidad;
                        articulo.FechaModificacion = DateTime.Now;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return RespuestaDto.Exitoso(
                    "Factura anulada",
                    $"La factura '{factura.NumeroFactura}' ha sido anulada correctamente",
                    null);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al anular factura {Id}", id);
                return RespuestaDto.ErrorInterno(ex.Message);
            }
        }

        public async Task<ValidacionFacturaDto> ValidarFacturaAsync(CrearFacturaDto crearFacturaDto)
        {
            var validacion = new ValidacionFacturaDto { EsValida = true };

            try
            {
                // Validar cliente
                var cliente = await _context.Clientes.FindAsync(crearFacturaDto.ClienteId);
                if (cliente == null || !cliente.Activo)
                {
                    validacion.Errores.Add("El cliente especificado no existe o está inactivo");
                    validacion.EsValida = false;
                }

                // Validar que hay detalles
                if (crearFacturaDto.Detalles == null || !crearFacturaDto.Detalles.Any())
                {
                    validacion.Errores.Add("Debe incluir al menos un artículo en la factura");
                    validacion.EsValida = false;
                    return validacion;
                }

                // Validar cada detalle
                foreach (var detalle in crearFacturaDto.Detalles)
                {
                    var articulo = await _context.Articulos.FindAsync(detalle.ArticuloId);

                    if (articulo == null || !articulo.Activo)
                    {
                        validacion.Errores.Add($"El artículo con ID {detalle.ArticuloId} no existe o está inactivo");
                        validacion.EsValida = false;
                        continue;
                    }

                    if (detalle.Cantidad <= 0)
                    {
                        validacion.Errores.Add($"La cantidad del artículo '{articulo.Nombre}' debe ser mayor a 0");
                        validacion.EsValida = false;
                    }

                    if (detalle.PrecioUnitario <= 0)
                    {
                        validacion.Errores.Add($"El precio del artículo '{articulo.Nombre}' debe ser mayor a 0");
                        validacion.EsValida = false;
                    }

                    if (detalle.Cantidad > articulo.Stock)
                    {
                        validacion.Errores.Add($"No hay suficiente stock del artículo '{articulo.Nombre}'. Stock disponible: {articulo.Stock}");
                        validacion.EsValida = false;
                    }

                    // Verificar si el precio está muy alejado del precio actual del artículo
                    var diferenciaPrecio = Math.Abs(detalle.PrecioUnitario - articulo.PrecioUnitario) / articulo.PrecioUnitario * 100;
                    if (diferenciaPrecio > 20) // 20% de diferencia
                    {
                        validacion.Advertencias.Add($"El precio del artículo '{articulo.Nombre}' difiere en {diferenciaPrecio:F1}% del precio actual ({CurrencyHelper.FormatCurrency(articulo.PrecioUnitario)})");
                    }
                }

                // Calcular totales si es válida
                if (validacion.EsValida)
                {
                    var totales = _calculoService.CalcularTotales(
                        _calculoService.CalcularSubtotal(crearFacturaDto.Detalles),
                        _porcentajeDescuento,
                        _montoMinimoDescuento,
                        _porcentajeIVA);

                    validacion.TotalesCalculados = totales;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar factura");
                validacion.Errores.Add($"Error interno en la validación: {ex.Message}");
                validacion.EsValida = false;
            }

            return validacion;
        }

        public async Task<FacturaCalculoDto> CalcularTotalesAsync(List<CrearFacturaDetalleDto> detalles)
        {
            var subtotal = _calculoService.CalcularSubtotal(detalles);
            var totales = _calculoService.CalcularTotales(subtotal, _porcentajeDescuento, _montoMinimoDescuento, _porcentajeIVA);

            return new FacturaCalculoDto
            {
                Detalles = detalles,
                Totales = totales,
                TotalesFormateados = totales.FormatearParaMostrar()
            };
        }

        public async Task<PaginacionDto<FacturaResumenDto>> ObtenerPaginadoAsync(
            int pagina,
            int elementosPorPagina,
            string? busqueda = null,
            DateTime? fechaInicio = null,
            DateTime? fechaFin = null,
            string? estado = null,
            int? clienteId = null)
        {
            _logger.LogInformation(
                "Obteniendo facturas paginadas. Página: {Pagina}, Elementos: {Elementos}, Búsqueda: {Busqueda}",
                pagina, elementosPorPagina, busqueda);

            IQueryable<Factura> query = _context.Facturas
                .Include(f => f.Cliente)
                .Include(f => f.CreadoPor)
                .Include(f => f.FacturaDetalles.Where(fd => fd.Activo))
                .Where(f => f.Activo);

            // Aplicar filtros
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                busqueda = busqueda.ToLower();
                query = query.Where(f =>
                    f.NumeroFactura!.ToLower().Contains(busqueda) ||
                    f.ClienteNombres.ToLower().Contains(busqueda) ||
                    f.ClienteApellidos.ToLower().Contains(busqueda) ||
                    f.ClienteNumeroDocumento.ToLower().Contains(busqueda));
            }

            if (fechaInicio.HasValue)
            {
                query = query.Where(f => f.Fecha >= fechaInicio.Value);
            }

            if (fechaFin.HasValue)
            {
                query = query.Where(f => f.Fecha <= fechaFin.Value);
            }

            if (!string.IsNullOrWhiteSpace(estado))
            {
                query = query.Where(f => f.Estado == estado);
            }

            if (clienteId.HasValue)
            {
                query = query.Where(f => f.ClienteId == clienteId.Value);
            }

            int totalRegistros = await query.CountAsync();
            int totalPaginas = (int)Math.Ceiling((double)totalRegistros / elementosPorPagina);

            var facturas = await query
                .OrderByDescending(f => f.FechaCreacion)
                .Skip((pagina - 1) * elementosPorPagina)
                .Take(elementosPorPagina)
                .AsNoTracking()
                .ToListAsync();

            var facturasDto = facturas.Select(f => new FacturaResumenDto
            {
                Id = f.Id,
                NumeroFactura = f.NumeroFactura,
                Fecha = f.Fecha,
                ClienteNombreCompleto = $"{f.ClienteNombres} {f.ClienteApellidos}",
                ClienteNumeroDocumento = f.ClienteNumeroDocumento,
                Total = f.Total,
                TotalFormateado = CurrencyHelper.FormatCurrency(f.Total),
                Estado = f.Estado,
                TotalArticulos = f.FacturaDetalles?.Count(fd => fd.Activo) ?? 0,
                CreadoPor = f.CreadoPor != null ? $"{f.CreadoPor.Nombre} {f.CreadoPor.Apellido}" : null,
                FechaFormateada = f.Fecha.ToString("dd/MM/yyyy HH:mm")
            }).ToList();

            return new PaginacionDto<FacturaResumenDto>
            {
                Pagina = pagina,
                ElementosPorPagina = elementosPorPagina,
                TotalPaginas = totalPaginas,
                TotalRegistros = totalRegistros,
                Lista = facturasDto
            };
        }

        public async Task<List<FacturaDto>> ObtenerPorClienteAsync(int clienteId)
        {
            var facturas = await _context.Facturas
                .Where(f => f.ClienteId == clienteId && f.Activo)
                .Include(f => f.Cliente)
                .Include(f => f.CreadoPor)
                .Include(f => f.ModificadoPor)
                .Include(f => f.FacturaDetalles.Where(fd => fd.Activo))
                .AsNoTracking()
                .OrderByDescending(f => f.FechaCreacion)
                .ToListAsync();

            var facturasDto = Mapping.ConvertirLista<Factura, FacturaDto>(facturas);

            for (int i = 0; i < facturas.Count; i++)
            {
                CompletarDatosFacturaDto(facturas[i], facturasDto[i]);
            }

            return facturasDto;
        }

        public async Task<List<FacturaDto>> ObtenerPorFechaAsync(DateTime fechaInicio, DateTime fechaFin)
        {
            var facturas = await _context.Facturas
                .Where(f => f.Fecha >= fechaInicio && f.Fecha <= fechaFin && f.Activo)
                .Include(f => f.Cliente)
                .Include(f => f.CreadoPor)
                .Include(f => f.ModificadoPor)
                .Include(f => f.FacturaDetalles.Where(fd => fd.Activo))
                .AsNoTracking()
                .OrderByDescending(f => f.FechaCreacion)
                .ToListAsync();

            var facturasDto = Mapping.ConvertirLista<Factura, FacturaDto>(facturas);

            for (int i = 0; i < facturas.Count; i++)
            {
                CompletarDatosFacturaDto(facturas[i], facturasDto[i]);
            }

            return facturasDto;
        }

        public async Task<ReporteVentasDto> GenerarReporteVentasAsync(DateTime fechaInicio, DateTime fechaFin)
        {
            var facturas = await _context.Facturas
                .Where(f => f.Fecha >= fechaInicio && f.Fecha <= fechaFin && f.Estado == "Activa" && f.Activo)
                .Include(f => f.FacturaDetalles.Where(fd => fd.Activo))
                .AsNoTracking()
                .ToListAsync();

            var totalVentas = facturas.Sum(f => f.Total);
            var totalIVA = facturas.Sum(f => f.ValorIva);
            var totalDescuentos = facturas.Sum(f => f.ValorDescuento);

            var reporte = new ReporteVentasDto
            {
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                TotalFacturas = facturas.Count,
                TotalVentas = totalVentas,
                TotalVentasFormateado = CurrencyHelper.FormatCurrency(totalVentas),
                TotalIVA = totalIVA,
                TotalIVAFormateado = CurrencyHelper.FormatCurrency(totalIVA),
                TotalDescuentos = totalDescuentos,
                TotalDescuentosFormateado = CurrencyHelper.FormatCurrency(totalDescuentos)
            };

            return reporte;
        }

        public async Task<bool> ExisteAsync(int id)
        {
            return await _context.Facturas.AnyAsync(f => f.Id == id);
        }

        // Método privado para completar datos del DTO
        private void CompletarDatosFacturaDto(Factura entidad, FacturaDto dto)
        {
            dto.ClienteNombreCompleto = $"{entidad.ClienteNombres} {entidad.ClienteApellidos}";
            dto.CreadoPor = entidad.CreadoPor != null ? $"{entidad.CreadoPor.Nombre} {entidad.CreadoPor.Apellido}" : null;
            dto.ModificadoPor = entidad.ModificadoPor != null ? $"{entidad.ModificadoPor.Nombre} {entidad.ModificadoPor.Apellido}" : null;
            dto.TotalArticulos = entidad.FacturaDetalles?.Count(fd => fd.Activo) ?? 0;
            dto.TotalCantidad = entidad.FacturaDetalles?.Where(fd => fd.Activo).Sum(fd => fd.Cantidad) ?? 0;
        }

        public Task<RespuestaDto> ActualizarAsync(int id, FacturaDto facturaDto, Guid usuarioId)
        {
            throw new NotImplementedException("La actualización de facturas no está permitida por reglas de negocio");
        }
    }
}
