// ConsultasReportesWindow.xaml.cs - CÓDIGO COMPLETO ACTUALIZADO  
using AplicacionDespacho.Models;
using AplicacionDespacho.Modules.Despacho.ViewModels;
using AplicacionDespacho.Services.DataAccess;
using AplicacionDespacho.utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AplicacionDespacho.Modules.Despacho.Views;
namespace AplicacionDespacho
{
    public partial class ConsultasReportesWindow : Window
    {
        private AccesoDatosViajes _accesoDatosViajes;
        private List<EmpresaTransporte> _listaEmpresas;
        private List<Conductor> _listaConductores;
        private List<Viaje> _resultadosBusqueda;

        public ConsultasReportesWindow()
        {
            InitializeComponent();
            _accesoDatosViajes = new AccesoDatosViajes();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CargarDatosIniciales();
        }
        //cargar datos
        private void CargarDatosIniciales()
        {
            try
            {
                // Cargar empresas    
                _listaEmpresas = _accesoDatosViajes.ObtenerEmpresas();
                cmbEmpresa.ItemsSource = _listaEmpresas;
                cmbEmpresa.DisplayMemberPath = "NombreEmpresa";
                cmbEmpresa.SelectedValuePath = "EmpresaId";

                // Agregar opción "Todas" al inicio    
                var empresasConTodas = new List<EmpresaTransporte>
                {
                    new EmpresaTransporte { EmpresaId = -1, NombreEmpresa = "-- Todas las Empresas --" }
                };
                empresasConTodas.AddRange(_listaEmpresas);
                cmbEmpresa.ItemsSource = empresasConTodas;

                // Cargar todos los viajes inicialmente    
                BuscarViajes();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos iniciales: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void cmbEmpresa_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbEmpresa.SelectedItem is EmpresaTransporte empresaSeleccionada && empresaSeleccionada.EmpresaId != -1)
            {
                try
                {
                    _listaConductores = _accesoDatosViajes.ObtenerConductoresPorEmpresa(empresaSeleccionada.EmpresaId);

                    var conductoresConTodos = new List<Conductor>
                    {
                        new Conductor { ConductorId = -1, NombreConductor = "-- Todos los Conductores --" }
                    };
                    conductoresConTodos.AddRange(_listaConductores);

                    cmbConductor.ItemsSource = conductoresConTodos;
                    cmbConductor.DisplayMemberPath = "NombreConductor";
                    cmbConductor.SelectedValuePath = "ConductorId";
                    cmbConductor.SelectedIndex = 0;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cargar conductores: {ex.Message}", "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                cmbConductor.ItemsSource = new List<Conductor>
                {
                    new Conductor { ConductorId = -1, NombreConductor = "-- Todos los Conductores --" }
                };
                cmbConductor.DisplayMemberPath = "NombreConductor";
                cmbConductor.SelectedValuePath = "ConductorId";
                cmbConductor.SelectedIndex = 0;
            }
        }

        private void btnBuscar_Click(object sender, RoutedEventArgs e)
        {
            BuscarViajes();
        }

        // MÉTODO ACTUALIZADO CON BÚSQUEDA POR PALLET  
        private void BuscarViajes()
        {
            try
            {
                if (!ValidarFechas())
                {
                    return; // Salir si las validaciones fallan  
                }
                string numeroGuia = string.IsNullOrWhiteSpace(txtNumeroGuia.Text) ? null : txtNumeroGuia.Text.Trim();
                string numeroPallet = string.IsNullOrWhiteSpace(txtNumeroPallet.Text) ? null : txtNumeroPallet.Text.Trim().ToUpper();
                int? empresaId = (cmbEmpresa.SelectedValue != null && (int)cmbEmpresa.SelectedValue != -1) ? (int)cmbEmpresa.SelectedValue : null;
                int? conductorId = (cmbConductor.SelectedValue != null && (int)cmbConductor.SelectedValue != -1) ? (int)cmbConductor.SelectedValue : null;
                DateTime? fechaDesde = dpFechaDesde.SelectedDate;
                DateTime? fechaHasta = dpFechaHasta.SelectedDate;

                _resultadosBusqueda = _accesoDatosViajes.BuscarViajesConFiltros(numeroGuia, empresaId, conductorId, fechaDesde, fechaHasta, numeroPallet);
                dgResultados.ItemsSource = _resultadosBusqueda;

                // Mostrar mensaje si no hay resultados    
                if (_resultadosBusqueda.Count == 0)
                {
                    MessageBox.Show("No se encontraron viajes que coincidan con los criterios de búsqueda.",
                                   "Sin Resultados", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al buscar viajes: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // MÉTODO ACTUALIZADO CON LIMPIEZA DEL CAMPO PALLET  
        private void btnLimpiar_Click(object sender, RoutedEventArgs e)
        {
            txtNumeroGuia.Text = string.Empty;
            txtNumeroPallet.Text = string.Empty; // NUEVO: Limpiar campo de pallet  
            cmbEmpresa.SelectedIndex = 0;
            cmbConductor.SelectedIndex = 0;
            dpFechaDesde.SelectedDate = null;
            dpFechaHasta.SelectedDate = null;

            // Recargar todos los viajes    
            BuscarViajes();
        }

        private void dgResultados_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Habilitar botones según la selección    
            bool haySeleccion = dgResultados.SelectedItem != null;
            btnGenerarReporte.IsEnabled = haySeleccion;
            btnImprimirDespacho.IsEnabled = haySeleccion;
            // Habilitar "Reabrir Viaje" solo para viajes finalizados  
            if (dgResultados.SelectedItem is Viaje viajeSeleccionado)
            {
                btnReabrirViaje.IsEnabled = viajeSeleccionado.Estado == "Finalizado";
            }
            else
            {
                btnReabrirViaje.IsEnabled = false;
            }
        }

        private void btnGenerarReporte_Click(object sender, RoutedEventArgs e)
        {
            if (dgResultados.SelectedItem is Viaje viajeSeleccionado)
            {
                try
                {
                    // Obtener pallets del viaje    
                    var pallets = _accesoDatosViajes.ObtenerPalletsDeViaje(viajeSeleccionado.ViajeId);

                    // Crear ventana de reporte    
                    var ventanaReporte = new ReporteViajeWindow(viajeSeleccionado, pallets);
                    ventanaReporte.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al generar reporte: {ex.Message}", "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Seleccione un viaje para generar el reporte.", "Selección Requerida",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnImprimirDespacho_Click(object sender, RoutedEventArgs e)
        {
            if (dgResultados.SelectedItem is Viaje viajeSeleccionado)
            {
                try
                {
                    // Obtener pallets del viaje    
                    var pallets = _accesoDatosViajes.ObtenerPalletsDeViaje(viajeSeleccionado.ViajeId);

                    // Crear ventana de impresión    
                    var ventanaImpresion = new ImpresionDespachoWindow(viajeSeleccionado, pallets);
                    ventanaImpresion.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al preparar impresión: {ex.Message}", "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Seleccione un viaje para imprimir el despacho.", "Selección Requerida",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void btnReabrirViaje_Click(object sender, RoutedEventArgs e)
        {
            if (dgResultados.SelectedItem is Viaje viajeSeleccionado)
            {
                if (viajeSeleccionado.Estado != "Finalizado")
                {
                    MessageBox.Show("Solo se pueden reabrir viajes finalizados.", "Acción No Permitida",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var resultado = MessageBox.Show($"¿Está seguro de reabrir el viaje #{viajeSeleccionado.NumeroViaje}?",
                                               "Confirmar Reapertura", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    try
                    {
                        _accesoDatosViajes.ReabrirViaje(viajeSeleccionado.ViajeId);

                        // NUEVO: Notificar vía SignalR después de la actualización exitosa  
                        NotificarReaperturaViaje(viajeSeleccionado.ViajeId);

                        MessageBox.Show("Viaje reabierto exitosamente.", "Éxito",
                                       MessageBoxButton.OK, MessageBoxImage.Information);

                        // Refrescar la búsqueda para mostrar el cambio de estado    
                        BuscarViajes();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al reabrir viaje: {ex.Message}", "Error",
                                       MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void NotificarReaperturaViaje(int viajeId)
        {
            // Obtener referencia al SignalRService desde MainWindow o ViewModelPrincipal  
            if (Application.Current.MainWindow is MainWindow mainWindow &&
                mainWindow.DataContext is ViewModelPrincipal viewModel)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await viewModel.SignalRService.NotifyTripReopenedAsync(viajeId.ToString());
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error notificando reapertura de viaje: {ex.Message}");
                    }
                });
            }
        }

        private void btnReporteConsolidado_Click(object sender, RoutedEventArgs e)
        {
            if (dpFechaDesde.SelectedDate == null || dpFechaHasta.SelectedDate == null)
            {
                MessageBox.Show("Debe seleccionar un rango de fechas para generar el reporte consolidado.",
                               "Fechas Requeridas", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // AGREGAR AQUÍ: Validación de fechas antes del try  
            if (!ValidarFechas())
            {
                return; // Salir si las validaciones fallan  
            }
            try
            {
                // Obtener filtros actuales  
                int? empresaId = (cmbEmpresa.SelectedValue != null && (int)cmbEmpresa.SelectedValue != -1)
                                ? (int)cmbEmpresa.SelectedValue : null;
                int? conductorId = (cmbConductor.SelectedValue != null && (int)cmbConductor.SelectedValue != -1)
                                  ? (int)cmbConductor.SelectedValue : null;

                var palletsEnviados = _accesoDatosViajes.ObtenerPalletsEnviadosPorFechas(
                    dpFechaDesde.SelectedDate.Value,
                    dpFechaHasta.SelectedDate.Value,
                    empresaId,
                    conductorId);

                if (palletsEnviados.Count == 0)
                {
                    MessageBox.Show("No se encontraron pallets enviados en el rango de fechas seleccionado.",
                                   "Sin Datos", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Crear ventana de reporte consolidado  
                var ventanaReporte = new ReporteConsolidadoWindow(palletsEnviados,
                    dpFechaDesde.SelectedDate.Value, dpFechaHasta.SelectedDate.Value);
                ventanaReporte.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar reporte consolidado: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void dpFecha_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            ValidarFechasEnTiempoReal();
            // Habilitar botón de reporte consolidado cuando hay rango de fechas  
            btnReporteConsolidado.IsEnabled = dpFechaDesde.SelectedDate.HasValue &&
                                             dpFechaHasta.SelectedDate.HasValue &&
                                             ValidarFechas();
        }

        private bool ValidarFechas()
        {
            DateTime? fechaDesde = dpFechaDesde.SelectedDate;
            DateTime? fechaHasta = dpFechaHasta.SelectedDate;
            DateTime fechaActual = FechaOperacionalHelper.ObtenerFechaOperacionalActual().Date;

            // Validar fechas futuras  
            if (fechaDesde.HasValue && fechaDesde.Value.Date > fechaActual)
            {
                MessageBox.Show("La fecha 'Desde' no puede ser una fecha futura.", "Validación de Fechas",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (fechaHasta.HasValue && fechaHasta.Value.Date > fechaActual)
            {
                MessageBox.Show("La fecha 'Hasta' no puede ser una fecha futura.", "Validación de Fechas",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Validar que fecha desde no sea mayor que fecha hasta  
            if (fechaDesde.HasValue && fechaHasta.HasValue && fechaDesde.Value.Date > fechaHasta.Value.Date)
            {
                MessageBox.Show("La fecha 'Desde' no puede ser mayor que la fecha 'Hasta'.", "Validación de Fechas",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void ValidarFechasEnTiempoReal()
        {
            DateTime? fechaDesde = dpFechaDesde.SelectedDate;
            DateTime? fechaHasta = dpFechaHasta.SelectedDate;
            DateTime fechaActual = FechaOperacionalHelper.ObtenerFechaOperacionalActual().Date;

            // Resetear colores de fondo  
            dpFechaDesde.Background = System.Windows.Media.Brushes.White;
            dpFechaHasta.Background = System.Windows.Media.Brushes.White;

            // Validar y marcar fechas futuras  
            if (fechaDesde.HasValue && fechaDesde.Value.Date > fechaActual)
            {
                dpFechaDesde.Background = System.Windows.Media.Brushes.LightCoral;
            }

            if (fechaHasta.HasValue && fechaHasta.Value.Date > fechaActual)
            {
                dpFechaHasta.Background = System.Windows.Media.Brushes.LightCoral;
            }

            // Validar rango de fechas  
            if (fechaDesde.HasValue && fechaHasta.HasValue && fechaDesde.Value.Date > fechaHasta.Value.Date)
            {
                dpFechaDesde.Background = System.Windows.Media.Brushes.LightCoral;
                dpFechaHasta.Background = System.Windows.Media.Brushes.LightCoral;
            }
        }
    }
}