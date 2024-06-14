using CommonPluginsShared;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SuccessStory.Services
{
    public class SuccessStoryTopPanelItem : TopPanelItem
    {
        public SuccessStoryTopPanelItem(SuccessStory plugin)
        {
            Icon = new TextBlock
            {
                Text = "\ue820",
                FontSize = 22,
                FontFamily = ResourceProvider.GetResource("FontIcoFont") as FontFamily
            };
            Title = ResourceProvider.GetString("LOCSuccessStoryViewGames");
            Activated = () =>
            {
                SuccessView ViewExtension = new SuccessView();

                WindowOptions windowOptions = new WindowOptions
                {
                    ShowMinimizeButton = false,
                    ShowMaximizeButton = true,
                    ShowCloseButton = true,
                    CanBeResizable = true,
                    Width = 1280,
                    Height = 740
                };

                Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(ResourceProvider.GetString("LOCSuccessStory"), ViewExtension, windowOptions);
                windowExtension.ShowDialog();
                SuccessStory.PluginDatabase.IsViewOpen = false;
            };
            Visible = plugin.PluginSettings.Settings.EnableIntegrationButtonHeader;
        }
    }
}
