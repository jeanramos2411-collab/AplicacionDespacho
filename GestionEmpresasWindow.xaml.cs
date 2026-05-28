using AplicacionDespacho.Models;
using AplicacionDespacho.Services.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AplicacionDespacho
{
    public partial class GestionEmpresasWindow : Window
    {
        private AccesoDatosViajes _accesoDatosViajes;
        private EmpresaTransporte _empresaSeleccionada;
        private Conductor _conductorSeleccionado;
        private Vehiculo _vehiculoSeleccionado;

        public GestionEmpresasWindow()
        {
            InitializeComponent();
            _accesoDatosViajes = new AccesoDatosViajes();
            CargarDatos();
        }

        private void CargarDatos()
        {
            try
            {
                CargarEmpresas();

                // Agregar opción "Todas" a los ComboBoxes de filtro  
                var empresasConTodas = new List<EmpresaTransporte>(_accesoDatosViajes.ObtenerEmpresas());
                empresasConTodas.Insert(0, new EmpresaTransporte
                {
                    EmpresaId = -1,
                    NombreEmpresa = "-- Todas las Empresas --"
                });

                cmbEmpresaConductores.ItemsSource = empresasConTodas;
                cmbEmpresaConductores.DisplayMemberPath = "NombreEmpresa";
                cmbEmpresaConductores.SelectedValuePath = "EmpresaId";
                cmbEmpresaConductores.SelectedIndex = 0; // Seleccionar "Todas" por defecto  

                cmbEmpresaVehiculos.ItemsSource = empresasConTodas;
                cmbEmpresaVehiculos.DisplayMemberPath = "NombreEmpresa";
                cmbEmpresaVehiculos.SelectedValuePath = "EmpresaId";
                cmbEmpresaVehiculos.SelectedIndex = 0; // Seleccionar "Todas" por defecto  

                CargarConductores();
                CargarVehiculos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CargarEmpresas()
        {
            try
            {
                var empresas = _accesoDatosViajes.ObtenerEmpresas();
                dgEmpresas.ItemsSource = empresas;

                // Cargar en ComboBoxes  
                cmbEmpresaConductor.ItemsSource = empresas;
                cmbEmpresaConductor.DisplayMemberPath = "NombreEmpresa";
                cmbEmpresaConductor.SelectedValuePath = "EmpresaId";

                cmbEmpresaVehiculo.ItemsSource = empresas;
                cmbEmpresaVehiculo.DisplayMemberPath = "NombreEmpresa";
                cmbEmpresaVehiculo.SelectedValuePath = "EmpresaId";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar empresas: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CargarConductores()
        {
            try
            {
                var conductores = new List<Conductor>();
                var empresas = _accesoDatosViajes.ObtenerEmpresas();

                foreach (var empresa in empresas)
                {
                    var conductoresEmpresa = _accesoDatosViajes.ObtenerConductoresPorEmpresa(empresa.EmpresaId);
                    conductores.AddRange(conductoresEmpresa);
                }

                dgConductores.ItemsSource = conductores;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar conductores: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CargarVehiculos()
        {
            try
            {
                var vehiculos = new List<Vehiculo>();
                var empresas = _accesoDatosViajes.ObtenerEmpresas();

                foreach (var empresa in empresas)
                {
                    var vehiculosEmpresa = _accesoDatosViajes.ObtenerVehiculosPorEmpresa(empresa.EmpresaId);
                    vehiculos.AddRange(vehiculosEmpresa);
                }

                dgVehiculos.ItemsSource = vehiculos;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar vehículos: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        // Eventos de selección  
        private void dgEmpresas_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _empresaSeleccionada = dgEmpresas.SelectedItem as EmpresaTransporte;
            if (_empresaSeleccionada != null)
            {
                txtNombreEmpresa.Text = _empresaSeleccionada.NombreEmpresa;
                txtRUC.Text = _empresaSeleccionada.RUC;
            }
        }

        private void dgConductores_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _conductorSeleccionado = dgConductores.SelectedItem as Conductor;
            if (_conductorSeleccionado != null)
            {
                txtNombreConductor.Text = _conductorSeleccionado.NombreConductor;
                cmbEmpresaConductor.SelectedValue = _conductorSeleccionado.EmpresaId;
            }
        }

        private void dgVehiculos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _vehiculoSeleccionado = dgVehiculos.SelectedItem as Vehiculo;
            if (_vehiculoSeleccionado != null)
            {
                txtPlacaVehiculo.Text = _vehiculoSeleccionado.Placa;
                cmbEmpresaVehiculo.SelectedValue = _vehiculoSeleccionado.EmpresaId;
            }
        }

        // Métodos para agregar nuevos registros  
        private void btnAgregarEmpresa_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtNombreEmpresa.Text) &&
                !string.IsNullOrWhiteSpace(txtRUC.Text))
            {
                try
                {
                    var nuevaEmpresa = new EmpresaTransporte
                    {
                        NombreEmpresa = txtNombreEmpresa.Text.Trim(),
                        RUC = txtRUC.Text.Trim()
                    };

                    _accesoDatosViajes.GuardarEmpresa(nuevaEmpresa);
                    MessageBox.Show("Empresa guardada con éxito.", "Éxito",
                                   MessageBoxButton.OK, MessageBoxImage.Information);

                    LimpiarFormularioEmpresa();
                    CargarEmpresas();
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show(ex.Message, "RUC Duplicado",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al guardar empresa: {ex.Message}", "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Complete todos los campos requeridos.", "Validación",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnAgregarConductor_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtNombreConductor.Text) &&
                cmbEmpresaConductor.SelectedValue != null)
            {
                try
                {
                    var nuevoConductor = new Conductor
                    {
                        NombreConductor = txtNombreConductor.Text.Trim(),
                        EmpresaId = (int)cmbEmpresaConductor.SelectedValue
                    };

                    bool guardado = _accesoDatosViajes.GuardarConductor(nuevoConductor);

                    if (guardado)
                    {
                        MessageBox.Show("Conductor guardado exitosamente.", "Éxito",
                                       MessageBoxButton.OK, MessageBoxImage.Information);
                        LimpiarFormularioConductor();
                        CargarConductores();
                    }
                    else
                    {
                        MessageBox.Show("Ya existe un conductor con ese nombre en la empresa seleccionada.",
                                       "Conductor Duplicado", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al guardar conductor: {ex.Message}", "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Complete todos los campos requeridos.", "Validación",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnAgregarVehiculo_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtPlacaVehiculo.Text) &&
                cmbEmpresaVehiculo.SelectedValue != null)
            {
                try
                {
                    var nuevoVehiculo = new Vehiculo
                    {
                        Placa = txtPlacaVehiculo.Text.Trim().ToUpper(),
                        EmpresaId = (int)cmbEmpresaVehiculo.SelectedValue
                    };

                    bool guardado = _accesoDatosViajes.GuardarVehiculo(nuevoVehiculo);

                    if (guardado)
                    {
                        MessageBox.Show("Vehículo guardado exitosamente.", "Éxito",
                                       MessageBoxButton.OK, MessageBoxImage.Information);
                        LimpiarFormularioVehiculo();
                        CargarVehiculos();
                    }
                    else
                    {
                        MessageBox.Show("Ya existe un vehículo con esa placa registrada.",
                                       "Placa Duplicada", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al guardar vehículo: {ex.Message}", "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Complete todos los campos requeridos.", "Validación",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        // Métodos para editar registros  
        private void btnEditarEmpresa_Click(object sender, RoutedEventArgs e)
        {
            if (_empresaSeleccionada != null &&
                !string.IsNullOrWhiteSpace(txtNombreEmpresa.Text) &&
                !string.IsNullOrWhiteSpace(txtRUC.Text))
            {
                try
                {
                    _empresaSeleccionada.NombreEmpresa = txtNombreEmpresa.Text.Trim();
                    _empresaSeleccionada.RUC = txtRUC.Text.Trim();

                    _accesoDatosViajes.ActualizarEmpresa(_empresaSeleccionada);
                    MessageBox.Show("Empresa actualizada exitosamente.", "Éxito",
                                   MessageBoxButton.OK, MessageBoxImage.Information);

                    CargarEmpresas();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al actualizar empresa: {ex.Message}", "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Seleccione una empresa y complete todos los campos.", "Validación",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnEliminarEmpresa_Click(object sender, RoutedEventArgs e)
        {
            if (_empresaSeleccionada != null)
            {
                var resultado = MessageBox.Show($"¿Está seguro de eliminar la empresa '{_empresaSeleccionada.NombreEmpresa}'?",
                                              "Confirmar Eliminación", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    try
                    {
                        _accesoDatosViajes.EliminarEmpresa(_empresaSeleccionada.EmpresaId);
                        MessageBox.Show("Empresa eliminada exitosamente.", "Éxito",
                                       MessageBoxButton.OK, MessageBoxImage.Information);

                        LimpiarFormularioEmpresa();
                        CargarDatos();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al eliminar empresa: {ex.Message}", "Error",
                                       MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Seleccione una empresa para eliminar.", "Validación",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Métodos auxiliares para limpiar formularios  
        private void LimpiarFormularioEmpresa()
        {
            txtNombreEmpresa.Clear();
            txtRUC.Clear();
            _empresaSeleccionada = null;
            dgEmpresas.SelectedItem = null;
        }

        private void LimpiarFormularioConductor()
        {
            txtNombreConductor.Clear();
            cmbEmpresaConductor.SelectedItem = null;
            _conductorSeleccionado = null;
            dgConductores.SelectedItem = null;
        }

        private void LimpiarFormularioVehiculo()
        {
            txtPlacaVehiculo.Clear();
            cmbEmpresaVehiculo.SelectedItem = null;
            _vehiculoSeleccionado = null;
            dgVehiculos.SelectedItem = null;
        }
        private void btnEditarConductor_Click(object sender, RoutedEventArgs e)
        {
            if (_conductorSeleccionado != null &&
                !string.IsNullOrWhiteSpace(txtNombreConductor.Text) &&
                cmbEmpresaConductor.SelectedValue != null)
            {
                try
                {
                    _conductorSeleccionado.NombreConductor = txtNombreConductor.Text.Trim();
                    _conductorSeleccionado.EmpresaId = (int)cmbEmpresaConductor.SelectedValue;

                    _accesoDatosViajes.ActualizarConductor(_conductorSeleccionado);
                    MessageBox.Show("Conductor actualizado exitosamente.", "Éxito",
                                   MessageBoxButton.OK, MessageBoxImage.Information);

                    CargarConductores();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al actualizar conductor: {ex.Message}", "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Seleccione un conductor y complete todos los campos.", "Validación",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnEditarVehiculo_Click(object sender, RoutedEventArgs e)
        {
            if (_vehiculoSeleccionado != null &&
                !string.IsNullOrWhiteSpace(txtPlacaVehiculo.Text) &&
                cmbEmpresaVehiculo.SelectedValue != null)
            {
                try
                {
                    _vehiculoSeleccionado.Placa = txtPlacaVehiculo.Text.Trim().ToUpper();
                    _vehiculoSeleccionado.EmpresaId = (int)cmbEmpresaVehiculo.SelectedValue;

                    _accesoDatosViajes.ActualizarVehiculo(_vehiculoSeleccionado);
                    MessageBox.Show("Vehículo actualizado exitosamente.", "Éxito",
                                   MessageBoxButton.OK, MessageBoxImage.Information);

                    CargarVehiculos();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al actualizar vehículo: {ex.Message}", "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Seleccione un vehículo y complete todos los campos.", "Validación",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnEliminarConductor_Click(object sender, RoutedEventArgs e)
        {
            if (_conductorSeleccionado != null)
            {
                var resultado = MessageBox.Show($"¿Está seguro de eliminar el conductor '{_conductorSeleccionado.NombreConductor}'?",
                                              "Confirmar Eliminación", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    try
                    {
                        _accesoDatosViajes.EliminarConductor(_conductorSeleccionado.ConductorId);
                        MessageBox.Show("Conductor eliminado exitosamente.", "Éxito",
                                       MessageBoxButton.OK, MessageBoxImage.Information);

                        LimpiarFormularioConductor();
                        CargarConductores();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al eliminar conductor: {ex.Message}", "Error",
                                       MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Seleccione un conductor para eliminar.", "Validación",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnEliminarVehiculo_Click(object sender, RoutedEventArgs e)
        {
            if (_vehiculoSeleccionado != null)
            {
                var resultado = MessageBox.Show($"¿Está seguro de eliminar el vehículo '{_vehiculoSeleccionado.Placa}'?",
                                              "Confirmar Eliminación", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    try
                    {
                        _accesoDatosViajes.EliminarVehiculo(_vehiculoSeleccionado.VehiculoId);
                        MessageBox.Show("Vehículo eliminado exitosamente.", "Éxito",
                                       MessageBoxButton.OK, MessageBoxImage.Information);

                        LimpiarFormularioVehiculo();
                        CargarVehiculos();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al eliminar vehículo: {ex.Message}", "Error",
                                       MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Seleccione un vehículo para eliminar.", "Validación",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private void btnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Métodos para gestión de empresas  
        private void btnNuevaEmpresa_Click(object sender, RoutedEventArgs e)
        {
            LimpiarFormularioEmpresa();
        }

        private void btnGuardarEmpresa_Click(object sender, RoutedEventArgs e)
        {
            // Usar el método btnAgregarEmpresa_Click que ya tienes implementado  
            btnAgregarEmpresa_Click(sender, e);
        }

        // Métodos para gestión de conductores  
        private void btnNuevoConductor_Click(object sender, RoutedEventArgs e)
        {
            LimpiarFormularioConductor();
        }

        private void btnGuardarConductor_Click(object sender, RoutedEventArgs e)
        {
            // Usar el método btnAgregarConductor_Click que ya tienes implementado  
            btnAgregarConductor_Click(sender, e);
        }



        // Métodos para gestión de vehículos  
        private void btnNuevoVehiculo_Click(object sender, RoutedEventArgs e)
        {
            LimpiarFormularioVehiculo();
        }

        private void btnGuardarVehiculo_Click(object sender, RoutedEventArgs e)
        {
            // Usar el método btnAgregarVehiculo_Click que ya tienes implementado  
            btnAgregarVehiculo_Click(sender, e);
        }

        private void cmbEmpresaVehiculos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbEmpresaVehiculos.SelectedValue != null)
            {
                int empresaId = (int)cmbEmpresaVehiculos.SelectedValue;

                if (empresaId == -1) // "Todas las Empresas"  
                {
                    CargarVehiculos(); // Cargar todos los vehículos  
                }
                else
                {
                    CargarVehiculosPorEmpresa(empresaId); // Filtrar por empresa específica  
                }
            }
        }
        private void cmbEmpresaConductores_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbEmpresaConductores.SelectedValue != null)
            {
                int empresaId = (int)cmbEmpresaConductores.SelectedValue;

                if (empresaId == -1) // "Todas las Empresas"  
                {
                    CargarConductores(); // Cargar todos los conductores  
                }
                else
                {
                    CargarConductoresPorEmpresa(empresaId); // Filtrar por empresa específica  
                }
            }
        }


        private void CargarConductoresPorEmpresa(int empresaId)
        {
            try
            {
                var conductores = _accesoDatosViajes.ObtenerConductoresPorEmpresa(empresaId);
                dgConductores.ItemsSource = conductores;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar conductores: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CargarVehiculosPorEmpresa(int empresaId)
        {
            try
            {
                var vehiculos = _accesoDatosViajes.ObtenerVehiculosPorEmpresa(empresaId);
                dgVehiculos.ItemsSource = vehiculos;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar vehículos: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


    }
}