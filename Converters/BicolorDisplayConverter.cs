// Converters/BicolorDisplayConverter.cs  
using System;
using System.Globalization;
using System.Windows.Data;
using AplicacionDespacho.Models;

namespace AplicacionDespacho.Converters
{
    public class BicolorDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool esBicolor)
            {
                return esBicolor ? "BICOLOR" : "NORMAL";
            }
            return "NORMAL";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}