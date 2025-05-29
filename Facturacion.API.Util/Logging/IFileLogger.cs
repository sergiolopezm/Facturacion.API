namespace Facturacion.API.Util.Logging
{
    public interface IFileLogger
    {
        Task WriteLogAsync(string? userId, string? ip, string? action, string? detail, string logType);
        void WriteLog(string? userId, string? ip, string? action, string? detail, string logType);
    }
}
