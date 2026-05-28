// Services/Logging/NLogService.cs - VERSIÓN CORREGIDA  
using System;
using NLog;
using AplicacionDespacho.Configuration;

namespace AplicacionDespacho.Services.Logging
{
    public class NLogService : ILoggingService
    {
        private readonly ILogger _logger;

        public NLogService(string loggerName = "AplicacionDespacho")
        {
            _logger = LogManager.GetLogger(loggerName);
        }

        public void LogInfo(string message, params object[] args)
        {
            _logger.Info(message, args);
        }

        public void LogWarning(string message, params object[] args)
        {
            _logger.Warn(message, args);
        }

        // ✅ MÉTODO CORREGIDO: Solo mensaje  
        public void LogError(string message, params object[] args)
        {
            _logger.Error(message, args);
        }

        // ✅ SOBRECARGA: Con Exception  
        public void LogError(Exception exception, string message, params object[] args)
        {
            _logger.Error(exception, message, args);
        }

        public void LogDebug(string message, params object[] args)
        {
            _logger.Debug(message, args);
        }

        // ✅ MÉTODO CORREGIDO: Solo mensaje  
        public void LogFatal(string message, params object[] args)
        {
            _logger.Fatal(message, args);
        }

        // ✅ SOBRECARGA: Con Exception  
        public void LogFatal(Exception exception, string message, params object[] args)
        {
            _logger.Fatal(exception, message, args);
        }
    }
}