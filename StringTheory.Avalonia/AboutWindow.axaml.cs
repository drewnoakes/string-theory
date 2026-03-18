using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace StringTheory.Avalonia;

public sealed partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        var link = this.FindControl<Button>("GitHubLink");
        if (link != null)
        {
            link.Click += OnGitHubLinkClicked;
        }

        Deactivated += OnWindowDeactivated;
    }

    private void OnGitHubLinkClicked(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("https://github.com/drewnoakes/string-theory") { UseShellExecute = true });
    }

    private void OnWindowDeactivated(object? sender, EventArgs e)
    {
        Close();
    }
}
