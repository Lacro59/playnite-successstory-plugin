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
    public class SuccessStoryViewRaSidebar : SidebarItem
    {
        public SuccessStoryViewRaSidebar(SuccessStory plugin)
        {
            Type = SiderbarItemType.View;
            Title = ResourceProvider.GetString("LOCSuccessStoryRetroAchievements");
            Icon = new TextBlock
            {
                Text = "\ue910",
                FontFamily = ResourceProvider.GetResource("CommonFont") as FontFamily
            };
            Opened = () =>
            {
                if (plugin.SidebarRaItemControl == null)
                {
                    plugin.SidebarRaItemControl = new SidebarItemControl();
                    plugin.SidebarRaItemControl.SetTitle(ResourceProvider.GetString("LOCSuccessStoryRetroAchievements"));
                    plugin.SidebarRaItemControl.AddContent(new SuccessView(true));
                }

                return plugin.SidebarRaItemControl;
            };
            Visible = plugin.PluginSettings.Settings.EnableIntegrationButtonSide && plugin.PluginSettings.Settings.EnableRetroAchievementsView;
        }
    }
}
