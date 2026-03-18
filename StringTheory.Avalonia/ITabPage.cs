using System;
using Avalonia.Media;

namespace StringTheory.Avalonia;

public interface ITabPage
{
    event Action? CloseRequested;

    string HeaderText { get; }

    bool CanClose { get; }

    DrawingImage? IconDrawingImage { get; }
}
