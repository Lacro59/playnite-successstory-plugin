using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace CommonPluginsShared.Controls
{
    public class ListViewExtend : ListView
    {
        #region HeightStretch
        public static readonly DependencyProperty HeightStretchProperty;
        public bool HeightStretch { get; set; }
        #endregion

        #region WidthStretch
        public static readonly DependencyProperty WidthStretchProperty;
        public bool WidthStretch { get; set; }
        #endregion  

        #region BubblingScrollEvents
        public bool BubblingScrollEvents
        {
            get { return (bool)GetValue(BubblingScrollEventsProperty); }
            set { SetValue(BubblingScrollEventsProperty, value); }
        }

        public static readonly DependencyProperty BubblingScrollEventsProperty = DependencyProperty.Register(
            nameof(BubblingScrollEvents),
            typeof(bool),
            typeof(ListViewExtend),
            new FrameworkPropertyMetadata(false, BubblingScrollEventsChangedCallback));

        private static void BubblingScrollEventsChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var obj = sender as ListViewExtend;
            if (obj != null && e.NewValue != e.OldValue)
            {
                if ((bool)e.NewValue)
                {
                    obj.PreviewMouseWheel += UI.HandlePreviewMouseWheel;
                }
                else
                {
                    obj.PreviewMouseWheel -= UI.HandlePreviewMouseWheel;
                }
            }
        }
        #endregion


        public ListViewExtend()
        {
            this.Loaded += ListViewExtend_Loaded;
            this.AddHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(ListViewExtend_onHeaderClick));
        }


        private void ListViewExtend_Loaded(object sender, RoutedEventArgs e)
        {
            EnableSorting_PropertyChanged(null, null);


            if (HeightStretch)
            {
                this.Height = ((FrameworkElement)sender).ActualHeight;
            }
            if (WidthStretch)
            {
                this.Width = ((FrameworkElement)sender).ActualWidth;
            }

            ((FrameworkElement)this.Parent).SizeChanged += Parent_SizeChanged;
        }

        private void Parent_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (HeightStretch)
            {
                this.Height = ((FrameworkElement)sender).ActualHeight;
            }
            if (WidthStretch)
            {
                this.Width = ((FrameworkElement)sender).ActualWidth;
            }
        }


        #region Sorting
        private readonly string CaretDown = "\uea67";
        private readonly string CaretUp = "\uea6a";


        public bool SortingEnable
        {
            get { return (bool)GetValue(SortingEnableProperty); }
            set { SetValue(SortingEnableProperty, value); }
        }

        public static readonly DependencyProperty SortingEnableProperty = DependencyProperty.Register(
            nameof(SortingEnable),
            typeof(bool),
            typeof(ListViewExtend),
            new FrameworkPropertyMetadata(false, SortingPropertyChangedCallback));


        public string SortingDefaultDataName
        {
            get { return (string)GetValue(SortingDefaultDataNameProperty); }
            set { SetValue(SortingDefaultDataNameProperty, value); }
        }

        public static readonly DependencyProperty SortingDefaultDataNameProperty = DependencyProperty.Register(
            nameof(SortingDefaultDataName),
            typeof(string),
            typeof(ListViewExtend),
            new FrameworkPropertyMetadata(string.Empty, SortingPropertyChangedCallback));


        public ListSortDirection SortingSortDirection
        {
            get { return (ListSortDirection)GetValue(SortingSortDirectionProperty); }
            set { SetValue(SortingSortDirectionProperty, value); }
        }

        public static readonly DependencyProperty SortingSortDirectionProperty = DependencyProperty.Register(
            nameof(SortingSortDirection),
            typeof(ListSortDirection),
            typeof(ListViewExtend),
            new FrameworkPropertyMetadata(ListSortDirection.Ascending, SortingPropertyChangedCallback));


        private static void SortingPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ListViewExtend obj = sender as ListViewExtend;
            if (obj != null && e.NewValue != e.OldValue)
            {
                if (e.NewValue is ListSortDirection)
                {
                    if ((ListSortDirection)e.NewValue == ListSortDirection.Ascending)
                    {
                        obj._lastDirection = ListSortDirection.Descending;
                    }
                    else
                    {
                        obj._lastDirection = ListSortDirection.Ascending;
                    }
                }
                else
                {
                    obj.EnableSorting_PropertyChanged(null, null);
                }
            }
        }


        private GridViewColumnHeader _lastHeaderClicked = null;
        private ListSortDirection? _lastDirection;

        private void EnableSorting_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (SortingEnable)
            {
                if (_lastHeaderClicked == null)
                {
                    if (SortingDefaultDataName.IsNullOrEmpty())
                    {
                        return;
                    }

                    try
                    {
                        GridViewColumnHeader gridViewColumnHeader = FindGridViewColumn(SortingDefaultDataName);
                        if (gridViewColumnHeader != null)
                        {
                            RoutedEventArgs routedEventArgs = new RoutedEventArgs(null, gridViewColumnHeader);
                            ListViewExtend_onHeaderClick(null, routedEventArgs);
                        }
                        else
                        {
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false);
                    }
                }
            }
            else
            {

            }
        }


        private GridViewColumnHeader FindGridViewColumn(string DataName)
        {
            if (this.View != null && this.View is GridView)
            {
                try
                {
                    GridView gridView = this.View as GridView;
                    foreach (GridViewColumn gridViewColumn in gridView.Columns)
                    {
                        if (((Binding)gridViewColumn.DisplayMemberBinding) != null)
                        {
                            string property = ((Binding)gridViewColumn.DisplayMemberBinding).Path.Path;
                            if (property == DataName)
                            {
                                return gridViewColumn.Header as GridViewColumnHeader;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false);
                }
            }
            return null;
        }


        private void ListViewExtend_onHeaderClick(object sender, RoutedEventArgs e)
        {
            if (SortingEnable)
            {
                try
                {
                    var headerClicked = e.OriginalSource as GridViewColumnHeader;
                    ListSortDirection direction;

                    if (headerClicked == null)
                    {
                        return;
                    }

                    // No sort
                    if (((string)headerClicked.Tag)?.ToLower() == "nosort")
                    {
                        headerClicked = null;
                    }

                    if (headerClicked != null)
                    {
                        if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                        {
                            if (_lastDirection == null)
                            {
                                direction = ListSortDirection.Ascending;
                            }
                            else if (_lastDirection == ListSortDirection.Ascending)
                            {
                                direction = ListSortDirection.Descending;
                            }
                            else
                            {
                                direction = ListSortDirection.Ascending;
                            }

                            if (_lastHeaderClicked != null && headerClicked != _lastHeaderClicked)
                            {
                                direction = ListSortDirection.Ascending;
                            }

                            if (headerClicked.Column != null)
                            {
                                Binding columnBinding;
                                string sortBy;

                                // Specific sort with another column
                                if (headerClicked.Tag != null && !((string)headerClicked.Tag).IsNullOrEmpty())
                                {
                                    GridViewColumnHeader gridViewColumnHeader = FindGridViewColumn((string)headerClicked.Tag);
                                    if (gridViewColumnHeader == null)
                                    {
                                        return;
                                    }

                                    columnBinding = gridViewColumnHeader.Column.DisplayMemberBinding as Binding;
                                    sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;
                                }
                                else
                                {
                                    columnBinding = headerClicked.Column.DisplayMemberBinding as Binding;
                                    sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;
                                }

                                Sort(sortBy, direction);


                                if (_lastHeaderClicked != null)
                                {
                                    StackPanel stackPanel_Last = _lastHeaderClicked.Content as StackPanel;
                                    Label label_Last = stackPanel_Last.Children[0] as Label;

                                    _lastHeaderClicked.Content = label_Last.Content;
                                }


                                // Show caret
                                if (headerClicked is GridViewColumnHeaderExtend)
                                {
                                    int RefIndex = ((GridViewColumnHeaderExtend)headerClicked).RefIndex;

                                    if (RefIndex != -1)
                                    {
                                        GridView gridView = this.View as GridView;
                                        GridViewColumn gridViewColumn = gridView.Columns[RefIndex];
                                        headerClicked = gridViewColumn.Header as GridViewColumnHeader;
                                    }
                                }

                                Label labelHeader = new Label();
                                labelHeader.Content = headerClicked.Content;

                                Label labelCaret = new Label();
                                labelCaret.FontFamily = Application.Current?.TryFindResource("FontIcoFont") as FontFamily;

                                if (direction == ListSortDirection.Ascending)
                                {
                                    labelCaret.Content = $" {CaretUp}";
                                }
                                else
                                {
                                    labelCaret.Content = $" {CaretDown}";
                                }

                                StackPanel stackPanel = new StackPanel();
                                stackPanel.Orientation = Orientation.Horizontal;
                                stackPanel.Children.Add(labelHeader);
                                stackPanel.Children.Add(labelCaret);

                                headerClicked.Content = stackPanel;


                                // Remove arrow from previously sorted header
                                if (_lastHeaderClicked != null && _lastHeaderClicked != headerClicked)
                                {
                                    _lastHeaderClicked.Column.HeaderTemplate = null;
                                }

                                _lastHeaderClicked = headerClicked;
                                _lastDirection = direction;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false);
                }
            }
        }


        public void Sorting()
        {
            if (_lastHeaderClicked != null)
            {
                var headerClicked = _lastHeaderClicked;
                if (headerClicked.Column != null)
                {
                    Binding columnBinding;
                    string sortBy;

                    // Specific sort with another column
                    if (headerClicked.Tag != null && !((string)headerClicked.Tag).IsNullOrEmpty())
                    {
                        GridViewColumnHeader gridViewColumnHeader = FindGridViewColumn((string)headerClicked.Tag);
                        if (gridViewColumnHeader == null)
                        {
                            return;
                        }

                        columnBinding = gridViewColumnHeader.Column.DisplayMemberBinding as Binding;
                        sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;
                    }
                    else
                    {
                        columnBinding = headerClicked.Column.DisplayMemberBinding as Binding;
                        sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;
                    }

                    Sort(sortBy, (ListSortDirection)_lastDirection);
                }
            }
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            if (this.ItemsSource != null)
            {
                ICollectionView dataView = CollectionViewSource.GetDefaultView(this.ItemsSource);
                dataView.SortDescriptions.Clear();
                SortDescription sd = new SortDescription(sortBy, direction);
                dataView.SortDescriptions.Add(sd);
                dataView.Refresh();
            }
        }
        #endregion
    }


    public class GridViewColumnHeaderExtend : GridViewColumnHeader
    {
        public int RefIndex
        {
            get { return (int)GetValue(RefIndexProperty); }
            set { SetValue(RefIndexProperty, value); }
        }

        public static readonly DependencyProperty RefIndexProperty = DependencyProperty.Register(
            nameof(RefIndex),
            typeof(int),
            typeof(GridViewColumnHeaderExtend),
            new FrameworkPropertyMetadata(-1));
    }
}
