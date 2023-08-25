using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Documents;

namespace DevZest.Windows.DataVirtualization
{
    public static class GridViewSort
    {
        #region Public attached properties

        public static ICommand GetCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(CommandProperty);
        }

        public static void SetCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(CommandProperty, value);
        }

        public static readonly DependencyProperty CommandProperty = DependencyProperty.RegisterAttached("Command", typeof(ICommand), typeof(GridViewSort),
                new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnCommandPropertyChanged)));

        private static void OnCommandPropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ItemsControl listView = o as ItemsControl;
            if (listView != null)
            {
                if (!GetAutoSort(listView)) // Don't change click handler if AutoSort enabled
                {
                    if (e.OldValue != null && e.NewValue == null)
                    {
                        listView.RemoveHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(ColumnHeader_Click));
                    }
                    if (e.OldValue == null && e.NewValue != null)
                    {
                        listView.AddHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(ColumnHeader_Click));
                    }
                }
            }
        }

        public static bool GetAutoSort(DependencyObject obj)
        {
            return (bool)obj.GetValue(AutoSortProperty);
        }

        public static void SetAutoSort(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoSortProperty, value);
        }

        public static readonly DependencyProperty AutoSortProperty = DependencyProperty.RegisterAttached("AutoSort", typeof(bool), typeof(GridViewSort),
                new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnAutoSortPropertyChanged)));

        private static void OnAutoSortPropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ListView listView = o as ListView;
            if (listView != null)
            {
                if (GetCommand(listView) == null) // Don't change click handler if a command is set
                {
                    bool oldValue = (bool)e.OldValue;
                    bool newValue = (bool)e.NewValue;
                    if (oldValue && !newValue)
                        listView.RemoveHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(ColumnHeader_Click));
                    if (!oldValue && newValue)
                        listView.AddHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(ColumnHeader_Click));
                }
            }
        }

        public static string GetPropertyName(DependencyObject obj)
        {
            return (string)obj.GetValue(PropertyNameProperty);
        }

        public static void SetPropertyName(DependencyObject obj, string value)
        {
            obj.SetValue(PropertyNameProperty, value);
        }

        public static readonly DependencyProperty PropertyNameProperty = DependencyProperty.RegisterAttached("PropertyName", typeof(string), typeof(GridViewSort),
                new FrameworkPropertyMetadata(null));

        private static readonly DependencyPropertyKey SortOrderPropertyKey = DependencyProperty.RegisterAttachedReadOnly("SortOrder", typeof(SortOrder), typeof(GridViewSort),
            new FrameworkPropertyMetadata(SortOrder.None));
        public static readonly DependencyProperty SortOrderProperty = SortOrderPropertyKey.DependencyProperty;

        public static SortOrder GetSortOrder(GridViewColumnHeader element)
        {
            return (SortOrder)element.GetValue(SortOrderProperty);
        }

        private static void SetSortOrder(GridViewColumnHeader element, SortOrder value)
        {
            element.SetValue(SortOrderPropertyKey, value);
        }

        #endregion

        #region Private attached properties

        private static GridViewColumnHeader GetSortedColumnHeader(DependencyObject obj)
        {
            return (GridViewColumnHeader)obj.GetValue(SortedColumnHeaderProperty);
        }

        private static void SetSortedColumnHeader(DependencyObject obj, GridViewColumnHeader value)
        {
            obj.SetValue(SortedColumnHeaderProperty, value);
        }

        private static readonly DependencyProperty SortedColumnHeaderProperty = DependencyProperty.RegisterAttached("SortedColumnHeader", typeof(GridViewColumnHeader), typeof(GridViewSort),
            new FrameworkPropertyMetadata(null));

        #endregion

        #region Column header click event handler

        private static void ColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader headerClicked = e.OriginalSource as GridViewColumnHeader;
            if (headerClicked != null && headerClicked.Column != null)
            {
                string propertyName = GetPropertyName(headerClicked.Column);
                if (!string.IsNullOrEmpty(propertyName))
                {
                    ListView listView = GetAncestor<ListView>(headerClicked);
                    if (listView != null)
                    {
                        ICommand command = GetCommand(listView);
                        if (command != null)
                        {
                            if (command.CanExecute(propertyName))
                                command.Execute(propertyName);
                        }
                        else if (GetAutoSort(listView))
                        {
                            ApplySort(listView, headerClicked, propertyName);
                        }
                    }
                }
            }
        }

        #endregion

        #region Helper methods

        public static T GetAncestor<T>(DependencyObject reference)
            where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(reference);
            while (!(parent is T))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            if (parent != null)
                return (T)parent;
            else
                return null;
        }

        static ListSortDirection GetSortDirection(ListView listView, GridViewColumnHeader currentSortedColumnHeader, GridViewColumnHeader clickedColumnHeader, string propertyName)
        {
            if (currentSortedColumnHeader != clickedColumnHeader)
                return ListSortDirection.Ascending;

            ICollectionView view = listView.Items;
            if (view.SortDescriptions.Count == 0)
                return ListSortDirection.Ascending;

            SortDescription currentSort = view.SortDescriptions[0];
            if (currentSort.PropertyName == propertyName)
            {
                if (currentSort.Direction == ListSortDirection.Ascending)
                    return ListSortDirection.Descending;
                else
                    return ListSortDirection.Ascending;
            }
            else
                return ListSortDirection.Ascending;
        }

        static void ApplySort(ListView listView, GridViewColumnHeader clickedColumnHeader, string propertyName)
        {
            GridViewColumnHeader currentSortedColumnHeader = GetSortedColumnHeader(listView);
            ListSortDirection direction = GetSortDirection(listView, currentSortedColumnHeader, clickedColumnHeader, propertyName);

            ICollectionView view = listView.Items;
            using (view.DeferRefresh())
            {
                view.SortDescriptions.Clear();
                view.SortDescriptions.Add(new SortDescription(propertyName, direction));
            }
            SetSortedColumnHeader(listView, clickedColumnHeader);

            if (currentSortedColumnHeader != null && currentSortedColumnHeader != clickedColumnHeader)
                SetSortOrder(currentSortedColumnHeader, SortOrder.None);
            SetSortOrder(clickedColumnHeader, direction == ListSortDirection.Ascending ? SortOrder.Ascending : SortOrder.Descending);
        }

        #endregion
    }
}