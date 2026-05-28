// Converters/BoolToColorConverter.cs - Versión mejorada para pallets bicolor  
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AplicacionDespacho.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // NUEVO: Soporte para diferentes contextos usando parameter  
                string context = parameter?.ToString();

                if (context == "bicolor")
                {
                    // Para pallets bicolor: Naranja si es bicolor, gris si no  
                    return boolValue ? Brushes.Orange : Brushes.Gray;
                }

                // Comportamiento original para viajes y modificaciones  
                return boolValue ? Brushes.Green : Brushes.Red;
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}