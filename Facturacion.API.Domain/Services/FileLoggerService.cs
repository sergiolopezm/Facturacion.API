using Facturacion.API.Util.Logging;
using System.Text;

namespace Facturacion.API.Domain.Services
{
    public class FileLoggerService : IFileLogger
    {
        private readonly string _logPath;
        private static readonly object _lock = new object();

        public FileLoggerService()
        {
            // La carpeta Logs estará en la raíz del proyecto
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            // Subimos hasta encontrar la raíz del proyecto
            string projectPath = basePath;
            while (!File.Exists(Path.Combine(projectPath, "Facturacion.API.csproj")) &&
                   Directory.GetParent(projectPath) != null)
            {
                projectPath = Directory.GetParent(projectPath)!.FullName;
            }

            // Crear la carpeta Logs en la raíz del proyecto
            _logPath = Path.Combine(projectPath, "Logs");

            if (!Directory.Exists(_logPath))
            {
                Directory.CreateDirectory(_logPath);
            }
        }

        public async Task WriteLogAsync(string? userId, string? ip, string? action, string? detail, string logType)
        {
            await Task.Run(() => WriteLog(userId, ip, action, detail, logType));
        }

        public void WriteLog(string? userId, string? ip, string? action, string? detail, string logType)
        {
            try
            {
                var serverDateTime = DateTime.Now;
                var logFileName = $"{serverDateTime:yyyy-MM-dd}.txt";
                var logFilePath = Path.Combine(_logPath, logFileName);

                // Crear el mensaje de log
                var logMessage = new StringBuilder();
                logMessage.AppendLine($"Fecha: {serverDateTime:yyyy-MM-dd HH:mm:ss}");
                logMessage.AppendLine($"Usuario: {userId ?? "N/A"}");
                logMessage.AppendLine($"IP: {ip ?? "N/A"}");
                logMessage.AppendLine($"Tipo: {logType}");
                logMessage.AppendLine($"Acción: {action ?? "N/A"}");
                logMessage.AppendLine($"Detalle: {detail ?? "N/A"}");
                logMessage.AppendLine(new string('-', 80));
                logMessage.AppendLine();

                // Escribir el log en el archivo de manera segura
                lock (_lock)
                {
                    File.AppendAllText(logFilePath, logMessage.ToString(), Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                // En caso de error, lo mostramos en la consola para debug
                Console.WriteLine($"Error escribiendo log en archivo: {ex.Message}. Ruta: {_logPath}");
                // También podríamos usar ILogger<FileLoggerService> aquí pero queremos evitar dependencias circulares
            }
        }
    }
}
