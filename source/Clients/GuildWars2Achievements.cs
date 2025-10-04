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
using Playnite.SDK;
using SuccessStory.Models.GuildWars2;

namespace SuccessStory.Clients
{
    public class GuildWars2Achievements : GenericAchievements
    {
        #region Url
        private static string UrlApi => @"https://api.guildwars2.com/v2";
        private static string UrlApiOwnedAchievements => UrlApi + @"/account/achievements";
        private static string UrlApiAchievementsList => UrlApi + @"/achievements";
        private static string UrlApiAchievements => UrlApiAchievementsList + @"?ids={0}&lang={1}";
        private static string UrlApiAchievementsGroups => UrlApi + @"/achievements/categories/";
        #endregion


        public GuildWars2Achievements() : base("GuildWars2", CodeLang.GetCountryFromLast(API.Instance.ApplicationSettings.Language))
        {

        }


        public override GameAchievements GetAchievements(Game game)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Achievement> AllAchievements = new List<Achievement>();

            try
            {
                // List achievements
                string DataList = Web.DownloadStringData(UrlApiAchievementsList).GetAwaiter().GetResult();
                _ = Serialization.TryFromJson(DataList, out List<int> gw2AchievementsList);

                if (gw2AchievementsList == null)
                {
                    ShowNotificationPluginWebError(new Exception("No data"), UrlApiAchievementsList);
                    gameAchievements.Items = AllAchievements;
                    return gameAchievements;
                }

                // List groups of achievements
                string DataGroups = Web.DownloadStringData(UrlApiAchievementsGroups).GetAwaiter().GetResult();
                _ = Serialization.TryFromJson(DataGroups, out List<int> gw2AchievementsGroupsList);

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
                    _ = Serialization.TryFromJson(DataAchievementsGroups, out GW2AchievementsGroups data);
                    if (data != null)
                    {
                        gw2AchievementsGroups.Add(data);
                    }
                });
                gw2AchievementsGroups.Add(new GW2AchievementsGroups
                {
                    Id = 0,
                    Name = "none",
                    Order = 0,
                    Icon = "default_icon.png"
                });

                // List owned achievements
                string DataOwned = Web.DownloadStringData(UrlApiOwnedAchievements, PluginDatabase.PluginSettings.Settings.GuildWars2ApiKey).GetAwaiter().GetResult();
                _ = Serialization.TryFromJson(DataOwned, out List<GW2OwnedAchievements> gw2OwnedAchievements);

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
                            _ = Serialization.TryFromJson(Data, out List<GW2Achievements> gw2Achievements);
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
                        Achievement ach = new Achievement
                        {
                            ApiName = x.Id.ToString(),
                            Name = x.Name,
                            Description = x.Description,
                            DateUnlocked = default
                        };

                        GW2AchievementsGroups gwCategory = gw2AchievementsGroups.Find(y => y.Achievements?.Contains(x.Id) ?? false);
                        if (gwCategory != null)
                        {
                            int CategoryOrder = gwCategory.Order;
                            string Category = gwCategory.Name;
                            string CategoryIcon = gwCategory.Icon;

                            ach.UrlUnlocked = gwCategory.Icon;
                            ach.CategoryOrder = CategoryOrder;
                            ach.CategoryIcon = CategoryIcon;
                            ach.Category = Category;
                        }
                        else
                        {
                            gwCategory = gw2AchievementsGroups.Find(y => y.Id == 0);

                            int CategoryOrder = gwCategory.Order;
                            string Category = gwCategory.Name;
                            string CategoryIcon = gwCategory.Icon;

                            ach.UrlUnlocked = gwCategory.Icon;
                            ach.CategoryOrder = CategoryOrder;
                            ach.CategoryIcon = CategoryIcon;
                            ach.Category = Category;
                        }

                        GW2OwnedAchievements gw2Owned = gw2OwnedAchievements.Find(y => y.Id == x.Id);
                        if (gw2Owned != null)
                        {
                            if (gw2Owned.Done)
                            {
                                ach.DateUnlocked = new DateTime(1982, 12, 15, 0, 0, 0, 0);
                                ach.GamerScore = x.Tiers.FirstOrDefault(y => y.Count <= gw2Owned.Current).Points;

                                if (gw2Owned.Current < x.Tiers.LastOrDefault().Count)
                                {
                                    ach.Progression = new AchProgression
                                    {
                                        Max = x.Tiers.LastOrDefault().Count,
                                        Value = gw2Owned.Current
                                    };
                                }
                            }
                            else
                            {
                                ach.GamerScore = x.Tiers.LastOrDefault().Points;
                                if (gw2Owned.Current != x.Tiers.LastOrDefault().Count)
                                {
                                    ach.Progression = new AchProgression
                                    {
                                        Max = x.Tiers.LastOrDefault().Count,
                                        Value = gw2Owned.Current
                                    };
                                }
                            }
                        }

                        AllAchievements.Add(ach);
                    });
                }
                else
                {
                    ShowNotificationPluginNoConfiguration();
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
            gameAchievements.SetRaretyIndicator();

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
