using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace FileOrganizer.Gui.Converters;

public class BoolToRowHighlightConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? new SolidColorBrush(Color.Parse("#332222")) : Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
