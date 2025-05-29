using Facturacion.API.Domain.Contracts.FacturacionRepository;
using Facturacion.API.Infrastructure;
using Facturacion.API.Shared.InDTO.FacturacionInDto;
using Facturacion.API.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Facturacion.API.Domain.Services.FacturacionService
{
    public class ValidacionNegocioRepository : IValidacionNegocioRepository
    {
        private readonly DBContext _context;
        private readonly ILogger<ValidacionNegocioRepository> _logger;
        private readonly ICalculoFacturacionRepository _calculoService;

        public ValidacionNegocioRepository(
            DBContext context,
            ILogger<ValidacionNegocioRepository> logger,
            ICalculoFacturacionRepository calculoService)
        {
            _context = context;
            _logger = logger;
            _calculoService = calculoService;
        }

        public async Task<ValidacionFacturaDto> ValidarFacturaCompletaAsync(CrearFacturaDto facturaDto)
        {
            var validacion = new ValidacionFacturaDto { EsValida = true };

            try
            {
                // Validar cliente
                if (!await ValidarClienteExisteAsync(facturaDto.ClienteId))
                {
                    validacion.Errores.Add("El cliente especificado no existe o está inactivo");
                    validacion.EsValida = false;
                }

                // Validar que hay detalles
                if (facturaDto.Detalles == null || !facturaDto.Detalles.Any())
                {
                    validacion.Errores.Add("Debe incluir al menos un artículo en la factura");
                    validacion.EsValida = false;
                    return validacion;
                }

                // Validar cada detalle
                foreach (var detalle in facturaDto.Detalles)
                {
                    if (!await ValidarArticuloExisteAsync(detalle.ArticuloId))
                    {
                        validacion.Errores.Add($"El artículo con ID {detalle.ArticuloId} no existe o está inactivo");
                        validacion.EsValida = false;
                        continue;
                    }

                    if (!await ValidarStockSuficienteAsync(detalle.ArticuloId, detalle.Cantidad))
                    {
                        var articulo = await _context.Articulos.FindAsync(detalle.ArticuloId);
                        validacion.Errores.Add($"No hay suficiente stock del artículo '{articulo?.Nombre}'. Stock disponible: {articulo?.Stock ?? 0}");
                        validacion.EsValida = false;
                    }
                }

                // Validar rangos numéricos
                if (!ValidarRangosNumericos(facturaDto))
                {
                    validacion.Errores.Add("Los valores numéricos están fuera de los rangos permitidos");
                    validacion.EsValida = false;
                }

                // Validar reglas de negocio adicionales
                var reglasNegocio = await ValidarReglasDenegocioAsync(facturaDto);
                validacion.Errores.AddRange(reglasNegocio);
                if (reglasNegocio.Any())
                {
                    validacion.EsValida = false;
                }

                // Si es válida, calcular totales
                if (validacion.EsValida)
                {
                    var subtotal = _calculoService.CalcularSubtotal(facturaDto.Detalles);
                    validacion.TotalesCalculados = _calculoService.CalcularTotales(subtotal);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar factura completa");
                validacion.Errores.Add($"Error interno en la validación: {ex.Message}");
                validacion.EsValida = false;
            }

            return validacion;
        }

        public async Task<bool> ValidarClienteExisteAsync(int clienteId)
        {
            try
            {
                return await _context.Clientes.AnyAsync(c => c.Id == clienteId && c.Activo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar existencia del cliente {ClienteId}", clienteId);
                return false;
            }
        }

        public async Task<bool> ValidarArticuloExisteAsync(int articuloId)
        {
            try
            {
                return await _context.Articulos.AnyAsync(a => a.Id == articuloId && a.Activo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar existencia del artículo {ArticuloId}", articuloId);
                return false;
            }
        }

        public async Task<bool> ValidarStockSuficienteAsync(int articuloId, int cantidadRequerida)
        {
            try
            {
                var articulo = await _context.Articulos.FindAsync(articuloId);
                return articulo != null && articulo.Stock >= cantidadRequerida;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar stock del artículo {ArticuloId}", articuloId);
                return false;
            }
        }

        public async Task<List<string>> ValidarReglasDenegocioAsync(CrearFacturaDto facturaDto)
        {
            var errores = new List<string>();

            try
            {
                // Validar límite máximo de artículos por factura (ejemplo: 50)
                if (facturaDto.Detalles.Count > 50)
                {
                    errores.Add("Una factura no puede tener más de 50 artículos diferentes");
                }

                // Validar cantidad máxima por artículo (ejemplo: 1000)
                foreach (var detalle in facturaDto.Detalles)
                {
                    if (detalle.Cantidad > 1000)
                    {
                        errores.Add($"La cantidad máxima por artículo es 1000 unidades");
                        break;
                    }
                }

                // Validar precio máximo por artículo (ejemplo: 50,000,000)
                foreach (var detalle in facturaDto.Detalles)
                {
                    if (detalle.PrecioUnitario > 50000000)
                    {
                        errores.Add($"El precio unitario máximo por artículo es $50,000,000");
                        break;
                    }
                }

                // Validar que no haya artículos duplicados
                var articulosDuplicados = facturaDto.Detalles
                    .GroupBy(d => d.ArticuloId)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key);

                if (articulosDuplicados.Any())
                {
                    errores.Add("No se pueden incluir artículos duplicados en la misma factura");
                }

                // Validar total máximo de factura (ejemplo: 100,000,000)
                var subtotal = _calculoService.CalcularSubtotal(facturaDto.Detalles);
                var totales = _calculoService.CalcularTotales(subtotal);

                if (totales.Total > 100000000)
                {
                    errores.Add("El total de la factura no puede exceder $100,000,000");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar reglas de negocio");
                errores.Add("Error al validar reglas de negocio");
            }

            return errores;
        }

        public bool ValidarFormatoMoneda(string montoTexto)
        {
            return CurrencyHelper.IsValidCurrencyFormat(montoTexto);
        }

        public bool ValidarRangosNumericos(CrearFacturaDto facturaDto)
        {
            try
            {
                // Validar que las cantidades estén en rango válido
                foreach (var detalle in facturaDto.Detalles)
                {
                    if (detalle.Cantidad < 1 || detalle.Cantidad > int.MaxValue)
                        return false;

                    if (detalle.PrecioUnitario < 0 || detalle.PrecioUnitario > decimal.MaxValue)
                        return false;
                }

                // Validar que el ClienteId esté en rango válido
                if (facturaDto.ClienteId < 1)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ValidarPermisoUsuarioAsync(Guid usuarioId, string accion)
        {
            try
            {
                // Aquí se pueden implementar validaciones de permisos específicos
                // Por ejemplo, verificar si el usuario tiene permisos para crear facturas
                var usuario = await _context.Usuarios
                    .Include(u => u.Rol)
                    .FirstOrDefaultAsync(u => u.Id == usuarioId && u.Activo);

                if (usuario == null)
                    return false;

                // Validaciones específicas por acción
                switch (accion.ToLower())
                {
                    case "crear_factura":
                        return usuario.Rol.Nombre == "Admin" || usuario.Rol.Nombre == "Vendedor";
                    case "anular_factura":
                        return usuario.Rol.Nombre == "Admin" || usuario.Rol.Nombre == "Supervisor";
                    case "ver_reportes":
                        return usuario.Rol.Nombre == "Admin" || usuario.Rol.Nombre == "Supervisor";
                    default:
                        return true; // Acciones básicas permitidas para todos
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar permisos del usuario {UsuarioId} para acción {Accion}", usuarioId, accion);
                return false;
            }
        }
    }
}
