// Services/DataAccess/AccesoDatosViajesSingleton.cs  
using AplicacionDespacho.Services.Logging;
using System;

namespace AplicacionDespacho.Services.DataAccess
{
    public sealed class AccesoDatosViajesSingleton
    {
        private static readonly Lazy<AccesoDatosViajes> _instance =
            new Lazy<AccesoDatosViajes>(() => new AccesoDatosViajes());

        private static readonly ILoggingService _logger =
            LoggingFactory.CreateLogger("AccesoDatosViajesSingleton");

        public static AccesoDatosViajes Instance
        {
            get
            {
                _logger.LogDebug("✅ Usando instancia singleton de AccesoDatosViajes");
                return _instance.Value;
            }
        }

        // Constructor privado para prevenir instanciación  
        private AccesoDatosViajesSingleton() { }

        // Método para verificar si la instancia ya fue creada  
        public static bool IsInstanceCreated => _instance.IsValueCreated;
    }
}