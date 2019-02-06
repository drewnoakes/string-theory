using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace StringTheory.UI
{
    public static class DataGridExtensions
    {
        public static readonly DependencyProperty SortDescProperty 
            = DependencyProperty.RegisterAttached(
                "SortDesc",
                typeof(bool),
                typeof(DataGridExtensions), 
                new PropertyMetadata(defaultValue: false, OnSortDescChanged));

        private static void OnSortDescChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGrid grid)
            {
                if (Equals(e.NewValue, true))
                {
                    grid.Sorting += OnGridOnSorting;
                }
                else if (Equals(e.NewValue, false))
                {
                    grid.Sorting -= OnGridOnSorting;
                }
            }
        }

        private static void OnGridOnSorting(object source, DataGridSortingEventArgs args)
        {
            if (args.Column.SortDirection == null)
            {
                // here we check an attached property value of target column
                var sortDesc = (bool) args.Column.GetValue(SortDescProperty);
                if (sortDesc)
                {
                    args.Column.SetCurrentValue(DataGridColumn.SortDirectionProperty, ListSortDirection.Ascending);
                }
            }
        }

        public static void SetSortDesc(DependencyObject element, bool value) => element.SetValue(SortDescProperty, value);
        public static bool GetSortDesc(DependencyObject element) => (bool)element.GetValue(SortDescProperty);
    }
}
