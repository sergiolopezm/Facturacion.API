namespace Facturacion.API.Domain.Contracts
{
    /// <summary>
    /// Interfaz para el registro de logs en el sistema
    /// </summary>
    public interface ILogRepository
    {
        Task AccionAsync(Guid? usuarioId, string? ip, string? accion, string? detalle);
        Task InfoAsync(Guid? usuarioId, string? ip, string? accion, string? detalle);
        Task ErrorAsync(Guid? usuarioId, string? ip, string? accion, string? error);
        Task LogAsync(Guid? usuarioId, string? ip, string? accion, string? detalle, string tipo);
    }
}
