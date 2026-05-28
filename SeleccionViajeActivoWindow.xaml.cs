// SeleccionViajeActivoWindow.xaml.cs - Versión corregida  
using AplicacionDespacho.Models;
using AplicacionDespacho.utilities;
using System.Collections.Generic;
using System.Windows;
using System.Threading.Tasks;

namespace AplicacionDespacho
{
    public partial class SeleccionViajeActivoWindow : Window
    {
        public Viaje ViajeSeleccionado { get; private set; } // Esta línea debe existir  
        private readonly List<Viaje> _viajesOriginales;

        public SeleccionViajeActivoWindow(List<Viaje> viajesActivos)
        {
            InitializeComponent();

            _viajesOriginales = viajesActivos;
            dgViajesActivos.ItemsSource = viajesActivos;

            // Actualizar estados una sola vez al cargar  
            _ = ActualizarEstadosViajes();

            System.Diagnostics.Debug.WriteLine($"[DEBUG] 📋 Ventana cargada con {viajesActivos.Count} viajes");
        }

        private async void btnActualizar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Deshabilitar botón durante la actualización  
                btnActualizar.IsEnabled = false;
                btnActualizar.Content = "Actualizando...";

                // Actualizar estados desde la base de datos  
                await ActualizarEstadosViajes();

                System.Diagnostics.Debug.WriteLine("[DEBUG] ✅ Actualización manual completada");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Error en actualización manual: {ex.Message}");
            }
            finally
            {
                // Rehabilitar botón  
                btnActualizar.IsEnabled = true;
                btnActualizar.Content = "Actualizar";
            }
        }
        private async Task ActualizarEstadosViajes()
        {
            try
            {
                btnActualizar.IsEnabled = false;
                btnActualizar.Content = "Actualizando...";

                // Una sola llamada para actualizar todos los estados  
                await ViajeTrackerDB.ActualizarEstadosAsync();

                // Actualizar propiedades EstaEnUso de todos los viajes  
                foreach (var viaje in _viajesOriginales)
                {
                    viaje.EstaEnUso = ViajeTrackerDB.EstaEnUsoLocal(viaje.NumeroGuia);
                }

                dgViajesActivos.Items.Refresh();
                System.Diagnostics.Debug.WriteLine("[DEBUG] ✅ Estados de viajes actualizados");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Error actualizando estados: {ex.Message}");
            }
            finally
            {
                btnActualizar.IsEnabled = true;
                btnActualizar.Content = "Actualizar";
            }
        }

        private async void btnContinuar_Click(object sender, RoutedEventArgs e)
        {
            if (dgViajesActivos.SelectedItem is Viaje viajeSeleccionado)
            {
                try
                {
                    // VALIDACIÓN CRÍTICA: Verificar estado actual en base de datos  
                    bool estaEnUso = await ViajeTrackerDB.EstaEnUsoAsync(viajeSeleccionado.NumeroGuia);

                    if (estaEnUso)
                    {
                        MessageBox.Show($"El viaje {viajeSeleccionado.NumeroGuia} está siendo utilizado por otro cliente.\\n\\n" +
                                       "Por favor, seleccione otro viaje o actualice la lista.",
                                       "Viaje No Disponible",
                                       MessageBoxButton.OK, MessageBoxImage.Warning);

                        // Actualizar automáticamente la lista para mostrar el estado actual  
                        await ViajeTrackerDB.ActualizarEstadosAsync();
                        dgViajesActivos.Items.Refresh();
                        return;
                    }

                    // Si el viaje está libre, proceder  
                    ViajeSeleccionado = viajeSeleccionado;
                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ❌ Error validando viaje: {ex.Message}");
                    MessageBox.Show($"Error al validar el estado del viaje: {ex.Message}", "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }/*
                this.ViajeSeleccionado = viajeSeleccionado; // Usar 'this.' para claridad  
                this.DialogResult = true;
                this.Close();*/
            }
            else
            {
                MessageBox.Show("Seleccione un viaje para continuar.", "Selección Requerida",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}