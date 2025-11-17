using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CommonPlayniteShared.Common;
using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using SuccessStory.Models;
using SuccessStory.Models.GenshinImpact;
using SuccessStory.Models.HonkaiStarRail;
using SuccessStory.Services;

namespace SuccessStory.Clients
{
    public class HonkaiStarRailAchievements : GenericAchievements
    {
        #region Urls

        private static string TurnBasedGameData_Url => "https://raw.githubusercontent.com/DimbreathBot/TurnBasedGameData/refs/heads/main";
        private static string TurnBasedGameData_UrlApi => "https://api.github.com/repos/DimbreathBot/TurnBasedGameData";
        private static string TurnBasedGameData_UrlSource => "https://github.com/DimbreathBot/TurnBasedGameData";
        private static string TurnBasedGameData_UrlTextMap => TurnBasedGameData_Url + "/TextMap/TextMap{0}.json";
        private static string TurnBasedGameData_UrlAchievementData => TurnBasedGameData_Url + "/ExcelOutput/AchievementData.json";
        private static string TurnBasedGameData_UrlAchievementSeries => TurnBasedGameData_Url + "/ExcelOutput/AchievementSeries.json";
        
        #endregion


        public HonkaiStarRailAchievements() : base("Honkai: Star Rail", CodeLang.GetGenshinLang(API.Instance.ApplicationSettings.Language))
        {

        }


        public override GameAchievements GetAchievements(Game game)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Models.Achievement> AllAchievements = new List<Models.Achievement>();

            try
            {
                string url = TurnBasedGameData_UrlApi;
                string reponse = Web.DownloadStringData(url).GetAwaiter().GetResult();
                _ = Serialization.TryFromJson(reponse, out dynamic gitHub);
                if (gitHub == null)
                {
                    throw new Exception($"No data with {url}");
                }
                string branch = gitHub["default_branch"];
                DateTime pushed_at = gitHub["pushed_at"];
                string pushedAt = pushed_at.ToString("yyyy-MM-dd_HH-mm-ss");


                string cachePath = Path.Combine(PluginDatabase.Paths.PluginCachePath, "HonkaiStarRail_" + pushedAt, "TextMap.json");
                _ = Serialization.TryFromJsonFile(cachePath, out dynamic textMap);
                _ = Serialization.TryFromJsonFile(cachePath.Replace("TextMap.json", "TextMapEN.json"), out dynamic textMapEn);
                if (textMap == null)
                {
                    url = string.Format(TurnBasedGameData_UrlTextMap, LocalLang.ToUpper());
                    string textMapString = Web.DownloadStringData(url).GetAwaiter().GetResult();
                    _ = Serialization.TryFromJson(textMapString, out textMap);

                    if (!LocalLang.IsEqual("en"))
                    {
                        url = string.Format(TurnBasedGameData_UrlTextMap, "EN");
                        string textMapEnString = Web.DownloadStringData(url).GetAwaiter().GetResult();
                        _ = Serialization.TryFromJson(textMapString, out textMapEn);
                        FileSystem.WriteStringToFile(cachePath.Replace("TextMap.json", "TextMapEN.json"), textMapEnString);
                    }
                    else
                    {
                        textMapEn = textMap;
                        FileSystem.WriteStringToFile(cachePath.Replace("TextMap.json", "TextMapEN.json"), textMapString);
                    }

                    if (textMap == null)
                    {
                        textMap = textMapEn;
                    }

                    FileSystem.WriteStringToFile(cachePath, textMapString);
                }

                cachePath = Path.Combine(PluginDatabase.Paths.PluginCachePath, "HonkaiStarRail_" + pushedAt, "AchievementData.json");
                _ = Serialization.TryFromJsonFile(cachePath, out List<AchievementData> achievementData);
                if (achievementData == null)
                {
                    url = string.Format(TurnBasedGameData_UrlAchievementData, branch);
                    reponse = Web.DownloadStringData(url).GetAwaiter().GetResult();
                    _ = Serialization.TryFromJson(reponse, out achievementData);
                    FileSystem.WriteStringToFile(cachePath, reponse);
                }

                cachePath = Path.Combine(PluginDatabase.Paths.PluginCachePath, "HonkaiStarRail_" + pushedAt, "AchievementSeries.json");
                _ = Serialization.TryFromJsonFile(cachePath, out List<AchievementSeries> achievementSeries);
                if (achievementSeries == null)
                {
                    url = string.Format(TurnBasedGameData_UrlAchievementSeries, branch);
                    reponse = Web.DownloadStringData(url).GetAwaiter().GetResult();
                    _ = Serialization.TryFromJson(reponse, out achievementSeries);
                    FileSystem.WriteStringToFile(cachePath, reponse);
                }


                achievementData.ForEach(x =>
                {
                    ParamList param = null;
                    if (x.ParamList?.Count() > 0)
                    {
                        _ = Serialization.TryFromJson(Serialization.ToJson(x.ParamList[0]), out param);
                    }
                    string description = ((string)textMap[x.AchievementDesc.Hash]).Replace("<unbreak>#1[i]</unbreak>", param?.Value.ToString())
                        .Replace("\\n", Environment.NewLine);
                    description = Regex.Replace(description, @"</?unbreak[^>]*>", string.Empty);
                    description = Regex.Replace(description, @"</?color[^>]*>", string.Empty);

                    string icon = Path.GetFileName(achievementSeries.FirstOrDefault(y => y.SeriesID == x.SeriesID).MainIconPath);

                    AllAchievements.Add(new Models.Achievement
                    {
                        ApiName = x.AchievementID.ToString(),
                        Name = (string)textMap[x.AchievementTitle.Hash],
                        NameEn = (string)textMapEn[x.AchievementTitle.Hash],
                        Description = description,
                        UrlUnlocked = "HonkaiStarRail\\" + icon,

                        CategoryOrder = x.SeriesID,
                        CategoryIcon = "HonkaiStarRail\\" + icon,
                        Category = textMap[achievementSeries.FirstOrDefault(y => y.SeriesID == x.SeriesID).SeriesTitle.Hash],

                        GamerScore = x.Rarity.IsEqual("Low") ? 5 : x.Rarity.IsEqual("Mid") ? 10 : 20,

                        DateUnlocked = default(DateTime)
                    });
                });

                gameAchievements.IsManual = true;
                gameAchievements.SourcesLink = new CommonPluginsShared.Models.SourceLink
                {
                    GameName = "Honkai: Star Rail",
                    Name = "GitHub",
                    Url = TurnBasedGameData_UrlSource
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
                string path = API.Instance.Dialogs.SelectFile("(*.dat, *.json)|*.dat;*.json");
                if (!string.IsNullOrWhiteSpace(path) && path.EndsWith(".dat", StringComparison.InvariantCultureIgnoreCase))
                {
                    bool done = false;
                    GameAchievements gameAchievements = GetAchievements(game);

                    string content = File.ReadAllText(path);

                    #region https://starrailstation.com/

                    if (content.StartsWith("srs"))
                    {
                        string compressed = content.Substring(3);
                        string json = LZString.DecompressFromUTF16(compressed);

                        if (Serialization.TryFromJson(json, out StairRailStationExport stairRailStationExport))
                        {
                            if (stairRailStationExport?.Data?.Stores?._1Achieve?.CompleteState != null)
                            {
                                string dataPath = Path.Combine(PluginDatabase.Paths.PluginPath, "Data", "starrailstation.json");
                                _ = Serialization.TryFromJsonFile(dataPath, out StairRailStationData stairRailStationData);

                                if (stairRailStationExport.Data.Stores._1Achieve.CompleteState._29325 != null)
                                {
                                    MatchCollection matches = Regex.Matches(stairRailStationExport.Data.Stores._1Achieve.CompleteState._29325.ToString(), @"\d+");
                                    foreach (Match match in matches)
                                    {
                                        Models.HonkaiStarRail.Achievement srData = stairRailStationData.Achievements.FirstOrDefault(x => x.Id == int.Parse(match.Value));
                                        if (srData != null)
                                        {
                                            Models.Achievement item = gameAchievements.Items.FirstOrDefault(z => srData.Title.IsEqual(z.NameEn));
                                            if (item != null)
                                            {
                                                item.DateUnlocked = new DateTime(1982, 12, 15, 0, 0, 0, 0);
                                            }
                                        }
                                    }
                                }

                                if (stairRailStationExport.Data.Stores._1Achieve.CompleteState._29327 != null)
                                {
                                    MatchCollection matches = Regex.Matches(stairRailStationExport.Data.Stores._1Achieve.CompleteState._29327.ToString(), @"\d+");
                                    foreach (Match match in matches)
                                    {
                                        Models.HonkaiStarRail.Achievement srData = stairRailStationData.Achievements.FirstOrDefault(x => x.Id == int.Parse(match.Value));
                                        if (srData != null)
                                        {
                                            Models.Achievement item = gameAchievements.Items.FirstOrDefault(z => srData.Title.IsEqual(z.NameEn));
                                            if (item != null)
                                            {
                                                item.DateUnlocked = new DateTime(1982, 12, 15, 0, 0, 0, 0);
                                            }
                                        }
                                    }
                                }

                                if (stairRailStationExport.Data.Stores._1Achieve.CompleteState._29323 != null)
                                {
                                    MatchCollection matches = Regex.Matches(stairRailStationExport.Data.Stores._1Achieve.CompleteState._29323.ToString(), @"\d+");
                                    foreach (Match match in matches)
                                    {
                                        Models.HonkaiStarRail.Achievement srData = stairRailStationData.Achievements.FirstOrDefault(x => x.Id == int.Parse(match.Value));
                                        if (srData != null)
                                        {
                                            Models.Achievement item = gameAchievements.Items.FirstOrDefault(z => srData.Title.IsEqual(z.NameEn));
                                            if (item != null)
                                            {
                                                item.DateUnlocked = new DateTime(1982, 12, 15, 0, 0, 0, 0);
                                            }
                                        }
                                    }
                                }

                                if (stairRailStationExport.Data.Stores._1Achieve.CompleteState._29328 != null)
                                {
                                    MatchCollection matches = Regex.Matches(stairRailStationExport.Data.Stores._1Achieve.CompleteState._29328.ToString(), @"\d+");
                                    foreach (Match match in matches)
                                    {
                                        Models.HonkaiStarRail.Achievement srData = stairRailStationData.Achievements.FirstOrDefault(x => x.Id == int.Parse(match.Value));
                                        if (srData != null)
                                        {
                                            Models.Achievement item = gameAchievements.Items.FirstOrDefault(z => srData.Title.IsEqual(z.NameEn));
                                            if (item != null)
                                            {
                                                item.DateUnlocked = new DateTime(1982, 12, 15, 0, 0, 0, 0);
                                            }
                                        }
                                    }
                                }

                                if (stairRailStationExport.Data.Stores._1Achieve.CompleteState._29333 != null)
                                {
                                    MatchCollection matches = Regex.Matches(stairRailStationExport.Data.Stores._1Achieve.CompleteState._29333.ToString(), @"\d+");
                                    foreach (Match match in matches)
                                    {
                                        Models.HonkaiStarRail.Achievement srData = stairRailStationData.Achievements.FirstOrDefault(x => x.Id == int.Parse(match.Value));
                                        if (srData != null)
                                        {
                                            Models.Achievement item = gameAchievements.Items.FirstOrDefault(z => srData.Title.IsEqual(z.NameEn));
                                            if (item != null)
                                            {
                                                item.DateUnlocked = new DateTime(1982, 12, 15, 0, 0, 0, 0);
                                            }
                                        }
                                    }
                                }

                                if (stairRailStationExport.Data.Stores._1Achieve.CompleteState._29324 != null)
                                {
                                    MatchCollection matches = Regex.Matches(stairRailStationExport.Data.Stores._1Achieve.CompleteState._29324.ToString(), @"\d+");
                                    foreach (Match match in matches)
                                    {
                                        Models.HonkaiStarRail.Achievement srData = stairRailStationData.Achievements.FirstOrDefault(x => x.Id == int.Parse(match.Value));
                                        if (srData != null)
                                        {
                                            Models.Achievement item = gameAchievements.Items.FirstOrDefault(z => srData.Title.IsEqual(z.NameEn));
                                            if (item != null)
                                            {
                                                item.DateUnlocked = new DateTime(1982, 12, 15, 0, 0, 0, 0);
                                            }
                                        }
                                    }
                                }

                                if (stairRailStationExport.Data.Stores._1Achieve.CompleteState._29334 != null)
                                {
                                    MatchCollection matches = Regex.Matches(stairRailStationExport.Data.Stores._1Achieve.CompleteState._29334.ToString(), @"\d+");
                                    foreach (Match match in matches)
                                    {
                                        Models.HonkaiStarRail.Achievement srData = stairRailStationData.Achievements.FirstOrDefault(x => x.Id == int.Parse(match.Value));
                                        if (srData != null)
                                        {
                                            Models.Achievement item = gameAchievements.Items.FirstOrDefault(z => srData.Title.IsEqual(z.NameEn));
                                            if (item != null)
                                            {
                                                item.DateUnlocked = new DateTime(1982, 12, 15, 0, 0, 0, 0);
                                            }
                                        }
                                    }
                                }

                                if (stairRailStationExport.Data.Stores._1Achieve.CompleteState._29321 != null)
                                {
                                    MatchCollection matches = Regex.Matches(stairRailStationExport.Data.Stores._1Achieve.CompleteState._29321.ToString(), @"\d+");
                                    foreach (Match match in matches)
                                    {
                                        Models.HonkaiStarRail.Achievement srData = stairRailStationData.Achievements.FirstOrDefault(x => x.Id == int.Parse(match.Value));
                                        if (srData != null)
                                        {
                                            Models.Achievement item = gameAchievements.Items.FirstOrDefault(z => srData.Title.IsEqual(z.NameEn));
                                            if (item != null)
                                            {
                                                item.DateUnlocked = new DateTime(1982, 12, 15, 0, 0, 0, 0);
                                            }
                                        }
                                    }
                                }

                                if (stairRailStationExport.Data.Stores._1Achieve.CompleteState._29322 != null)
                                {
                                    MatchCollection matches = Regex.Matches(stairRailStationExport.Data.Stores._1Achieve.CompleteState._29322.ToString(), @"\d+");
                                    foreach (Match match in matches)
                                    {
                                        Models.HonkaiStarRail.Achievement srData = stairRailStationData.Achievements.FirstOrDefault(x => x.Id == int.Parse(match.Value));
                                        if (srData != null)
                                        {
                                            Models.Achievement item = gameAchievements.Items.FirstOrDefault(z => srData.Title.IsEqual(z.NameEn));
                                            if (item != null)
                                            {
                                                item.DateUnlocked = new DateTime(1982, 12, 15, 0, 0, 0, 0);
                                            }
                                        }
                                    }
                                }

                                done = true;
                            }
                        }
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

                if (!string.IsNullOrWhiteSpace(path) && path.EndsWith(".json", StringComparison.InvariantCultureIgnoreCase))
                {
                    bool done = false;
                    GameAchievements gameAchievements = GetAchievements(game);

                    #region https://seelie.me

                    if (Serialization.TryFromJsonFile(path, out SeelieMeExport seelieMeExport))
                    {
                        if (seelieMeExport?.Achievements != null)
                        {
                            seelieMeExport.Achievements.ForEach(x =>
                            {
                                if (x.Value.Done)
                                {
                                    Models.Achievement item = gameAchievements.Items.FirstOrDefault(z => z.ApiName == x.Key);
                                    if (item != null)
                                    {
                                        item.DateUnlocked = new DateTime(1982, 12, 15, 0, 0, 0, 0);
                                    }
                                }
                            });
                            done = true;
                        }
                    }

                    #endregion

                    #region https://stardb.gg/

                    if (Serialization.TryFromJsonFile(path, out StartDbExport startDbExport))
                    {
                        if (startDbExport?.User?.Hsr != null)
                        {
                            startDbExport.User.Hsr.Achievements?.ForEach(x =>
                            {
                                Models.Achievement item = gameAchievements.Items.FirstOrDefault(z => z.ApiName == x.ToString());
                                if (item != null)
                                {
                                    item.DateUnlocked = new DateTime(1982, 12, 15, 0, 0, 0, 0);
                                }
                            });
                            done = true;
                        }
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
            return PluginDatabase.PluginSettings.Settings.EnableHonkaiStarRail;
        }

        #endregion
    }
}