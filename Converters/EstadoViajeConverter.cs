// Converters/EstadoViajeConverter.cs  
using System;
using System.Globalization;
using System.Windows.Data;

namespace AplicacionDespacho.Converters
{
    public class EstadoViajeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool estaEnUso)
            {
                return estaEnUso ? "🔒 EN USO" : "✅ LIBRE";
            }
            return "✅ LIBRE";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}