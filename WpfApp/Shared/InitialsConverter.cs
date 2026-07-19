using System.Globalization;
using System.Windows.Data;

namespace WpfApp;

/// <summary>Turns a username/full name into 1-2 uppercase initials for an avatar bubble.</summary>
public class InitialsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var text = value?.ToString()?.Trim();
        if (string.IsNullOrEmpty(text)) return "?";

        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
            return $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[^1][0])}";

        return text.Length >= 2
            ? text[..2].ToUpperInvariant()
            : text.ToUpperInvariant();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
