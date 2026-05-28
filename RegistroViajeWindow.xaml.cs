// RegistroViajeWindow.xaml.cs    
using AplicacionDespacho.Configuration;
using AplicacionDespacho.Models;
using AplicacionDespacho.Services.DataAccess;
using AplicacionDespacho.utilities;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;


namespace AplicacionDespacho
{
    public partial class RegistroViajeWindow : Window
    {
        private AccesoDatosViajes _accesoDatosViajes;
        private List<EmpresaTransporte> _listaEmpresas;
        private List<Vehiculo> _listaVehiculos;
        private List<Conductor> _listaConductores;

        public Viaje ViajeCreado { get; private set; }
        public bool ViajeGuardado { get; private set; } = false;
        private bool _modoEdicion = false;

        // Constructor para nuevo viaje    
        public RegistroViajeWindow()
        {

            InitializeComponent();
            _accesoDatosViajes = new AccesoDatosViajes();
            datePickerFecha.SelectedDateChanged += DatePickerFecha_SelectedDateChanged;
            // Usar configuración centralizada para valores por defecto  

            var fechaOperacional = FechaOperacionalHelper.ObtenerFechaOperacionalActual();



            datePickerFecha.SelectedDate = FechaOperacionalHelper.ObtenerFechaOperacionalActual();
            textBoxPuntoPartida.Text = AppConfig.DefaultDeparturePoint;
            textBoxPuntoLlegada.Text = AppConfig.DefaultArrivalPoint;
            textBoxResponsable.Text = AppConfig.DefaultResponsible;

            // NUEVO: Sugerir próximo número de viaje disponible  
            int proximoNumero = _accesoDatosViajes.ObtenerProximoNumeroViaje(fechaOperacional);
            textBoxNumeroViaje.Text = proximoNumero.ToString();


            InicializarNumeroGuia();
        }
        private void DatePickerFecha_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (datePickerFecha.SelectedDate.HasValue && !_modoEdicion)
            {
                // Solo sugerir nuevo número si estamos creando un viaje nuevo  
                int proximoNumero = _accesoDatosViajes.ObtenerProximoNumeroViaje(datePickerFecha.SelectedDate.Value);
                textBoxNumeroViaje.Text = proximoNumero.ToString();
            }
        }
        // CAMBIO: Método modificado para solo establecer prefijo  
        private void InicializarNumeroGuia()
        {
            textBoxNumeroGuia.Text = AppConfig.DefaultGuidePrefix;
            textBoxNumeroGuia.IsReadOnly = false;
            textBoxNumeroGuia.TextChanged += TextBoxNumeroGuia_TextChanged;
        }

        // NUEVO: Método para mantener el prefijo T004-  
        private void TextBoxNumeroGuia_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            var prefix = AppConfig.DefaultGuidePrefix;

            if (textBox != null && !textBox.Text.StartsWith(prefix))
            {
                int cursorPosition = textBox.SelectionStart;
                textBox.TextChanged -= TextBoxNumeroGuia_TextChanged;
                textBox.Text = prefix + textBox.Text.Replace(prefix, "");
                textBox.SelectionStart = Math.Max(prefix.Length, cursorPosition);
                textBox.TextChanged += TextBoxNumeroGuia_TextChanged;
            }
        }

        private bool ValidarFormulario()
        {
            // Validaciones básicas    
            if (datePickerFecha.SelectedDate == null ||
                string.IsNullOrWhiteSpace(textBoxNumeroViaje.Text) ||
                string.IsNullOrWhiteSpace(textBoxResponsable.Text.ToUpper()) ||
                string.IsNullOrWhiteSpace(textBoxNumeroGuia.Text) ||
                comboBoxEmpresa.SelectedValue == null ||
                comboBoxPlaca.SelectedValue == null ||
                comboBoxConductor.SelectedValue == null)
            {
                MessageBox.Show("Complete todos los campos obligatorios.", "Validación",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!int.TryParse(textBoxNumeroViaje.Text, out int numeroViaje))
            {
                MessageBox.Show("El número de viaje debe ser válido.", "Validación",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Validar unicidad de número de viaje por día    
            int? viajeIdExcluir = _modoEdicion ? ViajeCreado?.ViajeId : null;
            if (_accesoDatosViajes.ExisteNumeroViajePorFecha(numeroViaje, datePickerFecha.SelectedDate.Value, viajeIdExcluir))
            {
                MessageBox.Show($"Ya existe un viaje con el número {numeroViaje} para la fecha {datePickerFecha.SelectedDate.Value:dd/MM/yyyy}.",
                               "Número Duplicado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Validar formato y unicidad de número de guía    
            if (!textBoxNumeroGuia.Text.StartsWith("T004-"))
            {
                MessageBox.Show("El número de guía debe comenzar con 'T004-'.", "Formato Inválido",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // CAMBIO: Validar que tenga contenido después del prefijo  
            if (textBoxNumeroGuia.Text.Length <= 5)
            {
                MessageBox.Show("El número de guía debe tener contenido después de 'T004-'.", "Guía Incompleta",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (_accesoDatosViajes.ExisteNumeroGuia(textBoxNumeroGuia.Text, viajeIdExcluir))
            {
                MessageBox.Show($"Ya existe un viaje con el número de guía {textBoxNumeroGuia.Text}.",
                               "Guía Duplicada", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        // Constructor para editar viaje existente    
        public RegistroViajeWindow(Viaje viajeExistente) : this()
        {
            _modoEdicion = true;
            txtTitulo.Text = "Editar Viaje";
            btnGuardar.Content = "Guardar Cambios";
            ViajeCreado = viajeExistente;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CargarDatosIniciales();

            if (_modoEdicion && ViajeCreado != null)
            {
                CargarDatosViaje();
            }
        }

        private void CargarDatosIniciales()
        {
            _listaEmpresas = _accesoDatosViajes.ObtenerEmpresas();
            comboBoxEmpresa.ItemsSource = _listaEmpresas;
            comboBoxEmpresa.DisplayMemberPath = "NombreEmpresa";
            comboBoxEmpresa.SelectedValuePath = "EmpresaId";

            if (_listaEmpresas.Count > 0)
            {
                comboBoxEmpresa.SelectedIndex = 0;
            }
        }

        private void ComboBoxEmpresa_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBoxEmpresa.SelectedItem is EmpresaTransporte empresaSeleccionada)
            {
                _listaVehiculos = _accesoDatosViajes.ObtenerVehiculosPorEmpresa(empresaSeleccionada.EmpresaId);
                comboBoxPlaca.ItemsSource = _listaVehiculos;
                comboBoxPlaca.DisplayMemberPath = "Placa";
                comboBoxPlaca.SelectedValuePath = "VehiculoId";

                _listaConductores = _accesoDatosViajes.ObtenerConductoresPorEmpresa(empresaSeleccionada.EmpresaId);
                comboBoxConductor.ItemsSource = _listaConductores;
                comboBoxConductor.DisplayMemberPath = "NombreConductor";
                comboBoxConductor.SelectedValuePath = "ConductorId";
            }
            else
            {
                comboBoxPlaca.ItemsSource = null;
                comboBoxConductor.ItemsSource = null;
            }
        }

        private void CargarDatosViaje()
        {
            datePickerFecha.SelectedDate = ViajeCreado.Fecha;
            textBoxNumeroViaje.Text = ViajeCreado.NumeroViaje.ToString();
            textBoxResponsable.Text = ViajeCreado.Responsable;
            textBoxNumeroGuia.Text = ViajeCreado.NumeroGuia;
            textBoxPuntoPartida.Text = ViajeCreado.PuntoPartida;
            textBoxPuntoLlegada.Text = ViajeCreado.PuntoLlegada;

            // CAMBIO: Corregir la selección de empresa  
            // Buscar la empresa correcta basada en el VehiculoId del viaje  
            var vehiculo = _accesoDatosViajes.ObtenerVehiculoPorId(ViajeCreado.VehiculoId);
            if (vehiculo != null)
            {
                comboBoxEmpresa.SelectedValue = vehiculo.EmpresaId;
                // Esto disparará la carga de conductores y vehículos  
                comboBoxPlaca.SelectedValue = ViajeCreado.VehiculoId;
                comboBoxConductor.SelectedValue = ViajeCreado.ConductorId;
            }
        }


        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // CAMBIO: Usar el método ValidarFormulario antes de proceder  
            if (!ValidarFormulario())
            {
                return;
            }

            try
            {
                var empresaSeleccionada = comboBoxEmpresa.SelectedItem as EmpresaTransporte;
                var conductorSeleccionado = comboBoxConductor.SelectedItem as Conductor;
                var vehiculoSeleccionado = comboBoxPlaca.SelectedItem as Vehiculo;

                if (!_modoEdicion) // Nuevo viaje    
                {
                    ViajeCreado = new Viaje
                    {
                        Fecha = datePickerFecha.SelectedDate.GetValueOrDefault(),
                        NumeroViaje = int.Parse(textBoxNumeroViaje.Text),
                        Responsable = textBoxResponsable.Text.ToUpper(),
                        NumeroGuia = textBoxNumeroGuia.Text,
                        PuntoPartida = textBoxPuntoPartida.Text.ToUpper(),
                        PuntoLlegada = textBoxPuntoLlegada.Text.ToUpper(),
                        VehiculoId = (int)comboBoxPlaca.SelectedValue,
                        ConductorId = (int)comboBoxConductor.SelectedValue,
                        Estado = "Activo",
                        FechaCreacion = FechaOperacionalHelper.ObtenerFechaOperacionalActual(), // CAMBIO AQUÍ 
                        UsuarioCreacion = Environment.UserName,
                        NombreEmpresa = empresaSeleccionada?.NombreEmpresa,
                        NombreConductor = conductorSeleccionado?.NombreConductor,
                        PlacaVehiculo = vehiculoSeleccionado?.Placa
                    };
                    _accesoDatosViajes.GuardarViaje(ViajeCreado);
                }
                else // Editar viaje existente    
                {
                    ViajeCreado.Fecha = datePickerFecha.SelectedDate.GetValueOrDefault();
                    ViajeCreado.NumeroViaje = int.Parse(textBoxNumeroViaje.Text);
                    ViajeCreado.Responsable = textBoxResponsable.Text.ToUpper();
                    ViajeCreado.NumeroGuia = textBoxNumeroGuia.Text;
                    ViajeCreado.PuntoPartida = textBoxPuntoPartida.Text.ToUpper();
                    ViajeCreado.PuntoLlegada = textBoxPuntoLlegada.Text.ToUpper();
                    ViajeCreado.VehiculoId = (int)comboBoxPlaca.SelectedValue;
                    ViajeCreado.ConductorId = (int)comboBoxConductor.SelectedValue;
                    ViajeCreado.FechaModificacion = FechaOperacionalHelper.ObtenerFechaOperacionalActual();
                    ViajeCreado.NombreEmpresa = empresaSeleccionada?.NombreEmpresa;
                    ViajeCreado.NombreConductor = conductorSeleccionado?.NombreConductor;
                    ViajeCreado.PlacaVehiculo = vehiculoSeleccionado?.Placa;

                    _accesoDatosViajes.ActualizarViaje(ViajeCreado);
                }

                ViajeGuardado = true;
                this.DialogResult = true;
                this.Close();
            }
            catch (FormatException)
            {
                MessageBox.Show("El campo 'N° Viaje' debe ser un número válido.", "Error de Formato", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error al guardar el viaje: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}