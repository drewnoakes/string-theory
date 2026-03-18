using System;
using System.Windows.Media;

namespace StringTheory.Wpf;

public interface ITabPage
{
    event Action? CloseRequested;

    string HeaderText { get; }

    bool CanClose { get; }

    DrawingBrush IconDrawingBrush { get; }
}