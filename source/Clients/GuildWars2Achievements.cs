using CommonPluginsShared;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Clients
{
    class GuildWars2Achievements : GenericAchievements
    {
        private const string UrlApiOwnedAchievements = @"https://api.guildwars2.com/v2/account/achievements";
        private const string UrlApiAchievementsList = @"https://api.guildwars2.com/v2/achievements";
        private const string UrlApiAchievements = UrlApiAchievementsList + @"?ids={0}&lang={1}";
        private const string UrlApiAchievementsGroups = @"https://api.guildwars2.com/v2/achievements/categories/";


        public GuildWars2Achievements() : base("GuildWars2", CodeLang.GetOriginLangCountry(PluginDatabase.PlayniteApi.ApplicationSettings.Language))
        {
            
        }


        public override GameAchievements GetAchievements(Game game)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Achievements> AllAchievements = new List<Achievements>();

            try
            {
                // List achievements
                string DataList = Web.DownloadStringData(UrlApiAchievementsList).GetAwaiter().GetResult();
                Serialization.TryFromJson<List<int>>(DataList, out List<int> gw2AchievementsList);

                if (gw2AchievementsList == null)
                {
                    ShowNotificationPluginWebError(new Exception("No data"), UrlApiAchievementsList);
                    gameAchievements.Items = AllAchievements;
                    return gameAchievements;
                }

                // List groups of achievements
                string DataGroups = Web.DownloadStringData(UrlApiAchievementsGroups).GetAwaiter().GetResult();
                Serialization.TryFromJson<List<int>>(DataGroups, out List<int> gw2AchievementsGroupsList);

                if (gw2AchievementsGroupsList == null)
                {
                    ShowNotificationPluginWebError(new Exception("No data"), UrlApiAchievementsList);
                    gameAchievements.Items = AllAchievements;
                    return gameAchievements;
                }

                List<GW2AchievementsGroups> gw2AchievementsGroups = new List<GW2AchievementsGroups>();
                gw2AchievementsGroupsList.ForEach(x => 
                {
                    string DataAchievementsGroups = Web.DownloadStringData(UrlApiAchievementsGroups + x).GetAwaiter().GetResult();
                    Serialization.TryFromJson<GW2AchievementsGroups>(DataAchievementsGroups, out GW2AchievementsGroups data);
                    if (data != null)
                    {
                        gw2AchievementsGroups.Add(data);
                    }
                });
                gw2AchievementsGroups.Add(new GW2AchievementsGroups 
                { 
                    id = 0,
                    name = "none",
                    order = 0,
                    icon = "default_icon.png"
                });

                // List owned achievements
                string DataOwned = Web.DownloadStringData(UrlApiOwnedAchievements, PluginDatabase.PluginSettings.Settings.GuildWars2ApiKey).GetAwaiter().GetResult();
                Serialization.TryFromJson<List<GW2OwnedAchievements>>(DataOwned, out List<GW2OwnedAchievements> gw2OwnedAchievements);
               
                if (gw2OwnedAchievements != null)
                {
                    List<IEnumerable<int>> ids = gw2AchievementsList.Batch(20).ToList();

                    // Get achievements details
                    List<GW2Achievements> gw2AchievementsAll = new List<GW2Achievements>();
                    ids.ForEach(x => 
                    {
                        string url = string.Empty;
                        try
                        {
                            string idsString = string.Join(",", x);
                            url = string.Format(UrlApiAchievements, idsString, LocalLang);
                            string Data = Web.DownloadStringData(url).GetAwaiter().GetResult();
                            Serialization.TryFromJson<List<GW2Achievements>>(Data, out List<GW2Achievements> gw2Achievements);
                            if (gw2Achievements != null)
                            {
                                gw2Achievements.ForEach(z => gw2AchievementsAll.Add(z));
                            }
                        }
                        catch (Exception ex)
                        {
                            ShowNotificationPluginWebError(ex, url);
                        }
                    });


                    // Set achievements
                    gw2AchievementsAll.ForEach(x => 
                    {
                        if (x.id == 4202)
                        {

                        }

                        Achievements ach = new Achievements
                        {
                            ApiName = x.id.ToString(),
                            Name = x.name,
                            Description = x.description,
                            DateUnlocked = default(DateTime)
                        };

                        var gwCategory = gw2AchievementsGroups.Find(y => y.achievements?.Contains(x.id) ?? false);
                        if (gwCategory != null)
                        {
                            int CategoryOrder = gwCategory.order;
                            string Category = gwCategory.name;
                            string CategoryIcon = gwCategory.icon;

                            ach.UrlUnlocked = gwCategory.icon;
                            ach.CategoryOrder = CategoryOrder;
                            ach.CategoryIcon = CategoryIcon;
                            ach.Category = Category;
                        }
                        else
                        {
                            gwCategory = gw2AchievementsGroups.Find(y => y.id == 0);

                            int CategoryOrder = gwCategory.order;
                            string Category = gwCategory.name;
                            string CategoryIcon = gwCategory.icon;

                            ach.UrlUnlocked = gwCategory.icon;
                            ach.CategoryOrder = CategoryOrder;
                            ach.CategoryIcon = CategoryIcon;
                            ach.Category = Category;
                        }

                        GW2OwnedAchievements gw2Owned = gw2OwnedAchievements.Find(y => y.id == x.id);
                        if (gw2Owned != null)
                        {
                            if (gw2Owned.done)
                            {
                                ach.DateUnlocked = new DateTime(1982, 12, 15, 0, 0, 0, 0);
                            }
                            else
                            {
                                if (gw2Owned.current != gw2Owned.max)
                                {
                                    ach.Progression = new AchProgression
                                    {
                                        Max = gw2Owned.max,
                                        Value = gw2Owned.current
                                    };
                                }
                            }
                        }

                        AllAchievements.Add(ach);
                    });
                }
                else
                {
                    ShowNotificationPluginNoConfiguration(resources.GetString("LOCSuccessStoryNotificationsGw2BadConfig"));
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            if (AllAchievements.Count > 0)
            {
                AllAchievements = AllAchievements.Where(x => !x.Name.IsNullOrEmpty()).ToList();

                gameAchievements.SourcesLink = new CommonPluginsShared.Models.SourceLink
                {
                    GameName = "Guild Wars 2",
                    Name = "Guild Wars 2",
                    Url = "https://wiki.guildwars2.com/wiki/API:Main"
                };
            }

            gameAchievements.Items = AllAchievements;
            return gameAchievements;
        }


        #region Configuration
        public override bool ValidateConfiguration()
        {
            return !PluginDatabase.PluginSettings.Settings.GuildWars2ApiKey.IsNullOrEmpty();
        }

        public override bool EnabledInSettings()
        {
            return PluginDatabase.PluginSettings.Settings.EnableGuildWars2;
        }
        #endregion
    }
}
