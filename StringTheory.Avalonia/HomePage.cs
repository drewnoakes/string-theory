using System;
using System.IO;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using StringTheory.Analysis;

namespace StringTheory.Avalonia;

public sealed class HomePage : ITabPage
{
    event Action? ITabPage.CloseRequested { add { } remove { } }

    public ICommand OpenDumpCommand { get; }
    public ICommand AttachToProcessCommand { get; }
    public ICommand ShowAboutCommand { get; }

    public DrawingImage? IconDrawingImage { get; }

    public string HeaderText => "Home";
    public bool CanClose => false;

    public HomePage(MainWindow mainWindow)
    {
        OpenDumpCommand = new DelegateCommand(OpenDump);
        AttachToProcessCommand = new DelegateCommand(() => new AttachToProcessWindow(mainWindow).ShowDialog(mainWindow));
        ShowAboutCommand = new DelegateCommand(() => new AboutWindow { }.ShowDialog(mainWindow));

        IconDrawingImage = Application.Current?.Resources.TryGetResource("HomeIconImage", null, out var res) == true ? res as DrawingImage : null;

        async void OpenDump()
        {
            var topLevel = TopLevel.GetTopLevel(mainWindow);
            if (topLevel == null)
                return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open dump file",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("Dump files") { Patterns = ["*.dmp"] },
                    new FilePickerFileType("All files") { Patterns = ["*"] }
                ]
            });

            if (files.Count == 1)
            {
                var dumpFilePath = files[0].Path.LocalPath;

                OpenDumpFile(dumpFilePath);
            }
        }

        void OpenDumpFile(string dumpFilePath)
        {
            var operation = new LoadingOperation(
                (progressCallback, token) =>
                {
                    var analyzer = new HeapAnalyzer(dumpFilePath);

                    var summary = analyzer.GetStringSummary(progressCallback, token);

                    var description = $"All strings in {dumpFilePath}";

                    return new StringListPage(mainWindow, summary, analyzer, Path.GetFileNameWithoutExtension(dumpFilePath), description);
                });

            mainWindow.AddTab(new LoadingTabPage(Path.GetFileNameWithoutExtension(dumpFilePath), StringListPage.IconDrawingImage, operation));
        }
    }
}
