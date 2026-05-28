// ReporteConsolidadoWindow.xaml.cs  
using AplicacionDespacho.Models.Export;
using AplicacionDespacho.Models.Reports;
using AplicacionDespacho.Services.Export;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace AplicacionDespacho
{
    public partial class ReporteConsolidadoWindow : Window
    {
        private List<ReporteGeneralPallet> _pallets;
        private DateTime _fechaDesde;
        private DateTime _fechaHasta;

        public ReporteConsolidadoWindow(List<ReporteGeneralPallet> pallets, DateTime fechaDesde, DateTime fechaHasta)
        {
            InitializeComponent();
            _pallets = pallets;
            _fechaDesde = fechaDesde;
            _fechaHasta = fechaHasta;
            CargarDatos();
        }

        private void CargarDatos()
        {
            // Cargar información del período    
            txtFechaDesde.Text = _fechaDesde.ToString("dd/MM/yyyy");
            txtFechaHasta.Text = _fechaHasta.ToString("dd/MM/yyyy");
            txtTotalPallets.Text = _pallets.Count.ToString();

            // Cargar pallets en el DataGrid principal    
            dgPallets.ItemsSource = _pallets;

            // Generar resumen por variedad    
            var resumenVariedad = _pallets
                .GroupBy(p => p.VariedadParaReporte)
                .Select(g => new
                {
                    Variedad = g.Key,
                    TotalPallets = g.Count(),
                    TotalCajas = g.Sum(p => p.CajasParaReporte),
                    TotalKilos = g.Sum(p => p.PesoTotal)
                })
                .OrderBy(r => r.Variedad)
                .ToList();

            dgResumenVariedad.ItemsSource = resumenVariedad;

            // Generar resumen por empresa      
            var resumenEmpresa = _pallets
                .GroupBy(p => p.NombreEmpresa)
                .Select(g => new
                {
                    Empresa = g.Key,
                    TotalPallets = g.Count(),
                    TotalCajas = g.Sum(p => p.CajasParaReporte),
                    TotalKilos = g.Sum(p => p.PesoTotal),
                    CantidadViajes = g.Select(p => p.ViajeId).Distinct().Count()
                })
                .OrderBy(r => r.Empresa)
                .ToList();

            dgResumenEmpresa.ItemsSource = resumenEmpresa;

            // Calcular totales generales    
            txtTotalPalletsGeneral.Text = _pallets.Count.ToString();
            txtTotalCajasGeneral.Text = _pallets.Sum(p => p.CajasParaReporte).ToString();
            txtTotalKilosGeneral.Text = _pallets.Sum(p => p.PesoTotal).ToString("F3");

            // NUEVO: Contadores completos para los cuatro tipos de pallets  
            var totalPC = _pallets.Count(p => DeterminarTipoPallet(p.NumeroPallet) == "PC");
            var totalPH = _pallets.Count(p => DeterminarTipoPallet(p.NumeroPallet) == "PH");
            var totalCT = _pallets.Count(p => DeterminarTipoPallet(p.NumeroPallet) == "CT");
            var totalEN = _pallets.Count(p => DeterminarTipoPallet(p.NumeroPallet) == "EN");

            // ESTRATEGIA CONDICIONAL: Detectar presencia de pallets CT/EN  
            bool tieneCTEN = totalCT > 0 || totalEN > 0;

            // Mostrar contadores PC/PH (siempre visibles)  
            txtTotalPC.Text = $"{totalPC}";
            txtTotalPH.Text = $"{totalPH}";

            // Mostrar panel CT/EN solo si existen estos tipos de pallets  
            if (tieneCTEN)
            {
                panelCTEN.Visibility = Visibility.Visible;
                txtTotalCT.Text = $"{totalCT}";
                txtTotalEN.Text = $"{totalEN}";
            }
            else
            {
                panelCTEN.Visibility = Visibility.Collapsed;
            }

        }

        private async void btnExportar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Files|*.xlsx",
                    Title = "Guardar Reporte Consolidado",
                    FileName = $"ReporteConsolidado_{_fechaDesde:yyyyMMdd}_{_fechaHasta:yyyyMMdd}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Mostrar indicador de progreso  
                    btnExportar.IsEnabled = false;
                    btnExportar.Content = "Exportando...";

                    var exportService = new ConsolidadoExcelExportService();
                    bool success = await exportService.ExportarReporteConsolidado(
                        _pallets, _fechaDesde, _fechaHasta, saveFileDialog.FileName);

                    if (success)
                    {
                        MessageBox.Show($"Reporte consolidado exportado exitosamente a:\\n{saveFileDialog.FileName}",
                                       "Exportación Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Error al exportar el reporte consolidado.",
                                       "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    // Restaurar botón  
                    btnExportar.IsEnabled = true;
                    btnExportar.Content = "Exportar Excel";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);

                // Restaurar botón en caso de error  
                btnExportar.IsEnabled = true;
                btnExportar.Content = "Exportar Excel";
            }
        }

        private void btnImprimir_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Implementar impresión similar a ImpresionDespachoWindow  
                MessageBox.Show("Funcionalidad de impresión pendiente de implementar.",
                               "Información", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al imprimir: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private string DeterminarTipoPallet(string numeroPallet)
        {
            if (numeroPallet.ToUpper().EndsWith("PC") || numeroPallet.ToUpper().Contains("PC"))
                return "PC";
            else if (numeroPallet.ToUpper().EndsWith("PH") || numeroPallet.ToUpper().Contains("PH"))
                return "PH";
            else if (numeroPallet.ToUpper().EndsWith("CT") || numeroPallet.ToUpper().Contains("CT"))
                return "CT";
            else if (numeroPallet.ToUpper().EndsWith("EN") || numeroPallet.ToUpper().Contains("EN"))
                return "EN";
            else
                return "PC"; // Por defecto PC si no se puede determinar    
        }


    }
}