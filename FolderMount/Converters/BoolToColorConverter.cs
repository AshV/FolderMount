using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FolderMount.Converters
{
    /// <summary>Converts IsActive bool → Green or Gray brush for the status dot.</summary>
    [ValueConversion(typeof(bool), typeof(Brush))]
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool active = value is bool b && b;
            return active
                ? new SolidColorBrush(Color.FromRgb(0x4A, 0xDE, 0x80))   // green-400
                : new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80));  // gray-500
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
