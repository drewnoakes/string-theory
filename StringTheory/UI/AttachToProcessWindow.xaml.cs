using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;
using StringTheory.Analysis;

namespace StringTheory.UI
{
    public sealed partial class AttachToProcessWindow : INotifyPropertyChanged
    {
        public Process[] Processes { get; private set; }
        public ICommand RefreshProcessesCommand { get; }
        public ICommand AttachToProcessCommand { get; }

        public AttachToProcessWindow()
        {
            InitializeComponent();
        }

        public AttachToProcessWindow(MainWindow mainWindow)
        {
            RefreshProcessesCommand = new DelegateCommand(RefreshProcesses);
            AttachToProcessCommand = new DelegateCommand<Process>(
                process =>
                {
                    if (process == null)
                        return;

                    var operation = new LoadingOperation(
                        token =>
                        {
                            var analyzer = new HeapAnalyzer(process.Id);

                            var summary = analyzer.GetStringSummary(token);

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
            Processes = Process.GetProcesses();
            OnPropertyChanged(nameof(Processes));

            var view = CollectionViewSource.GetDefaultView(Processes);
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription(nameof(Process.ProcessName), ListSortDirection.Ascending));
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
