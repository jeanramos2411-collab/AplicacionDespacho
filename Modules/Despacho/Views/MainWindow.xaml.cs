using AplicacionDespacho.Models;
using AplicacionDespacho.Services.DataAccess;
using AplicacionDespacho.utilities;
using AplicacionDespacho.Modules.Despacho.ViewModels;  // ACTUALIZADO  

using System;
using System.Windows;
using System.Windows.Controls;
using AplicacionDespacho.Modules.Common.Views;


namespace AplicacionDespacho.Modules.Despacho.Views
{
    public partial class MainWindow : Window
    {

        private AccesoDatosViajes _accesoDatosViajes; // AGREGAR ESTA LÍNEA 
        public MainWindow()
        {
            InitializeComponent();

            var accesoDatosPallet = new AccesoDatosPallet();
            _accesoDatosViajes = new AccesoDatosViajes(); // AGREGAR ESTA LÍNEA 
            var viewModel = new ViewModelPrincipal(accesoDatosPallet);
            this.DataContext = viewModel;

            // NUEVO: Manejar cierre de ventana para desconectar SignalR  
            this.Closing += async (s, e) =>
            {
                var vm = this.DataContext as ViewModelPrincipal;
                if (vm?.SignalRService != null)
                {
                    await vm.SignalRService.StopConnectionAsync();
                }
            };

            // NUEVO: Inicializar ViajeTrackerDB en lugar de ViajeTracker  
            ViajeTrackerDB.Initialize(_accesoDatosViajes);

            // MODIFICADO: Manejar cierre de ventana para limpiar viajes en BD  
            this.Closing += async (s, e) =>
            {
                var vm = this.DataContext as ViewModelPrincipal;
                if (vm?.ViajeActivo != null)
                {
                    // Liberar viaje en base de datos  
                    await ViajeTrackerDB.MarcarViajeLibreAsync(vm.ViajeActivo.NumeroGuia);
                }

                // Detener el servicio de polling  
                ViajeTrackerDB.Stop();

                // Mantener limpieza SignalR si aún se usa para otras funciones  
                if (vm?.SignalRService != null)
                {
                    await vm.SignalRService.StopConnectionAsync();
                }
            };
            // Suscribirse a eventos para feedback visual en la UI  
            viewModel.SignalRService.PalletNumberReceived += OnPalletNumberReceivedFromMobile;
           
            //////////////////////////////////////////////////////////////////////////////


        }

        // NUEVO: Método para feedback visual en MainWindow  
        private void OnPalletNumberReceivedFromMobile(string palletNumber, string deviceId)
        {
            Dispatcher.Invoke(() =>
            {
                // SUGERENCIA DE MEJORA: Cambiar color del TextBox temporalmente  
                txtNumeroPallet.Background = System.Windows.Media.Brushes.LightGreen;

                // Restaurar color después de 2 segundos  
                var timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(2);
                timer.Tick += (s, e) =>
                {
                    txtNumeroPallet.Background = System.Windows.Media.Brushes.White;
                    timer.Stop();
                };
                timer.Start();
            });
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Ya no necesitamos cargar datos iniciales aquí        
            // porque se manejan en las ventanas modales
                CargarVariedades(); // AGREGAR ESTA LÍNEA 
                CargarEmbalajes();
        }

        private void CargarVariedades() // AGREGAR TODO ESTE MÉTODO  
        {
            try
            {
                var variedades = _accesoDatosViajes.ObtenerTodasLasVariedades();
                cmbVariedad.ItemsSource = variedades;
                // Para pallets bicolor  
                cmbVariedad1.ItemsSource = variedades;
                cmbVariedad2.ItemsSource = variedades;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar variedades: {ex.Message}");
            }
        }
        private void btnAbrirAdmin_Click(object sender, RoutedEventArgs e)
        {
            var gestionEmpresasWindow = new GestionEmpresasWindow();
            gestionEmpresasWindow.ShowDialog();
        }

        private void btnConsultasReportes_Click(object sender, RoutedEventArgs e)
        {
            var ventanaConsultas = new ConsultasReportesWindow();
            ventanaConsultas.ShowDialog();
        }

        private void btnGestionPesos_Click(object sender, RoutedEventArgs e)
        {
            var ventanaPesos = new PesosPorEmbalajeWindow();
            ventanaPesos.ShowDialog();
        }

        private void btnRevertir_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = this.DataContext as ViewModelPrincipal;
            if (viewModel?.ComandoRevertirCambios.CanExecute(null) == true)
            {
                viewModel.ComandoRevertirCambios.Execute(null);
            }
            else
            {
                MessageBox.Show("No hay pallet seleccionado para revertir.", "Advertencia",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Método para manejo de selección de pallets    
        private void dgPallets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var viewModel = this.DataContext as ViewModelPrincipal;
            if (viewModel?.PalletSeleccionado != null)
            {
                System.Diagnostics.Debug.WriteLine($"Pallet seleccionado: {viewModel.PalletSeleccionado.NumeroPallet}");
            }
        }
        private void MenuSalir_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void MenuConfiguracionBD_Click(object sender, RoutedEventArgs e)
        {
            var ventanaConfig = new AplicacionDespacho.Modules.Common.Views.ConfiguracionBaseDatosWindow();
            ventanaConfig.Owner = this;
            ventanaConfig.ShowDialog();
        }

        private void MenuConfiguracionSignalR_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = this.DataContext as ViewModelPrincipal;
            var ventanaConfig = new AplicacionDespacho.Modules.Common.Views.ConfiguracionSignalRWindow(viewModel?.SignalRService);
            ventanaConfig.Owner = this;
            ventanaConfig.ShowDialog();
        }
        private void CargarEmbalajes()
        {
            try
            {
                var embalajes = _accesoDatosViajes.ObtenerTodosLosEmbalajes();
                cmbEmbalaje.ItemsSource = embalajes;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar embalajes: {ex.Message}");
            }
        }
        private async void VerificarConfiguracionBD()
        {
            try
            {
                // Verificar que la tabla existe  
                var testResult = await ViajeTrackerDB.EstaEnUsoAsync("TEST");
                System.Diagnostics.Debug.WriteLine("[DEBUG] ✅ Configuración de BD verificada correctamente");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error en configuración de base de datos: {ex.Message}",
                               "Error de Configuración",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
            }
        }
        private void MenuAcercaDe_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Aplicación de Despacho\nVersión 1.0\n\nSistema de gestión de despachos y pallets",
                           "Acerca de",
                           MessageBoxButton.OK,
                           MessageBoxImage.Information);
        }
        
    }
}