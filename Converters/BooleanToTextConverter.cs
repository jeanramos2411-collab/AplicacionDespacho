// Converters/BooleanToTextConverter.cs - Versión mejorada para pallets bicolor  
using System;
using System.Globalization;
using System.Windows.Data;

namespace AplicacionDespacho.Converters
{
    public class BooleanToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // NUEVO: Soporte para diferentes contextos usando parameter  
                string context = parameter?.ToString();

                if (context == "bicolor")
                {
                    // Para indicar tipo de pallet  
                    return boolValue ? "BICOLOR" : "NORMAL";
                }

                // Comportamiento original para modificaciones  
                return boolValue ? "Sí" : "No";
            }
            return "No";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                if (stringValue.Equals("BICOLOR", StringComparison.OrdinalIgnoreCase))
                    return true;
                if (stringValue.Equals("NORMAL", StringComparison.OrdinalIgnoreCase))
                    return false;

                // Comportamiento original  
                return stringValue.Equals("Sí", StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }
    }
}