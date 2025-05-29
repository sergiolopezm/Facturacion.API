using Facturacion.API.Shared.InDTO.FacturacionInDto;

namespace Facturacion.API.Domain.Contracts.FacturacionRepository
{
    public interface IValidacionNegocioRepository
    {
        Task<ValidacionFacturaDto> ValidarFacturaCompletaAsync(CrearFacturaDto facturaDto);
        Task<bool> ValidarClienteExisteAsync(int clienteId);
        Task<bool> ValidarArticuloExisteAsync(int articuloId);
        Task<bool> ValidarStockSuficienteAsync(int articuloId, int cantidadRequerida);
        Task<List<string>> ValidarReglasDenegocioAsync(CrearFacturaDto facturaDto);
        bool ValidarFormatoMoneda(string montoTexto);
        bool ValidarRangosNumericos(CrearFacturaDto facturaDto);
        Task<bool> ValidarPermisoUsuarioAsync(Guid usuarioId, string accion);
    }
}
