using AplicacionDespacho.Configuration;
using AplicacionDespacho.Models;
using AplicacionDespacho.Models.Reports;
using AplicacionDespacho.Services;
using AplicacionDespacho.Services.DataAccess;
using AplicacionDespacho.Services.Logging;
using AplicacionDespacho.utilities;
using AplicacionDespacho.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace AplicacionDespacho.Modules.Despacho.ViewModels
{
    public class ViewModelPrincipal : INotifyPropertyChanged, IDisposable
    {
        private readonly IAccesoDatosPallet _accesoDatos;
        private readonly AccesoDatosViajes _accesoDatosViajes;
        private readonly ILoggingService _logger;
        private readonly AccesoDatosEmbalajeBicolor _accesoDatosEmbalajeBicolor;

        private string _estadoConexionTexto;
        // NUEVO: Propiedades para SignalR y cola de escaneos  
        private readonly SignalRService _signalRService;
        private readonly PalletScanQueue _palletScanQueue;
        private string _currentDeviceProcessing = null;

        // Conexión SignalR dedicada para Testeador (independiente de la conexión principal)
        private SignalRService _testeadorSignalRService;

        private string _entradaNumeroPallet;
        public string EntradaNumeroPallet
        {
            get => _entradaNumeroPallet;
            set
            {
                // 🔥 LIMPIAR: Eliminar espacios, saltos de línea, tabs y caracteres de control
                var cleaned = string.IsNullOrEmpty(value) ? value : LimpiarNumeroPallet(value);

                // Convertir a mayúsculas después de limpiar
                cleaned = cleaned?.ToUpper();

                if (_entradaNumeroPallet != cleaned)
                {
                    _entradaNumeroPallet = cleaned;
                    OnPropertyChanged(nameof(EntradaNumeroPallet));
                }
            }
        }

        // ✅ Método auxiliar para limpiar el número de pallet
        private string LimpiarNumeroPallet(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Eliminar TODOS los caracteres que NO sean letras o números
            // Esto elimina: espacios, saltos de línea (\n, \r), tabs (\t), etc.
            var cleaned = new string(input.Where(c => char.IsLetterOrDigit(c)).ToArray());

            return cleaned;
        }
        public ObservableCollection<string> Embalajes { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> Variedades { get; set; } = new ObservableCollection<string>();



        public ObservableCollection<InformacionPallet> PalletsEscaneados { get; } = new ObservableCollection<InformacionPallet>();

        private InformacionPallet _ultimoPalletEscaneado;
        public InformacionPallet UltimoPalletEscaneado
        {
            get => _ultimoPalletEscaneado;
            set
            {
                _ultimoPalletEscaneado = value;
                OnPropertyChanged(nameof(UltimoPalletEscaneado));
            }
        }

        // NUEVAS PROPIEDADES PARA SELECCIÓN DE PALLETS      
        private InformacionPallet _palletSeleccionado;
        public InformacionPallet PalletSeleccionado
        {
            get => _palletSeleccionado;
            set
            {
                _palletSeleccionado = value;
                OnPropertyChanged(nameof(PalletSeleccionado));
                OnPropertyChanged(nameof(TienePalletSeleccionado));

                if (value != null && (UltimoPalletEscaneado == null || UltimoPalletEscaneado.NumeroPallet != value.NumeroPallet))
                {
                    _logger.LogDebug("Cargando pallet para edición: {NumeroPallet}", value.NumeroPallet);
                    CargarPalletParaEdicion(value);
                }
            }
        }

        // NUEVO: Propiedad pública para acceso desde MainWindow  
        public SignalRService SignalRService => _signalRService;

        public ViewModelPrincipal(IAccesoDatosPallet accesoDatos)
        {
            _accesoDatos = accesoDatos;
            _accesoDatosViajes = new AccesoDatosViajes();
            _logger = LoggingFactory.CreateLogger("ViewModelPrincipal");
            CargarDatosComboBox();

            // NUEVO: Inicializar SignalR y cola de escaneos  
            _signalRService = new SignalRService(AppConfig.SignalRHubUrl);
            _palletScanQueue = new PalletScanQueue();

            // AGREGAR: Suscribirse a eventos de sincronización de viajes  
            _signalRService.ActiveTripChanged += OnActiveTripChangedFromRemote;
            _signalRService.NewTripCreated += OnNewTripCreatedFromRemote;
            _signalRService.TripFinalized += OnTripFinalizedFromRemote;
            _signalRService.ActiveTripRequested += OnActiveTripRequestedFromRemote;
            _signalRService.ConnectionStateChanged += OnSignalRConnectionChanged;

            _signalRService.TripReopened += OnTripReopenedFromRemote;

            // NUEVO: Suscribirse a eventos de SignalR para la nueva lógica
            // _signalRService.ConnectionStateChanged += OnSignalRConnectionChanged;  
            _signalRService.ConnectionError += OnSignalRConnectionError;
            _signalRService.PalletProcessed += OnPalletProcessedFromMobile;
            _signalRService.PalletError += OnPalletErrorFromMobile;

            _signalRService.PalletEditReceived += OnPalletEditReceivedFromMobile;
            _signalRService.PalletDeleteRequested += OnPalletDeleteRequestedFromMobile; // NUEVO
            _signalRService.PalletScanned += OnPalletScannedFromMobile;
            _signalRService.PalletUpdated += OnPalletUpdatedFromMobile;
            _signalRService.PalletDeleted += OnPalletDeletedFromMobile;
            _signalRService.PalletNumberReceived += OnPalletNumberReceivedFromMobile;
            _signalRService.BicolorPackagingTypesRequested += OnBicolorPackagingTypesRequestedFromMobile;

            // NUEVO: Inicializar acceso a datos para embalajes bicolor
            _accesoDatosEmbalajeBicolor = new AccesoDatosEmbalajeBicolor();

            // NUEVO: Suscribirse al procesamiento de cola  
            _palletScanQueue.ProcessRequest += OnProcessPalletFromQueue;

            // Inicializar conexión SignalR  
            _ = Task.Run(async () => await _signalRService.StartConnectionAsync());

            // Iniciar conexión dedicada para Testeador (independiente de la principal)
            _ = Task.Run(async () => await IniciarServicioTesteadorAsync());

            _logger.LogInfo("ViewModelPrincipal inicializado con SignalR y cola de escaneos");

            ComandoEscanear = new ComandoRelevo(EscanearPallet, PuedeEscanearPallet);
            ComandoFinalizarDespacho = new ComandoRelevo(FinalizarDespacho, PuedeFinalizarDespacho);
            ComandoNuevoViaje = new ComandoRelevo(NuevoViaje, parameter => true);
            ComandoEditarViaje = new ComandoRelevo(EditarViaje, parameter => TieneViajeActivo);
            ComandoEliminarPallet = new ComandoRelevo(EliminarPallet, parameter => TienePalletSeleccionado);
            ComandoAplicarCambios = new ComandoRelevo(AplicarCambios, parameter => UltimoPalletEscaneado != null);
            ComandoRevertirCambios = new ComandoRelevo(RevertirCambios, parameter => UltimoPalletEscaneado != null);
            ComandoContinuarViaje = new ComandoRelevo(ContinuarViaje, parameter => !TieneViajeActivo);


        }
        // Agregar este método para cargar los datos  
        private void CargarDatosComboBox()
        {
            try
            {
                // Cargar embalajes  
                var embalajes = _accesoDatosViajes.ObtenerTodosLosEmbalajes();
                Embalajes.Clear();
                foreach (var embalaje in embalajes)
                {
                    Embalajes.Add(embalaje);
                }

                // Cargar variedades  
                var variedades = _accesoDatosViajes.ObtenerTodasLasVariedades();
                Variedades.Clear();
                foreach (var variedad in variedades)
                {
                    Variedades.Add(variedad);
                }

                _logger.LogInfo("Datos de ComboBox cargados: {EmbalajesTotales} embalajes, {VariedadesTotales} variedades",
                               Embalajes.Count, Variedades.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando datos para ComboBox");
            }
        }
        // NUEVO: Método para manejar números de pallet recibidos desde móvil  
        private void OnPalletNumberReceivedFromMobile(string palletNumber, string deviceId)
        {
            _logger.LogInfo("Número de pallet recibido desde dispositivo móvil: {PalletNumber} desde {DeviceId}",
                           palletNumber, deviceId);

            // Agregar a la cola para procesamiento secuencial  
            _palletScanQueue.EnqueueScan(palletNumber, deviceId);
        }

        private void OnTripReopenedFromRemote(string tripId)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _logger.LogInfo("🔄 Viaje reabierto desde remoto: {TripId}", tripId);
                MessageBox.Show($"📱 Viaje #{tripId} ha sido reabierto desde otro dispositivo",
                               "Viaje Reabierto", MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }

        // NUEVO: Método para procesar pallets desde la cola  
        private void OnProcessPalletFromQueue(PalletScanRequest request)
        {
            Application.Current.Dispatcher.Invoke(async () =>
            {
                try
                {
                    _currentDeviceProcessing = request.DeviceId;

                    // SUGERENCIA DE MEJORA: Feedback visual  
                    await ShowMobileProcessingFeedback(request.PalletNumber, request.DeviceId);

                    // Establecer el número en el TextBox  
                    EntradaNumeroPallet = request.PalletNumber;

                    // Ejecutar automáticamente el escaneo  
                    if (ComandoEscanear.CanExecute(null))
                    {
                        ComandoEscanear.Execute(null);
                    }
                    else
                    {
                        // Enviar error si no se puede escanear  
                        await _signalRService.SendPalletErrorToMobileAsync(
                            ViajeActivo?.ViajeId.ToString() ?? "0",
                            "No se puede escanear en este momento. Verifique que haya un viaje activo.",
                            request.DeviceId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error procesando pallet desde móvil: {PalletNumber}", request.PalletNumber);

                    await _signalRService.SendPalletErrorToMobileAsync(
                        ViajeActivo?.ViajeId.ToString() ?? "0",
                        $"Error procesando pallet: {ex.Message}",
                        request.DeviceId);
                }
                finally
                {
                    _currentDeviceProcessing = null;
                }
            });
        }

        // SUGERENCIA DE MEJORA: Método para mostrar feedback visual  
        private async Task ShowMobileProcessingFeedback(string palletNumber, string deviceId)
        {
            // Mostrar notificación visual  
            _logger.LogInfo("📱 Procesando pallet {PalletNumber} desde dispositivo móvil {DeviceId}",
               palletNumber, deviceId);
        }

        // PARTE 2: Propiedades y Métodos Auxiliares    
        private bool PuedeEscanearPallet(object parameter)
        {
            return TieneViajeActivo && PalletsEscaneados.Count < AppConfig.MaxPalletsPerTrip;
        }

        /// <summary>
        /// Valida embalaje en PESOS_EMBALAJE con mensajes diferenciados:
        /// no existe en la tabla vs existe pero le faltan valores (peso unitario / ficha técnica PC).
        /// </summary>
        private bool ValidarEmbalajeEnGestionPesos(
            string nombreEmbalaje,
            string tipoPallet,
            bool esPalletCompleto,
            bool esEdicion,
            out string errorMessage)
        {
            errorMessage = null;

            nombreEmbalaje = nombreEmbalaje?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(nombreEmbalaje))
            {
                errorMessage = "Debe indicar un embalaje válido.";
                return false;
            }

            string accion = esEdicion ? "modificar el pallet" : "registrar el pallet";
            var pesoEmbalaje = _accesoDatosViajes.ObtenerPesoEmbalaje(nombreEmbalaje);

            if (pesoEmbalaje == null)
            {
                // En la ventana Gestión aparece con 0/0 pero PesoEmbalajeId=0: aún no está guardado en PESOS_EMBALAJE
                if (_accesoDatosViajes.ExisteEmbalajeEnCatalogoPacking(nombreEmbalaje))
                {
                    errorMessage =
                        $"El embalaje '{nombreEmbalaje}' está en el listado de embalajes, pero no tiene valores guardados en Gestión de Pesos por Embalaje.\n" +
                        $"Tiene peso unitario y/o cajas de ficha técnica en cero o sin guardar.\n" +
                        $"Abra \"Gestión de Pesos por Embalaje\", ingrese el peso unitario (mayor que cero)";
                    if (esPalletCompleto)
                    {
                        errorMessage += " y el total de cajas de ficha técnica";
                    }
                    errorMessage += $", pulse Guardar y luego {accion} ({tipoPallet}).";
                }
                else
                {
                    errorMessage =
                        $"El embalaje '{nombreEmbalaje}' no existe en el catálogo ni en Gestión de Pesos por Embalaje.\n" +
                        $"Verifique el nombre o regístrelo en \"Gestión de Pesos por Embalaje\" antes de {accion} ({tipoPallet}).";
                }
                return false;
            }

            if (pesoEmbalaje.PesoUnitario <= 0)
            {
                errorMessage =
                    $"El embalaje '{nombreEmbalaje}' está registrado en Gestión de Pesos por Embalaje, pero el peso unitario es cero o no fue ingresado.\n" +
                    $"Debe actualizar el peso unitario (mayor que cero) antes de {accion} ({tipoPallet}).";
                return false;
            }

            if (esPalletCompleto &&
                (!pesoEmbalaje.TotalCajasFichaTecnica.HasValue || pesoEmbalaje.TotalCajasFichaTecnica.Value <= 0))
            {
                errorMessage =
                    $"El embalaje '{nombreEmbalaje}' está registrado en Gestión de Pesos por Embalaje, pero el total de cajas de ficha técnica es cero o no fue ingresado.\n" +
                    "Para pallets completos (PC) debe ingresar ese valor y guardar el registro.\n" +
                    "(Los pallets PH, CT y EN no requieren ficha técnica.)";
                return false;
            }

            return true;
        }

        private bool ValidarEmbalajeParaEdicion(InformacionPallet pallet, out string errorMessage)
        {
            return ValidarEmbalajeEnGestionPesos(
                pallet.Embalaje,
                pallet.TipoPallet,
                pallet.EsPC,
                esEdicion: true,
                out errorMessage);
        }

        private void NotificarErrorEmbalajeEdicion(string errorMessage, string deviceId = null)
        {
            if (!string.IsNullOrEmpty(deviceId))
            {
                _ = _signalRService.SendPalletErrorToMobileAsync(
                    ViajeActivo.ViajeId.ToString(), errorMessage, deviceId);
            }
            else
            {
                MessageBox.Show(errorMessage, "Embalaje sin configuración",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public void RecalcularPesoPallet(InformacionPallet pallet)
        {
            try
            {
                _logger.LogDebug("Recalculando peso para pallet: {NumeroPallet}", pallet.NumeroPallet);

                var pesoEmbalaje = _accesoDatosViajes.ObtenerPesoEmbalaje(pallet.Embalaje);
                if (pesoEmbalaje != null)
                {
                    pallet.PesoUnitario = pesoEmbalaje.PesoUnitario;

                    // ✅ LÓGICA DE PALLET COMPLETO: Verificar si es PC        
                    bool esPalletCompleto = pallet.EsPC;

                    if (esPalletCompleto && pesoEmbalaje.TotalCajasFichaTecnica.HasValue)
                    {
                        // ✅ CAMBIO: Para pallets PC (bicolor o monocolor), usar ficha técnica    
                        int cajasFichaTecnica = pesoEmbalaje.TotalCajasFichaTecnica.Value;

                        if (pallet.EsBicolor)
                        {
                            // ✅ NUEVO: Validación de discrepancia para PC bicolor (igual que monocolor)  
                            int cajasManual = pallet.NumeroDeCajas;

                            if (cajasManual != cajasFichaTecnica)
                            {
                                // Mostrar mensaje informativo sobre la discrepancia        
                                string mensaje = $"⚠️ VALIDACIÓN PALLET COMPLETO (PC) BICOLOR\n" +
                                               $"Pallet: {pallet.NumeroPallet}\n" +
                                               $"Cajas ingresadas manualmente: {cajasManual}\n" +
                                               $"Cajas según ficha técnica: {cajasFichaTecnica}\n" +
                                               $"Para pallets completos (PC) bicolor, el número de cajas debe coincidir exactamente con la ficha técnica.\n" +
                                               $"Se utilizará el valor de ficha técnica para mantener la integridad del pallet completo.";

                                _logger.LogDebug("🔍 Verificando envío a móvil - _currentDeviceProcessing: '{DeviceId}', EsPC: {EsPC}, Bicolor: {EsBicolor}, Discrepancia: {Discrepancia}",
                                               _currentDeviceProcessing ?? "NULL", esPalletCompleto, pallet.EsBicolor, cajasManual != cajasFichaTecnica);

                                // ✅ MANTENER: Enviar mensaje al móvil PRIMERO (si aplica)      
                                if (!string.IsNullOrEmpty(_currentDeviceProcessing))
                                {
                                    // Si viene desde móvil, enviar mensaje al dispositivo móvil        
                                    var sendTask = Task.Run(async () =>
                                    {
                                        try
                                        {
                                            await _signalRService.SendPalletInfoToMobileAsync(
                                                ViajeActivo?.ViajeId.ToString() ?? "0",
                                                mensaje,
                                                _currentDeviceProcessing);

                                            _logger.LogInfo("📱 Mensaje de validación PC bicolor enviado al móvil: {DeviceId}", _currentDeviceProcessing);
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.LogError(ex, "Error enviando mensaje de validación PC bicolor al móvil");
                                        }
                                    });

                                    // ⚠️ CRÍTICO: Esperar un momento para que SignalR envíe el mensaje      
                                    // antes de mostrar el MessageBox bloqueante      
                                    Task.Delay(500).Wait();
                                }

                                // Ahora sí mostrar MessageBox en desktop (después de enviar al móvil)      
                                if (string.IsNullOrEmpty(_currentDeviceProcessing))
                                {
                                    // Solo mostrar MessageBox si es desde desktop      
                                    MessageBox.Show(mensaje, "Validación Pallet Completo Bicolor",
                                                  MessageBoxButton.OK, MessageBoxImage.Information);
                                }

                                _logger.LogWarning("Discrepancia en pallet PC bicolor {NumeroPallet}: Manual={CajasManual}, FichaTecnica={CajasFichaTecnica}",
                                                 pallet.NumeroPallet, cajasManual, cajasFichaTecnica);
                            }

                            // ✅ NUEVO: Validación visual de inconsistencia en peso total para bicolor  
                            decimal pesoCalculadoCorrect = cajasFichaTecnica * pesoEmbalaje.PesoUnitario;

                            if (Math.Abs(pallet.PesoTotal - pesoCalculadoCorrect) > 0.001m)
                            {
                                // Marcar pallet con inconsistencia visual        
                                pallet.TienePesoInconsistente = true;

                                _logger.LogWarning("Inconsistencia detectada en pallet PC bicolor {NumeroPallet}: " +
                                                 "Peso actual {PesoActual} vs esperado {PesoEsperado}",
                                                 pallet.NumeroPallet, pallet.PesoTotal, pesoCalculadoCorrect);
                            }
                            else
                            {
                                pallet.TienePesoInconsistente = false;
                            }

                            // Restaurar NumeroDeCajas al valor de ficha técnica para mantener consistencia
                            pallet.NumeroDeCajas = cajasFichaTecnica;
                            pallet.PesoTotal = pesoCalculadoCorrect;

                            _logger.LogInfo("Peso recalculado para pallet PC bicolor {NumeroPallet}: {TotalCajas} cajas = {PesoTotal} kg",
                                          pallet.NumeroPallet, cajasFichaTecnica, pallet.PesoTotal);
                        }
                        else
                        {
                            // ✅ MANTENER: Validación para PC monocolor (funcionalidad original preservada)    
                            int cajasManual = pallet.NumeroDeCajas;

                            if (cajasManual != cajasFichaTecnica)
                            {
                                // Mostrar mensaje informativo sobre la discrepancia        
                                string mensaje = $"⚠️ VALIDACIÓN PALLET COMPLETO (PC)\n" +
                                               $"Pallet: {pallet.NumeroPallet}\n" +
                                               $"Cajas ingresadas manualmente: {cajasManual}\n" +
                                               $"Cajas según ficha técnica: {cajasFichaTecnica}\n" +
                                               $"Para pallets completos (PC) monocolor, el número de cajas debe coincidir exactamente con la ficha técnica.\n" +
                                               $"Se utilizará el valor de ficha técnica para mantener la integridad del pallet completo.";

                                _logger.LogDebug("🔍 Verificando envío a móvil - _currentDeviceProcessing: '{DeviceId}', EsPC: {EsPC}, Discrepancia: {Discrepancia}",
                                               _currentDeviceProcessing ?? "NULL", esPalletCompleto, cajasManual != cajasFichaTecnica);

                                // ✅ MANTENER: Enviar mensaje al móvil PRIMERO (si aplica)      
                                if (!string.IsNullOrEmpty(_currentDeviceProcessing))
                                {
                                    // Si viene desde móvil, enviar mensaje al dispositivo móvil        
                                    var sendTask = Task.Run(async () =>
                                    {
                                        try
                                        {
                                            await _signalRService.SendPalletInfoToMobileAsync(
                                                ViajeActivo?.ViajeId.ToString() ?? "0",
                                                mensaje,
                                                _currentDeviceProcessing);

                                            _logger.LogInfo("📱 Mensaje de validación PC enviado al móvil: {DeviceId}", _currentDeviceProcessing);
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.LogError(ex, "Error enviando mensaje de validación PC al móvil");
                                        }
                                    });

                                    // ⚠️ CRÍTICO: Esperar un momento para que SignalR envíe el mensaje      
                                    // antes de mostrar el MessageBox bloqueante      
                                    Task.Delay(500).Wait();
                                }

                                // Ahora sí mostrar MessageBox en desktop (después de enviar al móvil)      
                                if (string.IsNullOrEmpty(_currentDeviceProcessing))
                                {
                                    // Solo mostrar MessageBox si es desde desktop      
                                    MessageBox.Show(mensaje, "Validación Pallet Completo",
                                                  MessageBoxButton.OK, MessageBoxImage.Information);
                                }

                                _logger.LogWarning("Discrepancia en pallet PC {NumeroPallet}: Manual={CajasManual}, FichaTecnica={CajasFichaTecnica}",
                                                 pallet.NumeroPallet, cajasManual, cajasFichaTecnica);
                            }

                            // Para PC normal, usar ficha técnica (funcionalidad original preservada)        
                            decimal pesoCalculadoCorrect = cajasFichaTecnica * pesoEmbalaje.PesoUnitario;

                            // ✅ MANTENER: Validación visual de inconsistencia en peso total        
                            if (Math.Abs(pallet.PesoTotal - pesoCalculadoCorrect) > 0.001m)
                            {
                                // Marcar pallet con inconsistencia visual        
                                pallet.TienePesoInconsistente = true;

                                _logger.LogWarning("Inconsistencia detectada en pallet PC {NumeroPallet}: " +
                                                 "Peso actual {PesoActual} vs esperado {PesoEsperado}",
                                                 pallet.NumeroPallet, pallet.PesoTotal, pesoCalculadoCorrect);
                            }
                            else
                            {
                                pallet.TienePesoInconsistente = false;
                            }

                            // Restaurar NumeroDeCajas al valor de ficha técnica para mantener consistencia
                            pallet.NumeroDeCajas = cajasFichaTecnica;
                            pallet.PesoTotal = pesoCalculadoCorrect;

                            _logger.LogInfo("Peso recalculado para pallet PC {NumeroPallet}: {TotalCajas} cajas = {PesoTotal} kg",
                                          pallet.NumeroPallet, cajasFichaTecnica, pallet.PesoTotal);
                        }
                    }
                    else
                    {
                        // PH, CT, EN (y PC sin ficha, bloqueado antes por ValidarEmbalajeParaEdicion):
                        // respeta NumeroDeCajas ingresado por el operador.
                        pallet.PesoTotal = pallet.NumeroDeCajas * pesoEmbalaje.PesoUnitario;

                        _logger.LogInfo("Peso recalculado para pallet {TipoPallet} {NumeroPallet}: {NumeroCajas} cajas = {PesoTotal} kg",
                                      pallet.TipoPallet,
                                      pallet.NumeroPallet,
                                      pallet.NumeroDeCajas,
                                      pallet.PesoTotal);
                    }
                }
                else
                {
                    // No poner ceros: ValidarEmbalajeParaEdicion debe bloquear antes de llegar aquí.
                    _logger.LogWarning("No se encontró configuración de peso para embalaje: {Embalaje}. Recálculo omitido.", pallet.Embalaje);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recalculando peso del pallet: {NumeroPallet}", pallet.NumeroPallet);
            }
        }

        // NUEVO: Método auxiliar para marcar inconsistencias visuales  
        private void MarcarPalletConInconsistencia(InformacionPallet pallet, decimal pesoEsperado)
        {
            // Agregar propiedad visual para marcar inconsistencia  
            // Esto se puede implementar agregando una propiedad al modelo InformacionPallet  
            // como "TieneInconsistenciaPeso" que se use en el DataGrid para colorear la fila  

            _logger.LogInfo("Marcando pallet {NumeroPallet} con inconsistencia visual. " +
                           "Peso esperado: {PesoEsperado}, Peso actual: {PesoActual}",
                           pallet.NumeroPallet, pesoEsperado, pallet.PesoTotal);
        }

        // NUEVO MÉTODO: Mostrar mensaje informativo para validación PC  
        private void MostrarMensajeValidacionPC(string numeroPallet, int cajasManual, int cajasFichaTecnica)
        {
            string mensaje = $"VALIDACIÓN PALLET COMPLETO (PC)" +
                            $"Pallet: {numeroPallet} " +
                            $" Cajas ingresadas manualmente: {cajasManual} " +
                            $" Cajas según ficha técnica: {cajasFichaTecnica} " +
                            $" Para pallets completos (PC) monocolor, el número de cajas debe coincidir exactamente con la ficha técnica. " +
                            $" Se usará el valor de ficha técnica para mantener la integridad del pallet completo. ";

            MessageBox.Show(mensaje, "Validación Pallet Completo",
                           MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private bool ValidarCajasPalletCompleto(InformacionPallet pallet, PesoEmbalaje pesoEmbalaje)
        {
            // Solo validar para pallets PC monocolor  
            if (!pallet.EsPC || pallet.EsBicolor || !pesoEmbalaje.TotalCajasFichaTecnica.HasValue)
                return true;

            return pallet.NumeroDeCajas == pesoEmbalaje.TotalCajasFichaTecnica.Value;
        }
        public bool ValidarConsistenciaPesoPallet(InformacionPallet pallet)
        {
            try
            {
                var pesoEmbalaje = _accesoDatosViajes.ObtenerPesoEmbalaje(pallet.Embalaje);
                if (pesoEmbalaje == null) return true; // Si no hay configuración, no validar  

                // Solo validar pallets PC monocolor  
                if (pallet.EsPC && !pallet.EsBicolor && pesoEmbalaje.TotalCajasFichaTecnica.HasValue)
                {
                    // Calcular peso esperado usando ficha técnica  
                    decimal pesoEsperado = pesoEmbalaje.TotalCajasFichaTecnica.Value * pesoEmbalaje.PesoUnitario;

                    // Comparar con tolerancia mínima para errores de redondeo  
                    decimal diferencia = Math.Abs(pallet.PesoTotal - pesoEsperado);

                    return diferencia < 0.01m; // Tolerancia de 0.01 kg  
                }

                return true; // Para otros tipos de pallets, no validar  
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando consistencia de peso para pallet: {NumeroPallet}", pallet.NumeroPallet);
                return true; // En caso de error, no marcar como inconsistente  
            }
        }

        public bool TienePalletSeleccionado => PalletSeleccionado != null;

        // NUEVAS PROPIEDADES PARA MANEJO DE VIAJES      
        private Viaje _viajeActivo;
        public Viaje ViajeActivo
        {
            get => _viajeActivo;
            set
            {
                var viajeAnterior = _viajeActivo;
                _viajeActivo = value;

                // Liberar viaje anterior de forma asíncrona  
                if (viajeAnterior != null)
                {
                    _ = Task.Run(async () => await ViajeTrackerDB.MarcarComoLibreAsync(viajeAnterior.NumeroGuia));
                }

                // Marcar nuevo viaje como en uso de forma asíncrona  
                if (value != null)
                {
                    _ = Task.Run(async () => await ViajeTrackerDB.MarcarComoEnUsoAsync(value.NumeroGuia));
                }

                OnPropertyChanged(nameof(ViajeActivo));
                OnPropertyChanged(nameof(TieneViajeActivo));
                OnPropertyChanged(nameof(InformacionViajeActivo));

                // NUEVO: Gestión automática de grupos SignalR  
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Salir del grupo anterior si existe  
                        if (viajeAnterior != null && _signalRService.IsConnected)
                        {
                            await _signalRService.LeaveTripGroupAsync(viajeAnterior.ViajeId.ToString());
                            _logger.LogInfo("👋 Salió del grupo del viaje anterior: {ViajeId}", viajeAnterior.ViajeId);
                        }

                        // Unirse al nuevo grupo si existe  
                        if (value != null && _signalRService.IsConnected)
                        {
                            await _signalRService.JoinTripGroupAsync(value.ViajeId.ToString());
                            _logger.LogInfo("👥 Se unió al grupo del nuevo viaje: {ViajeId}", value.ViajeId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error gestionando grupos SignalR: {ErrorMessage}", ex.Message);
                    }
                });

                if (value != null)
                {
                    _logger.LogInfo("Viaje activo establecido: #{NumeroViaje} - {Responsable}",
                                  value.NumeroViaje, value.Responsable);
                }
                else
                {
                    _logger.LogInfo("Viaje activo limpiado");
                }
            }
        }

        public bool TieneViajeActivo => ViajeActivo != null;

        public string InformacionViajeActivo
        {
            get
            {
                if (ViajeActivo == null)
                    return "No hay viaje activo";

                return $"Viaje #{ViajeActivo.NumeroViaje} - Guía: {ViajeActivo.NumeroGuia} - {ViajeActivo.Fecha:dd/MM/yyyy} - {ViajeActivo.Responsable}";
            }
        }

        public int TotalCajas
        {
            get
            {
                // ✅ CAMBIO: Usar solo NumeroDeCajas para todos los pallets (bicolor y monocolor)  
                return PalletsEscaneados.Sum(p => p.NumeroDeCajas);
            }
        }

        public decimal PesoTotalViaje
        {
            get
            {
                return PalletsEscaneados.Sum(p => p.PesoTotal);
            }
        }
        public int TotalPC => PalletsEscaneados.Count(p => p.EsPC);
        public int TotalPH => PalletsEscaneados.Count(p => p.EsPH);
        public int TotalCT => PalletsEscaneados.Count(p => p.EsCT);
        public int TotalEN => PalletsEscaneados.Count(p => p.EsEN);
        // COMANDOS      
        public ICommand ComandoEscanear { get; }
        public ICommand ComandoFinalizarDespacho { get; }
        public ICommand ComandoNuevoViaje { get; }
        public ICommand ComandoEditarViaje { get; }
        public ICommand ComandoContinuarViaje { get; }
        public ICommand ComandoEliminarPallet { get; }
        public ICommand ComandoAplicarCambios { get; }
        public ICommand ComandoRevertirCambios { get; }

        private async void EscanearPallet(object parameter)
        {
            _logger.LogInfo("Iniciando escaneo de pallet: {NumeroPallet}", EntradaNumeroPallet);

            if (!TieneViajeActivo)
            {
                _logger.LogWarning("Intento de escaneo sin viaje activo");
                var errorMsg = "Debe crear un viaje antes de escanear pallets.";

                // NUEVO: Enviar error al dispositivo móvil si está procesando desde móvil        
                if (!string.IsNullOrEmpty(_currentDeviceProcessing))
                {
                    await _signalRService.SendPalletErrorToMobileAsync(
                        "0", errorMsg, _currentDeviceProcessing);
                }
                else
                {
                    MessageBox.Show(errorMsg, "Viaje Requerido",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                return;
            }

            // VALIDACIÓN: Verificar que el pallet no esté duplicado en el viaje actual                  
            if (PalletsEscaneados.Any(p => p.NumeroPallet == EntradaNumeroPallet))
            {
                _logger.LogWarning("Pallet duplicado detectado: {NumeroPallet}", EntradaNumeroPallet);
                var errorMsg = $"El pallet {EntradaNumeroPallet} ya fue escaneado en este viaje.";

                // NUEVO: Enviar error al dispositivo móvil si está procesando desde móvil        
                if (!string.IsNullOrEmpty(_currentDeviceProcessing))
                {
                    await _signalRService.SendPalletErrorToMobileAsync(
                        ViajeActivo.ViajeId.ToString(), errorMsg, _currentDeviceProcessing);
                }
                else
                {
                    MessageBox.Show(errorMsg, "Pallet Duplicado",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                EntradaNumeroPallet = string.Empty;
                return;
            }

            // VALIDACIÓN: Verificar que el pallet no haya sido enviado en otro viaje                  
            if (_accesoDatosViajes.PalletYaFueEnviado(EntradaNumeroPallet))
            {
                _logger.LogWarning("Pallet ya enviado en otro viaje: {NumeroPallet}", EntradaNumeroPallet);
                var errorMsg = $"El pallet {EntradaNumeroPallet} ya fue enviado en otro viaje o esta en un viaje activo y no puede ser reutilizado.";

                // NUEVO: Enviar error al dispositivo móvil si está procesando desde móvil        
                if (!string.IsNullOrEmpty(_currentDeviceProcessing))
                {
                    await _signalRService.SendPalletErrorToMobileAsync(
                        ViajeActivo.ViajeId.ToString(), errorMsg, _currentDeviceProcessing);
                }
                else
                {
                    MessageBox.Show(errorMsg, "Pallet Ya Enviado",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }

                EntradaNumeroPallet = string.Empty;
                return;
            }

            InformacionPallet pallet = _accesoDatos.ObtenerDatosPallet(EntradaNumeroPallet);

            if (pallet != null)
            {
                _logger.LogInfo("Pallet encontrado: {NumeroPallet} - {Variedad}", pallet.NumeroPallet, pallet.Variedad);

                // Guardar datos originales para tracking de modificaciones                  
                pallet.VariedadOriginal = pallet.Variedad;
                pallet.CalibreOriginal = pallet.Calibre;
                pallet.EmbalajeOriginal = pallet.Embalaje;
                pallet.NumeroDeCajasOriginal = pallet.NumeroDeCajas;

                // NUEVO: Detectar si es pallet bicolor       
                if (_accesoDatosEmbalajeBicolor.EsEmbalajeBicolor(pallet.Embalaje))
                {
                    _logger.LogInfo("🎯 Pallet bicolor detectado: {NumeroPallet} - Embalaje: {Embalaje}",
                    pallet.NumeroPallet, pallet.Embalaje);

                    // Marcar como bicolor      
                    pallet.EsBicolor = true;

                    // Guardar datos originales para campos bicolor      
                    pallet.SegundaVariedadOriginal = pallet.SegundaVariedad;
                    pallet.CajasSegundaVariedadOriginal = pallet.CajasSegundaVariedad;
                }

                // ✅ LÓGICA RESTAURADA: Verificar si es pallet completo y usar TotalCajasFichaTecnica            
                var pesoEmbalaje = _accesoDatosViajes.ObtenerPesoEmbalaje(pallet.Embalaje);
                if (pesoEmbalaje != null && pesoEmbalaje.PesoUnitario > 0)
                {
                    pallet.PesoUnitario = pesoEmbalaje.PesoUnitario;

                    // ✅ LÓGICA DE PALLET COMPLETO: Si el pallet es PC            
                    bool esPalletCompleto = pallet.EsPC;

                    if (esPalletCompleto && pesoEmbalaje.TotalCajasFichaTecnica.HasValue)
                    {
                        _logger.LogInfo("Pallet completo detectado: {NumeroPallet}, usando {TotalCajas} cajas de ficha técnica",
                                      pallet.NumeroPallet, pesoEmbalaje.TotalCajasFichaTecnica.Value);

                        // ✅ CAMBIO: Para PC (bicolor o monocolor), usar ficha técnica directamente  
                        int totalCajasFicha = pesoEmbalaje.TotalCajasFichaTecnica.Value;

                        // Ya no distribuimos cajas para bicolor - usamos el total directamente en NumeroDeCajas  
                        pallet.NumeroDeCajas = totalCajasFicha;
                        pallet.PesoTotal = totalCajasFicha * pesoEmbalaje.PesoUnitario;

                        if (pallet.EsBicolor)
                        {
                            _logger.LogInfo("Pallet PC bicolor: {NumeroPallet} - {TotalCajas} cajas totales (sin distribución)",
                                          pallet.NumeroPallet, totalCajasFicha);
                        }
                    }
                    else
                    {
                        // ✅ CAMBIO: Para pallets normales (no PC), bicolor y monocolor usan la misma lógica  
                        // Ya no sumamos CajasSegundaVariedad para bicolor - solo usamos NumeroDeCajas  
                        pallet.PesoTotal = pallet.NumeroDeCajas * pesoEmbalaje.PesoUnitario;

                        _logger.LogInfo("Peso calculado para pallet {TipoPallet}: {NumeroCajas} cajas = {PesoTotal} kg",
                                      pallet.EsBicolor ? "bicolor" : "normal",
                                      pallet.NumeroDeCajas,
                                      pallet.PesoTotal);
                    }
                }
                else
                {
                    _logger.LogWarning("Embalaje sin peso/cajas válidos: {Embalaje} (PesoUnitario={Peso}). Pallet NO será registrado.",
                        pallet.Embalaje, pesoEmbalaje?.PesoUnitario ?? 0);

                    ValidarEmbalajeEnGestionPesos(
                        pallet.Embalaje,
                        pallet.TipoPallet,
                        pallet.EsPC,
                        esEdicion: false,
                        out string errorMsg);

                    if (!string.IsNullOrEmpty(_currentDeviceProcessing))
                    {
                        await _signalRService.SendPalletErrorToMobileAsync(
                            ViajeActivo.ViajeId.ToString(), errorMsg, _currentDeviceProcessing);
                    }
                    else
                    {
                        MessageBox.Show(errorMsg, "Embalaje Sin Configuración",
                                       MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    EntradaNumeroPallet = string.Empty;
                    return;
                }

                // Asegurar que FechaModificacion tenga un valor              
                if (pallet.FechaModificacion == null)
                {
                    pallet.FechaModificacion = FechaOperacionalHelper.ObtenerFechaOperacionalActual();
                }

                // ✅ GUARDADO AUTOMÁTICO: Guardar inmediatamente en BD              
                try
                {
                    _accesoDatosViajes.GuardarPalletViaje(pallet, ViajeActivo.ViajeId);
                    _logger.LogInfo("Pallet guardado exitosamente en BD: {NumeroPallet}", pallet.NumeroPallet);

                    // Solo agregar a la colección si se guardó exitosamente              
                    PalletsEscaneados.Add(pallet);
                    UltimoPalletEscaneado = pallet;
                    EntradaNumeroPallet = string.Empty;

                    // NUEVO: Notificar cambios considerando pallets bicolor      
                    OnPropertyChanged(nameof(TotalCajas));
                    OnPropertyChanged(nameof(PesoTotalViaje));

                    OnPropertyChanged(nameof(TotalPC));
                    OnPropertyChanged(nameof(TotalPH));
                    OnPropertyChanged(nameof(TotalCT));
                    OnPropertyChanged(nameof(TotalEN));

                    // NUEVO: Sincronizar escaneo con APK        
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _signalRService.BroadcastPalletListUpdateAsync(ViajeActivo.ViajeId.ToString(), PalletsEscaneados.ToList());
                            _logger.LogInfo("📤 Lista actualizada broadcast enviada después de escanear pallet");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error enviando broadcast después de escanear: {ErrorMessage}", ex.Message);
                        }
                    });

                    // NUEVO: Enviar resultado exitoso al dispositivo móvil si está procesando desde móvil        
                    if (!string.IsNullOrEmpty(_currentDeviceProcessing))
                    {
                        await _signalRService.SendPalletProcessedToMobileAsync(
                            ViajeActivo.ViajeId.ToString(), pallet, _currentDeviceProcessing);

                        _logger.LogInfo("Resultado de pallet enviado al dispositivo móvil: {DeviceId}", _currentDeviceProcessing);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al guardar pallet {NumeroPallet}: {ErrorMessage}",
                                    pallet.NumeroPallet, ex.Message);

                    var errorMsg = $"Error al guardar pallet en la base de datos: {ex.Message}";

                    // NUEVO: Enviar error al dispositivo móvil si está procesando desde móvil        
                    if (!string.IsNullOrEmpty(_currentDeviceProcessing))
                    {
                        await _signalRService.SendPalletErrorToMobileAsync(
                            ViajeActivo.ViajeId.ToString(), errorMsg, _currentDeviceProcessing);
                    }
                    else
                    {
                        MessageBox.Show(errorMsg, "Error de Guardado",
                                       MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                _logger.LogWarning("Pallet no encontrado: {NumeroPallet}", EntradaNumeroPallet);
                var errorMsg = "El número de pallet no se encontró en la base de datos.";

                // NUEVO: Enviar error al dispositivo móvil si está procesando desde móvil        
                if (!string.IsNullOrEmpty(_currentDeviceProcessing))
                {
                    await _signalRService.SendPalletErrorToMobileAsync(
                        ViajeActivo?.ViajeId.ToString() ?? "0", errorMsg, _currentDeviceProcessing);
                }
                else
                {
                    MessageBox.Show(errorMsg, "Error de Búsqueda",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        // PARTE 4: Métodos de Finalización y Gestión de Viajes    
        private void FinalizarDespacho(object parameter)
        {
            _logger.LogInfo("Iniciando finalización de despacho para viaje #{NumeroViaje}", ViajeActivo?.NumeroViaje);

            if (!TieneViajeActivo || ViajeActivo == null)
            {
                _logger.LogWarning("Intento de finalizar despacho sin viaje activo");
                MessageBox.Show("No hay viaje activo para finalizar.", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (PalletsEscaneados.Count > 0)
            {
                try
                {
                    // ✅ CRÍTICO: Capturar referencias locales antes de operaciones async    
                    var viajeParaFinalizar = ViajeActivo;
                    var palletsParaImprimir = new List<InformacionPallet>(PalletsEscaneados);

                    viajeParaFinalizar.Estado = "Finalizado";
                    viajeParaFinalizar.FechaModificacion = FechaOperacionalHelper.ObtenerFechaOperacionalActual();
                    _accesoDatosViajes.ActualizarViaje(viajeParaFinalizar);

                    // NUEVO: Marcar todos los pallets del viaje como enviados  
                    _accesoDatosViajes.MarcarPalletsComoEnviados(viajeParaFinalizar.ViajeId, Environment.UserName);

                    // Notificación SignalR sin bloquear    
                    if (_signalRService != null)
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await _signalRService.NotifyTripFinalizedAsync(viajeParaFinalizar.ViajeId.ToString());
                                _logger.LogInfo("Notificación SignalR enviada exitosamente para viaje #{NumeroViaje}",
                                               viajeParaFinalizar.NumeroViaje);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error notificando finalización: {ErrorMessage}", ex.Message);
                            }
                        });
                    }

                    _logger.LogInfo("Despacho finalizado exitosamente - Viaje #{NumeroViaje}, {TotalPallets} pallets",
                                  viajeParaFinalizar.NumeroViaje, palletsParaImprimir.Count);

                    MessageBox.Show($"Despacho finalizado con éxito. Se registraron {palletsParaImprimir.Count} pallets." +
                                   $"Total de cajas: {palletsParaImprimir.Sum(p => p.NumeroDeCajas)}n" +
                                   $"Peso total: {palletsParaImprimir.Sum(p => p.PesoTotal):F3} kg",
                                   "Despacho Completo", MessageBoxButton.OK, MessageBoxImage.Information);

                    // ✅ USAR DATOS CAPTURADOS: Usar las referencias locales para la impresión    
                    AbrirImpresionDespacho(viajeParaFinalizar, palletsParaImprimir);

                    // Limpiar datos DESPUÉS de abrir la ventana de impresión    
                    PalletsEscaneados.Clear();
                    UltimoPalletEscaneado = null;
                    PalletSeleccionado = null;
                    ViajeActivo = null;

                    OnPropertyChanged(nameof(TotalCajas));
                    OnPropertyChanged(nameof(PesoTotalViaje));

                    OnPropertyChanged(nameof(TotalPC));
                    OnPropertyChanged(nameof(TotalPH));
                    OnPropertyChanged(nameof(TotalCT));
                    OnPropertyChanged(nameof(TotalEN));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al finalizar despacho para viaje #{NumeroViaje}: {ErrorMessage}",
                                    ViajeActivo?.NumeroViaje, ex.Message);
                    MessageBox.Show($"Error al finalizar el viaje: {ex.Message}",
                                   "Error al Finalizar", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                _logger.LogWarning("Intento de finalizar despacho sin pallets");
                MessageBox.Show("No hay pallets registrados para finalizar el despacho.", "Advertencia",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void AbrirImpresionDespacho(Viaje viaje, List<InformacionPallet> pallets)
        {
            _logger.LogInfo("Abriendo ventana de impresión para viaje #{NumeroViaje}", viaje.NumeroViaje);

            try
            {
                var ventanaImpresion = new ImpresionDespachoWindow(viaje, pallets);
                ventanaImpresion.ShowDialog();

                _logger.LogInfo("Ventana de impresión cerrada para viaje #{NumeroViaje}", viaje.NumeroViaje);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error al abrir ventana de impresión para viaje #{NumeroViaje}: {ErrorMessage}",
                               viaje.NumeroViaje, ex.Message, ex);
                MessageBox.Show($"Error al abrir la ventana de impresión: {ex.Message}",
                               "Error de Impresión", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ContinuarViaje(object parameter)
        {
            _logger.LogInfo("Iniciando continuación de viaje activo");

            var viajesActivos = _accesoDatosViajes.ObtenerViajesActivos();

            if (viajesActivos.Count == 0)
            {
                _logger.LogInfo("No hay viajes activos disponibles para continuar");
                MessageBox.Show("No hay viajes activos para continuar.", "Sin Viajes Activos",
                               MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Crear ventana de selección de viajes activos      
            var ventanaSeleccion = new SeleccionViajeActivoWindow(viajesActivos);
            if (ventanaSeleccion.ShowDialog() == true && ventanaSeleccion.ViajeSeleccionado != null)
            {
                ViajeActivo = ventanaSeleccion.ViajeSeleccionado;
                _logger.LogInfo("Viaje seleccionado para continuar: #{NumeroViaje}", ViajeActivo.NumeroViaje);

                // Cargar pallets existentes del viaje      
                var palletsExistentes = _accesoDatosViajes.ObtenerPalletsDeViaje(ViajeActivo.ViajeId);

                PalletsEscaneados.Clear();
                foreach (var pallet in palletsExistentes)
                {
                    PalletsEscaneados.Add(pallet);
                }

                UltimoPalletEscaneado = PalletsEscaneados.LastOrDefault();
                PalletSeleccionado = null;

                OnPropertyChanged(nameof(TotalCajas));
                OnPropertyChanged(nameof(PesoTotalViaje));

                OnPropertyChanged(nameof(TotalPC));
                OnPropertyChanged(nameof(TotalPH));
                OnPropertyChanged(nameof(TotalCT));
                OnPropertyChanged(nameof(TotalEN));

                _logger.LogInfo("Viaje #{NumeroViaje} cargado con {TotalPallets} pallets",
                              ViajeActivo.NumeroViaje, PalletsEscaneados.Count);
                MessageBox.Show($"Viaje #{ViajeActivo.NumeroViaje} cargado con {PalletsEscaneados.Count} pallets.",
                               "Viaje Continuado", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private bool PuedeFinalizarDespacho(object parameter)
        {
            return TieneViajeActivo && PalletsEscaneados.Count > 0;
        }

        private void NuevoViaje(object parameter)
        {
            _logger.LogInfo("Iniciando creación de nuevo viaje");

            var ventanaRegistro = new RegistroViajeWindow();
            if (ventanaRegistro.ShowDialog() == true && ventanaRegistro.ViajeGuardado)
            {
                ViajeActivo = ventanaRegistro.ViajeCreado;
                PalletsEscaneados.Clear();
                UltimoPalletEscaneado = null;
                PalletSeleccionado = null;

                OnPropertyChanged(nameof(TotalCajas));
                OnPropertyChanged(nameof(PesoTotalViaje));

                // ✅ Capturar referencia local  
                var viajeCreado = ViajeActivo;
                _logger.LogInfo("Nuevo viaje creado: #{NumeroViaje}", viajeCreado.NumeroViaje);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _signalRService.NotifyTripCreatedAsync(viajeCreado);
                        await _signalRService.NotifyActiveTripAsync(viajeCreado.ViajeId.ToString(), viajeCreado);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error notificando nuevo viaje: {ErrorMessage}", ex.Message);
                    }
                });
            }
        }

        private void EditarViaje(object parameter)
        {
            if (ViajeActivo != null)
            {
                _logger.LogInfo("Iniciando edición de viaje: #{NumeroViaje}", ViajeActivo.NumeroViaje);

                var ventanaRegistro = new RegistroViajeWindow(ViajeActivo);
                if (ventanaRegistro.ShowDialog() == true && ventanaRegistro.ViajeGuardado)
                {
                    ViajeActivo = ventanaRegistro.ViajeCreado;
                    _logger.LogInfo("Viaje editado exitosamente: #{NumeroViaje}", ViajeActivo.NumeroViaje);
                    // NUEVO: Sincronizar edición de viaje con APK  
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var tripData = new
                            {
                                tripId = ViajeActivo.ViajeId.ToString(),
                                numeroViaje = ViajeActivo.NumeroViaje,
                                numeroGuia = ViajeActivo.NumeroGuia,
                                responsable = ViajeActivo.Responsable,
                                fecha = ViajeActivo.Fecha.ToString("dd/MM/yyyy"),
                                estado = ViajeActivo.Estado
                            };

                            await _signalRService.NotifyActiveTripAsync(ViajeActivo.ViajeId.ToString(), tripData);
                            _logger.LogInfo("📤 Edición de viaje sincronizada con APK");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error sincronizando edición de viaje: {ErrorMessage}", ex.Message);
                        }
                    });
                }
            }
        }
        // PARTE 5: Métodos de Edición de Pallets    
        private void CargarPalletParaEdicion(InformacionPallet pallet)
        {
            UltimoPalletEscaneado = new InformacionPallet
            {
                NumeroPallet = pallet.NumeroPallet,
                Variedad = pallet.Variedad,
                Calibre = pallet.Calibre,
                Embalaje = pallet.Embalaje,
                NumeroDeCajas = pallet.NumeroDeCajas,
                PesoUnitario = pallet.PesoUnitario,
                PesoTotal = pallet.PesoTotal,

                // NUEVO: Campos bicolor  
                EsBicolor = pallet.EsBicolor,
                SegundaVariedad = pallet.SegundaVariedad,
                CajasSegundaVariedad = pallet.CajasSegundaVariedad,

                // Preservar valores originales  
                VariedadOriginal = pallet.VariedadOriginal ?? pallet.Variedad,
                CalibreOriginal = pallet.CalibreOriginal ?? pallet.Calibre,
                EmbalajeOriginal = pallet.EmbalajeOriginal ?? pallet.Embalaje,
                NumeroDeCajasOriginal = pallet.NumeroDeCajasOriginal != 0 ? pallet.NumeroDeCajasOriginal : pallet.NumeroDeCajas,

                // NUEVO: Valores originales bicolor  
                SegundaVariedadOriginal = pallet.SegundaVariedadOriginal ?? pallet.SegundaVariedad,
                CajasSegundaVariedadOriginal = pallet.CajasSegundaVariedadOriginal != 0 ? pallet.CajasSegundaVariedadOriginal : pallet.CajasSegundaVariedad,

                Modificado = pallet.Modificado,
                FechaModificacion = pallet.FechaModificacion
            };

            _logger.LogInfo("Pallet cargado para edición: {NumeroPallet} - Bicolor: {EsBicolor}", pallet.NumeroPallet, pallet.EsBicolor);
        }

        private void AplicarCambios(object parameter)
        {
            if (PalletSeleccionado != null && UltimoPalletEscaneado != null)
            {
                _logger.LogInfo("Aplicando cambios al pallet: {NumeroPallet}", UltimoPalletEscaneado.NumeroPallet);

                // ✅ CAMBIO: Comparar con valores originales sin incluir CajasSegundaVariedad  
                bool huboModificacion =
                    UltimoPalletEscaneado.Variedad != UltimoPalletEscaneado.VariedadOriginal ||
                    UltimoPalletEscaneado.Calibre != UltimoPalletEscaneado.CalibreOriginal ||
                    UltimoPalletEscaneado.Embalaje != UltimoPalletEscaneado.EmbalajeOriginal ||
                    UltimoPalletEscaneado.NumeroDeCajas != UltimoPalletEscaneado.NumeroDeCajasOriginal;

                // ✅ CAMBIO: Para bicolor, solo verificar cambio en SegundaVariedad (no en cajas)  
                if (UltimoPalletEscaneado.EsBicolor)
                {
                    huboModificacion = huboModificacion ||
                        UltimoPalletEscaneado.SegundaVariedad != UltimoPalletEscaneado.SegundaVariedadOriginal;
                }

                _logger.LogDebug("Modificación detectada: {HuboModificacion}", huboModificacion);

                // Validar/recalcular solo si cambió embalaje o cajas.
                // Todos los tipos (PC, PH, CT, EN): embalaje debe existir en Gestión de Pesos.
                // Solo PC usa ficha técnica en RecalcularPesoPallet; PH/CT/EN conservan cajas del operador.
                if (UltimoPalletEscaneado.Embalaje != UltimoPalletEscaneado.EmbalajeOriginal ||
                    UltimoPalletEscaneado.NumeroDeCajas != UltimoPalletEscaneado.NumeroDeCajasOriginal)
                {
                    if (!ValidarEmbalajeParaEdicion(UltimoPalletEscaneado, out string errorEmbalaje))
                    {
                        UltimoPalletEscaneado.Embalaje = UltimoPalletEscaneado.EmbalajeOriginal ?? PalletSeleccionado.Embalaje;
                        UltimoPalletEscaneado.NumeroDeCajas = UltimoPalletEscaneado.NumeroDeCajasOriginal != 0
                            ? UltimoPalletEscaneado.NumeroDeCajasOriginal
                            : PalletSeleccionado.NumeroDeCajas;
                        UltimoPalletEscaneado.PesoUnitario = PalletSeleccionado.PesoUnitario;
                        UltimoPalletEscaneado.PesoTotal = PalletSeleccionado.PesoTotal;
                        OnPropertyChanged(nameof(UltimoPalletEscaneado));
                        NotificarErrorEmbalajeEdicion(errorEmbalaje);
                        return;
                    }

                    RecalcularPesoPallet(UltimoPalletEscaneado);
                }

                var index = PalletsEscaneados.IndexOf(PalletSeleccionado);
                if (index >= 0)
                {
                    // ✅ PRESERVAR: Estado de modificación correctamente              
                    UltimoPalletEscaneado.Modificado = huboModificacion;
                    UltimoPalletEscaneado.FechaModificacion = huboModificacion ? DateTime.Now : PalletSeleccionado.FechaModificacion;

                    // ✅ AGREGAR: Actualizar en BD después de modificar            
                    try
                    {
                        _accesoDatosViajes.ActualizarPalletViaje(UltimoPalletEscaneado, ViajeActivo.ViajeId);
                        _logger.LogInfo("Pallet actualizado exitosamente en BD: {NumeroPallet}", UltimoPalletEscaneado.NumeroPallet);

                        PalletsEscaneados[index] = UltimoPalletEscaneado;
                        PalletSeleccionado = UltimoPalletEscaneado;

                        // Cargar pallets existentes del viaje desde la base          
                        var palletsExistentes = _accesoDatosViajes.ObtenerPalletsDeViaje(ViajeActivo.ViajeId);

                        PalletsEscaneados.Clear();
                        foreach (var pallet in palletsExistentes)
                        {
                            PalletsEscaneados.Add(pallet);
                        }

                        OnPropertyChanged(nameof(TotalCajas));
                        OnPropertyChanged(nameof(PesoTotalViaje));
                        OnPropertyChanged(nameof(TotalPC));
                        OnPropertyChanged(nameof(TotalPH));
                        OnPropertyChanged(nameof(TotalCT));
                        OnPropertyChanged(nameof(TotalEN));

                        // NUEVO: Sincronizar cambios con APK      
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await _signalRService.BroadcastPalletListUpdateAsync(ViajeActivo.ViajeId.ToString(), PalletsEscaneados.ToList());
                                _logger.LogInfo("📤 Lista actualizada broadcast enviada después de aplicar cambios");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error enviando broadcast después de aplicar cambios: {ErrorMessage}", ex.Message);
                            }
                        });

                        // NUEVO: Mensaje específico para pallets bicolor    
                        string mensaje = huboModificacion ?
                            UltimoPalletEscaneado.EsBicolor ? "Cambios aplicados. Pallet bicolor marcado como modificado." : "Cambios aplicados. Pallet marcado como modificado." :
                            "Cambios aplicados.";

                        MessageBox.Show(mensaje, "Edición", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Error al actualizar pallet {NumeroPallet}: {ErrorMessage}",
                                       UltimoPalletEscaneado.NumeroPallet, ex.Message, ex);
                        MessageBox.Show($"Error al actualizar pallet: {ex.Message}", "Error",
                                       MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }

        private void RevertirCambios(object parameter)
        {
            if (PalletSeleccionado != null)
            {
                _logger.LogInfo("Iniciando reversión de cambios para pallet: {NumeroPallet}", PalletSeleccionado.NumeroPallet);

                // ✅ PASO 1: Mostrar primero los datos originales en la UI            
                UltimoPalletEscaneado = new InformacionPallet
                {
                    NumeroPallet = PalletSeleccionado.NumeroPallet,
                    Variedad = PalletSeleccionado.VariedadOriginal ?? PalletSeleccionado.Variedad,
                    Calibre = PalletSeleccionado.CalibreOriginal ?? PalletSeleccionado.Calibre,
                    Embalaje = PalletSeleccionado.EmbalajeOriginal ?? PalletSeleccionado.Embalaje,
                    NumeroDeCajas = PalletSeleccionado.NumeroDeCajasOriginal != 0 ?
                                   PalletSeleccionado.NumeroDeCajasOriginal : PalletSeleccionado.NumeroDeCajas,

                    // ✅ CAMBIO: Revertir solo SegundaVariedad (no CajasSegundaVariedad)  
                    EsBicolor = PalletSeleccionado.EsBicolor,
                    SegundaVariedad = PalletSeleccionado.SegundaVariedadOriginal ?? PalletSeleccionado.SegundaVariedad,
                    // Mantener CajasSegundaVariedad por compatibilidad BD, pero ya no se usa en lógica  
                    CajasSegundaVariedad = PalletSeleccionado.CajasSegundaVariedadOriginal != 0 ?
                                          PalletSeleccionado.CajasSegundaVariedadOriginal : PalletSeleccionado.CajasSegundaVariedad,

                    // ✅ PRESERVAR: Referencias originales              
                    VariedadOriginal = PalletSeleccionado.VariedadOriginal,
                    CalibreOriginal = PalletSeleccionado.CalibreOriginal,
                    EmbalajeOriginal = PalletSeleccionado.EmbalajeOriginal,
                    NumeroDeCajasOriginal = PalletSeleccionado.NumeroDeCajasOriginal,

                    // ✅ CAMBIO: Preservar referencias originales de campos bicolor (por compatibilidad)  
                    SegundaVariedadOriginal = PalletSeleccionado.SegundaVariedadOriginal,
                    CajasSegundaVariedadOriginal = PalletSeleccionado.CajasSegundaVariedadOriginal,

                    // ✅ RESETEAR: Estado de modificación a false al revertir              
                    Modificado = false,
                    FechaModificacion = PalletSeleccionado.FechaModificacion
                };

                // ✅ RECALCULAR: Peso con valores originales (ahora usa solo NumeroDeCajas)  
                RecalcularPesoPallet(UltimoPalletEscaneado);

                // ✅ NOTIFICAR: Actualizar la UI para mostrar los datos originales            
                OnPropertyChanged(nameof(UltimoPalletEscaneado));

                // ✅ PASO 2: Ahora pedir confirmación para guardar            
                string tipoMensaje = UltimoPalletEscaneado.EsBicolor ? "bicolor " : "";
                var resultado = MessageBox.Show(
                    $"Se han mostrado los valores originales del pallet {tipoMensaje}{PalletSeleccionado.NumeroPallet}. ¿Desea guardar estos cambios en la base de datos?",
                    "Confirmar Guardado de Reversión",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    // ✅ PASO 3: Guardar en BD solo si confirma            
                    var index = PalletsEscaneados.IndexOf(PalletSeleccionado);
                    if (index >= 0)
                    {
                        try
                        {
                            _accesoDatosViajes.ActualizarPalletViaje(UltimoPalletEscaneado, ViajeActivo.ViajeId);
                            _logger.LogInfo("Cambios revertidos y guardados para pallet: {NumeroPallet}", UltimoPalletEscaneado.NumeroPallet);

                            PalletsEscaneados[index] = UltimoPalletEscaneado;
                            PalletSeleccionado = UltimoPalletEscaneado;

                            // NUEVO: Recargar desde BD para mantener consistencia con AplicarCambios    
                            var palletsExistentes = _accesoDatosViajes.ObtenerPalletsDeViaje(ViajeActivo.ViajeId);
                            PalletsEscaneados.Clear();
                            foreach (var pallet in palletsExistentes)
                            {
                                PalletsEscaneados.Add(pallet);
                            }

                            // Notificar totales después de recargar    
                            OnPropertyChanged(nameof(TotalCajas));
                            OnPropertyChanged(nameof(PesoTotalViaje));
                            OnPropertyChanged(nameof(TotalPC));
                            OnPropertyChanged(nameof(TotalPH));
                            OnPropertyChanged(nameof(TotalCT));
                            OnPropertyChanged(nameof(TotalEN));

                            // NUEVO: Sincronizar reversión con APK      
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    await _signalRService.BroadcastPalletListUpdateAsync(ViajeActivo.ViajeId.ToString(), PalletsEscaneados.ToList());
                                    _logger.LogInfo("📤 Lista actualizada broadcast enviada después de revertir cambios");
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Error enviando broadcast después de revertir: {ErrorMessage}", ex.Message);
                                }
                            });

                            MessageBox.Show("Cambios revertidos y guardados exitosamente.", "Reversión Completada",
                                           MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("Error al guardar cambios revertidos para pallet {NumeroPallet}: {ErrorMessage}",
                                           UltimoPalletEscaneado.NumeroPallet, ex.Message, ex);
                            MessageBox.Show($"Error al guardar cambios revertidos: {ex.Message}", "Error",
                                           MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
                else
                {
                    // ✅ PASO 4: Si cancela, restaurar los valores modificados            
                    CargarPalletParaEdicion(PalletSeleccionado);
                    _logger.LogInfo("Reversión cancelada para pallet: {NumeroPallet}", PalletSeleccionado.NumeroPallet);
                    MessageBox.Show("Reversión cancelada. Se mantienen los valores modificados.", "Operación Cancelada",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        // PARTE 6: Métodos de Eliminación y Finalización    
        private void EliminarPallet(object parameter)
        {
            if (PalletSeleccionado != null)
            {
                _logger.LogInfo("Iniciando eliminación de pallet: {NumeroPallet}", PalletSeleccionado.NumeroPallet);

                var resultado = MessageBox.Show($"¿Está seguro de eliminar el pallet {PalletSeleccionado.NumeroPallet}?",
                                               "Confirmar Eliminación", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    // ✅ AGREGAR: Eliminar de BD primero        
                    try
                    {
                        _accesoDatosViajes.EliminarPalletViaje(PalletSeleccionado.NumeroPallet, ViajeActivo.ViajeId);
                        _logger.LogInfo("Pallet eliminado exitosamente de BD: {NumeroPallet}", PalletSeleccionado.NumeroPallet);

                        // Luego eliminar de la colección        
                        PalletsEscaneados.Remove(PalletSeleccionado);
                        PalletSeleccionado = null;
                        UltimoPalletEscaneado = null;

                        // Actualizar totales          
                        OnPropertyChanged(nameof(TotalCajas));
                        OnPropertyChanged(nameof(PesoTotalViaje));

                        OnPropertyChanged(nameof(TotalPC));
                        OnPropertyChanged(nameof(TotalPH));
                        OnPropertyChanged(nameof(TotalCT));
                        OnPropertyChanged(nameof(TotalEN));

                        // NUEVO: Sincronizar eliminación con APK  
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await _signalRService.BroadcastPalletListUpdateAsync(ViajeActivo.ViajeId.ToString(), PalletsEscaneados.ToList());
                                _logger.LogInfo("📤 Lista actualizada broadcast enviada después de eliminar pallet");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error enviando broadcast después de eliminar: {ErrorMessage}", ex.Message);
                            }
                        });

                        MessageBox.Show("Pallet eliminado exitosamente.", "Eliminación",
                                       MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Error al eliminar pallet {NumeroPallet}: {ErrorMessage}",
                                       PalletSeleccionado.NumeroPallet, ex.Message, ex);
                        MessageBox.Show($"Error al eliminar pallet: {ex.Message}", "Error",
                                       MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    _logger.LogInfo("Eliminación de pallet cancelada por el usuario: {NumeroPallet}", PalletSeleccionado.NumeroPallet);
                }
            }
        }

        public void ActualizarTotales()
        {
            _logger.LogDebug("Actualizando totales de cajas y peso");
            OnPropertyChanged(nameof(TotalCajas));
            OnPropertyChanged(nameof(PesoTotalViaje));

            // NUEVO: Agregar notificaciones para contadores PC/PH/CT  
            OnPropertyChanged(nameof(TotalPC));
            OnPropertyChanged(nameof(TotalPH));
            OnPropertyChanged(nameof(TotalCT));
            OnPropertyChanged(nameof(TotalEN));
        }
        public List<ReporteGeneralPallet> GenerarReporteGeneralConCTEN()
        {
            return PalletsEscaneados.Select(p => new ReporteGeneralPallet
            {
                
                NumeroPallet = p.NumeroPallet,
                Variedad = p.VariedadParaReporte,
                Calibre = p.Calibre,
                Embalaje = p.Embalaje,
                NumeroDeCajas = p.CajasParaReporte,
                PesoUnitario = p.PesoUnitario,
                PesoTotal = p.PesoTotal,
                FechaEscaneo = p.FechaEscaneo,
                Modificado = p.Modificado,
                // Campos bicolor  
                EsBicolor = p.EsBicolor,
                SegundaVariedad = p.SegundaVariedad,
                CajasSegundaVariedad = p.CajasSegundaVariedad
            }).ToList();
        }
        public List<ResumenPorVariedad> CrearResumenVariedadConTipos()
        {
            return PalletsEscaneados
                .GroupBy(p => p.VariedadParaReporte)
                .Select(g => new ResumenPorVariedad
                {
                    Variedad = g.Key,
                    TotalCajas = g.Sum(p => p.CajasParaReporte),
                    TotalKilos = g.Sum(p => p.PesoTotal),
                    TotalPallets = g.Count(),
                    // NUEVO: Contadores específicos por tipo  
                    TotalCT = g.Count(p => p.EsCT),
                    TotalEN = g.Count(p => p.EsEN),
                    DetallesPorEmbalaje = g.GroupBy(p => p.Embalaje)
                        .Select(embalajeGroup => new ResumenVariedadEmbalaje
                        {
                            VariedadEmbalaje = $"{g.Key} - {embalajeGroup.Key}",
                            TotalCajas = embalajeGroup.Sum(p => p.CajasParaReporte),
                            TotalKilos = embalajeGroup.Sum(p => p.PesoTotal)
                        })
                        .OrderBy(e => e.VariedadEmbalaje)
                        .ToList()
                })
                .OrderBy(r => r.Variedad)
                .ToList();
        }
        public Dictionary<string, int> CalcularTotalesPorTipo()
        {
            return new Dictionary<string, int>
            {
                ["PC"] = TotalPC,
                ["PH"] = TotalPH,
                ["CT"] = TotalCT,
                ["EN"] = TotalEN,
                ["Total"] = PalletsEscaneados.Count
            };
        }
        // MÉTODOS DE SIGNALR ACTUALIZADOS con mejoras de feedback  
        private void OnPalletScannedFromMobile(object palletData)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // SUGERENCIA DE MEJORA: Mostrar notificación más informativa  
                MessageBox.Show($"✅ Pallet procesado exitosamente desde dispositivo móvil:\n{palletData}",
                               "Escaneo Remoto Exitoso", MessageBoxButton.OK, MessageBoxImage.Information);

                // Recargar datos para mostrar cambios    
                if (ViajeActivo != null)
                {
                    // Actualizar totales automáticamente  
                    OnPropertyChanged(nameof(TotalCajas));
                    OnPropertyChanged(nameof(PesoTotalViaje));
                }
            });
        }

        private void OnPalletUpdatedFromMobile(object palletData)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show($"📝 Pallet actualizado desde dispositivo móvil:\n{palletData}",
                               "Actualización Remota", MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }

        private void OnPalletDeletedFromMobile(string palletNumber)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show($"🗑️ Pallet eliminado desde dispositivo móvil: {palletNumber}",
                               "Eliminación Remota", MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }

        // NUEVO: Manejadores de sincronización de viajes  
        private void OnActiveTripChangedFromRemote(string tripId, object tripData)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    // Solo actualizar si no es el viaje actual  
                    if (ViajeActivo?.ViajeId.ToString() != tripId)
                    {
                        _logger.LogInfo("🔄 Sincronizando viaje activo desde remoto: {TripId}", tripId);

                        MessageBox.Show($"📱 Viaje activo sincronizado desde dispositivo móvil:\nViaje #{tripId}",
                                       "Sincronización de Viaje", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sincronizando viaje activo: {ErrorMessage}", ex.Message);
                }
            });
        }

        private void OnNewTripCreatedFromRemote(object tripData)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show("🆕 Nuevo viaje creado desde dispositivo móvil",
                               "Nuevo Viaje Remoto", MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }

        private void OnTripFinalizedFromRemote(string tripId)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (ViajeActivo?.ViajeId.ToString() == tripId)
                {
                    _logger.LogInfo("🏁 Viaje actual finalizado desde remoto: {TripId}", tripId);

                    MessageBox.Show("🏁 El viaje activo ha sido finalizado desde un dispositivo móvil",
                                   "Viaje Finalizado", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Limpiar viaje activo  
                    ViajeActivo = null;
                    PalletsEscaneados.Clear();
                    UltimoPalletEscaneado = null;
                    PalletSeleccionado = null;

                    OnPropertyChanged(nameof(TotalCajas));
                    OnPropertyChanged(nameof(PesoTotalViaje));

                    OnPropertyChanged(nameof(TotalPC));
                    OnPropertyChanged(nameof(TotalPH));
                    OnPropertyChanged(nameof(TotalCT));
                    OnPropertyChanged(nameof(TotalEN));
                }
            });
        }

        // REEMPLAZAR el método OnActiveTripRequestedFromRemote:  
        private void OnActiveTripRequestedFromRemote(string deviceId)
        {
            Application.Current.Dispatcher.Invoke(async () =>
            {
                var viajeActual = ViajeActivo;
                _logger.LogInfo("📤 Solicitud de viaje activo recibida desde dispositivo: {DeviceId}", deviceId);

                if (viajeActual != null)
                {
                    _logger.LogInfo("📤 Enviando viaje activo con pallets a dispositivo móvil: {DeviceId} - Viaje #{NumeroViaje}",
                                   deviceId, viajeActual.NumeroViaje);

                    try
                    {
                        var tripData = new
                        {
                            tripId = viajeActual.ViajeId.ToString(),
                            numeroViaje = viajeActual.NumeroViaje,
                            numeroGuia = viajeActual.NumeroGuia,
                            responsable = viajeActual.Responsable,
                            fecha = viajeActual.Fecha.ToString("dd/MM/yyyy")
                        };

                        // CAMBIO CLAVE: Enviar viaje activo CON lista de pallets  
                        await _signalRService.SendActiveTripWithPalletsToMobileAsync(deviceId, tripData, PalletsEscaneados.ToList());
                        _logger.LogInfo("✅ Viaje activo con pallets enviado exitosamente al dispositivo: {DeviceId}", deviceId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Error enviando viaje activo con pallets: {ErrorMessage}", ex.Message);
                    }
                }
                else
                {
                    _logger.LogInfo("⚠️ No hay viaje activo para enviar al dispositivo: {DeviceId}", deviceId);
                    try
                    {
                        await _signalRService.SendNoActiveTripAsync(deviceId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Error notificando ausencia de viaje activo: {ErrorMessage}", ex.Message);
                    }
                }
            });
        }

        // Implementación de INotifyPropertyChanged  
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string nombrePropiedad)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombrePropiedad));
        }
        // NUEVO: Manejadores de eventos SignalR adicionales  
        private void OnPalletProcessedFromMobile(object palletData, string deviceId)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    _logger.LogInfo("📱 Respuesta procesada desde móvil - Device: {DeviceId}", deviceId);

                    // CRÍTICO: Determinar si es una edición o un escaneo nuevo  
                    bool esEdicionProcesada = !string.IsNullOrEmpty(_currentDeviceProcessing) && _currentDeviceProcessing == deviceId;

                    if (!esEdicionProcesada)
                    {
                        // SOLO para escaneos nuevos: Recargar lista completa desde BD  
                        if (ViajeActivo != null)
                        {
                            _logger.LogDebug("Recargando lista completa para escaneo nuevo");
                            var palletsExistentes = _accesoDatosViajes.ObtenerPalletsDeViaje(ViajeActivo.ViajeId);
                            PalletsEscaneados.Clear();
                            foreach (var pallet in palletsExistentes)
                            {
                                PalletsEscaneados.Add(pallet);
                            }
                            _logger.LogInfo("Lista de pallets recargada desde BD - Total: {Count}", PalletsEscaneados.Count);
                        }
                    }
                    else
                    {
                        // SOLO para ediciones: Actualizar la colección local sin recargar desde BD  
                        _logger.LogDebug("Procesando edición - Actualizando colección local sin recargar BD");

                        // Deserializar los datos del pallet editado recibido  
                        var palletDataJson = System.Text.Json.JsonSerializer.Serialize(palletData);
                        var palletEditado = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(palletDataJson);

                        if (palletEditado.TryGetProperty("numeroPallet", out var numeroPalletElement))
                        {
                            string numeroPallet = numeroPalletElement.GetString();

                            // Buscar y actualizar el pallet en la colección local  
                            var index = PalletsEscaneados.ToList().FindIndex(p => p.NumeroPallet == numeroPallet);
                            if (index >= 0)
                            {
                                // Crear objeto InformacionPallet actualizado  
                                var palletActualizado = new InformacionPallet
                                {
                                    NumeroPallet = numeroPallet,
                                    Variedad = palletEditado.TryGetProperty("variedad", out var variedadEl) ? variedadEl.GetString() : PalletsEscaneados[index].Variedad,
                                    Calibre = palletEditado.TryGetProperty("calibre", out var calibreEl) ? calibreEl.GetString() : PalletsEscaneados[index].Calibre,
                                    Embalaje = palletEditado.TryGetProperty("embalaje", out var embalajeEl) ? embalajeEl.GetString() : PalletsEscaneados[index].Embalaje,
                                    NumeroDeCajas = palletEditado.TryGetProperty("numeroDeCajas", out var cajasEl) ? (int)cajasEl.GetDouble() : PalletsEscaneados[index].NumeroDeCajas,
                                    PesoUnitario = palletEditado.TryGetProperty("pesoUnitario", out var pesoUnitEl) ? (decimal)pesoUnitEl.GetDouble() : PalletsEscaneados[index].PesoUnitario,
                                    PesoTotal = palletEditado.TryGetProperty("pesoTotal", out var pesoTotalEl) ? (decimal)pesoTotalEl.GetDouble() : PalletsEscaneados[index].PesoTotal,
                                    Modificado = palletEditado.TryGetProperty("modificado", out var modificadoEl) ? modificadoEl.GetBoolean() : true,
                                    FechaEscaneo = PalletsEscaneados[index].FechaEscaneo,
                                    FechaModificacion = palletEditado.TryGetProperty("fechaModificacion", out var fechaModEl) ? DateTime.Parse(fechaModEl.GetString()) : FechaOperacionalHelper.ObtenerFechaOperacionalActual(),
                                    // Mantener valores originales  
                                    VariedadOriginal = PalletsEscaneados[index].VariedadOriginal,
                                    CalibreOriginal = PalletsEscaneados[index].CalibreOriginal,
                                    EmbalajeOriginal = PalletsEscaneados[index].EmbalajeOriginal,
                                    NumeroDeCajasOriginal = PalletsEscaneados[index].NumeroDeCajasOriginal
                                };

                                // CRÍTICO: Actualizar el pallet en la colección observable  
                                PalletsEscaneados[index] = palletActualizado;
                                _logger.LogDebug("Pallet actualizado en colección local en índice: {Index} - {NumeroPallet}", index, numeroPallet);

                                // CRÍTICO: Forzar actualización de la UI del DataGrid  
                                OnPropertyChanged(nameof(PalletsEscaneados));
                            }
                        }

                        // Limpiar el flag de procesamiento  
                        _currentDeviceProcessing = null;
                    }

                    // Actualizar totales en ambos casos  
                    OnPropertyChanged(nameof(TotalCajas));
                    OnPropertyChanged(nameof(PesoTotalViaje));

                    OnPropertyChanged(nameof(TotalPC));
                    OnPropertyChanged(nameof(TotalPH));
                    OnPropertyChanged(nameof(TotalCT));
                    OnPropertyChanged(nameof(TotalEN));
                    _logger.LogInfo("✅ Respuesta procesada exitosamente desde móvil - Device: {DeviceId}", deviceId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error procesando respuesta desde móvil: {ErrorMessage}", ex.Message);
                    // Limpiar flag en caso de error  
                    _currentDeviceProcessing = null;
                }
            });
        }

        private void OnPalletErrorFromMobile(string errorMessage, string deviceId)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show($"Error desde dispositivo móvil: {errorMessage}",
                               "Error de Dispositivo Móvil",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
            });
        }

        private void OnSignalRConnectionChanged(bool isConnected)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _logger.LogInfo("🔗 Estado de conexión SignalR: {IsConnected}", isConnected ? "Conectado" : "Desconectado");

                EstadoConexionTexto = isConnected ? "Conectado" : "Desconectado";
                EstadoConexionColor = isConnected ? Brushes.Green : Brushes.Red;

                // ✅ Capturar referencia local  
                var viajeActual = ViajeActivo;
                if (isConnected && viajeActual != null)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _signalRService.JoinTripGroupAsync(viajeActual.ViajeId.ToString());
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error uniéndose al grupo tras reconexión: {ErrorMessage}", ex.Message);
                        }
                    });
                }
            });
        }

        private void OnSignalRConnectionError(string errorMessage)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _logger.LogError("Error de conexión SignalR: {ErrorMessage}", errorMessage);
            });
        }
        public void Dispose()
        {
            try
            {
                _signalRService?.StopConnectionAsync().Wait(5000);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cerrando conexión SignalR: {ErrorMessage}", ex.Message);
            }
        }
        public string EstadoConexionTexto
        {
            get => _estadoConexionTexto;
            set
            {
                _estadoConexionTexto = value;
                OnPropertyChanged(nameof(EstadoConexionTexto));
            }
        }

        private Brush _estadoConexionColor = Brushes.Red;
        public Brush EstadoConexionColor
        {
            get => _estadoConexionColor;
            set
            {
                _estadoConexionColor = value;
                OnPropertyChanged(nameof(EstadoConexionColor));
            }
        }
        // NUEVO: Método para manejar eliminaciones desde móvil
        private void OnPalletDeleteRequestedFromMobile(string tripId, string palletNumber, string deviceId)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (ViajeActivo?.ViajeId.ToString() == tripId)
                {
                    var palletToDelete = PalletsEscaneados.FirstOrDefault(p => p.NumeroPallet == palletNumber);
                    if (palletToDelete != null)
                    {
                        try
                        {
                            // Usar la misma lógica que la aplicación de escritorio  
                            _accesoDatosViajes.EliminarPalletViaje(palletNumber, ViajeActivo.ViajeId);

                            // Actualizar colección local  
                            PalletsEscaneados.Remove(palletToDelete);

                            // Actualizar totales  
                            OnPropertyChanged(nameof(TotalCajas));
                            OnPropertyChanged(nameof(PesoTotalViaje));

                            // Notificar éxito al dispositivo móvil  
                            _ = Task.Run(async () =>
                            {
                                await _signalRService.BroadcastPalletListUpdateAsync(tripId, PalletsEscaneados.ToList());
                                await _signalRService.SendPalletSuccessToMobileAsync(tripId,
                                    $"Pallet {palletNumber} eliminado exitosamente", deviceId);
                            });
                        }
                        catch (Exception ex)
                        {
                            _ = Task.Run(async () =>
                            {
                                await _signalRService.SendPalletErrorToMobileAsync(tripId,
                                    $"Error al eliminar pallet: {ex.Message}", deviceId);
                            });
                        }
                    }
                }
            });
        }
        private async void OnBicolorPackagingTypesRequestedFromMobile(string deviceId)
        {
            try
            {
                _logger.LogInfo("📱 Solicitud de tipos de embalaje bicolor desde dispositivo: {DeviceId}", deviceId);

                // Obtener lista de embalajes bicolor activos desde la base de datos  
                var embalajesBicolor = _accesoDatosEmbalajeBicolor.ObtenerEmbalajeBicolorActivos();

                // Enviar al dispositivo móvil específico  
                await _signalRService.SendBicolorPackagingTypesToMobileAsync(deviceId, embalajesBicolor);

                _logger.LogInfo("✅ Lista de embalajes bicolor enviada al dispositivo: {DeviceId}", deviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error enviando tipos de embalaje bicolor: {ErrorMessage}", ex.Message);
            }
        }
        // NUEVO: Método para manejar ediciones desde móvil  
        private async void OnPalletEditReceivedFromMobile(string palletNumber, object editedData, string deviceId)
        {
            Application.Current.Dispatcher.Invoke(async () =>
            {
                try
                {
                    _logger.LogInfo("📝 Procesando edición de pallet desde móvil: {PalletNumber}, Device: {DeviceId}",
                                   palletNumber, deviceId);

                    // ✅ SOLUCIÓN: Establecer deviceId ANTES de cualquier procesamiento    
                    _currentDeviceProcessing = deviceId;
                    _logger.LogDebug("🔧 _currentDeviceProcessing establecido: {DeviceId}", deviceId);

                    var existingPallet = PalletsEscaneados.FirstOrDefault(p => p.NumeroPallet == palletNumber);
                    if (existingPallet != null)
                    {
                        // PASO 1: Guardar valores ORIGINALES antes de cualquier cambio        
                        string calibreOriginal = existingPallet.Calibre;
                        string variedadOriginal = existingPallet.Variedad;
                        string embalajeOriginal = existingPallet.Embalaje;
                        int cajasOriginales = existingPallet.NumeroDeCajas;
                        bool modificadoOriginal = existingPallet.Modificado;
                        decimal pesoUnitarioOriginal = existingPallet.PesoUnitario;
                        decimal pesoTotalOriginal = existingPallet.PesoTotal;

                        // ✅ CAMBIO: Solo guardar SegundaVariedad original (no cajas)  
                        string segundaVariedadOriginal = existingPallet.SegundaVariedad;

                        // PASO 2: Deserializar datos editados        
                        var editedDataJson = System.Text.Json.JsonSerializer.Serialize(editedData);
                        var editedPalletData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(editedDataJson);

                        // PASO 3: Aplicar cambios UNA SOLA VEZ        
                        if (editedPalletData.TryGetProperty("calibre", out var calibreElement))
                            existingPallet.Calibre = calibreElement.GetString();

                        if (editedPalletData.TryGetProperty("variedad", out var variedadElement))
                            existingPallet.Variedad = variedadElement.GetString();

                        if (editedPalletData.TryGetProperty("embalaje", out var embalajeElement))
                            existingPallet.Embalaje = embalajeElement.GetString();

                        if (editedPalletData.TryGetProperty("numeroDeCajas", out var cajasElement))
                        {
                            if (cajasElement.ValueKind == JsonValueKind.Number)
                                existingPallet.NumeroDeCajas = cajasElement.TryGetInt32(out var intValue) ? intValue : (int)cajasElement.GetDouble();
                        }

                        // ✅ CAMBIO: Procesar solo SegundaVariedad (no cajasSegundaVariedad)  
                        if (editedPalletData.TryGetProperty("segundaVariedad", out var segundaVariedadElement))
                        {
                            existingPallet.SegundaVariedad = segundaVariedadElement.GetString();
                            _logger.LogDebug("Segunda variedad actualizada: {SegundaVariedad}", existingPallet.SegundaVariedad);
                        }

                        // ✅ CAMBIO: Actualizar valor original de SegundaVariedad si no existía  
                        if (string.IsNullOrEmpty(existingPallet.SegundaVariedadOriginal) && !string.IsNullOrEmpty(existingPallet.SegundaVariedad))
                        {
                            existingPallet.SegundaVariedadOriginal = existingPallet.SegundaVariedad;
                        }

                        // ✅ CAMBIO: Comparar valores originales con valores finales (sin CajasSegundaVariedad)  
                        bool huboModificacion =
                            existingPallet.Calibre != calibreOriginal ||
                            existingPallet.Variedad != variedadOriginal ||
                            existingPallet.Embalaje != embalajeOriginal ||
                            existingPallet.NumeroDeCajas != cajasOriginales ||
                            existingPallet.SegundaVariedad != segundaVariedadOriginal;

                        existingPallet.Modificado = huboModificacion;
                        existingPallet.FechaModificacion = huboModificacion ? FechaOperacionalHelper.ObtenerFechaOperacionalActual() : existingPallet.FechaModificacion;

                        _logger.LogDebug("Modificación detectada en edición desde móvil: {HuboModificacion}", huboModificacion);

                        // ✅ CAMBIO: Log específico para pallets bicolor con cantidad única  
                        if (existingPallet.EsBicolor)
                        {
                            _logger.LogInfo("🎨 Pallet bicolor editado desde móvil - Variedad1: {Variedad1}, Variedad2: {Variedad2}, Total Cajas: {TotalCajas}",
                                           existingPallet.Variedad, existingPallet.SegundaVariedad, existingPallet.NumeroDeCajas);
                        }

                        // Validar/recalcular solo si cambió embalaje o cajas.
                        // PH/CT/EN: cajas manuales se respetan en RecalcularPesoPallet (rama no-PC).
                        // PC: RecalcularPesoPallet aplica ficha técnica cuando corresponde.
                        bool embalajeOCajasCambiaron =
                            existingPallet.Embalaje != embalajeOriginal ||
                            existingPallet.NumeroDeCajas != cajasOriginales;

                        if (embalajeOCajasCambiaron)
                        {
                            if (!ValidarEmbalajeParaEdicion(existingPallet, out string errorEmbalajeMovil))
                            {
                                string embalajeIntentado = existingPallet.Embalaje;

                                existingPallet.Calibre = calibreOriginal;
                                existingPallet.Variedad = variedadOriginal;
                                existingPallet.Embalaje = embalajeOriginal;
                                existingPallet.NumeroDeCajas = cajasOriginales;
                                existingPallet.SegundaVariedad = segundaVariedadOriginal;
                                existingPallet.Modificado = modificadoOriginal;
                                existingPallet.PesoUnitario = pesoUnitarioOriginal;
                                existingPallet.PesoTotal = pesoTotalOriginal;

                                _logger.LogWarning("Edición desde móvil rechazada ({TipoPallet}) - embalaje inválido: {Embalaje}",
                                    existingPallet.TipoPallet, embalajeIntentado);

                                await _signalRService.SendPalletErrorToMobileAsync(
                                    ViajeActivo.ViajeId.ToString(), errorEmbalajeMovil, deviceId);

                                var palletsSinCambio = _accesoDatosViajes.ObtenerPalletsDeViaje(ViajeActivo.ViajeId);
                                PalletsEscaneados.Clear();
                                foreach (var pallet in palletsSinCambio)
                                {
                                    PalletsEscaneados.Add(pallet);
                                }

                                await _signalRService.SendPalletListToMobileAsync(deviceId, PalletsEscaneados.ToList());
                                return;
                            }

                            RecalcularPesoPallet(existingPallet);
                        }

                        _accesoDatosViajes.ActualizarPalletViaje(existingPallet, ViajeActivo.ViajeId);

                        var palletsExistentes = _accesoDatosViajes.ObtenerPalletsDeViaje(ViajeActivo.ViajeId);
                        PalletsEscaneados.Clear();
                        foreach (var pallet in palletsExistentes)
                        {
                            PalletsEscaneados.Add(pallet);
                        }

                        OnPropertyChanged(nameof(TotalCajas));
                        OnPropertyChanged(nameof(PesoTotalViaje));
                        OnPropertyChanged(nameof(TotalPC));
                        OnPropertyChanged(nameof(TotalPH));
                        OnPropertyChanged(nameof(TotalCT));
                        OnPropertyChanged(nameof(TotalEN));

                        await _signalRService.SendPalletListToMobileAsync(deviceId, PalletsEscaneados.ToList());
                        _logger.LogInfo("✅ Edición procesada y lista sincronizada: {PalletNumber}", palletNumber);
                    }
                    else
                    {
                        var errorMsg = $"No se encontró el pallet {palletNumber} para editar.";
                        await _signalRService.SendPalletErrorToMobileAsync(ViajeActivo.ViajeId.ToString(), errorMsg, deviceId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error procesando edición de pallet: {ErrorMessage}", ex.Message);
                    await _signalRService.SendPalletErrorToMobileAsync(ViajeActivo.ViajeId.ToString(),
                        $"Error procesando edición: {ex.Message}", deviceId);
                }
                finally
                {
                    // ✅ IMPORTANTE: Limpiar el flag después de procesar    
                    _currentDeviceProcessing = null;
                    _logger.LogDebug("🧹 _currentDeviceProcessing limpiado");
                }
            });
        }

        // ============================================================================
        // SERVICIO TESTEADOR: Conexión SignalR dedicada (independiente de la principal)
        // Replica el patrón de TesteadorWindow pero sin depender de la ventana.
        // ============================================================================

        private async Task IniciarServicioTesteadorAsync()
        {
            try
            {
                _testeadorSignalRService = new SignalRService(AppConfig.SignalRHubUrl);
                await _testeadorSignalRService.StartConnectionAsync();

                // Registrar handlers DESPUÉS de que la conexión esté establecida
                _testeadorSignalRService.OnPalletInfoRequested(async (palletNumber, deviceId) =>
                {
                    await AtenderSolicitudInfoPalletTesteador(palletNumber, deviceId);
                });

                _testeadorSignalRService.OnPalletDeletionRequested(async (palletNumber, deviceId) =>
                {
                    await AtenderSolicitudEliminacionTesteador(palletNumber, deviceId);
                });

                _logger.LogInfo("[Testeador] Servicio Testeador iniciado con conexión dedicada");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Testeador] Error iniciando servicio Testeador: {Error}", ex.Message);
            }
        }

        private async Task AtenderSolicitudInfoPalletTesteador(string palletNumber, string deviceId)
        {
            try
            {
                _logger.LogInfo("[Testeador] Solicitud de info de pallet {NumeroPallet} desde {DeviceId}", palletNumber, deviceId);

                var accesoDatosPallet = new AccesoDatosPallet();
                var (pallet, lotes, estadoValidacion) = accesoDatosPallet.ObtenerPalletConLotes(palletNumber);

                if (pallet != null && lotes != null)
                {
                    // Calcular lotes reales (cuarteles únicos) y tipos de etiqueta
                    int lotesReales = lotes.Select(l => l.CodigoCuartel).Distinct().Count();
                    int tiposDeEtiqueta = lotes.Count;
                    int sumaCajas = lotes.Sum(l => l.CantidadCajas);

                    // Generar estado de validación detallado
                    string estadoDetallado;
                    if (estadoValidacion == "DISCREPANCIA")
                    {
                        estadoDetallado = $"DISCREPANCIA - Declarado: {pallet.NumeroDeCajas} cajas, Contado: {sumaCajas} cajas";
                    }
                    else
                    {
                        estadoDetallado = $"OK - {lotesReales} lote(s), {tiposDeEtiqueta} tipo(s) de etiqueta";
                    }

                    var palletData = new
                    {
                        Pallet = new
                        {
                            pallet.NumeroPallet,
                            pallet.NumeroDeCajas,
                            pallet.Calibre,
                            pallet.Embalaje,
                            pallet.Variedad
                        },
                        Lotes = lotes.Select(l => new
                        {
                            l.CodigoCuartel,
                            l.CSGPredio,
                            l.NombrePredio,
                            l.NombreProductor,
                            l.CalibreLote,
                            l.EmbalajeLote,
                            l.VariedadLote,
                            l.CantidadCajas,
                            l.EsMinoritario,
                            l.CalibreMayoritario,
                            l.EmbalajeMayoritario,
                            l.VariedadMayoritaria
                        }).ToList(),
                        EstadoValidacion = estadoDetallado,
                        LotesReales = lotesReales,
                        TiposDeEtiqueta = tiposDeEtiqueta,
                        Incompleto = false
                    };

                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(palletData);
                    await _testeadorSignalRService.SendPalletInfoToMobileTesteadorAsync(json, deviceId, true, "");
                }
                else
                {
                    var (encontrado, completo, tablasConRegistros, mensaje) =
                        accesoDatosPallet.VerificarEstadoPallet(palletNumber);

                    if (encontrado)
                    {
                        var palletData = new
                        {
                            Incompleto = true,
                            Mensaje = mensaje,
                            TablasConRegistros = tablasConRegistros
                        };

                        string json = Newtonsoft.Json.JsonConvert.SerializeObject(palletData);
                        await _testeadorSignalRService.SendPalletInfoToMobileTesteadorAsync(json, deviceId, true, "");
                    }
                    else
                    {
                        await _testeadorSignalRService.SendPalletInfoToMobileTesteadorAsync("", deviceId, false, "Pallet no encontrado en la base de datos");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Testeador] Error consultando pallet {NumeroPallet}: {Error}", palletNumber, ex.Message);
                try
                {
                    await _testeadorSignalRService.SendPalletInfoToMobileTesteadorAsync("", deviceId, false, $"Error: {ex.Message}");
                }
                catch { }
            }
        }

        private async Task AtenderSolicitudEliminacionTesteador(string palletNumber, string deviceId)
        {
            try
            {
                _logger.LogInfo("[Testeador] Solicitud de eliminación de pallet {NumeroPallet} desde {DeviceId}", palletNumber, deviceId);

                var accesoDatosPallet = new AccesoDatosPallet();
                bool eliminado = accesoDatosPallet.EliminarPallet(palletNumber);

                string mensaje = eliminado
                    ? $"Pallet {palletNumber} eliminado exitosamente de todas las tablas"
                    : $"No se pudo eliminar el pallet {palletNumber}. Verifique que exista en la base de datos";

                await _testeadorSignalRService.SendDeletionResultToMobileAsync(palletNumber, deviceId, eliminado, mensaje);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Testeador] Error eliminando pallet {NumeroPallet}: {Error}", palletNumber, ex.Message);
                try
                {
                    await _testeadorSignalRService.SendDeletionResultToMobileAsync(palletNumber, deviceId, false, $"Error: {ex.Message}");
                }
                catch { }
            }
        }
    }
}