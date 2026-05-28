// ReporteViajeWindow.xaml.cs - Versión escalable  
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using AplicacionDespacho.Models;
using AplicacionDespacho.Models.Export;
using AplicacionDespacho.Services.Export;

namespace AplicacionDespacho
{
    public partial class ReporteViajeWindow : Window
    {
        private Viaje _viaje;
        private List<InformacionPallet> _pallets;
        private readonly ExportServiceFactory _exportFactory;

        public ReporteViajeWindow(Viaje viaje, List<InformacionPallet> pallets)
        {
            InitializeComponent();
            _viaje = viaje;
            _pallets = pallets;
            _exportFactory = new ExportServiceFactory();
            CargarDatos();
        }

        private void CargarDatos()
        {
            // Cargar información del viaje      
            txtFecha.Text = _viaje.Fecha.ToString("dd/MM/yyyy");
            txtNumeroViaje.Text = _viaje.NumeroViaje.ToString();
            txtNumeroGuia.Text = _viaje.NumeroGuia;
            txtResponsable.Text = _viaje.Responsable;
            txtEmpresa.Text = _viaje.NombreEmpresa;
            txtConductor.Text = _viaje.NombreConductor;
            txtPlacaVehiculo.Text = _viaje.PlacaVehiculo ?? "N/A";
            txtPuntoPartida.Text = _viaje.PuntoPartida ?? "N/A";
            txtPuntoLlegada.Text = _viaje.PuntoLlegada ?? "N/A";

            // Cargar pallets      
            dgPallets.ItemsSource = _pallets;

            // Calcular totales generales  
            txtTotalPallets.Text = _pallets.Count.ToString();
            txtTotalCajas.Text = _pallets.Sum(p => p.CajasParaReporte).ToString();
            txtPesoTotal.Text = _pallets.Sum(p => p.PesoTotal).ToString("F3");

            // Calcular contadores por tipo (PC, PH, CT, EN)  
            var totalPC = _pallets.Count(p => p.EsPC);
            var totalPH = _pallets.Count(p => p.EsPH);
            var totalCT = _pallets.Count(p => p.EsCT);
            var totalEN = _pallets.Count(p => p.EsEN);

            // Mostrar contadores PC/PH (siempre visibles)  
            txtTotalPC.Text = $"{totalPC}";
            txtTotalPH.Text = $"{totalPH}";

            // Mostrar contadores CT/EN  
            txtTotalCT.Text = $"{totalCT}";
            txtTotalEN.Text = $"{totalEN}";

            // Mostrar panel CT/EN solo si hay pallets de estos tipos  
            bool tieneCTEN = totalCT > 0 || totalEN > 0;
            panelCTEN.Visibility = tieneCTEN ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void btnExportar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Mostrar opciones de formato  
                var formatoSeleccionado = MostrarDialogoFormato();
                if (formatoSeleccionado == null) return;

                // Configurar el diálogo para guardar archivo  
                var exportService = _exportFactory.CreateViajeExporter(formatoSeleccionado.Value);
                var extension = exportService.GetDefaultFileExtension(formatoSeleccionado.Value);

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = $"{formatoSeleccionado} files (*.{extension})|*.{extension}",
                    DefaultExt = extension,
                    FileName = $"Reporte_Viaje_{_viaje.NumeroViaje}_{_viaje.Fecha:yyyyMMdd}.{extension}"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Preparar datos para exportación  
                    var reportData = new ViajeReportData
                    {
                        Viaje = _viaje,
                        Pallets = _pallets
                    };

                    var options = new ExportOptions
                    {
                        FilePath = saveFileDialog.FileName,
                        Format = formatoSeleccionado.Value,
                        Title = "REPORTE DETALLADO DE VIAJE"
                    };

                    // Exportar usando el servicio escalable  
                    var result = await exportService.ExportAsync(new[] { reportData }, options);

                    if (result.Success)
                    {
                        MessageBox.Show($"Reporte exportado exitosamente a:\n{result.FilePath}",
                                       "Exportación Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Error al exportar: {result.Message}", "Error",
                                       MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private ExportFormat? MostrarDialogoFormato()
        {
            // Diálogo simple para seleccionar formato  
            var formatos = _exportFactory.GetSupportedFormats().ToList();

            if (formatos.Count == 1)
            {
                return formatos.First();
            }

            // Por ahora, defaultear a Excel. En el futuro se puede crear un diálogo más sofisticado  
            return ExportFormat.Excel;
        }

        private void btnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}