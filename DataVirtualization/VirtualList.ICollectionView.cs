using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;

namespace DevZest.Windows.DataVirtualization
{
    partial class VirtualList<T> : ICollectionView, ICollectionViewFactory
    {
        private static readonly PropertyChangedEventArgs _culturePropertyChanged = new PropertyChangedEventArgs("Culture");
        private static readonly PropertyChangedEventArgs _isCurrentBeforeFirstChanged = new PropertyChangedEventArgs("IsCurrentBeforeFirst");
        private static readonly PropertyChangedEventArgs _isCurrentAfterLastChanged = new PropertyChangedEventArgs("IsCurrentAfterLast");
        private static readonly PropertyChangedEventArgs _currentPositionChanged = new PropertyChangedEventArgs("CurrentPosition");
        private static readonly PropertyChangedEventArgs _currentItemChanged = new PropertyChangedEventArgs("CurrentItem");
        private int _deferRefreshCount;
        private bool _needsRefresh;
        private CultureInfo _cultureInfo;
        private int _currentPosition = -1;
        private VirtualListItem<T> _currentItem;
        private bool _isCurrentAfterLast = false;
        private bool _isCurrentBeforeFirst = true;
        private SortDescriptionCollection _sortDescriptionCollection;

        private class RefreshDeferrer : IDisposable
        {
            private VirtualList<T> _list;

            public RefreshDeferrer(VirtualList<T> list)
            {
                _list = list;
            }

            #region IDisposable Members

            public void Dispose()
            {
                if (_list != null)
                {
                    _list.EndDeferRefresh();
                    _list = null;
                }
            }

            #endregion
        }

        private bool IsRefreshDeferred
        {
            get { return _deferRefreshCount > 0; }
        }

        private void ThrowIfDeferred()
        {
            if (IsRefreshDeferred)
                throw new Exception("Can't do this while CollectionView refresh is deferred.");
        }

        private void RefreshOrDefer()
        {
            if (IsRefreshDeferred)
                _needsRefresh = true;
            else
                Refresh();
        }

        private void EndDeferRefresh()
        {
            if (0 == --_deferRefreshCount && _needsRefresh)
            {
                _needsRefresh = false;
                Refresh();
            }
        }

        private void OnCurrentChanged()
        {
            if (CurrentChanged != null)
                CurrentChanged(this, EventArgs.Empty);
        }

        private void SortDescriptionsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RefreshOrDefer();
        }

        private void SetCurrent(VirtualListItem<T> newItem, int newPosition)
        {
            bool isCurrentBeforeFirst = _isCurrentBeforeFirst;
            bool isCurrentAfterLast = _isCurrentAfterLast;
            VirtualListItem<T> currentItem = _currentItem;
            int currentPosition = _currentPosition;

            _isCurrentBeforeFirst = newPosition < 0;
            _isCurrentAfterLast = newPosition >= Count;
            _currentItem = newItem;
            _currentPosition = newPosition;

            if (currentItem != _currentItem)
                OnCurrentChanged();

            if (isCurrentBeforeFirst != _isCurrentBeforeFirst)
                OnPropertyChanged(_isCurrentBeforeFirstChanged);
            if (isCurrentAfterLast != _isCurrentAfterLast)
                OnPropertyChanged(_isCurrentAfterLastChanged);
            if (currentItem != _currentItem)
                OnPropertyChanged(_currentItemChanged);
            if (currentPosition != _currentPosition)
                OnPropertyChanged(_currentPositionChanged);
        }

        private bool OnCurrentChanging()
        {
            if (CurrentChanging == null)
                return true;
            else
            {
                CurrentChangingEventArgs e = new CurrentChangingEventArgs();
                CurrentChanging(this, e);
                return !e.Cancel;
            }
        }

        #region ICollectionView Members

        bool ICollectionView.CanFilter
        {
            get { return false; }
        }

        bool ICollectionView.CanGroup
        {
            get { return false; }
        }

        bool ICollectionView.CanSort
        {
            get { return CanSort; }
        }

        bool CanSort
        {
            get { return _loader.CanSort; }
        }

        bool ICollectionView.Contains(object item)
        {
            return Contains(item);
        }

        bool Contains(object item)
        {
            return Contains(item as VirtualListItem<T>);
        }

        CultureInfo ICollectionView.Culture
        {
            get { return _cultureInfo; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                if (_cultureInfo != value)
                {
                    _cultureInfo = value;
                    OnPropertyChanged(_culturePropertyChanged);
                }
            }
        }

        event EventHandler CurrentChanged;
        event EventHandler ICollectionView.CurrentChanged
        {
            add { CurrentChanged += value; }
            remove { CurrentChanged -= value; }
        }

        event CurrentChangingEventHandler CurrentChanging;
        event CurrentChangingEventHandler ICollectionView.CurrentChanging
        {
            add { CurrentChanging += value; }
            remove { CurrentChanging -= value; }
        }

        object ICollectionView.CurrentItem
        {
            get { return CurrentItem; }
        }

        object CurrentItem
        {
            get
            {
                ThrowIfDeferred();
                return _currentItem;
            }
        }

        int ICollectionView.CurrentPosition
        {
            get { return CurrentPosition; }
        }

        int CurrentPosition
        {
            get
            {
                ThrowIfDeferred();
                return _currentPosition;
            }
        }

        IDisposable ICollectionView.DeferRefresh()
        {
            ++_deferRefreshCount;
            return new RefreshDeferrer(this);
        }

        Predicate<object> ICollectionView.Filter
        {
            get { return null; }
            set { throw new NotSupportedException(); }
        }

        ObservableCollection<GroupDescription> ICollectionView.GroupDescriptions
        {
            get { return null; }
        }

        ReadOnlyObservableCollection<object> ICollectionView.Groups
        {
            get { return null; }
        }

        bool ICollectionView.IsCurrentAfterLast
        {
            get
            {
                ThrowIfDeferred();
                return _isCurrentAfterLast; 
            }
        }

        bool ICollectionView.IsCurrentBeforeFirst
        {
            get 
            {
                ThrowIfDeferred();
                return _isCurrentBeforeFirst; 
            }
        }

        bool ICollectionView.IsEmpty
        {
            get { return IsEmpty; }
        }

        bool IsEmpty
        {
            get { return Count == 0; }
        }

        bool ICollectionView.MoveCurrentTo(object item)
        {
            ThrowIfDeferred();
            int position = IndexOf(item as VirtualListItem<T>);
            return this.MoveCurrentToPosition(position);
        }

        bool ICollectionView.MoveCurrentToFirst()
        {
            ThrowIfDeferred();
            return MoveCurrentToPosition(0);
        }

        bool ICollectionView.MoveCurrentToLast()
        {
            ThrowIfDeferred();
            return MoveCurrentToPosition(Count - 1);
        }

        bool ICollectionView.MoveCurrentToNext()
        {
            ThrowIfDeferred();
            int position = _currentPosition + 1;
            return position <= Count && MoveCurrentToPosition(position);
        }

        bool ICollectionView.MoveCurrentToPosition(int position)
        {
            return MoveCurrentToPosition(position);
        }

        bool MoveCurrentToPosition(int position)
        {
            ThrowIfDeferred();
            if (position < -1 || position > Count)
                throw new ArgumentOutOfRangeException("position");

            if (position != _currentPosition && OnCurrentChanging())
            {
                if (position == -1 || position == Count)
                    SetCurrent(null, position);
                else
                    SetCurrent(this[position], position);
            }
            return true;
          }

        bool ICollectionView.MoveCurrentToPrevious()
        {
            ThrowIfDeferred();
            int position = _currentPosition - 1;
            return position >= -1 && MoveCurrentToPosition(position);
        }

        SortDescriptionCollection ICollectionView.SortDescriptions
        {
            get { return SortDescriptions; }
        }

        SortDescriptionCollection SortDescriptions
        {
            get
            {
                if (!CanSort)
                    return null;

                if (_sortDescriptionCollection == null)
                {
                    _sortDescriptionCollection = new SortDescriptionCollection();
                    ((INotifyCollectionChanged)_sortDescriptionCollection).CollectionChanged += SortDescriptionsChanged;
                }
                return _sortDescriptionCollection;
            }
        }

        IEnumerable ICollectionView.SourceCollection
        {
            get { return this; }
        }

        #endregion

        #region ICollectionViewFactory Members

        ICollectionView ICollectionViewFactory.CreateView()
        {
            return this;
        }

        #endregion

    }
}
