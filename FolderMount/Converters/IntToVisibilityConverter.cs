using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FolderMount.Converters
{
    /// <summary>
    /// Converts an int → Visibility.
    /// Pass ConverterParameter="zeroIsVisible" to show when value == 0.
    /// Used by the empty-state panel to display when TotalCount is 0.
    /// </summary>
    [ValueConversion(typeof(int), typeof(Visibility))]
    public class IntToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string param = parameter as string ?? "";

            if (param == "zeroIsVisible" && value is int count)
                return count == 0 ? Visibility.Visible : Visibility.Collapsed;

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
