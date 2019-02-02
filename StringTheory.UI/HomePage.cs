using System.Windows.Input;
using Microsoft.Win32;

namespace StringTheory.UI
{
    public sealed class HomePage : ITabPage
    {
        public ICommand OpenDumpCommand { get; }
        public string HeaderText => "Home";
        public bool CanClose => false;

        public HomePage(MainWindow mainWindow)
        {
            OpenDumpCommand = new DelegateCommand(OpenDump);

            void OpenDump()
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "Dump files (*.dmp)|*.dmp|All files (*.*)|*.*"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var dumpFilePath = openFileDialog.FileName;

                    OpenDumpFile(dumpFilePath);
                }
            }

            void OpenDumpFile(string dumpFilePath)
            {
                var analyzer = new HeapAnalyzer(dumpFilePath);

                var summary = analyzer.GetStringSummary();

                mainWindow.AddTab(new StringListPage(mainWindow, summary, analyzer, "All strings"));
            }
        }
    }
}