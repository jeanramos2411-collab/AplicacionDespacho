using AplicacionDespacho.Configuration;
using AplicacionDespacho.Models;
using AplicacionDespacho.Services.DataAccess;
using AplicacionDespacho.Services.Logging;
using AplicacionDespacho.utilities;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;
using System.Threading;


namespace AplicacionDespacho.Services
{
    public class SignalRService
    {
        private HubConnection _connection;
        private readonly string _hubUrl;
        private readonly ILoggingService _logger;

        // NUEVO: Eventos para sincronización de viajes        
        public event Action<string, object> ActiveTripChanged; // tripId, tripData        
        public event Action<object> NewTripCreated; // tripData        
        public event Action<string> TripFinalized; // tripId        
        public event Action<string> ActiveTripRequested; // deviceId        

        public event Action<string> TripReopened; // tripId
        public event Action<string> BicolorPackagingTypesRequested;
        // Eventos existentes          
        public event Action<object> PalletScanned;
        public event Action<object> PalletUpdated;
        public event Action<string> PalletDeleted;
        // CORREGIDO: Evento para solicitud de eliminación de pallet desde móvil
        public event Action<string, string, string> PalletDeleteRequested; // tripId, palletNumber, deviceId
        // NUEVO: Evento para ediciones de pallets desde móvil  
        public event Action<string, object, string> PalletEditReceived;

        // NUEVO: Eventos para la nueva lógica          
        public event Action<string, string> PalletNumberReceived; // palletNumber, deviceId          
        public event Action<object, string> PalletProcessed; // palletData, deviceId          
        public event Action<string, string> PalletError; // errorMessage, deviceId          

        // NUEVO: Eventos de estado de conexión        
        public event Action<bool> ConnectionStateChanged;
        public event Action<string> ConnectionError;
        
        private int _reconnectionAttempts = 0;
        private readonly int _maxReconnectionAttempts = 10;
        private Timer _healthCheckTimer;
        private Timer _reconnectionTimer;
        private bool _isReconnecting = false;
        private DateTime _lastSuccessfulConnection = DateTime.MinValue;
        private readonly Random _jitterRandom = new Random();
        // CORREGIDO: Constructor que usa configuración dinámica completamente  
        public SignalRService(string hubUrl = null)
        {
            _logger = LoggingFactory.CreateLogger("SignalRService");
            _hubUrl = hubUrl ?? AppConfig.SignalRHubUrl; // Usar parámetro o configuración  
        }

        // ELIMINADO: Método GetDefaultHubUrl() ya no es necesario  

        public async Task StartConnectionAsync()
        {
            try
            {
                // VALIDACIÓN: Verificar que tenemos una URL válida  
                if (string.IsNullOrWhiteSpace(_hubUrl))
                {
                    throw new InvalidOperationException("No se ha configurado una URL válida para SignalR");
                }

                _logger.LogInfo("🔄 Iniciando conexión SignalR a {HubUrl}", _hubUrl);

                _connection = new HubConnectionBuilder()
                    .WithUrl(_hubUrl)
                    .WithAutomaticReconnect() // NUEVO: Reconexión automática        
                    .Build();

                // Configurar eventos de conexión        
                _connection.Closed += OnConnectionClosed;
                _connection.Reconnecting += OnReconnecting;
                _connection.Reconnected += OnReconnected;

                _connection.On<string>("TripReopened", (tripId) =>
                {
                    _logger.LogInfo("🔄 Viaje reabierto: {TripId}", tripId);
                    TripReopened?.Invoke(tripId);
                });
                // Eventos existentes          
                _connection.On<object>("PalletScanned", (pallet) =>
                {
                    _logger.LogInfo("📦 Pallet escaneado recibido desde servidor");
                    PalletScanned?.Invoke(pallet);
                });

                _connection.On<object>("PalletUpdated", (pallet) =>
                {
                    _logger.LogInfo("📝 Pallet actualizado recibido desde servidor");
                    PalletUpdated?.Invoke(pallet);
                });

                _connection.On<string>("PalletDeleted", (palletNumber) =>
                {
                    _logger.LogInfo("🗑️ Pallet eliminado recibido desde servidor: {PalletNumber}", palletNumber);
                    PalletDeleted?.Invoke(palletNumber);
                });
                _connection.On<string, string, string>("PalletDeleteRequested", (tripId, palletNumber, deviceId) =>
                {
                    _logger.LogInfo("🗑️ Solicitud de eliminación recibida desde móvil: {PalletNumber}, Device: {DeviceId}",
                                   palletNumber, deviceId);
                    PalletDeleteRequested?.Invoke(tripId, palletNumber, deviceId);
                });
                _connection.On<string, object>("ActiveTripChanged", (tripId, tripData) =>
                {
                    _logger.LogInfo("🔄 Viaje activo cambiado: {TripId}", tripId);
                    ActiveTripChanged?.Invoke(tripId, tripData);
                });



                // NUEVO: Evento para nuevo viaje creado
                _connection.On<object>("NewTripCreated", (tripData) =>
                {
                    _logger.LogInfo("🆕 Nuevo viaje creado recibido");
                    NewTripCreated?.Invoke(tripData);
                });
                // NUEVO: Evento para ediciones de pallets  
                _connection.On<string, object, string>("PalletEditReceived", (palletNumber, editedData, deviceId) =>
                {
                    _logger.LogInfo("📝 Edición de pallet recibida desde móvil: {PalletNumber}, Device: {DeviceId}",
                                   palletNumber, deviceId);
                    PalletEditReceived?.Invoke(palletNumber, editedData, deviceId);
                });
                _connection.On<string>("TripFinalized", (tripId) =>
                {
                    _logger.LogInfo("🏁 Viaje finalizado: {TripId}", tripId);
                    TripFinalized?.Invoke(tripId);
                });
                // NUEVO: Evento para solicitud de variedades desde móvil
                _connection.On<string>("VariedadesRequested", async (deviceId) =>
                {
                    _logger.LogInfo("📱 Solicitud de variedades recibida desde móvil: {DeviceId}", deviceId);

                    // Obtener variedades desde la base de datos  
                    var accesoDatosViajes = new AccesoDatosViajes();
                    var variedades = accesoDatosViajes.ObtenerTodasLasVariedades();

                    // Enviar de vuelta al hub para reenvío al móvil  
                    await _connection.InvokeAsync("SendVariedadesListToMobile", deviceId, variedades);
                });

                // NUEVO: Evento para solicitud de viaje activo desde móvil
                _connection.On<string>("ActiveTripRequested", (deviceId) =>
                {
                    _logger.LogInfo("📱 Solicitud de viaje activo desde: {DeviceId}", deviceId);
                    ActiveTripRequested?.Invoke(deviceId);
                });

                // NUEVO: Eventos para la nueva lógica          
                _connection.On<string, string>("PalletNumberReceived", (palletNumber, deviceId) =>
                {
                    _logger.LogInfo("📱 Número de pallet recibido desde móvil: {PalletNumber}, Device: {DeviceId}",
                                   palletNumber, deviceId);
                    PalletNumberReceived?.Invoke(palletNumber, deviceId);
                });

                // NUEVO: Evento para pallets con ediciones desde móvil    
                _connection.On<string, object, string>("PalletNumberReceivedWithEdits", (palletNumber, editedData, deviceId) =>
                {
                    _logger.LogInfo("📝 Pallet con ediciones recibido desde móvil: {PalletNumber}, Device: {DeviceId}",
                                   palletNumber, deviceId);
                    // Invocar el evento existente con los datos editados    
                    PalletNumberReceived?.Invoke(palletNumber, deviceId);
                });

                _connection.On<object, string>("PalletProcessed", (palletData, deviceId) =>
                {
                    _logger.LogInfo("✅ Pallet procesado exitosamente para device: {DeviceId}", deviceId);
                    PalletProcessed?.Invoke(palletData, deviceId);
                });

                _connection.On<string, string>("PalletError", (errorMessage, deviceId) =>
                {
                    _logger.LogWarning("❌ Error de pallet recibido para device {DeviceId}: {ErrorMessage}",
                                      deviceId, errorMessage);
                    PalletError?.Invoke(errorMessage, deviceId);
                });
                // NUEVO: Evento para solicitud de tipos de embalaje bicolor desde móvil
                _connection.On<string>("BicolorPackagingTypesRequested", (deviceId) =>
                {
                    BicolorPackagingTypesRequested?.Invoke(deviceId);
                });

                _connection.On<object>("PalletListUpdated", (palletsData) =>
                {
                    _logger.LogInfo("📋 Lista de pallets actualizada recibida");
                    // Este evento será manejado por el APK, no por el escritorio  
                });


                // NUEVO: Evento para recibir información del viaje activo    
                _connection.On<object, object>("ActiveTripInfo", (tripData, palletsData) =>
                {
                    _logger.LogInfo("📤 Información del viaje activo recibida desde servidor");
                    // Este evento será usado por el futuro APK    
                });

                await _connection.StartAsync();

                _logger.LogInfo("✅ Conexión SignalR establecida exitosamente a {HubUrl}", _hubUrl);
                ConnectionStateChanged?.Invoke(true);
                StartHealthCheckTimer();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al iniciar conexión SignalR a {HubUrl}: {ErrorMessage}", _hubUrl, ex.Message);
                ConnectionError?.Invoke($"Error de conexión a {_hubUrl}: {ex.Message}");
                ConnectionStateChanged?.Invoke(false);
                throw;
            }
        }



        // NUEVO: Propiedad para obtener la URL actual      
        public string CurrentHubUrl => _hubUrl;

        // Resto de métodos existentes sin cambios...      
        public async Task NotifyActiveTripAsync(string tripId, object tripData)
        {
            if (_connection?.State == HubConnectionState.Connected)
            {
                await _connection.InvokeAsync("NotifyActiveTrip", tripId, tripData);
            }
        }

        public async Task NotifyTripCreatedAsync(object tripData)
        {
            if (_connection?.State == HubConnectionState.Connected)
            {
                await _connection.InvokeAsync("NotifyTripCreated", tripData);
            }
        }

        public async Task NotifyTripFinalizedAsync(string tripId)
        {
            if (_connection?.State == HubConnectionState.Connected)
            {
                await _connection.InvokeAsync("NotifyTripFinalized", tripId);
            }
        }

        public async Task RequestActiveTripAsync(string deviceId)
        {
            if (_connection?.State == HubConnectionState.Connected)
            {
                await _connection.InvokeAsync("RequestActiveTrip", deviceId);
                _logger.LogInfo("📱 Solicitud de viaje activo procesada para device: {DeviceId}", deviceId);
            }
            else
            {
                _logger.LogWarning("⚠️ No se puede procesar solicitud de viaje activo - conexión no disponible");
            }
        }

        public async Task SendNoActiveTripAsync(string deviceId)
        {
            if (_connection?.State == HubConnectionState.Connected)
            {
                try
                {
                    await _connection.InvokeAsync("SendNoActiveTripToMobile", "No hay viaje activo disponible", deviceId);
                    _logger.LogInfo("📤 Notificación de ausencia de viaje activo enviada a device: {DeviceId}", deviceId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error enviando notificación de ausencia de viaje activo: {ErrorMessage}", ex.Message);
                    throw;
                }
            }
        }
        /// <summary>  
        /// Suscribe al evento de solicitud de eliminación de pallet desde móvil  
        /// </summary>  
        public void OnPalletDeletionRequested(Func<string, string, Task> handler)
        {
            _connection.On<string, string>("OnPalletDeletionRequested", handler);
        }

        /// <summary>  
        /// Envía información de pallet al móvil testeador  
        /// </summary>  
        public async Task SendPalletInfoToMobileTesteadorAsync(string palletDataJson, string deviceId, bool success, string errorMessage = "")
        {
            await _connection.InvokeAsync("SendPalletInfoToMobileTesteador", palletDataJson, deviceId, success, errorMessage);
        }

        /// <summary>  
        /// Envía resultado de eliminación al móvil  
        /// </summary>  
        public async Task SendDeletionResultToMobileAsync(string palletNumber, string deviceId, bool success, string message)
        {
            await _connection.InvokeAsync("SendDeletionResultToMobile", palletNumber, deviceId, success, message);
        }


        /// /////////////////////////////////////////////////////

        public void OnPalletInfoRequested(Func<string, string, Task> handler)
        {
            _connection.On<string, string>("OnPalletInfoRequested", handler);
        }




        // NUEVO: Métodos de manejo de eventos de conexión        
        private async Task OnConnectionClosed(Exception exception)
        {
            _logger.LogWarning("🔌 Conexión SignalR cerrada: {ErrorMessage}", exception?.Message ?? "Sin error");
            ConnectionStateChanged?.Invoke(false);
        }

        private async Task OnReconnecting(Exception exception)
        {
            _logger.LogInfo("🔄 Reconectando SignalR...");
            ConnectionStateChanged?.Invoke(false);
        }

        private async Task OnReconnected(string connectionId)
        {
            _logger.LogInfo("✅ SignalR reconectado con ID: {ConnectionId}", connectionId);
            ConnectionStateChanged?.Invoke(true);
            StartHealthCheckTimer();
        }

        public async Task JoinTripGroupAsync(string tripId)
        {
            if (_connection?.State == HubConnectionState.Connected)
            {
                try
                {
                    await _connection.InvokeAsync("JoinTripGroup", tripId);
                    _logger.LogInfo("👥 Unido al grupo del viaje: {TripId}", tripId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error uniéndose al grupo del viaje {TripId}: {ErrorMessage}",
                                    tripId, ex.Message);
                    throw;
                }
            }
            else
            {
                _logger.LogWarning("⚠️ No se puede unir al grupo - conexión no disponible");
            }
        }

        public async Task LeaveTripGroupAsync(string tripId)
        {
            if (_connection?.State == HubConnectionState.Connected)
            {
                try
                {
                    await _connection.InvokeAsync("LeaveTripGroup", tripId);
                    _logger.LogInfo("👋 Salió del grupo del viaje: {TripId}", tripId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error saliendo del grupo del viaje {TripId}: {ErrorMessage}",
                                    tripId, ex.Message);
                }
            }
        }
        // AGREGAR después del método LeaveTripGroupAsync:  
        public async Task SendPalletListToMobileAsync(string deviceId, List<InformacionPallet> pallets)
        {
            if (_connection?.State == HubConnectionState.Connected)
            {
                try
                {
                    await _connection.InvokeAsync("SendPalletListToMobile", deviceId, pallets);
                    _logger.LogInfo("📋 Lista de pallets enviada al móvil - Device: {DeviceId}, Count: {Count}",
                                   deviceId, pallets.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error enviando lista de pallets al móvil: {ErrorMessage}", ex.Message);
                    throw;
                }
            }
        }
        // NUEVO: Método para enviar resultado procesado de vuelta al móvil          
        public async Task SendPalletProcessedToMobileAsync(string tripId, InformacionPallet pallet, string deviceId)
        {
            if (_connection?.State == HubConnectionState.Connected)
            {
                try
                {
                    await _connection.InvokeAsync("SendPalletProcessedToMobile", tripId, pallet, deviceId);
                    _logger.LogInfo("📤 Pallet procesado enviado al móvil - Trip: {TripId}, Device: {DeviceId}",
                                   tripId, deviceId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error enviando pallet procesado al móvil: {ErrorMessage}", ex.Message);
                    throw;
                }
            }
        }

        // NUEVO: Método para enviar error de vuelta al móvil          
        public async Task SendPalletErrorToMobileAsync(string tripId, string errorMessage, string deviceId)
        {
            if (_connection?.State == HubConnectionState.Connected)
            {
                try
                {
                    await _connection.InvokeAsync("SendPalletErrorToMobile", tripId, errorMessage, deviceId);
                    _logger.LogInfo("📤 Error enviado al móvil - Trip: {TripId}, Device: {DeviceId}", tripId, deviceId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error enviando error al móvil: {ErrorMessage}", ex.Message);
                    throw;
                }
            }
        }

        // Agregar en Services/SignalRService.cs después del método SendPalletErrorToMobileAsync  
        public async Task SendPalletInfoToMobileAsync(string tripId, string infoMessage, string deviceId)
        {
            if (_connection?.State == HubConnectionState.Connected)
            {
                try
                {
                    await _connection.InvokeAsync("SendPalletInfoToMobile", tripId, infoMessage, deviceId);
                    _logger.LogInfo("ℹ️ Mensaje informativo enviado al móvil - Trip: {TripId}, Device: {DeviceId}",
                                   tripId, deviceId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error enviando mensaje informativo al móvil: {ErrorMessage}", ex.Message);
                    throw;
                }
            }
        }
        // NUEVO: Método para broadcast de mensajes informativos a todos los móviles del viaje  
        public async Task BroadcastPalletInfoToTripAsync(string tripId, string infoMessage)
        {
            if (_connection?.State == HubConnectionState.Connected)
            {
                try
                {
                    await _connection.InvokeAsync("BroadcastPalletInfoToTrip", tripId, infoMessage);
                    _logger.LogInfo("📢 Mensaje informativo broadcast al viaje: {TripId}", tripId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error enviando broadcast informativo: {ErrorMessage}", ex.Message);
                    throw;
                }
            }
        }
        // NUEVO: Método para enviar información del viaje activo al móvil    
        public async Task SendActiveTripInfoToMobileAsync(string deviceId, object tripData, object palletsData)
        {
            if (_connection?.State == HubConnectionState.Connected)
            {
                try
                {
                    await _connection.InvokeAsync("SendActiveTripInfoToMobile", deviceId, tripData, palletsData);
                    _logger.LogInfo("📤 Información del viaje activo enviada al móvil - Device: {DeviceId}", deviceId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error enviando información del viaje al móvil: {ErrorMessage}", ex.Message);
                    throw;
                }
            }
        }
        // AGREGAR después de SendActiveTripInfoToMobileAsync  
        public async Task SendActiveTripWithPalletsToMobileAsync(string deviceId, object tripData, List<InformacionPallet> pallets)
        {
            if (_connection?.State == HubConnectionState.Connected)
            {
                try
                {
                    await _connection.InvokeAsync("SendActiveTripWithPalletsToMobile", deviceId, tripData, pallets);
                    _logger.LogInfo("📤 Viaje activo con pallets enviado al móvil - Device: {DeviceId}, Count: {Count}",
                                   deviceId, pallets.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error enviando viaje activo con pallets: {ErrorMessage}", ex.Message);
                    throw;
                }
            }
        }
        // NUEVO: Método para verificar estado de conexión        
        public bool IsConnected => _connection?.State == HubConnectionState.Connected;

        public async Task StopConnectionAsync()
        {
            if (_connection != null)
            {
                try
                {
                    await _connection.StopAsync();
                    await _connection.DisposeAsync();
                    _logger.LogInfo("🔌 Conexión SignalR cerrada correctamente");
                    ConnectionStateChanged?.Invoke(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error cerrando conexión SignalR: {ErrorMessage}", ex.Message);
                }
            }
        }
        public async Task SendPalletSuccessToMobileAsync(string tripId, string successMessage, string deviceId)
        {
            if (_connection?.State == HubConnectionState.Connected)
            {
                try
                {
                    await _connection.InvokeAsync("SendPalletSuccessToMobile", tripId, successMessage, deviceId);
                    _logger.LogInfo("✅ Mensaje de éxito enviado al móvil - Trip: {TripId}, Device: {DeviceId}", tripId, deviceId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error enviando mensaje de éxito al móvil: {ErrorMessage}", ex.Message);
                    throw;
                }
            }
        }
        public async Task SendPalletDeleteRequestAsync(string tripId, string palletNumber, string deviceId)
        {
            if (_connection?.State == HubConnectionState.Connected)
            {
                await _connection.InvokeAsync("DeletePalletFromMobile", tripId, palletNumber, deviceId);
                _logger.LogInfo("🗑️ Solicitud de eliminación enviada para pallet: {PalletNumber}", palletNumber);
            }
        }
        public async Task NotifyTripReopenedAsync(string tripId)
        {
            if (_connection?.State == HubConnectionState.Connected)
            {
                await _connection.InvokeAsync("NotifyTripReopened", tripId);
            }
        }
        // Agregar este método a la clase SignalRService  
        public async Task NotifyTripInUseAsync(string numeroGuia, bool enUso)
        {
            if (_connection?.State == HubConnectionState.Connected)
            {
                try
                {
                    await _connection.InvokeAsync("NotifyTripInUse", numeroGuia, enUso);
                    _logger.LogInfo("📤 Notificación de viaje en uso enviada: {NumeroGuia} - {EnUso}", numeroGuia, enUso);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error enviando notificación de viaje en uso: {ErrorMessage}", ex.Message);
                }
            }
            else
            {
                _logger.LogWarning("⚠️ No se puede enviar notificación de viaje en uso - conexión no disponible");
            }
        }

        // AGREGAR método para broadcast  
        public async Task BroadcastPalletListUpdateAsync(string tripId, List<InformacionPallet> pallets)
        {
            if (_connection?.State == HubConnectionState.Connected)
            {
                try
                {
                    await _connection.InvokeAsync("BroadcastPalletListUpdate", tripId, pallets);
                    _logger.LogInfo("📤 Lista de pallets broadcast enviada para viaje: {TripId}", tripId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error enviando broadcast de lista: {ErrorMessage}", ex.Message);
                }
            }
        }

        public async Task RequestCurrentTripStatusAsync()
        {
            if (_connection?.State == HubConnectionState.Connected)
            {
                try
                {
                    await _connection.InvokeAsync("RequestCurrentTripStatus");
                    _logger.LogInfo("🔄 Solicitando estado actual de viajes");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error solicitando estado actual: {ErrorMessage}", ex.Message);
                }
            }
        }
        public async Task SendBicolorPackagingTypesToMobileAsync(string deviceId, List<string> packagingTypes)
        {
            if (_connection?.State == HubConnectionState.Connected)
            {
                await _connection.InvokeAsync("SendBicolorPackagingTypesToMobile", deviceId, packagingTypes);
                _logger.LogInfo("📤 Lista de embalajes bicolor enviada vía SignalR: {DeviceId}", deviceId);
            }
            else
            {
                _logger.LogWarning("⚠️ No se puede enviar embalajes bicolor - SignalR desconectado");
            }
        }
        private void StartHealthCheckTimer()
        {
            _healthCheckTimer?.Dispose();
            _healthCheckTimer = new Timer(async _ =>
            {
                if (!await PerformHealthCheck())
                {
                    _logger.LogWarning("🏥 Health check falló - iniciando reconexión");
                    _ = Task.Run(ReconnectWithBackoff);
                }
            }, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }
        private TimeSpan CalculateBackoffDelay(int attempt)
        {
            // Backoff exponencial: 2^attempt segundos, máximo 60 segundos  
            var baseDelay = Math.Min(Math.Pow(2, attempt), 60);

            // Agregar jitter (±25%) para evitar thundering herd  
            var jitter = _jitterRandom.NextDouble() * 0.5 - 0.25; // -25% a +25%  
            var delayWithJitter = baseDelay * (1 + jitter);

            return TimeSpan.FromSeconds(Math.Max(1, delayWithJitter));
        }
        private async Task<bool> PerformHealthCheck()
        {
            try
            {
                if (_connection?.State != HubConnectionState.Connected)
                    return false;

                // Enviar ping y esperar respuesta en máximo 5 segundos  
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await _connection.InvokeAsync("Ping", cts.Token);

                _lastSuccessfulConnection = DateTime.Now;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Health check falló: {ErrorMessage}", ex.Message);
                return false;
            }
        }
        private async Task ReconnectWithBackoff()
        {
            if (_isReconnecting) return;

            _isReconnecting = true;
            _healthCheckTimer?.Dispose();

            try
            {
                while (_reconnectionAttempts < _maxReconnectionAttempts && _connection?.State != HubConnectionState.Connected)
                {
                    var delay = CalculateBackoffDelay(_reconnectionAttempts);
                    _logger.LogInfo("🔄 Intento de reconexión {Attempt}/{MaxAttempts} en {Delay}ms",
                                   _reconnectionAttempts + 1, _maxReconnectionAttempts, delay.TotalMilliseconds);

                    await Task.Delay(delay);

                    try
                    {
                        await _connection.StartAsync();
                        _logger.LogInfo("✅ Reconexión exitosa después de {Attempts} intentos", _reconnectionAttempts + 1);
                        _reconnectionAttempts = 0;
                        StartHealthCheckTimer();
                        break;
                    }
                    catch (Exception ex)
                    {
                        _reconnectionAttempts++;
                        _logger.LogWarning("❌ Intento de reconexión {Attempt} falló: {Error}",
                                         _reconnectionAttempts, ex.Message);
                    }
                }
            }
            finally
            {
                _isReconnecting = false;
            }
        }
    }
}