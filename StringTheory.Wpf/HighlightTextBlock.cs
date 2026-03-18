using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace StringTheory.Wpf;

/// <summary>
/// A TextBlock that highlights occurrences of a search term within its text.
/// </summary>
internal sealed class HighlightTextBlock : TextBlock
{
    public static readonly DependencyProperty SourceTextProperty =
        DependencyProperty.Register(
            nameof(SourceText),
            typeof(string),
            typeof(HighlightTextBlock),
            new FrameworkPropertyMetadata(null, OnPropertyChanged));

    public static readonly DependencyProperty HighlightTextProperty =
        DependencyProperty.Register(
            nameof(HighlightText),
            typeof(string),
            typeof(HighlightTextBlock),
            new FrameworkPropertyMetadata(null, OnPropertyChanged));

    private static readonly SolidColorBrush s_defaultHighlightBrush = CreateDefaultBrush();

    public static readonly DependencyProperty HighlightBackgroundProperty =
        DependencyProperty.Register(
            nameof(HighlightBackground),
            typeof(Brush),
            typeof(HighlightTextBlock),
            new FrameworkPropertyMetadata(
                s_defaultHighlightBrush,
                OnPropertyChanged));

    private static SolidColorBrush CreateDefaultBrush()
    {
        var brush = new SolidColorBrush(Color.FromRgb(255, 210, 0));
        brush.Freeze();
        return brush;
    }

    public string? SourceText
    {
        get => (string?)GetValue(SourceTextProperty);
        set => SetValue(SourceTextProperty, value);
    }

    public string? HighlightText
    {
        get => (string?)GetValue(HighlightTextProperty);
        set => SetValue(HighlightTextProperty, value);
    }

    public Brush HighlightBackground
    {
        get => (Brush)GetValue(HighlightBackgroundProperty);
        set => SetValue(HighlightBackgroundProperty, value);
    }

    private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((HighlightTextBlock)d).UpdateInlines();
    }

    private void UpdateInlines()
    {
        Inlines.Clear();

        var text = SourceText;
        if (string.IsNullOrEmpty(text))
            return;

        var highlight = HighlightText;
        if (string.IsNullOrEmpty(highlight))
        {
            Inlines.Add(new Run(text));
            return;
        }

        var highlightBrush = HighlightBackground;
        var index = 0;

        while (index < text.Length)
        {
            var matchIndex = text.IndexOf(highlight, index, StringComparison.OrdinalIgnoreCase);

            if (matchIndex < 0)
            {
                Inlines.Add(new Run(text[index..]));
                break;
            }

            if (matchIndex > index)
            {
                Inlines.Add(new Run(text[index..matchIndex]));
            }

            Inlines.Add(new Run(text[matchIndex..(matchIndex + highlight.Length)])
            {
                Background = highlightBrush,
                FontWeight = FontWeights.SemiBold
            });

            index = matchIndex + highlight.Length;
        }
    }
}
