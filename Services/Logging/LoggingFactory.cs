// Services/Logging/LoggingFactory.cs  
using AplicacionDespacho.Configuration;

namespace AplicacionDespacho.Services.Logging
{
    public static class LoggingFactory
    {
        public static ILoggingService CreateLogger(string loggerName = "AplicacionDespacho")
        {
            return new NLogService(loggerName);
        }
    }
}