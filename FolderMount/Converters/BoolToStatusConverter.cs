using System;
using System.Globalization;
using System.Windows.Data;

namespace FolderMount.Converters
{
    /// <summary>Converts IsActive bool → "Mounted" or "Inactive" status string.</summary>
    [ValueConversion(typeof(bool), typeof(string))]
    public class BoolToStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && b ? "Mounted" : "Inactive";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
