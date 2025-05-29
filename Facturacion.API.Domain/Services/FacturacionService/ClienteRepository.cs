using Facturacion.API.Domain.Contracts.FacturacionRepository;
using Facturacion.API.Infrastructure;
using Facturacion.API.Shared.GeneralDTO;
using Facturacion.API.Shared.InDTO.ClienteInDto;
using Facturacion.API.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Facturacion.API.Domain.Services.FacturacionService
{
    public class ClienteRepository : IClienteRepository
    {
        private readonly DBContext _context;
        private readonly ILogger<ClienteRepository> _logger;

        public ClienteRepository(DBContext context, ILogger<ClienteRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<ClienteDto>> ObtenerTodosAsync()
        {
            _logger.LogInformation("Obteniendo todos los clientes");

            var clientes = await _context.Clientes
                .Where(c => c.Activo)
                .Include(c => c.CreadoPor)
                .Include(c => c.ModificadoPor)
                .AsNoTracking()
                .ToListAsync();

            var clientesDto = Mapping.ConvertirLista<Cliente, ClienteDto>(clientes);

            for (int i = 0; i < clientes.Count; i++)
            {
                var entidad = clientes[i];
                var dto = clientesDto[i];

                dto.NombreCompleto = $"{entidad.Nombres} {entidad.Apellidos}";
                dto.CreadoPor = entidad.CreadoPor != null ? $"{entidad.CreadoPor.Nombre} {entidad.CreadoPor.Apellido}" : null;
                dto.ModificadoPor = entidad.ModificadoPor != null ? $"{entidad.ModificadoPor.Nombre} {entidad.ModificadoPor.Apellido}" : null;

                // Calcular estadísticas del cliente
                var estadisticas = await ObtenerEstadisticasClienteAsync(entidad.Id);
                dto.TotalFacturas = estadisticas.totalFacturas;
                dto.MontoTotalCompras = estadisticas.montoTotal;
                dto.MontoTotalComprasFormateado = CurrencyHelper.FormatCurrency(estadisticas.montoTotal);
            }

            return clientesDto.OrderBy(c => c.NombreCompleto).ToList();
        }

        public async Task<ClienteDto?> ObtenerPorIdAsync(int id)
        {
            _logger.LogInformation("Obteniendo cliente con ID: {Id}", id);

            var cliente = await _context.Clientes
                .Where(c => c.Id == id)
                .Include(c => c.CreadoPor)
                .Include(c => c.ModificadoPor)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (cliente == null)
                return null;

            var dto = Mapping.Convertir<Cliente, ClienteDto>(cliente);
            dto.NombreCompleto = $"{cliente.Nombres} {cliente.Apellidos}";
            dto.CreadoPor = cliente.CreadoPor != null ? $"{cliente.CreadoPor.Nombre} {cliente.CreadoPor.Apellido}" : null;
            dto.ModificadoPor = cliente.ModificadoPor != null ? $"{cliente.ModificadoPor.Nombre} {cliente.ModificadoPor.Apellido}" : null;

            // Obtener estadísticas
            var estadisticas = await ObtenerEstadisticasClienteAsync(cliente.Id);
            dto.TotalFacturas = estadisticas.totalFacturas;
            dto.MontoTotalCompras = estadisticas.montoTotal;
            dto.MontoTotalComprasFormateado = CurrencyHelper.FormatCurrency(estadisticas.montoTotal);

            return dto;
        }

        public async Task<ClienteDto?> ObtenerPorDocumentoAsync(string numeroDocumento)
        {
            _logger.LogInformation("Obteniendo cliente con documento: {Documento}", numeroDocumento);

            var cliente = await _context.Clientes
                .Where(c => c.NumeroDocumento == numeroDocumento && c.Activo)
                .Include(c => c.CreadoPor)
                .Include(c => c.ModificadoPor)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (cliente == null)
                return null;

            var dto = Mapping.Convertir<Cliente, ClienteDto>(cliente);
            dto.NombreCompleto = $"{cliente.Nombres} {cliente.Apellidos}";
            dto.CreadoPor = cliente.CreadoPor != null ? $"{cliente.CreadoPor.Nombre} {cliente.CreadoPor.Apellido}" : null;
            dto.ModificadoPor = cliente.ModificadoPor != null ? $"{cliente.ModificadoPor.Nombre} {cliente.ModificadoPor.Apellido}" : null;

            return dto;
        }

        public async Task<RespuestaDto> CrearAsync(ClienteDto clienteDto, Guid usuarioId)
        {
            try
            {
                _logger.LogInformation("Creando cliente: {Documento}", clienteDto.NumeroDocumento);

                // Validar que no exista un cliente con el mismo número de documento
                if (await ExistePorDocumentoAsync(clienteDto.NumeroDocumento))
                {
                    return RespuestaDto.ParametrosIncorrectos(
                        "Creación fallida",
                        $"Ya existe un cliente con el número de documento '{clienteDto.NumeroDocumento}'");
                }

                var cliente = new Cliente
                {
                    NumeroDocumento = clienteDto.NumeroDocumento,
                    Nombres = clienteDto.Nombres,
                    Apellidos = clienteDto.Apellidos,
                    Direccion = clienteDto.Direccion,
                    Telefono = clienteDto.Telefono,
                    Email = clienteDto.Email,
                    Activo = true,
                    FechaCreacion = DateTime.Now,
                    CreadoPorId = usuarioId
                };

                await _context.Clientes.AddAsync(cliente);
                await _context.SaveChangesAsync();

                var dto = Mapping.Convertir<Cliente, ClienteDto>(cliente);
                dto.NombreCompleto = $"{cliente.Nombres} {cliente.Apellidos}";

                return RespuestaDto.Exitoso(
                    "Cliente creado",
                    $"El cliente '{dto.NombreCompleto}' ha sido creado correctamente",
                    dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear cliente {Documento}", clienteDto.NumeroDocumento);
                return RespuestaDto.ErrorInterno(ex.Message);
            }
        }

        public async Task<RespuestaDto> ActualizarAsync(int id, ClienteDto clienteDto, Guid usuarioId)
        {
            try
            {
                _logger.LogInformation("Actualizando cliente con ID: {Id}", id);

                var cliente = await _context.Clientes.FindAsync(id);
                if (cliente == null)
                {
                    return RespuestaDto.NoEncontrado("Cliente");
                }

                // Validar que no exista otro cliente con el mismo número de documento
                if (cliente.NumeroDocumento != clienteDto.NumeroDocumento &&
                    await _context.Clientes.AnyAsync(c => c.NumeroDocumento == clienteDto.NumeroDocumento && c.Id != id))
                {
                    return RespuestaDto.ParametrosIncorrectos(
                        "Actualización fallida",
                        $"Ya existe un cliente con el número de documento '{clienteDto.NumeroDocumento}'");
                }

                cliente.NumeroDocumento = clienteDto.NumeroDocumento;
                cliente.Nombres = clienteDto.Nombres;
                cliente.Apellidos = clienteDto.Apellidos;
                cliente.Direccion = clienteDto.Direccion;
                cliente.Telefono = clienteDto.Telefono;
                cliente.Email = clienteDto.Email;
                cliente.Activo = clienteDto.Activo;
                cliente.FechaModificacion = DateTime.Now;
                cliente.ModificadoPorId = usuarioId;

                await _context.SaveChangesAsync();

                var dto = Mapping.Convertir<Cliente, ClienteDto>(cliente);
                dto.NombreCompleto = $"{cliente.Nombres} {cliente.Apellidos}";

                return RespuestaDto.Exitoso(
                    "Cliente actualizado",
                    $"El cliente '{dto.NombreCompleto}' ha sido actualizado correctamente",
                    dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar cliente {Id}", id);
                return RespuestaDto.ErrorInterno(ex.Message);
            }
        }

        public async Task<RespuestaDto> EliminarAsync(int id)
        {
            try
            {
                _logger.LogInformation("Eliminando cliente con ID: {Id}", id);

                var cliente = await _context.Clientes
                    .Include(c => c.Facturas)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (cliente == null)
                {
                    return RespuestaDto.NoEncontrado("Cliente");
                }

                // Verificar si tiene facturas asociadas
                if (cliente.Facturas?.Any(f => f.Activo) ?? false)
                {
                    return RespuestaDto.ParametrosIncorrectos(
                        "Eliminación fallida",
                        $"No se puede eliminar el cliente '{cliente.Nombres} {cliente.Apellidos}' porque tiene facturas asociadas");
                }

                cliente.Activo = false;
                cliente.FechaModificacion = DateTime.Now;

                await _context.SaveChangesAsync();

                return RespuestaDto.Exitoso(
                    "Cliente eliminado",
                    $"El cliente '{cliente.Nombres} {cliente.Apellidos}' ha sido eliminado correctamente",
                    null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar cliente {Id}", id);
                return RespuestaDto.ErrorInterno(ex.Message);
            }
        }

        public async Task<bool> ExisteAsync(int id)
        {
            return await _context.Clientes.AnyAsync(c => c.Id == id);
        }

        public async Task<bool> ExistePorDocumentoAsync(string numeroDocumento)
        {
            return await _context.Clientes.AnyAsync(c => c.NumeroDocumento == numeroDocumento);
        }

        public async Task<PaginacionDto<ClienteDto>> ObtenerPaginadoAsync(int pagina, int elementosPorPagina, string? busqueda = null)
        {
            _logger.LogInformation(
                "Obteniendo clientes paginados. Página: {Pagina}, Elementos: {Elementos}, Búsqueda: {Busqueda}",
                pagina, elementosPorPagina, busqueda);

            IQueryable<Cliente> query = _context.Clientes
                .Include(c => c.CreadoPor)
                .Include(c => c.ModificadoPor)
                .Where(c => c.Activo);

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                busqueda = busqueda.ToLower();
                query = query.Where(c =>
                    c.NumeroDocumento.ToLower().Contains(busqueda) ||
                    c.Nombres.ToLower().Contains(busqueda) ||
                    c.Apellidos.ToLower().Contains(busqueda) ||
                    (c.Email != null && c.Email.ToLower().Contains(busqueda)) ||
                    c.Telefono.ToLower().Contains(busqueda));
            }

            int totalRegistros = await query.CountAsync();
            int totalPaginas = (int)Math.Ceiling((double)totalRegistros / elementosPorPagina);

            var clientes = await query
                .OrderBy(c => c.Apellidos).ThenBy(c => c.Nombres)
                .Skip((pagina - 1) * elementosPorPagina)
                .Take(elementosPorPagina)
                .AsNoTracking()
                .ToListAsync();

            var clientesDto = Mapping.ConvertirLista<Cliente, ClienteDto>(clientes);

            for (int i = 0; i < clientes.Count; i++)
            {
                var entidad = clientes[i];
                var dto = clientesDto[i];

                dto.NombreCompleto = $"{entidad.Nombres} {entidad.Apellidos}";
                dto.CreadoPor = entidad.CreadoPor != null ? $"{entidad.CreadoPor.Nombre} {entidad.CreadoPor.Apellido}" : null;
                dto.ModificadoPor = entidad.ModificadoPor != null ? $"{entidad.ModificadoPor.Nombre} {entidad.ModificadoPor.Apellido}" : null;
            }

            return new PaginacionDto<ClienteDto>
            {
                Pagina = pagina,
                ElementosPorPagina = elementosPorPagina,
                TotalPaginas = totalPaginas,
                TotalRegistros = totalRegistros,
                Lista = clientesDto
            };
        }

        public async Task<List<ClienteFrecuenteDto>> ObtenerClientesFrecuentesAsync(int top = 10)
        {
            var clientesFrecuentes = await _context.Clientes
                .Where(c => c.Activo)
                .Select(c => new ClienteFrecuenteDto
                {
                    ClienteId = c.Id,
                    NombreCompleto = $"{c.Nombres} {c.Apellidos}",
                    NumeroDocumento = c.NumeroDocumento,
                    TotalFacturas = c.Facturas!.Count(f => f.Activo && f.Estado == "Activa"),
                    MontoTotalCompras = c.Facturas!.Where(f => f.Activo && f.Estado == "Activa").Sum(f => f.Total),
                    UltimaCompra = c.Facturas!.Where(f => f.Activo && f.Estado == "Activa").Max(f => (DateTime?)f.Fecha)
                })
                .Where(c => c.TotalFacturas > 0)
                .OrderByDescending(c => c.MontoTotalCompras)
                .Take(top)
                .ToListAsync();

            foreach (var cliente in clientesFrecuentes)
            {
                cliente.MontoTotalComprasFormateado = CurrencyHelper.FormatCurrency(cliente.MontoTotalCompras);
            }

            return clientesFrecuentes;
        }

        private async Task<(int totalFacturas, decimal montoTotal)> ObtenerEstadisticasClienteAsync(int clienteId)
        {
            var facturas = await _context.Facturas
                .Where(f => f.ClienteId == clienteId && f.Activo && f.Estado == "Activa")
                .Select(f => f.Total)
                .ToListAsync();

            return (facturas.Count, facturas.Sum());
        }
    }
}
