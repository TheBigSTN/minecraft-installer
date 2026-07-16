using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ModpackInstaller.Converters;

public class NullToBoolConverter : IValueConverter {
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        var isNull = value is null;

        if (parameter?.ToString() == "Invert")
            isNull = !isNull;

        return isNull;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}