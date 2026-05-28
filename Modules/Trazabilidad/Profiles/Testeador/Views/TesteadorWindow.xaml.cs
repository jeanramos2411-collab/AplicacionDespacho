using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using AplicacionDespacho.Modules.Trazabilidad.Profiles.Testeador.ViewModels;
using AplicacionDespacho.Services;
using AplicacionDespacho.Services.DataAccess;

namespace AplicacionDespacho.Modules.Trazabilidad.Profiles.Testeador.Views
{
    public partial class TesteadorWindow : Window
    {
        private readonly TesteadorViewModel _viewModel;
        private readonly SignalRService _signalRService;

        public TesteadorWindow(SignalRService signalRService = null)
        {
            InitializeComponent();
            _viewModel = new TesteadorViewModel(signalRService);
            this.DataContext = _viewModel;

            _signalRService = signalRService;

            if (_signalRService != null)
            {
                // Iniciar conexión igual que ViewModelPrincipal  
                _ = Task.Run(async () => await _signalRService.StartConnectionAsync());

                // Esperar un momento para que la conexión se establezca  
                Task.Delay(1000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() => SuscribirEventosSignalR());
                });
            }

            // ⭐ NUEVO: Agregar manejador de cierre para limpiar SignalR  
            this.Closing += async (s, e) =>
            {
                if (_signalRService != null)
                {
                    try
                    {
                        await _signalRService.StopConnectionAsync();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al detener SignalR: {ex.Message}");
                    }
                }
            };
        }

        /// <summary>  
        /// Suscribe eventos de SignalR para atender solicitudes del módulo Testeador móvil  
        /// </summary>  
        private void SuscribirEventosSignalR()
        {
            // Evento: Móvil solicita información de pallet  
            _signalRService.OnPalletInfoRequested(async (palletNumber, deviceId) =>
            {
                await Dispatcher.InvokeAsync(async () =>
                {
                    await AtenderSolicitudInfoPallet(palletNumber, deviceId);
                });
            });

            // Evento: Móvil solicita eliminación de pallet  
            _signalRService.OnPalletDeletionRequested(async (palletNumber, deviceId) =>
            {
                await Dispatcher.InvokeAsync(async () =>
                {
                    await AtenderSolicitudEliminacion(palletNumber, deviceId);
                });
            });
        }

        /// <summary>  
        /// Atiende solicitud de información de pallet desde móvil  
        /// </summary>  
        private async Task AtenderSolicitudInfoPallet(string palletNumber, string deviceId)
        {
            try
            {
                var accesoDatosPallet = new AccesoDatosPallet();
                var (pallet, lotes, estadoValidacion) = accesoDatosPallet.ObtenerPalletConLotes(palletNumber);

                if (pallet != null && lotes != null)
                {
                    var palletData = new
                    {
                        Pallet = new
                        {
                            pallet.NumeroPallet,
                            pallet.NumeroDeCajas,
                            pallet.Calibre,
                            pallet.Embalaje,
                            pallet.Variedad
                        },
                        Lotes = lotes.Select(l => new
                        {
                            l.CodigoCuartel,
                            l.CSGPredio,
                            l.NombrePredio,
                            l.NombreProductor,
                            l.CalibreLote,
                            l.EmbalajeLote,
                            l.VariedadLote,
                            l.CantidadCajas,
                            l.EsMinoritario,
                            l.CalibreMayoritario,
                            l.EmbalajeMayoritario,
                            l.VariedadMayoritaria
                        }).ToList(),
                        EstadoValidacion = estadoValidacion,
                        Incompleto = false
                    };

                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(palletData);
                    await _signalRService.SendPalletInfoToMobileTesteadorAsync(json, deviceId, true, "");
                }
                else
                {
                    var (encontrado, completo, tablasConRegistros, mensaje) =
                        accesoDatosPallet.VerificarEstadoPallet(palletNumber);

                    if (encontrado)
                    {
                        var palletData = new
                        {
                            Incompleto = true,
                            Mensaje = mensaje,
                            TablasConRegistros = tablasConRegistros
                        };

                        string json = Newtonsoft.Json.JsonConvert.SerializeObject(palletData);
                        await _signalRService.SendPalletInfoToMobileTesteadorAsync(json, deviceId, true, "");
                    }
                    else
                    {
                        await _signalRService.SendPalletInfoToMobileTesteadorAsync("", deviceId, false, "Pallet no encontrado");
                    }
                }
            }
            catch (Exception ex)
            {
                await _signalRService.SendPalletInfoToMobileTesteadorAsync("", deviceId, false, $"Error: {ex.Message}");
            }
        }

        /// <summary>  
        /// Atiende solicitud de eliminación de pallet desde móvil  
        /// </summary>  
        private async Task AtenderSolicitudEliminacion(string palletNumber, string deviceId)
        {
            try
            {
                var accesoDatosPallet = new AccesoDatosPallet();
                bool eliminado = accesoDatosPallet.EliminarPallet(palletNumber);

                string mensaje = eliminado
                    ? $"Pallet {palletNumber} eliminado exitosamente de todas las tablas"
                    : $"No se pudo eliminar el pallet {palletNumber}. Verifique que exista en la base de datos";

                await _signalRService.SendDeletionResultToMobileAsync(palletNumber, deviceId, eliminado, mensaje);
            }
            catch (Exception ex)
            {
                await _signalRService.SendDeletionResultToMobileAsync(palletNumber, deviceId, false, $"Error: {ex.Message}");
            }
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnLimpiar_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is TesteadorViewModel viewModel)
            {
                viewModel.NumeroPallet = string.Empty;
                viewModel.Lotes.Clear();
                viewModel.PalletInfo = string.Empty;
                viewModel.EstadoValidacion = string.Empty;
            }
        }
    }

    // Convertidores (sin cambios)  
    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                return count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InverseCountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                return count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool tieneDiscrepancia && tieneDiscrepancia)
                return Brushes.Red;
            return Brushes.Green;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToFontWeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool tieneDiscrepancia && tieneDiscrepancia)
                return FontWeights.Bold;
            return FontWeights.Normal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CalibreDiscrepancyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is LoteInfo lote)
            {
                bool calibreDifiere = !string.Equals(
                    lote.CalibreLote?.Trim(),
                    lote.CalibreMayoritario?.Trim(),
                    StringComparison.OrdinalIgnoreCase);

                bool embalajeDifiere = !string.Equals(
                    lote.EmbalajeLote?.Trim(),
                    lote.EmbalajeMayoritario?.Trim(),
                    StringComparison.OrdinalIgnoreCase);

                bool variedadDifiere = !string.Equals(
                    lote.VariedadLote?.Trim(),
                    lote.VariedadMayoritaria?.Trim(),
                    StringComparison.OrdinalIgnoreCase);

                return calibreDifiere || embalajeDifiere || variedadDifiere;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}