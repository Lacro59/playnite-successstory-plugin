using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace CommonPluginsShared.Controls
{
    public class ListBoxExtend : ListBox
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
            typeof(ListBoxExtend),
            new FrameworkPropertyMetadata(false, BubblingScrollEventsChangedCallback));

        private static void BubblingScrollEventsChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var obj = sender as ListBoxExtend;
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


        public ListBoxExtend()
        {
            this.Loaded += ListBoxExtend_Loaded;
        }


        private void ListBoxExtend_Loaded(object sender, RoutedEventArgs e)
        {
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
    }
}
