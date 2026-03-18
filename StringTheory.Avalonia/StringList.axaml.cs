using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace StringTheory.Avalonia;

public partial class StringList : UserControl
{
    public static readonly StyledProperty<StringListPage?> StringListPageProperty =
        AvaloniaProperty.Register<StringList, StringListPage?>(nameof(StringListPage));

    public StringList()
    {
        InitializeComponent();
    }

    public StringListPage? StringListPage
    {
        get => GetValue(StringListPageProperty);
        set => SetValue(StringListPageProperty, value);
    }

    private void OnGridDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (StringListPage is { ShowReferrersCommand: { } command } && sender is DataGrid grid)
        {
            if (command.CanExecute(grid.SelectedItems))
            {
                command.Execute(grid.SelectedItems);
            }
        }
    }
}
