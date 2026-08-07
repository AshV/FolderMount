using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FolderMount.Converters
{
    /// <summary>
    /// Converts a bool → Visibility.
    /// Pass ConverterParameter="invert" to flip the logic (false → Visible).
    /// Pass ConverterParameter="zeroIsVisible" to show when value == 0 (int).
    /// </summary>
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string param = parameter as string ?? "";

            // Special case: int 0 → Visible (used for empty-state panel)
            if (param == "zeroIsVisible" && value is int count)
                return count == 0 ? Visibility.Visible : Visibility.Collapsed;

            bool b = value is bool boolVal && boolVal;
            if (param == "invert") b = !b;
            return b ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is Visibility v && v == Visibility.Visible;
    }
}
