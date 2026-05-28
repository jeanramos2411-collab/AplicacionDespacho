using AplicacionDespacho.Configuration;
using AplicacionDespacho.Services.Logging;
using AplicacionDespacho.utilities;
using System;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AplicacionDespacho.Modules.Common.Views
{
    public partial class ConfiguracionBaseDatosWindow : Window, INotifyPropertyChanged
    {
        #region Fields  

        private readonly ILoggingService _logger;
        private string _servidor = "";
        private string _usuario = "";
        private string _password = "";
        private string _timeout = "30";
        private string _resultadoPrueba = "";
        private Brush _colorResultado = Brushes.Red;
        private bool _puedeProbarConexion = false;
        private bool _probandoConexion = false;
        private Visibility _mostrarProgress = Visibility.Collapsed;
        private Visibility _mostrarResultado = Visibility.Collapsed;
        private string _logActividad = "";

        #endregion  

        #region Properties  

        public string Servidor
        {
            get => _servidor;
            set
            {
                _servidor = value;
                OnPropertyChanged();
                ValidarEntrada();
            }
        }

        public string Usuario
        {
            get => _usuario;
            set
            {
                _usuario = value;
                OnPropertyChanged();
                ValidarEntrada();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();
                ValidarEntrada();
            }
        }

        public string Timeout
        {
            get => _timeout;
            set
            {
                _timeout = value;
                OnPropertyChanged();
                ValidarEntrada();
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

        public bool PuedeProbarConexion
        {
            get => _puedeProbarConexion;
            set
            {
                _puedeProbarConexion = value;
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

        #endregion

        #region Constructor  

        public ConfiguracionBaseDatosWindow()
        {
            InitializeComponent();
            _logger = LoggingFactory.CreateLogger("ConfiguracionBaseDatos");
            DataContext = this;
            CargarConfiguracionActual();
        }

        #endregion

        #region Private Methods  

        private void CargarConfiguracionActual()
        {
            try
            {
                var config = AppConfig.GetDatabaseConfiguration();
                Servidor = config.Servidor;
                Usuario = config.Usuario;
                Timeout = config.Timeout.ToString();

                AgregarLog($"Configuración cargada: {config.DisplayText}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando configuración de base de datos");
                AgregarLog($"Error: {ex.Message}");
            }
        }

        private bool ValidarFormulario()
        {
            if (string.IsNullOrWhiteSpace(Servidor))
            {
                MessageBox.Show("El servidor es requerido.", "Error de Validación",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(Usuario))
            {
                MessageBox.Show("El usuario es requerido.", "Error de Validación",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                MessageBox.Show("La contraseña es requerida.", "Error de Validación",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!int.TryParse(Timeout, out int timeoutValue) || timeoutValue <= 0)
            {
                MessageBox.Show("El timeout debe ser un número mayor a 0.", "Error de Validación",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void ValidarEntrada()
        {
            var esValido = !string.IsNullOrWhiteSpace(Servidor) &&
                          !string.IsNullOrWhiteSpace(Usuario) &&
                          !string.IsNullOrWhiteSpace(Password) &&
                          int.TryParse(Timeout, out int timeoutValue) && timeoutValue > 0;

            PuedeProbarConexion = esValido;
        }

        private void AgregarLog(string mensaje)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            LogActividad += $"[{timestamp}] {mensaje}\\n";

            // Auto-scroll al final  
            Dispatcher.BeginInvoke(new Action(() =>
            {
                txtLogActividad.ScrollToEnd();
            }));
        }

        private string ConstruirCadenaConexion(string database)
        {
            return AppConfig.BuildConnectionString(Servidor, database, Usuario, Password, int.Parse(Timeout));
        }

        #endregion

        #region Event Handlers  

        private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            var passwordBox = sender as PasswordBox;
            Password = passwordBox?.Password ?? "";
        }

        private async void BtnProbarConexion_Click(object sender, RoutedEventArgs e)
        {
            await ProbarConexionAsync();
        }

        private async Task ProbarConexionAsync()
        {
            PuedeProbarConexion = false;
            MostrarProgress = Visibility.Visible;
            MostrarResultado = Visibility.Collapsed;

            try
            {
                AgregarLog($"Probando conexión a {Servidor}...");

                // Probar conexión a base de datos master primero  
                var connectionStringMaster = ConstruirCadenaConexion("master");

                using (var connection = new SqlConnection(connectionStringMaster))
                {
                    await connection.OpenAsync();
                    AgregarLog("Conexión a master exitosa");
                }

                // Probar conexión a bases de datos específicas  
                var databases = new[] { "Despachos_SJP", "Packing_SJP" };
                foreach (var db in databases)
                {
                    try
                    {
                        var connectionString = ConstruirCadenaConexion(db);
                        using (var connection = new SqlConnection(connectionString))
                        {
                            await connection.OpenAsync();
                            AgregarLog($"Conexión a {db} exitosa");
                        }
                    }
                    catch (Exception ex)
                    {
                        AgregarLog($"Advertencia: No se pudo conectar a {db}: {ex.Message}");
                    }
                }

                ResultadoPrueba = "✅ Conexión exitosa al servidor";
                ColorResultado = Brushes.Green;
                AgregarLog("Todas las pruebas de conexión completadas");
            }
            catch (Exception ex)
            {
                ResultadoPrueba = $"❌ Error de conexión: {ex.Message}";
                ColorResultado = Brushes.Red;
                AgregarLog($"Error de conexión: {ex.Message}");
                _logger.LogError(ex, "Error probando conexión de base de datos");
            }
            finally
            {
                PuedeProbarConexion = true;
                MostrarProgress = Visibility.Collapsed;
                MostrarResultado = Visibility.Visible;
            }
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidarFormulario())
                    return;

                // Guardar configuración    
                AppConfig.SetDatabaseConfiguration(Servidor, Usuario, Password, int.Parse(Timeout));

                AgregarLog($"Configuración guardada: {Servidor}");
                _logger.LogInfo("Configuración de base de datos guardada: {Servidor}", Servidor);

                // Usar ApplicationRestartHelper con la ventana actual  
                ApplicationRestartHelper.PromptAndRestartIfConfirmed("Base de Datos", this);
            }
            catch (Exception ex)
            {
                var mensaje = $"Error al guardar configuración: {ex.Message}";
                MessageBox.Show(mensaje, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                AgregarLog(mensaje);
                _logger.LogError(ex, "Error guardando configuración de base de datos");
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
                    DatabaseConfigManager.EliminarConfiguracion();
                    CargarConfiguracionActual();
                    AgregarLog("Configuración restablecida a valores por defecto");
                    _logger.LogInfo("Configuración de base de datos restablecida");
                }
            }
            catch (Exception ex)
            {
                var mensaje = $"Error al restablecer configuración: {ex.Message}";
                MessageBox.Show(mensaje, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                AgregarLog(mensaje);
                _logger.LogError(ex, "Error restableciendo configuración de base de datos");
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