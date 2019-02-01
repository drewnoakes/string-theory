using System;

namespace StringTheory.UI
{
    public sealed class ReferrersPage : ITabPage
    {
        public ReferrerTreeViewModel ReferrerTree { get; }

        // TODO contextual tab header text
        public string HeaderText => "Referrers";
        public bool CanClose => true;

        public ReferrersPage(ReferrerTreeViewModel referrerTree)
        {
            ReferrerTree = referrerTree ?? throw new ArgumentNullException(nameof(referrerTree));
        }
    }
}