using System.Windows.Media;

namespace StringTheory.UI
{
    public interface ITabPage
    {
        string HeaderText { get; }

        bool CanClose { get; }

        DrawingBrush IconDrawingBrush { get; }
    }
}