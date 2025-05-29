using Facturacion.API.Util.Logging;

namespace Facturacion.API.Domain.Contracts;

public interface ILoggerFactory
{
    IExtendedLogger CreateLogger(string? userId, string? ip, string context);
}
