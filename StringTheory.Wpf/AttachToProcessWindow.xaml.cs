using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;
using StringTheory.Analysis;

namespace StringTheory.Wpf;

public sealed partial class AttachToProcessWindow : INotifyPropertyChanged
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

                mainWindow.AddTab(new LoadingTabPage(process.ProcessName, StringListPage.IconDrawingBrush, operation));

                Close();
            });

        RefreshProcesses();

        InitializeComponent();
    }

    private void RefreshProcesses()
    {
        _allProcesses = Process.GetProcesses()
            .Select(p => new ProcessInfo(p))
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

        var view = CollectionViewSource.GetDefaultView(Processes);
        view.SortDescriptions.Clear();
        view.SortDescriptions.Add(new SortDescription(nameof(ProcessInfo.ProcessName), ListSortDirection.Ascending));
    }

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}
