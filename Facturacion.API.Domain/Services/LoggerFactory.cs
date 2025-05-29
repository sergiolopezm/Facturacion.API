using Facturacion.API.Domain.Contracts;
using Facturacion.API.Util.Logging;

namespace Facturacion.API.Domain.Services
{
    public class LoggerFactory : ILoggerFactory
    {
        private readonly IFileLogger _fileLogger;
        private readonly ILogRepository _logRepository;

        public LoggerFactory(IFileLogger fileLogger, ILogRepository logRepository)
        {
            _fileLogger = fileLogger;
            _logRepository = logRepository;
        }

        public IExtendedLogger CreateLogger(string? userId, string? ip, string context)
        {
            return new ExtendedLogger(_fileLogger, _logRepository, userId, ip, context);
        }
    }
}
