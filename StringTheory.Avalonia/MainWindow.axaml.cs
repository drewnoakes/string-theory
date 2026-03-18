using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Input;

namespace StringTheory.Avalonia;

public sealed partial class MainWindow : global::Avalonia.Controls.Window, INotifyPropertyChanged
{
    public ObservableCollection<ITabPage> TabPages { get; }

    public int SelectedTabIndex { get; set; }

    public ICommand CloseCommand { get; }

    public double Scale { get; private set; } = 1.0;

    public MainWindow()
    {
        TabPages =
        [
            new HomePage(this)
        ];

        CloseCommand = new DelegateCommand<ITabPage>(RemoveTabPage);

        DataContext = this;

        InitializeComponent();

        KeyDown += OnKeyDown;
        PointerWheelChanged += OnPointerWheelChanged;
    }

    public void AddTab(ITabPage tabPage)
    {
        TabPages.Add(tabPage);

        SelectedTabIndex = TabPages.Count - 1;
        OnPropertyChanged(nameof(SelectedTabIndex));

        tabPage.CloseRequested += delegate { RemoveTabPage(tabPage); };

        if (tabPage is LoadingTabPage loading)
        {
            loading.PageLoaded += page => ReplaceTab(loading, page);
        }
    }

    public void ReplaceTab(ITabPage oldTab, ITabPage newTab)
    {
        var index = TabPages.IndexOf(oldTab);
        if (index < 0)
            return;

        TabPages[index] = newTab;
        newTab.CloseRequested += delegate { RemoveTabPage(newTab); };

        SelectedTabIndex = index;
        OnPropertyChanged(nameof(SelectedTabIndex));
    }

    private void RemoveTabPage(ITabPage tabPage)
    {
        if (tabPage.CanClose)
        {
            var index = TabPages.IndexOf(tabPage);

            TabPages.Remove(tabPage);

            if (tabPage is IDisposable disposable)
                disposable.Dispose();

            if (index >= 0 && TabPages.Count > 0)
            {
                SelectedTabIndex = Math.Min(index, TabPages.Count - 1);
                OnPropertyChanged(nameof(SelectedTabIndex));
            }
        }
    }

    private void OnTabHeaderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(null).Properties.IsMiddleButtonPressed
            && sender is global::Avalonia.Controls.Control { DataContext: ITabPage tabPage })
        {
            RemoveTabPage(tabPage);
            e.Handled = true;
        }
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            const double min = 1;
            const double max = 4;
            const double step = 0.1;

            if (e.Delta.Y > 0 && Scale < max)
            {
                Scale = Math.Min(max, Scale + step);
                OnPropertyChanged(nameof(Scale));
            }
            else if (e.Delta.Y < 0 && Scale > min)
            {
                Scale = Math.Max(min, Scale - step);
                OnPropertyChanged(nameof(Scale));
            }

            e.Handled = true;
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            switch (e.Key)
            {
                case Key.F4 when CanCloseCurrentTab():
                case Key.W when CanCloseCurrentTab():
                {
                    RemoveTabPage(TabPages[SelectedTabIndex]);
                    e.Handled = true;
                    return;
                }
                case Key.PageDown:
                {
                    SelectedTabIndex = (SelectedTabIndex + 1) % TabPages.Count;
                    OnPropertyChanged(nameof(SelectedTabIndex));
                    e.Handled = true;
                    return;
                }
                case Key.PageUp:
                {
                    SelectedTabIndex = (SelectedTabIndex - 1 + TabPages.Count) % TabPages.Count;
                    OnPropertyChanged(nameof(SelectedTabIndex));
                    e.Handled = true;
                    return;
                }
            }
        }

        bool CanCloseCurrentTab() => SelectedTabIndex >= 0 &&
                                     SelectedTabIndex < TabPages.Count &&
                                     TabPages[SelectedTabIndex].CanClose;
    }

    protected override void OnClosed(EventArgs e)
    {
        foreach (var disposable in TabPages.OfType<IDisposable>())
            disposable.Dispose();

        base.OnClosed(e);
    }

    #region INotifyPropertyChanged

    public new event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}
