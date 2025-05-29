using Facturacion.API.Domain.Contracts;
using Facturacion.API.Util.Logging;

namespace Facturacion.API.Domain.Services
{
    public class ExtendedLogger : IExtendedLogger
    {
        private readonly IFileLogger _fileLogger;
        private readonly ILogRepository _logRepository;
        private readonly string? _userId;
        private readonly string? _ip;
        private readonly string _context;

        public ExtendedLogger(
            IFileLogger fileLogger,
            ILogRepository logRepository,
            string? userId,
            string? ip,
            string context)
        {
            _fileLogger = fileLogger;
            _logRepository = logRepository;
            _userId = userId;
            _ip = ip;
            _context = context;
        }

        public void Debug(string message)
        {
            _fileLogger.WriteLog(_userId, _ip, _context, message, "DEBUG");
        }

        public void Info(string message, bool logToDb = false)
        {
            _fileLogger.WriteLog(_userId, _ip, _context, message, "INFO");

            if (logToDb)
            {
                Task.Run(async () => await _logRepository.InfoAsync(
                    string.IsNullOrEmpty(_userId) ? null : Guid.Parse(_userId),
                    _ip,
                    _context,
                    message));
            }
        }

        public void Warning(string message, bool logToDb = false)
        {
            _fileLogger.WriteLog(_userId, _ip, _context, message, "WARNING");

            if (logToDb)
            {
                Task.Run(async () => await _logRepository.LogAsync(
                    string.IsNullOrEmpty(_userId) ? null : Guid.Parse(_userId),
                    _ip,
                    _context,
                    message,
                    "400"));
            }
        }

        public void Error(string message, Exception? ex = null)
        {
            var fullMessage = ex != null ? $"{message}. Error: {ex.Message}\n{ex.StackTrace}" : message;

            _fileLogger.WriteLog(_userId, _ip, _context, fullMessage, "ERROR");

            Task.Run(async () => await _logRepository.ErrorAsync(
                string.IsNullOrEmpty(_userId) ? null : Guid.Parse(_userId),
                _ip,
                _context,
                fullMessage));
        }

        public void Action(string message)
        {
            _fileLogger.WriteLog(_userId, _ip, _context, message, "ACTION");

            Task.Run(async () => await _logRepository.AccionAsync(
                string.IsNullOrEmpty(_userId) ? null : Guid.Parse(_userId),
                _ip,
                _context,
                message));
        }

        // Métodos asíncronos
        public async Task InfoAsync(string message, bool logToDb = false)
        {
            await _fileLogger.WriteLogAsync(_userId, _ip, _context, message, "INFO");

            if (logToDb)
            {
                await _logRepository.InfoAsync(
                    string.IsNullOrEmpty(_userId) ? null : Guid.Parse(_userId),
                    _ip,
                    _context,
                    message);
            }
        }

        public async Task WarningAsync(string message, bool logToDb = false)
        {
            await _fileLogger.WriteLogAsync(_userId, _ip, _context, message, "WARNING");

            if (logToDb)
            {
                await _logRepository.LogAsync(
                    string.IsNullOrEmpty(_userId) ? null : Guid.Parse(_userId),
                    _ip,
                    _context,
                    message,
                    "400");
            }
        }

        public async Task ErrorAsync(string message, Exception? ex = null)
        {
            var fullMessage = ex != null ? $"{message}. Error: {ex.Message}\n{ex.StackTrace}" : message;

            await _fileLogger.WriteLogAsync(_userId, _ip, _context, fullMessage, "ERROR");
            await _logRepository.ErrorAsync(
                string.IsNullOrEmpty(_userId) ? null : Guid.Parse(_userId),
                _ip,
                _context,
                fullMessage);
        }

        public async Task ActionAsync(string message)
        {
            await _fileLogger.WriteLogAsync(_userId, _ip, _context, message, "ACTION");
            await _logRepository.AccionAsync(
                string.IsNullOrEmpty(_userId) ? null : Guid.Parse(_userId),
                _ip,
                _context,
                message);
        }
    }
}
