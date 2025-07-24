using System;
using System.IO;
using System.Reflection;
using System.Configuration;

namespace ShutdownApp
{
    /// <summary>
    /// Proporciona funcionalidades estáticas para el registro de eventos y errores en un archivo de log.
    /// Implementa rotación de logs por tamaño y retención de archivos.
    /// </summary>
    public static class Logger
    {
        private static string logFilePath;
        private static readonly object lockObject = new object();
        private const long MaxLogFileSizeMB = 10 * 1024 * 1024; // 10 MB
        private const int MaxLogFilesToRetain = 5;

        static Logger()
        {
            string? appDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (appDirectory == null)
            {
                appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            }

            string logFileName = ConfigurationManager.AppSettings["LogFilePath"] ?? "application.log";
            logFilePath = Path.Combine(appDirectory, logFileName);
        }

        /// <summary>
        /// Registra un mensaje informativo en el log.
        /// </summary>
        /// <param name="message">El mensaje informativo a registrar.</param>
        public static void LogInfo(string message)
        {
            Log("INFO", message);
        }

        /// <summary>
        /// Registra un mensaje de error en el log.
        /// </summary>
        /// <param name="message">El mensaje de error a registrar.</param>
        public static void LogError(string message)
        {
            Log("ERROR", message);
        }

        /// <summary>
        /// Registra un mensaje de advertencia en el log.
        /// </summary>
        /// <param name="message">El mensaje de advertencia a registrar.</param>
        public static void LogWarning(string message)
        {
            Log("WARNING", message);
        }

        /// <summary>
        /// Registra una excepción en el log, incluyendo detalles de la excepción y el stack trace.
        /// </summary>
        /// <param name="ex">La excepción a registrar.</param>
        /// <param name="message">Un mensaje adicional opcional para describir el contexto de la excepción.</param>
        public static void LogException(Exception ex, string? message = null)
        {
            string logMessage = message ?? LocalizationManager.GetString("LogUnexpectedErrorOccurred");
            logMessage += $"\nException Type: {ex.GetType().Name}";
            logMessage += $"\nMessage: {ex.Message}";
            logMessage += $"\nStack Trace: {ex.StackTrace}";
            if (ex.InnerException != null)
            {
                logMessage += $"\nInner Exception Type: {ex.InnerException.GetType().Name}";
                logMessage += $"\nInner Exception Message: {ex.InnerException.Message}";
                logMessage += $"\nInner Exception Stack Trace: {ex.InnerException.StackTrace}";
            }
            Log("EXCEPTION", logMessage);
        }

        /// <summary>
        /// Escribe un mensaje en el archivo de log con el nivel especificado.
        /// </summary>
        /// <param name="level">El nivel del log (INFO, ERROR, WARNING, EXCEPTION).</param>
        /// <param name="message">El mensaje a escribir en el log.</param>
        private static void Log(string level, string message)
        {
            lock (lockObject)
            {
                try
                {
                    RotateLogs(); // Llamar a la rotación antes de escribir
                    string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
                    File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    // Fallback: Si no se puede escribir en el log, al menos intentar escribir en la consola
                    Console.WriteLine($"ERROR: Could not write to log file. {ex.Message}");
                    Console.WriteLine(message); // Imprimir el mensaje original que se intentó loguear
                }
            }
        }

        /// <summary>
        /// Realiza la rotación de los archivos de log, eliminando los más antiguos y renombrando los existentes.
        /// </summary>
        private static void RotateLogs()
        {
            try
            {
                FileInfo logFileInfo = new FileInfo(logFilePath);
                if (logFileInfo.Exists && logFileInfo.Length >= MaxLogFileSizeMB)
                {
                    // Eliminar el archivo más antiguo si excede el límite
                    string oldestLogPath = $"{logFilePath}.{MaxLogFilesToRetain}";
                    if (File.Exists(oldestLogPath))
                    {
                        File.Delete(oldestLogPath);
                    }

                    // Renombrar los archivos existentes
                    for (int i = MaxLogFilesToRetain - 1; i >= 1; i--)
                    {
                        string sourcePath = $"{logFilePath}.{i}";
                        string destinationPath = $"{logFilePath}.{i + 1}";
                        if (File.Exists(sourcePath))
                        {
                            File.Move(sourcePath, destinationPath);
                        }
                    }

                    // Renombrar el archivo actual
                    File.Move(logFilePath, $"{logFilePath}.1");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Could not rotate log files. {ex.Message}");
            }
        }
    }
}
