using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;

namespace StringTheory.Avalonia;

public partial class ReferrerTree : UserControl
{
    public static readonly StyledProperty<ReferrerTreeViewModel?> TreeProperty =
        AvaloniaProperty.Register<ReferrerTree, ReferrerTreeViewModel?>(nameof(Tree));

    public static readonly StyledProperty<ICommand?> ShowStringReferencedByFieldCommandProperty =
        AvaloniaProperty.Register<ReferrerTree, ICommand?>(nameof(ShowStringReferencedByFieldCommand));

    public ReferrerTree()
    {
        InitializeComponent();
    }

    public ReferrerTreeViewModel? Tree
    {
        get => GetValue(TreeProperty);
        set => SetValue(TreeProperty, value);
    }

    public ICommand? ShowStringReferencedByFieldCommand
    {
        get => GetValue(ShowStringReferencedByFieldCommandProperty);
        set => SetValue(ShowStringReferencedByFieldCommandProperty, value);
    }
}
