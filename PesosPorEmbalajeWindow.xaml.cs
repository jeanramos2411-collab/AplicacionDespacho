using AplicacionDespacho.Models;
using AplicacionDespacho.Services.DataAccess;
using AplicacionDespacho.utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AplicacionDespacho
{
    public partial class PesosPorEmbalajeWindow : Window
    {
        private AccesoDatosViajes _accesoDatosViajes;
        private List<PesoEmbalaje> _listaPesos;

        public PesosPorEmbalajeWindow()
        {
            InitializeComponent();
            _accesoDatosViajes = new AccesoDatosViajes();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CargarPesosEmbalaje();
        }

        private void CargarPesosEmbalaje()
        {
            try
            {
                var nuevaListaPesos = new List<PesoEmbalaje>();

                // Obtener todos los embalajes únicos de la base Packing_SJP  
                var embalajesUnicos = _accesoDatosViajes.ObtenerTodosLosEmbalajes();

                // Obtener pesos existentes de la base Despachos_SJP  
                var pesosExistentes = _accesoDatosViajes.ObtenerTodosPesosEmbalaje();

                // Crear lista combinada preservando valores existentes  
                foreach (var embalaje in embalajesUnicos)
                {
                    var pesoExistente = pesosExistentes.FirstOrDefault(p => p.NombreEmbalaje.Equals(embalaje, StringComparison.OrdinalIgnoreCase));

                    if (pesoExistente != null)
                    {
                        // Ya existe un peso configurado - mantener el valor existente  
                        nuevaListaPesos.Add(pesoExistente);
                    }
                    else
                    {
                        // Solo crear nuevo registro con peso 0 si no existe  
                        // Verificar si ya está en la lista actual para preservar cambios temporales  
                        var embalajeEnListaActual = _listaPesos?.FirstOrDefault(p =>
                            p.NombreEmbalaje.Equals(embalaje, StringComparison.OrdinalIgnoreCase) &&
                            p.PesoEmbalajeId == 0);

                        if (embalajeEnListaActual != null && embalajeEnListaActual.PesoUnitario > 0)
                        {
                            // Preservar el valor temporal que el usuario pudo haber ingresado  
                            nuevaListaPesos.Add(embalajeEnListaActual);
                        }
                        else
                        {
                            // Crear nuevo registro con peso 0  
                            nuevaListaPesos.Add(new PesoEmbalaje
                            {
                                PesoEmbalajeId = 0, // Indica que es un nuevo registro  
                                NombreEmbalaje = embalaje,
                                PesoUnitario = 0,
                                FechaCreacion = FechaOperacionalHelper.ObtenerFechaOperacionalActual(),
                                Activo = true
                            });
                        }
                    }
                }

                _listaPesos = nuevaListaPesos;
                dgPesosEmbalaje.ItemsSource = _listaPesos;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar los pesos de embalaje: {ex.Message}",
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void dgPesosEmbalaje_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgPesosEmbalaje.SelectedItem is PesoEmbalaje pesoSeleccionado)
            {
                // Cargar datos en el formulario para edición    
                txtPesoEmbalajeId.Text = pesoSeleccionado.PesoEmbalajeId.ToString();
                txtNombreEmbalaje.Text = pesoSeleccionado.NombreEmbalaje;
                txtPesoUnitario.Text = pesoSeleccionado.PesoUnitario.ToString("F3", CultureInfo.InvariantCulture);
                txtTotalCajasFichaTecnica.Text = pesoSeleccionado.TotalCajasFichaTecnica?.ToString() ?? "";
                // Cambiar comportamiento del botón según contexto  
                if (pesoSeleccionado.PesoEmbalajeId == 0)
                {
                    // Es un embalaje nuevo sin peso - habilitar guardar  
                    btnGuardar.IsEnabled = true;
                    btnActualizar.IsEnabled = false;
                    btnActualizar.Content = "Actualizar desde Base";
                }
                else
                {
                    // Es un embalaje existente - habilitar actualizar  
                    btnActualizar.IsEnabled = true;
                    btnActualizar.Content = "Actualizar Registro";
                    btnGuardar.IsEnabled = false;
                }
            }
            else
            {
                LimpiarFormulario();
                btnActualizar.Content = "Actualizar desde Base";
                btnActualizar.IsEnabled = true;
            }
        }

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (ValidarFormulario())
            {
                try
                {
                    // Verificar si es un embalaje seleccionado de la grilla  
                    if (dgPesosEmbalaje.SelectedItem is PesoEmbalaje embalajeSeleccionado && embalajeSeleccionado.PesoEmbalajeId == 0)
                    {
                        // Es un embalaje nuevo sin peso configurado  
                        decimal nuevoPeso = decimal.Parse(txtPesoUnitario.Text, CultureInfo.InvariantCulture);

                        if (nuevoPeso > 0)
                        {
                            var nuevoPesoEmbalaje = new PesoEmbalaje
                            {
                                NombreEmbalaje = txtNombreEmbalaje.Text.Trim(),
                                PesoUnitario = nuevoPeso,
                                TotalCajasFichaTecnica = int.TryParse(txtTotalCajasFichaTecnica.Text, out int totalCajas) ? totalCajas : (int?)null,
                                FechaCreacion = FechaOperacionalHelper.ObtenerFechaOperacionalActual(),
                                Activo = true
                            };

                            _accesoDatosViajes.GuardarPesoEmbalaje(nuevoPesoEmbalaje);
                            MessageBox.Show("Peso de embalaje guardado con éxito.", "Éxito",
                                           MessageBoxButton.OK, MessageBoxImage.Information);

                            LimpiarFormulario();
                            CargarPesosEmbalaje();
                        }
                        else
                        {
                            MessageBox.Show("El peso debe ser mayor que cero para guardar el embalaje.", "Validación",
                                           MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    else
                    {
                        // Verificar si ya existe un peso para este embalaje (validación adicional)  
                        var pesoExistente = _listaPesos.FirstOrDefault(p =>
                            p.NombreEmbalaje.Equals(txtNombreEmbalaje.Text.Trim(), StringComparison.OrdinalIgnoreCase) &&
                            p.PesoEmbalajeId > 0);

                        if (pesoExistente != null)
                        {
                            MessageBox.Show("Ya existe un peso registrado para este embalaje. Use 'Actualizar' para modificarlo.",
                                           "Embalaje Duplicado", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        // Crear nuevo embalaje manualmente  
                        decimal nuevoPeso = decimal.Parse(txtPesoUnitario.Text, CultureInfo.InvariantCulture);

                        if (nuevoPeso > 0)
                        {
                            var nuevoPesoEmbalaje = new PesoEmbalaje
                            {
                                NombreEmbalaje = txtNombreEmbalaje.Text.Trim(),
                                PesoUnitario = nuevoPeso,
                                FechaCreacion = FechaOperacionalHelper.ObtenerFechaOperacionalActual(),
                                Activo = true
                            };

                            _accesoDatosViajes.GuardarPesoEmbalaje(nuevoPesoEmbalaje);
                            MessageBox.Show("Peso de embalaje guardado con éxito.", "Éxito",
                                           MessageBoxButton.OK, MessageBoxImage.Information);

                            LimpiarFormulario();
                            CargarPesosEmbalaje();
                        }
                        else
                        {
                            MessageBox.Show("El peso debe ser mayor que cero para guardar el embalaje.", "Validación",
                                           MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al guardar el peso de embalaje: {ex.Message}",
                                   "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnActualizar_Click(object sender, RoutedEventArgs e)
        {
            // FUNCIONALIDAD DUAL: Actualizar registro individual O cargar desde base  
            if (dgPesosEmbalaje.SelectedItem != null && ValidarFormulario() && !string.IsNullOrEmpty(txtPesoEmbalajeId.Text))
            {
                // Hay un elemento seleccionado - actualizar ese registro específico  
                try
                {
                    var pesoActualizar = new PesoEmbalaje
                    {
                        PesoEmbalajeId = int.Parse(txtPesoEmbalajeId.Text),
                        NombreEmbalaje = txtNombreEmbalaje.Text.Trim(),
                        PesoUnitario = decimal.Parse(txtPesoUnitario.Text, CultureInfo.InvariantCulture),
                        TotalCajasFichaTecnica = int.TryParse(txtTotalCajasFichaTecnica.Text, out int totalCajas) ? totalCajas : (int?)null,
                        FechaModificacion = FechaOperacionalHelper.ObtenerFechaOperacionalActual(),
                        Activo = true
                    };

                    _accesoDatosViajes.ActualizarPesoEmbalaje(pesoActualizar);
                    MessageBox.Show("Peso de embalaje actualizado con éxito.", "Éxito",
                                   MessageBoxButton.OK, MessageBoxImage.Information);

                    LimpiarFormulario();
                    CargarPesosEmbalaje();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al actualizar el peso de embalaje: {ex.Message}",
                                   "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                // No hay selección - cargar todos los embalajes únicos desde la base  
                CargarPesosEmbalaje();
                MessageBox.Show("Lista actualizada con todos los embalajes únicos de la base de datos.", "Actualización",
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnLimpiar_Click(object sender, RoutedEventArgs e)
        {
            LimpiarFormulario();
        }

        private void btnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private bool ValidarFormulario()
        {
            if (string.IsNullOrWhiteSpace(txtNombreEmbalaje.Text))
            {
                MessageBox.Show("El nombre del embalaje es obligatorio.", "Validación",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNombreEmbalaje.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtPesoUnitario.Text))
            {
                MessageBox.Show("El peso unitario es obligatorio.", "Validación",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPesoUnitario.Focus();
                return false;
            }

            if (!decimal.TryParse(txtPesoUnitario.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal peso) || peso <= 0)
            {
                MessageBox.Show("El peso unitario debe ser un número válido mayor que cero.", "Validación",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPesoUnitario.Focus();
                return false;
            }

            return true;
        }

        private void LimpiarFormulario()
        {
            txtPesoEmbalajeId.Text = string.Empty;
            txtNombreEmbalaje.Text = string.Empty;
            txtPesoUnitario.Text = string.Empty;
            txtTotalCajasFichaTecnica.Text = string.Empty;
            btnGuardar.IsEnabled = true;
            btnActualizar.IsEnabled = true;
            btnActualizar.Content = "Actualizar desde Base";

            dgPesosEmbalaje.SelectedItem = null;
        }
    }
}