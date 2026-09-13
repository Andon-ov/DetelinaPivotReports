using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DetelinaPivotReports.Converters;

/// <summary>
/// Преобразува 0 или null в тире (-) за чиста визуализация на матрицата.
/// </summary>
public class ZeroToDashConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || value == DBNull.Value) return "-";

        if (decimal.TryParse(value.ToString(), out decimal num))
        {
            if (num == 0m) return "-";
            return num.ToString("#,##0.##", culture);
        }

        return value.ToString() ?? "-";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Обръщане на булева стойност във Visibility.
/// </summary>
public class BooleanToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; } = false;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool b = value is true;
        bool invert = Invert;
        if (parameter is string s && s.Equals("invert", StringComparison.OrdinalIgnoreCase))
        {
            invert = !invert;
        }

        if (invert) b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Показва контрола само ако обектът/текстът не е празен.
/// </summary>
public class NotNullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return Visibility.Collapsed;
        if (value is string s && string.IsNullOrWhiteSpace(s)) return Visibility.Collapsed;
        return Visibility.Visible;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
