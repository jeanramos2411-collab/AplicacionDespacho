using AplicacionDespacho.Configuration;
using AplicacionDespacho.Services;
using AplicacionDespacho.Services.Logging;
using AplicacionDespacho.utilities;
using System;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace AplicacionDespacho.Modules.Common.Views
{
    public partial class ConfiguracionSignalRWindow : Window, INotifyPropertyChanged
    {
        private readonly ILoggingService _logger;
        private readonly SignalRService _signalRService;
        private string _serverIp = "192.168.0.103";
        private string _serverPort = "7164";
        private string _urlActual;
        private string _estadoConexionTexto = "Desconectado";
        private Brush _estadoConexionColor = Brushes.Red;
        private string _mensajeValidacion;
        private Brush _colorValidacion = Brushes.Red;
        private Visibility _mostrarValidacion = Visibility.Collapsed;
        private bool _probandoConexion = false;
        private Visibility _mostrarProgress = Visibility.Collapsed;
        private string _resultadoPrueba;
        private Brush _colorResultado = Brushes.Black;
        private Visibility _mostrarResultado = Visibility.Collapsed;
        private string _logActividad = "";

        public ConfiguracionSignalRWindow(SignalRService signalRService = null)
        {
            InitializeComponent();
            _logger = LoggingFactory.CreateLogger("ConfiguracionSignalR");
            _signalRService = signalRService; // Asignar la referencia  
            DataContext = this;
            CargarConfiguracionActual();
        }

        #region Propiedades para Binding  

        public string ServerIp
        {
            get => _serverIp;
            set
            {
                _serverIp = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UrlCompleta));
                ValidarEntrada();
            }
        }

        public string ServerPort
        {
            get => _serverPort;
            set
            {
                _serverPort = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UrlCompleta));
                ValidarEntrada();
            }
        }

        public string UrlCompleta => $"http://{ServerIp}:{ServerPort}/pallethub";

        public string UrlActual
        {
            get => _urlActual;
            set
            {
                _urlActual = value;
                OnPropertyChanged();
            }
        }

        public string EstadoConexionTexto
        {
            get => _estadoConexionTexto;
            set
            {
                _estadoConexionTexto = value;
                OnPropertyChanged();
            }
        }

        public Brush EstadoConexionColor
        {
            get => _estadoConexionColor;
            set
            {
                _estadoConexionColor = value;
                OnPropertyChanged();
            }
        }

        public string MensajeValidacion
        {
            get => _mensajeValidacion;
            set
            {
                _mensajeValidacion = value;
                OnPropertyChanged();
            }
        }

        public Brush ColorValidacion
        {
            get => _colorValidacion;
            set
            {
                _colorValidacion = value;
                OnPropertyChanged();
            }
        }

        public Visibility MostrarValidacion
        {
            get => _mostrarValidacion;
            set
            {
                _mostrarValidacion = value;
                OnPropertyChanged();
            }
        }

        public bool ProbandoConexion
        {
            get => _probandoConexion;
            set
            {
                _probandoConexion = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PuedeProbarConexion));
            }
        }

        public Visibility MostrarProgress
        {
            get => _mostrarProgress;
            set
            {
                _mostrarProgress = value;
                OnPropertyChanged();
            }
        }

        public string ResultadoPrueba
        {
            get => _resultadoPrueba;
            set
            {
                _resultadoPrueba = value;
                OnPropertyChanged();
            }
        }

        public Brush ColorResultado
        {
            get => _colorResultado;
            set
            {
                _colorResultado = value;
                OnPropertyChanged();
            }
        }

        public Visibility MostrarResultado
        {
            get => _mostrarResultado;
            set
            {
                _mostrarResultado = value;
                OnPropertyChanged();
            }
        }

        public string LogActividad
        {
            get => _logActividad;
            set
            {
                _logActividad = value;
                OnPropertyChanged();
            }
        }

        public bool PuedeProbarConexion => !ProbandoConexion && AppConfig.IsValidSignalRUrl(UrlCompleta);

        public bool PuedeGuardar => AppConfig.IsValidSignalRUrl(UrlCompleta) && !ProbandoConexion;

        #endregion

        #region Métodos Privados  

        private void CargarConfiguracionActual()
        {
            try
            {
                var config = AppConfig.GetSignalRConfiguration();
                UrlActual = config.HubUrl;

                // Extraer IP y puerto de la URL actual  
                if (Uri.TryCreate(config.HubUrl, UriKind.Absolute, out Uri uri))
                {
                    ServerIp = uri.Host;
                    ServerPort = uri.Port.ToString();
                }

                // Verificar estado de conexión actual  
                VerificarEstadoConexionActual();

                AgregarLog($"Configuración cargada: {config.HubUrl}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando configuración actual");
                AgregarLog($"Error: {ex.Message}");
            }
        }

        private void VerificarEstadoConexionActual()
        {
            bool isConnected = _signalRService?.IsConnected ?? false;

            if (isConnected)
            {
                EstadoConexionTexto = "Conectado";
                EstadoConexionColor = Brushes.Green;
            }
            else
            {
                EstadoConexionTexto = "Desconectado";
                EstadoConexionColor = Brushes.Red;
            }
        }

        private void ValidarEntrada()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ServerIp) || string.IsNullOrWhiteSpace(ServerPort))
                {
                    MostrarMensajeValidacion("IP y Puerto son requeridos", Brushes.Orange);
                    return;
                }

                if (!int.TryParse(ServerPort, out int puerto) || puerto < 1 || puerto > 65535)
                {
                    MostrarMensajeValidacion("Puerto debe ser un número entre 1 y 65535", Brushes.Red);
                    return;
                }

                if (AppConfig.IsValidSignalRUrl(UrlCompleta))
                {
                    MostrarMensajeValidacion("URL válida", Brushes.Green);
                }
                else
                {
                    MostrarMensajeValidacion("URL inválida", Brushes.Red);
                }

                OnPropertyChanged(nameof(PuedeProbarConexion));
                OnPropertyChanged(nameof(PuedeGuardar));
            }
            catch (Exception ex)
            {
                MostrarMensajeValidacion($"Error de validación: {ex.Message}", Brushes.Red);
            }
        }

        private void MostrarMensajeValidacion(string mensaje, Brush color)
        {
            MensajeValidacion = mensaje;
            ColorValidacion = color;
            MostrarValidacion = Visibility.Visible;
        }

        private void AgregarLog(string mensaje)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            LogActividad += $"[{timestamp}] {mensaje}\n";

            // Scroll al final del log  
            Dispatcher.BeginInvoke(new Action(() =>
            {
                txtLog.ScrollToEnd();
            }));
        }

        #endregion

        #region Event Handlers  

        private void BtnConfigLocal_Click(object sender, RoutedEventArgs e)
        {
            ServerIp = "127.0.0.1";
            ServerPort = "7164";
            AgregarLog("Configuración local aplicada");
        }

        private void BtnConfigDefault_Click(object sender, RoutedEventArgs e)
        {
            ServerIp = "192.168.0.103";
            ServerPort = "7164";
            AgregarLog("Configuración por defecto aplicada");
        }

        private async void BtnProbarConexion_Click(object sender, RoutedEventArgs e)
        {
            await ProbarConexionAsync();
        }

        private async Task ProbarConexionAsync()
        {
            ProbandoConexion = true;
            MostrarProgress = Visibility.Visible;
            MostrarResultado = Visibility.Collapsed;

            try
            {
                AgregarLog($"Probando conexión a {UrlCompleta}...");

                // Probar conectividad básica HTTP  
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);

                    // Intentar conectar al endpoint base  
                    var baseUrl = $"http://{ServerIp}:{ServerPort}";
                    var response = await client.GetAsync(baseUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        ResultadoPrueba = "✅ Conexión exitosa al servidor";
                        ColorResultado = Brushes.Green;
                        AgregarLog("Conexión exitosa");
                    }
                    else
                    {
                        ResultadoPrueba = $"⚠️ Servidor responde pero con código: {response.StatusCode}";
                        ColorResultado = Brushes.Orange;
                        AgregarLog($"Respuesta del servidor: {response.StatusCode}");
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                ResultadoPrueba = $"❌ Error de conexión: {ex.Message}";
                ColorResultado = Brushes.Red;
                AgregarLog($"Error de conexión: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                ResultadoPrueba = "❌ Timeout: El servidor no responde";
                ColorResultado = Brushes.Red;
                AgregarLog("Timeout de conexión");
            }
            catch (Exception ex)
            {
                ResultadoPrueba = $"❌ Error inesperado: {ex.Message}";
                ColorResultado = Brushes.Red;
                AgregarLog($"Error inesperado: {ex.Message}");
            }
            finally
            {
                ProbandoConexion = false;
                MostrarProgress = Visibility.Collapsed;
                MostrarResultado = Visibility.Visible;
            }
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!AppConfig.IsValidSignalRUrl(UrlCompleta))
                {
                    MessageBox.Show("La URL ingresada no es válida.", "Error de Validación",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Guardar configuración usando AppConfig    
                AppConfig.SetSignalRHubUrl(UrlCompleta);

                AgregarLog($"Configuración guardada: {UrlCompleta}");
                _logger.LogInfo("Configuración SignalR guardada: {HubUrl}", UrlCompleta);

                // Usar ApplicationRestartHelper con la ventana actual  
                ApplicationRestartHelper.PromptAndRestartIfConfirmed("SignalR", this);
            }
            catch (Exception ex)
            {
                var mensaje = $"Error al guardar configuración: {ex.Message}";
                MessageBox.Show(mensaje, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                AgregarLog(mensaje);
                _logger.LogError(ex, "Error guardando configuración SignalR");
            }
        }

        private void BtnRestablecer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var resultado = MessageBox.Show("¿Está seguro de restablecer la configuración a los valores por defecto?",
                                               "Confirmar Restablecimiento",
                                               MessageBoxButton.YesNo,
                                               MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    AppConfig.ResetSignalRHubUrl();
                    CargarConfiguracionActual();
                    AgregarLog("Configuración restablecida a valores por defecto");
                    _logger.LogInfo("Configuración SignalR restablecida");
                }
            }
            catch (Exception ex)
            {
                var mensaje = $"Error al restablecer configuración: {ex.Message}";
                MessageBox.Show(mensaje, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                AgregarLog(mensaje);
                _logger.LogError(ex, "Error restableciendo configuración SignalR");
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        #endregion

        #region INotifyPropertyChanged  

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}