using System;
using System.Globalization;
using System.Windows.Data;

namespace DXHistogramN.Converters
{
    public class DecimalToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
                return (decimal)intValue;

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal decimalValue)
                return (int)Math.Round(decimalValue);

            if (value is double doubleValue)
                return (int)Math.Round(doubleValue);

            if (value is float floatValue)
                return (int)Math.Round(floatValue);

            return value;
        }
    }
}