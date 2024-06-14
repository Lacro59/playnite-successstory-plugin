using CommonPluginsShared.Controls;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace SuccessStory.Services
{
    public class SuccessStoryViewSidebar : SidebarItem
    {
        public SuccessStoryViewSidebar(SuccessStory plugin)
        {
            Type = SiderbarItemType.View;
            Title = ResourceProvider.GetString("LOCSuccessStoryAchievements");
            Icon = new TextBlock
            {
                Text = "\ue820",
                FontFamily = ResourceProvider.GetResource("FontIcoFont") as FontFamily
            };
            Opened = () =>
            {
                if (plugin.SidebarItemControl == null)
                {
                    plugin.SidebarItemControl = new SidebarItemControl();
                    plugin.SidebarItemControl.SetTitle(ResourceProvider.GetString("LOCSuccessStoryAchievements"));
                    plugin.SidebarItemControl.AddContent(new SuccessView());
                }

                return plugin.SidebarItemControl;
            };
            Visible = plugin.PluginSettings.Settings.EnableIntegrationButtonSide;
        }
    }
}
