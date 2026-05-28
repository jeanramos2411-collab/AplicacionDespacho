// Converters/PesoTotalDisplayConverter.cs  
using System;
using System.Globalization;
using System.Windows.Data;
using AplicacionDespacho.Models;

namespace AplicacionDespacho.Converters
{
    public class PesoTotalDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is InformacionPallet pallet)
            {
                return pallet.PesoTotal.ToString("F3");
            }
            return "0.000";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}