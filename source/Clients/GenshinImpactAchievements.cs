using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommonPlayniteShared.Common;
using CommonPluginsShared;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using SuccessStory.Models;
using SuccessStory.Models.GenshinImpact;

namespace SuccessStory.Clients
{
    public class GenshinImpactAchievements : GenericAchievements
    {
        #region Urls
        private static string Url => @"https://raw.githubusercontent.com/Sycamore0/GenshinData/main";
        private static string UrlSource => @"https://github.com/Sycamore0/GenshinData";

        private static string UrlTextMap => Url + @"/TextMap/TextMap{0}.json";
        private static string UrlAchievementsCategory => Url + @"/ExcelBinOutput/AchievementGoalExcelConfigData.json";
        private static string UrlAchievements => Url + @"/ExcelBinOutput/AchievementExcelConfigData.json";

        private static string PaimonMoe_Url => "https://raw.githubusercontent.com/MadeBaruna/paimon-moe/main";
        private static string PaimonMoe_UrlApi => "https://api.github.com/repos/MadeBaruna/paimon-moe";
        private static string PaimonMoe_UrlSource => "https://github.com//MadeBaruna/paimon-moe";
        private static string PaimonMoe_UrlAchievements => PaimonMoe_Url + "/src/data/achievement/{0}.json";
        #endregion


        public GenshinImpactAchievements() : base("Genshin Impact", CodeLang.GetGenshinLang(API.Instance.ApplicationSettings.Language))
        {

        }


        public override GameAchievements GetAchievements(Game game)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Achievement> AllAchievements = new List<Achievement>();

            try
            {
                string url = PaimonMoe_UrlApi;
                string reponse = Web.DownloadStringData(url).GetAwaiter().GetResult();
                _ = Serialization.TryFromJson(reponse, out dynamic gitHub);
                if (gitHub == null)
                {
                    throw new Exception($"No data with {url}");
                }
                string branch = gitHub["default_branch"];
                DateTime pushed_at = gitHub["pushed_at"];
                string pushedAt = pushed_at.ToString("yyyy-MM-dd_HH-mm-ss");


                string cachePath = Path.Combine(PluginDatabase.Paths.PluginCachePath, "Genshin_" + pushedAt, "TextMap.json");
                _ = Serialization.TryFromJsonFile(cachePath, out dynamic textMap);
                if (textMap == null)
                {
                    url = string.Format(PaimonMoe_UrlAchievements, LocalLang.ToLower());
                    string textMapString = Web.DownloadStringData(url).GetAwaiter().GetResult();
                    _ = Serialization.TryFromJson(textMapString, out textMap);
                    if (textMap == null)
                    {
                        url = string.Format(PaimonMoe_UrlAchievements, "en");
                        textMapString = Web.DownloadStringData(url).GetAwaiter().GetResult();
                        _ = Serialization.TryFromJson(textMapString, out textMap);
                        if (textMap == null)
                        {
                            throw new Exception($"No data from {url}");
                        }
                    }
                    FileSystem.WriteStringToFile(cachePath, textMapString);
                }


                for (int i = 0; i < 80; i++)
                {
                    string map = Serialization.ToJson(textMap[i.ToString()]);
                    if (Serialization.TryFromJson(map, out Data data) && data != null)
                    {
                        int categoryOrder = data.Order;
                        string category = data.Name;
                        string categoryIcon = string.Format("GenshinImpact\\{0}.png", categoryOrder - 1);


                        string ach = Serialization.ToJson(data.Achievements);
                        ach = "[" + ach.Replace("[{", "{").Replace("}]", "}") + "]";
                        ach = ach.Replace("[[", "[").Replace("]]", "]");
                        if (Serialization.TryFromJson(ach, out List<GenshinAchievement> genshinAchievements))
                        {
                            genshinAchievements.ForEach(x =>
                            {
                                AllAchievements.Add(new Achievement
                                {
                                    ApiName = x.Id.ToString(),
                                    Name = x.Name,
                                    Description = x.Desc,
                                    UrlUnlocked = "GenshinImpact\\ac.png",

                                    CategoryOrder = categoryOrder,
                                    CategoryIcon = categoryIcon,
                                    Category = category,

                                    GamerScore = x.Reward,

                                    DateUnlocked = default(DateTime)
                                });
                            });
                        }
                    }
                }

                gameAchievements.IsManual = true;
                gameAchievements.SourcesLink = new CommonPluginsShared.Models.SourceLink
                {
                    GameName = "Genshin Impact",
                    Name = "GitHub",
                    Url = PaimonMoe_UrlSource
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
                IsIndeterminate = false
            };

            _ = API.Instance.Dialogs.ActivateGlobalProgress((a) =>
            {
                string path = API.Instance.Dialogs.SelectFile("JSON (.json)|*.json");
                if (!string.IsNullOrWhiteSpace(path) && path.EndsWith(".json", StringComparison.InvariantCultureIgnoreCase))
                {
                    bool done = false;
                    GameAchievements gameAchievements = GetAchievements(game);

                    if (Serialization.TryFromJsonFile(path, out PaimonMoeLocalData paimonMoeLocalData))
                    {
                        if (paimonMoeLocalData?.Achievement != null)
                        {
                            paimonMoeLocalData.Achievement.ForEach(x =>
                            {
                                x.Value.ForEach(y =>
                                {
                                    Achievement item = gameAchievements.Items.FirstOrDefault(z => z.ApiName == y.Key && y.Value);
                                    if (item != null)
                                    {
                                        item.DateUnlocked = new DateTime(1982, 12, 15, 0, 0, 0, 0);
                                    }
                                });
                            });
                            done = true;
                        }
                    }

                    if (Serialization.TryFromJsonFile(path, out SeelieMeLocalData seelieMeLocalData))
                    {
                        if (seelieMeLocalData?.Achievements != null)
                        {
                            seelieMeLocalData.Achievements.ForEach(x =>
                            {
                                if (x.Value.Done)
                                {
                                    Achievement item = gameAchievements.Items.FirstOrDefault(z => z.ApiName == x.Key);
                                    if (item != null)
                                    {
                                        item.DateUnlocked = new DateTime(1982, 12, 15, 0, 0, 0, 0);
                                    }
                                }
                            });
                            done = true;
                        }
                    }

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
            return PluginDatabase.PluginSettings.Settings.EnableGenshinImpact;
        }
        #endregion
    }
}
