// Converters/CajasDisplayConverter.cs  
using System;
using System.Globalization;
using System.Windows.Data;
using AplicacionDespacho.Models;

namespace AplicacionDespacho.Converters
{
    public class CajasDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is InformacionPallet pallet)
            {
                if (pallet.EsBicolor)
                {
                    int totalCajas = pallet.NumeroDeCajas + pallet.CajasSegundaVariedad;
                    return $"{totalCajas} ({pallet.NumeroDeCajas}+{pallet.CajasSegundaVariedad})";
                }
                return pallet.NumeroDeCajas.ToString();
            }
            return "0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}