using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Win32;

namespace StringTheory.UI
{
    public sealed partial class MainWindow : INotifyPropertyChanged
    {
        public MainWindow()
        {
            OpenDumpCommand = new DelegateCommand(OpenDump);
            DataContext = this;

            InitializeComponent();
        }

        private void OpenDump()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Dump files (*.dmp)|*.dmp|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var file = openFileDialog.FileName;

                var summary = HeapAnalyzer.GetStringSummary(file);

                StringItems = summary.Strings;
                OnPropertyChanged(nameof(StringItems));
            }
        }

        public ICommand OpenDumpCommand { get; }

        public IEnumerable<StringItem> StringItems { get; set; }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
