using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Input;
using Avalonia.Interactivity;
using StringTheory.Analysis;

namespace StringTheory.Avalonia;

public sealed partial class AttachToProcessWindow : global::Avalonia.Controls.Window, INotifyPropertyChanged
{
    private string _filterText = "";
    private ProcessInfo[]? _allProcesses;

    public ProcessInfo[]? Processes { get; private set; }
    public ICommand? RefreshProcessesCommand { get; }
    public ICommand? AttachToProcessCommand { get; }

    public string FilterText
    {
        get => _filterText;
        set
        {
            if (_filterText == value)
                return;
            _filterText = value;
            OnPropertyChanged();
            ApplyFilter();
        }
    }

    public AttachToProcessWindow()
    {
        InitializeComponent();
    }

    public AttachToProcessWindow(MainWindow mainWindow)
    {
        RefreshProcessesCommand = new DelegateCommand(RefreshProcesses);
        AttachToProcessCommand = new DelegateCommand<ProcessInfo>(
            process =>
            {
                if (process == null)
                    return;

                var operation = new LoadingOperation(
                    (progressCallback, token) =>
                    {
                        var analyzer = new HeapAnalyzer(process.Id);

                        var summary = analyzer.GetStringSummary(progressCallback, token);

                        var description = $"All strings in process {process.Id} ({process.ProcessName})";

                        return new StringListPage(mainWindow, summary, analyzer, process.ProcessName, description);
                    });

                mainWindow.AddTab(new LoadingTabPage(process.ProcessName, StringListPage.IconDrawingImage, operation));

                Close();
            });

        RefreshProcesses();

        InitializeComponent();
    }

    private void OnGridDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (_grid.SelectedItem is ProcessInfo process)
        {
            AttachToProcessCommand?.Execute(process);
        }
    }

    private void RefreshProcesses()
    {
        _allProcesses = Process.GetProcesses()
            .Select(p => new ProcessInfo(p))
            .OrderBy(p => p.ProcessName)
            .ToArray();

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (_allProcesses is null)
            return;

        var filter = _filterText;

        Processes = string.IsNullOrWhiteSpace(filter)
            ? _allProcesses
            : _allProcesses.Where(p => p.MatchesFilter(filter)).ToArray();

        OnPropertyChanged(nameof(Processes));
    }

    #region INotifyPropertyChanged

    public new event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}
