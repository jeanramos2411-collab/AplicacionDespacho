// Converters/VariedadDisplayConverter.cs - Actualizado  
using System;
using System.Globalization;
using System.Windows.Data;
using AplicacionDespacho.Models;

namespace AplicacionDespacho.Converters
{
    public class VariedadDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is InformacionPallet pallet)
            {
                if (pallet.EsBicolor && !string.IsNullOrEmpty(pallet.SegundaVariedad))
                {
                    return $"{pallet.Variedad} + {pallet.SegundaVariedad}";
                }
                return pallet.Variedad ?? "";
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}