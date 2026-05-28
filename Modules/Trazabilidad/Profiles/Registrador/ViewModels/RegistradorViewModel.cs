using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using AplicacionDespacho.Models;
using AplicacionDespacho.Services.DataAccess;

namespace AplicacionDespacho.Modules.Trazabilidad.Profiles.Registrador.ViewModels
{
    /// <summary>  
    /// ViewModel para el perfil Registrador del módulo de Trazabilidad  
    /// Permite registrar manualmente pallets con información de lotes/cuarteles  
    /// </summary>  
    public class RegistradorViewModel : INotifyPropertyChanged
    {
        private readonly AccesoDatosViajes _accesoDatosViajes;

        // Propiedades para el formulario de registro  
        private string _numeroPallet;
        public string NumeroPallet
        {
            get => _numeroPallet;
            set
            {
                _numeroPallet = value;
                OnPropertyChanged(nameof(NumeroPallet));
            }
        }

        private string _variedadSeleccionada;
        public string VariedadSeleccionada
        {
            get => _variedadSeleccionada;
            set
            {
                _variedadSeleccionada = value;
                OnPropertyChanged(nameof(VariedadSeleccionada));
            }
        }

        private string _calibreSeleccionado;
        public string CalibreSeleccionado
        {
            get => _calibreSeleccionado;
            set
            {
                _calibreSeleccionado = value;
                OnPropertyChanged(nameof(CalibreSeleccionado));
            }
        }

        private string _embalajeSeleccionado;
        public string EmbalajeSeleccionado
        {
            get => _embalajeSeleccionado;
            set
            {
                _embalajeSeleccionado = value;
                OnPropertyChanged(nameof(EmbalajeSeleccionado));
            }
        }

        private string _numeroMatch;
        public string NumeroMatch
        {
            get => _numeroMatch;
            set
            {
                _numeroMatch = value;
                OnPropertyChanged(nameof(NumeroMatch));
            }
        }

        // Propiedades para agregar nuevo lote  
        private string _codigoCuartel;
        public string CodigoCuartel
        {
            get => _codigoCuartel;
            set
            {
                _codigoCuartel = value;
                OnPropertyChanged(nameof(CodigoCuartel));
            }
        }

        private string _csgPredio;
        public string CSGPredio
        {
            get => _csgPredio;
            set
            {
                _csgPredio = value;
                OnPropertyChanged(nameof(CSGPredio));
            }
        }

        private string _nombrePredio;
        public string NombrePredio
        {
            get => _nombrePredio;
            set
            {
                _nombrePredio = value;
                OnPropertyChanged(nameof(NombrePredio));
            }
        }

        private string _nombreProductor;
        public string NombreProductor
        {
            get => _nombreProductor;
            set
            {
                _nombreProductor = value;
                OnPropertyChanged(nameof(NombreProductor));
            }
        }

        private int _cantidadCajasLote;
        public int CantidadCajasLote
        {
            get => _cantidadCajasLote;
            set
            {
                _cantidadCajasLote = value;
                OnPropertyChanged(nameof(CantidadCajasLote));
                OnPropertyChanged(nameof(TotalCajasCalculado));
            }
        }

        // Colecciones  
        public ObservableCollection<string> Variedades { get; set; }
        public ObservableCollection<string> Calibres { get; set; }
        public ObservableCollection<string> Embalajes { get; set; }
        public ObservableCollection<LoteRegistro> LotesAgregados { get; set; }

        // Propiedades calculadas  
        public int TotalCajasCalculado => LotesAgregados.Sum(l => l.CantidadCajas);

        // Comandos  
        public ICommand AgregarLoteCommand { get; }
        public ICommand EliminarLoteCommand { get; }
        public ICommand GuardarPalletCommand { get; }
        public ICommand LimpiarFormularioCommand { get; }

        public RegistradorViewModel()
        {
            _accesoDatosViajes = new AccesoDatosViajes();

            Variedades = new ObservableCollection<string>();
            Calibres = new ObservableCollection<string>();
            Embalajes = new ObservableCollection<string>();
            LotesAgregados = new ObservableCollection<LoteRegistro>();

            // Inicializar comandos  
            AgregarLoteCommand = new RelayCommand(AgregarLote, PuedeAgregarLote);
            EliminarLoteCommand = new RelayCommand(EliminarLote, PuedeEliminarLote);
            GuardarPalletCommand = new RelayCommand(GuardarPallet, PuedeGuardarPallet);
            LimpiarFormularioCommand = new RelayCommand(LimpiarFormulario);

            // Cargar datos de catálogos  
            //CargarCatalogos();
        }
        /*
        private void CargarCatalogos()
        {
            try
            {
                // Cargar variedades  
                var variedades = _accesoDatosViajes.ObtenerTodasLasVariedades();
                Variedades.Clear();
                foreach (var variedad in variedades)
                {
                    Variedades.Add(variedad);
                }

                // Cargar calibres  
                var calibres = _accesoDatosViajes.ObtenerTodosLosCalibre();
                Calibres.Clear();
                foreach (var calibre in calibres)
                {
                    Calibres.Add(calibre);
                }

                // Cargar embalajes  
                var embalajes = _accesoDatosViajes.ObtenerTodosLosEmbalajes();
                Embalajes.Clear();
                foreach (var embalaje in embalajes)
                {
                    Embalajes.Add(embalaje);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar catálogos: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }*/

        private void AgregarLote(object parameter)
        {
            var nuevoLote = new LoteRegistro
            {
                CodigoCuartel = CodigoCuartel,
                CSGPredio = CSGPredio,
                NombrePredio = NombrePredio,
                NombreProductor = NombreProductor,
                CantidadCajas = CantidadCajasLote
            };

            LotesAgregados.Add(nuevoLote);

            // Limpiar campos de lote  
            CodigoCuartel = string.Empty;
            CSGPredio = string.Empty;
            NombrePredio = string.Empty;
            NombreProductor = string.Empty;
            CantidadCajasLote = 0;

            OnPropertyChanged(nameof(TotalCajasCalculado));
        }

        private bool PuedeAgregarLote(object parameter)
        {
            return !string.IsNullOrWhiteSpace(CodigoCuartel) &&
                   !string.IsNullOrWhiteSpace(CSGPredio) &&
                   CantidadCajasLote > 0;
        }

        private void EliminarLote(object parameter)
        {
            if (parameter is LoteRegistro lote)
            {
                LotesAgregados.Remove(lote);
                OnPropertyChanged(nameof(TotalCajasCalculado));
            }
        }

        private bool PuedeEliminarLote(object parameter)
        {
            return parameter is LoteRegistro;
        }

        private void GuardarPallet(object parameter)
        {
            try
            {
                // Validar que haya al menos un lote  
                if (LotesAgregados.Count == 0)
                {
                    MessageBox.Show("Debe agregar al menos un lote al pallet.", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // TODO: Implementar lógica de guardado en Despachos_SJP  
                // Por ahora solo mostramos confirmación  
                var resultado = MessageBox.Show(
                    $"¿Confirmar registro del pallet {NumeroPallet}?\n\n" +
                    $"Variedad: {VariedadSeleccionada}\n" +
                    $"Calibre: {CalibreSeleccionado}\n" +
                    $"Embalaje: {EmbalajeSeleccionado}\n" +
                    $"Total Cajas: {TotalCajasCalculado}\n" +
                    $"Lotes: {LotesAgregados.Count}\n" +
                    $"Match: {NumeroMatch}",
                    "Confirmar Registro",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    // Aquí iría la lógica de guardado  
                    MessageBox.Show("Pallet registrado exitosamente.", "Éxito",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    LimpiarFormulario(null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar pallet: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool PuedeGuardarPallet(object parameter)
        {
            return !string.IsNullOrWhiteSpace(NumeroPallet) &&
                   !string.IsNullOrWhiteSpace(VariedadSeleccionada) &&
                   !string.IsNullOrWhiteSpace(CalibreSeleccionado) &&
                   !string.IsNullOrWhiteSpace(EmbalajeSeleccionado) &&
                   !string.IsNullOrWhiteSpace(NumeroMatch) &&
                   LotesAgregados.Count > 0;
        }

        private void LimpiarFormulario(object parameter)
        {
            NumeroPallet = string.Empty;
            VariedadSeleccionada = null;
            CalibreSeleccionado = null;
            EmbalajeSeleccionado = null;
            NumeroMatch = string.Empty;
            LotesAgregados.Clear();

            CodigoCuartel = string.Empty;
            CSGPredio = string.Empty;
            NombrePredio = string.Empty;
            NombreProductor = string.Empty;
            CantidadCajasLote = 0;

            OnPropertyChanged(nameof(TotalCajasCalculado));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Clase auxiliar para representar un lote en el registro  
    public class LoteRegistro
    {
        public string CodigoCuartel { get; set; }
        public string CSGPredio { get; set; }
        public string NombrePredio { get; set; }
        public string NombreProductor { get; set; }
        public int CantidadCajas { get; set; }
    }

    // Comando auxiliar (si no existe ya en el proyecto)  
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}