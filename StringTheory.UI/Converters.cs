using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace StringTheory.UI
{
    [ValueConversion(typeof(double), typeof(string))]
    public sealed class PercentageConverter : IValueConverter
    {
        public static readonly PercentageConverter Default = new PercentageConverter();
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d && d != 0)
            {
                return ((int) (d * 100)).ToString();
            }

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}