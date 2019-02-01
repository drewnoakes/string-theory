namespace StringTheory.UI
{
    public interface ITabPage
    {
        string HeaderText { get; }

        bool CanClose { get; }
    }
}