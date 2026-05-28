// utilities/ViajeTrackerDB.cs  
using AplicacionDespacho.Services.DataAccess;
using AplicacionDespacho.Services.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace AplicacionDespacho.utilities
{
    public static class ViajeTrackerDB
    {
        private static readonly ILoggingService _logger = LoggingFactory.CreateLogger("ViajeTrackerDB");
        private static AccesoDatosViajes _accesoDatos;
        private static readonly object _lock = new object();
        private static readonly Dictionary<string, bool> _cacheLocal = new Dictionary<string, bool>();
        private static Timer _pollingTimer;
        private static Timer _heartbeatTimer;

        private static readonly string _clienteId = Environment.MachineName + "_" + Environment.UserName;

        // Variables para polling inteligente    
        private static int _pollingInterval = 10000;
        private static DateTime _lastActivity = DateTime.Now;
        private static readonly int _minPollingInterval = 5000;
        private static readonly int _maxPollingInterval = 30000;
        private static string _viajeActual = null;

        public static void Initialize(AccesoDatosViajes accesoDatos)
        {
            if (_accesoDatos != null)
            {
                _logger.LogInfo("⚠️ ViajeTrackerDB ya estaba inicializado, reutilizando instancia");
                return;
            }

            // CORREGIR: Usar la instancia pasada como parámetro  
            _accesoDatos = accesoDatos;
            _logger.LogInfo("🚀 ViajeTrackerDB inicializado");

            IniciarPollingInteligente();
            IniciarHeartbeat();
        }
        private static void IniciarHeartbeat()
        {
            _heartbeatTimer = new Timer(async _ => await EnviarHeartbeat(), null,
                                      TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2));
            _logger.LogInfo("💓 Heartbeat iniciado con intervalo de 2 minutos");
        }

        private static void IniciarPollingInteligente()
        {
            _pollingTimer = new Timer(async _ => await ActualizarEstadosAsync(), null,
                                     _pollingInterval, Timeout.Infinite);
            _logger.LogInfo($"⏰ Polling inteligente iniciado con intervalo: {_pollingInterval}ms");
        }
        private static void AjustarIntervaloPolling()
        {
            var tiempoSinActividad = DateTime.Now - _lastActivity;

            if (tiempoSinActividad.TotalMinutes < 2)
            {
                // Actividad reciente - polling más frecuente  
                _pollingInterval = _minPollingInterval;
            }
            else if (tiempoSinActividad.TotalMinutes < 10)
            {
                // Actividad moderada  
                _pollingInterval = 15000;
            }
            else
            {
                // Sin actividad - polling menos frecuente  
                _pollingInterval = _maxPollingInterval;
            }

            // Reiniciar timer con nuevo intervalo  
            _pollingTimer?.Change(_pollingInterval, Timeout.Infinite);
            _logger.LogDebug($"🔄 Intervalo de polling ajustado a: {_pollingInterval}ms");
        }

        public static async Task<bool> MarcarComoEnUsoAsync(string numeroGuia)
        {
            if (string.IsNullOrEmpty(numeroGuia))
                return false;

            try
            {
                var clienteId = Environment.MachineName + "_" + Environment.UserName;
                var exito = await _accesoDatos?.MarcarViajeEnUsoAsync(numeroGuia, clienteId);

                if (exito)
                {
                    lock (_lock)
                    {
                        _cacheLocal[numeroGuia] = true;
                        _viajeActual = numeroGuia;
                        _lastActivity = DateTime.Now;
                    }

                    // Ajustar polling para mayor frecuencia  
                    AjustarIntervaloPolling();

                    _logger.LogInfo($"✅ Viaje marcado como en uso: {numeroGuia}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error marcando viaje en uso: {numeroGuia}");
                return false;
            }
        }

        public static async Task<bool> MarcarComoLibreAsync(string numeroGuia)
        {
            try
            {
                // CORREGIR: Usar método existente en AccesoDatosViajes  
                var clienteId = Environment.MachineName + "_" + Environment.UserName;
                bool exito = false;
                if (_accesoDatos != null)
                {
                    exito = await _accesoDatos.MarcarViajeLibreAsync(numeroGuia, clienteId);
                }

                if (exito)
                {
                    lock (_lock)
                    {
                        _cacheLocal.Remove(numeroGuia);
                        if (_viajeActual == numeroGuia)
                        {
                            _viajeActual = null;
                        }
                        _lastActivity = DateTime.Now;
                    }

                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ✅ Viaje liberado exitosamente: {numeroGuia}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error liberando viaje: {NumeroGuia}", numeroGuia);
                return false;
            }
        }

        public static bool EstaEnUso(string numeroGuia)
        {
            lock (_lock)
            {
                // Respuesta inmediata desde cache local  
                return _cacheLocal.ContainsKey(numeroGuia) && _cacheLocal[numeroGuia];
            }
        }

        public static async Task ActualizarEstadosAsync()
        {
            try
            {
                var viajesEnUso = await _accesoDatos?.ObtenerViajesEnUsoAsync() ?? new List<string>();

                lock (_lock)
                {
                    _cacheLocal.Clear();
                    foreach (var viaje in viajesEnUso)
                    {
                        _cacheLocal[viaje] = true;
                    }
                }

                _logger.LogDebug($"🔄 Estados actualizados - {viajesEnUso.Count} viajes en uso");
                AjustarIntervaloPolling();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error actualizando estados desde BD");
            }
            finally
            {
                _pollingTimer?.Change(_pollingInterval, Timeout.Infinite);
            }
        }
        private static async Task EnviarHeartbeat()
        {
            if (!string.IsNullOrEmpty(_viajeActual))
            {
                try
                {
                    // SIMPLIFICAR: Solo actualizar timestamp por ahora  
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] 💓 Heartbeat para: {_viajeActual}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error enviando heartbeat");
                }
            }
        }


        public static event Action OnEstadosActualizados;

        public static void Dispose()
        {
            _pollingTimer?.Dispose();
            _heartbeatTimer?.Dispose();

            if (!string.IsNullOrEmpty(_viajeActual))
            {
                Task.Run(async () => await MarcarComoLibreAsync(_viajeActual));
            }
        }
        public static bool EstaEnUsoLocal(string numeroGuia)
        {
            if (string.IsNullOrEmpty(numeroGuia))
                return false;

            lock (_lock)
            {
                // Verificar primero en cache local para respuesta rápida    
                if (_cacheLocal.ContainsKey(numeroGuia))
                {
                    return _cacheLocal[numeroGuia];
                }
            }

            // Si no está en cache, retornar false por defecto  
            // NOTA: No usar .Result aquí para evitar bloqueo del hilo UI  
            return false;
        }
        public static async Task<bool> MarcarViajeLibreAsync(string numeroGuia)
        {
            return await MarcarComoLibreAsync(numeroGuia);
        }

        // Método 2: Stop (alias para Dispose)  
        public static void Stop()
        {
            Dispose();
        }

        // Método 3: EstaEnUsoAsync (versión async de EstaEnUso)  
        public static async Task<bool> EstaEnUsoAsync(string numeroGuia)
        {
            if (string.IsNullOrEmpty(numeroGuia))
                return false;

            lock (_lock)
            {
                // Verificar primero en cache local para respuesta rápida  
                if (_cacheLocal.ContainsKey(numeroGuia))
                {
                    return _cacheLocal[numeroGuia];
                }
            }

            // Si no está en cache, consultar base de datos de forma async  
            try
            {
                if (_accesoDatos != null)
                {
                    return await _accesoDatos.VerificarViajeEnUsoAsync(numeroGuia);
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando estado async: {NumeroGuia}", numeroGuia);
                return false;
            }
        }

    }
}