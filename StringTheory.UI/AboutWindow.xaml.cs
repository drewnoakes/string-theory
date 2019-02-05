using System;
using System.Diagnostics;
using System.Windows.Navigation;

namespace StringTheory.UI
{
    public sealed partial class AboutWindow
    {
        public AboutWindow()
        {
            InitializeComponent();
        }

        private void OnGitHubLinkClicked(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            Close();
        }
    }
}
