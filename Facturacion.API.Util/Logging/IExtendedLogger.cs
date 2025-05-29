namespace Facturacion.API.Util.Logging
{
    public interface IExtendedLogger
    {
        void Debug(string message);
        void Info(string message, bool logToDb = false);
        void Warning(string message, bool logToDb = false);
        void Error(string message, Exception? ex = null);
        void Action(string message);

        Task InfoAsync(string message, bool logToDb = false);
        Task WarningAsync(string message, bool logToDb = false);
        Task ErrorAsync(string message, Exception? ex = null);
        Task ActionAsync(string message);
    }
}
