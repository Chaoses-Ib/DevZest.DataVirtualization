using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Data;
using System.Windows.Threading;
using DevZest.Windows.DataVirtualization;

namespace DevZest.DataVirtualizationDemo
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window, IVirtualListLoader<Person>
    {
        public readonly DependencyProperty ItemCountProperty = DependencyProperty.Register("ItemCount", typeof(int), typeof(Window1), new PropertyMetadata(100000,null ,ItemCountCoerceValue));
        public readonly DependencyProperty CreationOverheadProperty = DependencyProperty.Register("CreationOverhead", typeof(int), typeof(Window1), new PropertyMetadata(3000, null, CreationOverheadCoerceValue));
        public readonly DependencyProperty SimulateDataLoadingErrorProperty = DependencyProperty.Register("SimulateDataLoadingError", typeof(bool), typeof(Window1), new PropertyMetadata(false));
        public Window1()
        {
            InitializeComponent();
        }


        public int ItemCount
        {
            get { return (int)GetValue(ItemCountProperty); }
            set { SetValue(ItemCountProperty, value); }
        }

        public int CreationOverhead
        {
            get { return (int)GetValue(CreationOverheadProperty); }
            set { SetValue(CreationOverheadProperty, value); }
        }

        public bool SimulateDataLoadingError
        {
            get { return (bool)GetValue(SimulateDataLoadingErrorProperty); }
            set { SetValue(SimulateDataLoadingErrorProperty, value); }
        }
        
        private static object ItemCountCoerceValue(DependencyObject d, object baseValue)
        {
            int value;
            // if the item count is not an integral value greater than 0, coerce it to 100
            if (baseValue == null || !int.TryParse(baseValue.ToString(), out value) || value < 1)
                return 100;
            return baseValue;
        }

        private static object CreationOverheadCoerceValue(DependencyObject d, object baseValue)
        {
            int value;
            // if the creation overhead is not an integral value greater than 0, coerce it to 1
            if (baseValue == null || !int.TryParse(baseValue.ToString(), out value) || value < 1)
                return 1;
            // creation overhead should not be greater than 10 seconds
            if (value > 10000)
                return 10000;
            return baseValue;
        }

        private Person LoadPerson(int id)
        {
            return new Person(id);
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            VirtualList<Person> data = new VirtualList<Person>(this);
            listView.ItemsSource = data;
        }

        // This method helps to get dependency property value in another thread.
        // Usage: Invoke(() => { return CreationOverhead; });
        private T Invoke<T>(Func<T> callback)
        {
            return (T)Dispatcher.Invoke(DispatcherPriority.Send, new Func<object>(() => { return callback(); }));
        }

        #region IVirtualListLoader<Person> Members

        public bool CanSort
        {
            get { return true; }
        }

        public IList<Person> LoadRange(int startIndex, int count, SortDescriptionCollection sortDescriptions, out int overallCount)
        {
            int creationOverhead = Invoke(() => { return CreationOverhead; });
            Thread.Sleep(creationOverhead);

            bool simulateError = Invoke(() => { return SimulateDataLoadingError; });
            if (simulateError)
                throw new ApplicationException("An simulated data loading error occured. Clear the \"Simulate Data Loading Error\" checkbox and retry.");

            overallCount = Invoke(() => { return ItemCount; });

            // because the all fields are sorted ascending, the PropertyName is ignored in this sample
            // only Direction is considered.
            SortDescription sortDescription = sortDescriptions == null || sortDescriptions.Count == 0 ? new SortDescription() : sortDescriptions[0];
            ListSortDirection direction = string.IsNullOrEmpty(sortDescription.PropertyName) ? ListSortDirection.Ascending : sortDescription.Direction;

            Person[] persons = new Person[count];
            for (int i = 0; i < count; i++)
            {
                int index;
                if (direction == ListSortDirection.Ascending)
                    index = startIndex + i;
                else
                    index = overallCount - 1 - startIndex - i;

                persons[i] = new Person(index);
            }

            return persons;
        }

        #endregion

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Hyperlink hyperLink = (Hyperlink)sender;
            NavigateUri(hyperLink.NavigateUri);
        }

        private static void NavigateUri(Uri uri)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                Process.Start(uri.ToString());
            }
            catch (Win32Exception ex)
            {
                Mouse.OverrideCursor = null;
                MessageBox.Show(ex.Message);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }
    }
}
