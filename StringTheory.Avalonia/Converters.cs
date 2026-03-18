using System;
using System.Buffers;
using System.Globalization;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace StringTheory.Avalonia;

internal static class Converters
{
    public static IValueConverter VisibleWhenTrue { get; } = new BooleanPassthroughConverter();

    public static IValueConverter FirstLineOnly { get; } = new FirstLineOnlyConverter();

    public static IValueConverter VisibleWhenNull { get; } = new NullBooleanConverter(nullValue: true);

    public static IValueConverter TrueWhenNonNull { get; } = new NullBooleanConverter(nullValue: false);

    public static IValueConverter NodeTypeIcon { get; } = new NodeTypeIconConverter();
}

internal sealed class BooleanPassthroughConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b;
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

internal sealed class NullBooleanConverter(bool nullValue) : IValueConverter
{
    private readonly object _nullValue = nullValue;
    private readonly object _nonNullValue = !nullValue;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is null ? _nullValue : _nonNullValue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

public sealed class PercentageConverter : IValueConverter
{
    public static readonly PercentageConverter Default = new PercentageConverter();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d && d != 0)
        {
            return ((int)Math.Round(d * 100)).ToString();
        }

        return null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

internal sealed class FirstLineOnlyConverter : IValueConverter
{
    private static readonly SearchValues<char> s_newLineChars = SearchValues.Create(['\r', '\n']);

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s)
        {
            var i = s.AsSpan().IndexOfAny(s_newLineChars);

            if (i != -1)
            {
                return s[..i] + "...";
            }

            return s;
        }

        return null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

internal sealed class NodeTypeIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ReferrerTreeNodeType type && Application.Current is { } app)
        {
            var key = type switch
            {
                ReferrerTreeNodeType.TargetString => "StringIconImage",
                ReferrerTreeNodeType.FieldReference => "FieldIconImage",
                ReferrerTreeNodeType.StaticVar => "StaticVarIconImage",
                ReferrerTreeNodeType.ThreadStaticVar => "StaticVarIconImage",
                ReferrerTreeNodeType.Pinning => "GCHandleIconImage",
                ReferrerTreeNodeType.AsyncPinning => "GCHandleIconImage",
                ReferrerTreeNodeType.LocalVar => "LocalVarIconImage",
                ReferrerTreeNodeType.StrongHandle => "StrongHandleIconImage",
                ReferrerTreeNodeType.WeakHandle => "WeakHandleIconImage",
                ReferrerTreeNodeType.Finalizer => "FinalizerQueueIconImage",
                _ => (string?)null
            };

            if (key is not null && app.TryGetResource(key, app.ActualThemeVariant, out var resource))
            {
                return resource;
            }
        }

        return null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}
