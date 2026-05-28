// Services/Logging/ILoggingService.cs  
using System;

namespace AplicacionDespacho.Services.Logging
{
    public interface ILoggingService
    {
        void LogInfo(string message, params object[] args);
        void LogWarning(string message, params object[] args);
        void LogError(string message, params object[] args); // ✅ CORREGIDO: Sin Exception obligatoria  
        void LogError(Exception exception, string message, params object[] args); // ✅ SOBRECARGA para Exception  
        void LogDebug(string message, params object[] args);
        void LogFatal(string message, params object[] args);
        void LogFatal(Exception exception, string message, params object[] args); // ✅ SOBRECARGA para Exception  
    }
}