using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shell;

namespace CommonPluginsShared.Controls
{
    public partial class SidebarItemControl : UserControl
    {
        private TextBlock PART_TextBlockTitle;
        private Grid PART_GridContener;


        public SidebarItemControl(IPlayniteAPI PlayniteApi)
        {
            // Link
            TextBlock textBlockLink = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center
            };
            WindowChrome.SetIsHitTestVisibleInChrome(textBlockLink, true);

            Hyperlink hyperlink = new Hyperlink();
            hyperlink.Click += (s, e) => { PlayniteApi.MainView.SwitchToLibraryView(); };
            hyperlink.Inlines.Add(new TextBlock
            {
                Text = "\uea5c",
                FontFamily = ResourceProvider.GetResource("FontIcoFont") as FontFamily,
                FontSize = 26
            });
            textBlockLink.Inlines.Add(hyperlink);

            // Title link
            PART_TextBlockTitle = new TextBlock
            {
                Style = ResourceProvider.GetResource("BaseTextBlockStyle") as Style,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10,0,0,0),
                FontSize = 18
            };

            // Link contener
            StackPanel stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(10, 10, 0, 0)
            };
            DockPanel.SetDock(stackPanel, Dock.Top);

            stackPanel.Children.Add(textBlockLink);
            stackPanel.Children.Add(PART_TextBlockTitle);


            // Content Grid
            PART_GridContener = new Grid();

            // Content ScrollViewer
            ScrollViewer scrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            };

            scrollViewer.Content = PART_GridContener;


            // Control
            DockPanel dockPanel = new DockPanel();
            dockPanel.Children.Add(stackPanel);
            dockPanel.Children.Add(scrollViewer);

            this.Content = dockPanel;
        }


        public void SetTitle(string Title)
        {
            PART_TextBlockTitle.Text = Title;
        }

        public void AddContent(FrameworkElement content)
        {
            PART_GridContener.Children.Add(content);
        }
    }
}
