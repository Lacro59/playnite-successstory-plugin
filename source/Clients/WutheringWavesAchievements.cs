using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommonPlayniteShared.Common;
using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using SuccessStory.Models;
using SuccessStory.Models.WutheringWaves;

namespace SuccessStory.Clients
{
    // https://wuwa.mana.wiki/c/trophy-categories
    // https://wutheringwaves.gg/achievements/
    public class WutheringWavesAchievements : GenericAchievements
    {
        #region Urls
        private static string WuWa_UrlSource => "https://github.com/Arikatsu/WutheringWaves_Data";
        private static string WuWa_UrlApi => "https://api.github.com/repos/Arikatsu/WutheringWaves_Data";
        private static string WuWa_UrlRaw => "https://raw.githubusercontent.com/Arikatsu/WutheringWaves_Data/refs/heads/{0}";
        private static string WuWa_UrlAchievements => WuWa_UrlRaw + "/BinData/achievement/achievement.json";
        private static string WuWa_UrlCategory => WuWa_UrlRaw + "/BinData/achievement/achievementcategory.json";
        private static string WuWa_UrlGroup => WuWa_UrlRaw + "/BinData/achievement/achievementgroup.json";
        private static string WuWa_UrlTraduction => WuWa_UrlRaw + "/Textmaps/{1}/multi_text/MultiText.json";
        #endregion


        public WutheringWavesAchievements() : base("Wuthering Waves", CodeLang.GetWuWaLang(API.Instance.ApplicationSettings.Language))
        {

        }


        public override GameAchievements GetAchievements(Game game)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Achievement> AllAchievements = new List<Achievement>();

            try
            {
                string url = WuWa_UrlApi;
                string reponse = Web.DownloadStringData(url).GetAwaiter().GetResult();
                _ = Serialization.TryFromJson(reponse, out dynamic gitHub);
                if (gitHub == null)
                {
                    throw new Exception($"No data with {url}");
                }
                string branch = gitHub["default_branch"];
                DateTime pushed_at = gitHub["pushed_at"];
                string pushedAt = pushed_at.ToString("yyyy-MM-dd_HH-mm-ss");


                string cachePath = Path.Combine(PluginDatabase.Paths.PluginCachePath, "WuWa_" + pushedAt, "WuWaAchievement.json");
                _ = Serialization.TryFromJsonFile(cachePath, out List<WuWaAchievement> wuWaAchievement);
                if (wuWaAchievement == null)
                {
                    url = string.Format(WuWa_UrlAchievements, branch);
                    reponse = Web.DownloadStringData(url).GetAwaiter().GetResult();
                    _ = Serialization.TryFromJson(reponse, out wuWaAchievement);
                    FileSystem.WriteStringToFile(cachePath, reponse);
                }

                cachePath = Path.Combine(PluginDatabase.Paths.PluginCachePath, "WuWa_" + pushedAt, "WuWaCategory.json");
                _ = Serialization.TryFromJsonFile(cachePath, out List<WuWaCategory> wuWaCategory);
                if (wuWaCategory == null)
                {
                    url = string.Format(WuWa_UrlCategory, branch);
                    reponse = Web.DownloadStringData(url).GetAwaiter().GetResult();
                    _ = Serialization.TryFromJson(reponse, out wuWaCategory);
                    FileSystem.WriteStringToFile(cachePath, reponse);
                }

                cachePath = Path.Combine(PluginDatabase.Paths.PluginCachePath, "WuWa_" + pushedAt, "WuWaGroup.json");
                _ = Serialization.TryFromJsonFile(cachePath, out List<WuWaGroup> wuWaGroup);
                if (wuWaGroup == null)
                {
                    url = string.Format(WuWa_UrlGroup, branch);
                    reponse = Web.DownloadStringData(url).GetAwaiter().GetResult();
                    _ = Serialization.TryFromJson(reponse, out wuWaGroup);
                    FileSystem.WriteStringToFile(cachePath, reponse);
                }

                cachePath = Path.Combine(PluginDatabase.Paths.PluginCachePath, "WuWa_" + pushedAt, "WuWaTraduction.json");
                _ = Serialization.TryFromJsonFile(cachePath, out List<WuWaTraduction> wuWaTraduction);
                if (wuWaTraduction == null)
                {
                    url = string.Format(WuWa_UrlTraduction, branch, LocalLang.ToLower());
                    reponse = Web.DownloadStringData(url).GetAwaiter().GetResult();
                    _ = Serialization.TryFromJson(reponse, out wuWaTraduction);
                    if (wuWaTraduction == null)
                    {
                        url = string.Format(WuWa_UrlTraduction, "en");
                        reponse = Web.DownloadStringData(url).GetAwaiter().GetResult();
                        _ = Serialization.TryFromJson(reponse, out wuWaTraduction);
                    }
                    FileSystem.WriteStringToFile(cachePath, reponse);
                }


                if (wuWaAchievement == null || wuWaCategory == null || wuWaGroup == null || wuWaTraduction == null)
                {
                    throw new Exception($"No data with {LocalLang}");
                }

                Dictionary<string, WuWaTraduction> traductionById = wuWaTraduction.ToDictionary(t => t.Id);
                Dictionary<int, WuWaGroup> groupById = wuWaGroup.ToDictionary(g => g.Id);
                Dictionary<string, string> traductionByGroupName = wuWaTraduction
                    .Where(t => t.Id != null)
                    .ToDictionary(t => t.Id, t => t.Content);

                foreach (WuWaAchievement x in wuWaAchievement)
                {
                    _ = groupById.TryGetValue(x.GroupId, out WuWaGroup group);

                    string name = null;
                    if (traductionById.TryGetValue(x.Name, out WuWaTraduction nameTrad))
                    {
                        name = nameTrad.Content;
                    }

                    string desc = null;
                    if (traductionById.TryGetValue(x.Desc, out WuWaTraduction descTrad))
                    {
                        desc = descTrad.Content;
                    }

                    string category = null;
                    if (group != null && !string.IsNullOrEmpty(group.Name))
                    {
                        _ = traductionByGroupName.TryGetValue(group.Name, out category);
                    }

                    int gamerScore = 20;
                    if (x.Level == 1)
                    {
                        gamerScore = 5;
                    }
                    else if (x.Level == 2)
                    {
                        gamerScore = 10;
                    }

                    AllAchievements.Add(new Achievement
                    {
                        ApiName = x.Id.ToString(),
                        Name = name,
                        Description = desc,
                        UrlUnlocked = "WutheringWaves\\ac.png",

                        CategoryOrder = group != null ? group.Id : 0,
                        CategoryIcon = "WutheringWaves\\" + group.Icon.Split('.')[1] + ".png",
                        Category = category,

                        GamerScore = gamerScore,
                        IsHidden = x.Hidden,
                        DateUnlocked = default(DateTime)
                    });
                }


                gameAchievements.IsManual = true;
                gameAchievements.SourcesLink = new CommonPluginsShared.Models.SourceLink
                {
                    GameName = "Wuthering Waves",
                    Name = "GitHub",
                    Url = WuWa_UrlSource
                };
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            if (AllAchievements.Count > 0)
            {
                AllAchievements = AllAchievements.Where(x => !x.Name.IsNullOrEmpty()).ToList();
            }

            gameAchievements.Items = AllAchievements;
            gameAchievements.SetRaretyIndicator();

            return gameAchievements;
        }


        public void ImportAchievements(Game game)
        {
            GlobalProgressOptions options = new GlobalProgressOptions(ResourceProvider.GetString("LOCCommonImporting"))
            {
                Cancelable = true,
                IsIndeterminate = true
            };

            _ = API.Instance.Dialogs.ActivateGlobalProgress((a) =>
            {
                string path = API.Instance.Dialogs.SelectFile("JSON (.json)|*.json");
                if (!string.IsNullOrWhiteSpace(path) && path.EndsWith(".json", StringComparison.InvariantCultureIgnoreCase))
                {
                    bool done = false;
                    GameAchievements gameAchievements = GetAchievements(game);

                    #region https://wuwatracker.com/
                    if (Serialization.TryFromJsonFile(path, out List<int> wuwatracker))
                    {
                        wuwatracker?.ForEach(x =>
                        {
                            Achievement item = gameAchievements.Items.FirstOrDefault(z => z.ApiName.IsEqual(x.ToString()));
                            if (item != null)
                            {
                                item.DateUnlocked = new DateTime(1982, 12, 15, 0, 0, 0, 0);
                            }
                        });
                        done = true;
                    }
                    #endregion

                    if (done)
                    {
                        PluginDatabase.Update(gameAchievements);
                        _ = API.Instance.Dialogs.ShowMessage(ResourceProvider.GetString("LOCImportCompleted"), ResourceProvider.GetString("LOCSuccessStory"));
                    }
                    else
                    {
                        _ = API.Instance.Dialogs.ShowErrorMessage(ResourceProvider.GetString("LOCImportError"), ResourceProvider.GetString("LOCSuccessStory"));
                    }
                }
            }, options);
        }


        #region Configuration
        public override bool ValidateConfiguration()
        {
            return true;
        }

        public override bool EnabledInSettings()
        {
            return PluginDatabase.PluginSettings.Settings.EnableWutheringWaves;
        }
        #endregion
    }
}
