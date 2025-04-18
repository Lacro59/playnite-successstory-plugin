using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using CommonPlayniteShared.Common;
using CommonPluginsShared;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using SuccessStory.Models;
using SuccessStory.Models.GenshinImpact;
using SuccessStory.Models.ZenlessZoneZero;

namespace SuccessStory.Clients
{
    public class ZenlessZoneZeroAchievements : GenericAchievements
    {
        #region Urls
        private static string UrlSource => @"https://zzz.seelie.me";


        #endregion


        public ZenlessZoneZeroAchievements() : base("Zenless Zone Zero", CodeLang.GetGenshinLang(API.Instance.ApplicationSettings.Language))
        {

        }


        public override GameAchievements GetAchievements(Game game)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Achievement> AllAchievements = new List<Achievement>();

            try
            {
                string response = Web.DownloadStringData(UrlSource).GetAwaiter().GetResult();
                string pattern = @"src=""(/assets/index-[a-z0-9]+.js)""></script>";

                string indexFile = string.Empty;
                Match match = Regex.Match(response, pattern);
                indexFile = match.Success ? match.Groups[1].Value : throw new Exception("No index.js found");

                string id = indexFile.Replace("/assets/", string.Empty).Replace(".js", string.Empty);
                string cachePath = Path.Combine(PluginDatabase.Paths.PluginCachePath, "ZenlessZoneZero_" + id);
                if (!Directory.Exists(cachePath))
                {
                    response = Web.DownloadStringData(UrlSource + indexFile).GetAwaiter().GetResult();
                    pattern = @"/locale\/achievements-[a-z0-9-]+\.js";

                    MatchCollection matches = Regex.Matches(response, pattern);
                    foreach (Match matchAch in matches)
                    {
                        string result = matchAch.Groups[0].Value;
                        response = Web.DownloadStringData(UrlSource + "/assets" + result).GetAwaiter().GetResult();
                        response = Regex.Replace(response, @"const [a-z]=", string.Empty);
                        response = Regex.Replace(response, @";export{[a-z] as default};", string.Empty);
                        string json = FixToValidJson(response);
                        string file = Regex.Replace(Path.GetFileNameWithoutExtension(result), @"(?<=achievements-[a-z0-9]+)-[a-z0-9]+", string.Empty);
                        FileSystem.WriteStringToFile(Path.Combine(cachePath, file + ".json"), json.Replace("3001e3", "3001000"));
                    }
                }

                string filePath = Path.Combine(cachePath, $"achievements-{LocalLang.ToLower()}.json");
                if (Serialization.TryFromJsonFile(filePath, out Dictionary<string, SeelieMeData> seelieMeData, out Exception ex))
                {
                    seelieMeData.ForEach(x =>
                    {
                        x.Value.A.ForEach(a =>
                        {
                            AllAchievements.Add(new Achievement
                            {
                                ApiName = a.Id.ToString(),
                                Name = a.N,
                                Description = a.D,
                                UrlUnlocked = "ZenlessZoneZero\\ac.png",

                                CategoryOrder = x.Value.O,
                                CategoryIcon = string.Empty,
                                Category = x.Value.N,

                                GamerScore = a.R,

                                DateUnlocked = default(DateTime)
                            });
                        });
                    });
                }

                gameAchievements.Items = AllAchievements;
                gameAchievements.IsManual = true;
                gameAchievements.SourcesLink = new CommonPluginsShared.Models.SourceLink
                {
                    GameName = "Zenless Zone Zero",
                    Name = "SEELIE.me",
                    Url = UrlSource
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

                    #region https://seelie.me
                    if (Serialization.TryFromJsonFile(path, out SeelieMeExport seelieMeExport))
                    {
                        if (seelieMeExport?.Achievements != null)
                        {
                            seelieMeExport.Achievements.ForEach(x =>
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
                    #endregion

                    #region https://zzz.rng.moe/
                    if (Serialization.TryFromJsonFile(path, out RngMoeExport rngMoeExport))
                    {
                        if (rngMoeExport?.Data?.Profiles != null)
                        {
                            ProfileData profileData = rngMoeExport.Data.Profiles.First().Value;
                            profileData.Stores._2.Enabled?.ForEach(x =>
                            {
                                if (x.Value)
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
                    #endregion

                    #region https://stardb.gg/
                    if (Serialization.TryFromJsonFile(path, out StartDbExport startDbExport))
                    {
                        if (startDbExport?.User?.Zzz != null)
                        {
                            startDbExport.User.Zzz.Achievements?.ForEach(x =>
                            {
                                Achievement item = gameAchievements.Items.FirstOrDefault(z => z.ApiName == x.ToString());
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
            return PluginDatabase.PluginSettings.Settings.EnableGenshinImpact;
        }
        #endregion


        /// <summary>
        /// Converts a JS-style object string to valid JSON:
        /// - Adds quotes to unquoted property names
        /// - Replaces single-quoted string values with double quotes
        /// - Leaves internal apostrophes untouched
        /// </summary>
        public static string FixToValidJson(string jsObject)
        {
            if (string.IsNullOrWhiteSpace(jsObject))
                return "{}";

            // 1. Quote unquoted object keys (e.g. id: → "id":)
            jsObject = Regex.Replace(jsObject, @"(?<={|,)\s*(\w+)\s*:", "\"$1\":");

            // 2. Quote numeric keys (e.g. 3001: → "3001":)
            jsObject = Regex.Replace(jsObject, @"({|,)\s*(\d+)\s*:", "$1\"$2\":");

            // 3. Replace only full single-quoted string values with double quotes
            // This avoids modifying apostrophes inside the string.
            jsObject = Regex.Replace(jsObject, @"(?<=[\[\{:,]\s*)'(.*?)'(?=\s*[,}\]])", m =>
            {
                string content = m.Groups[1].Value.Replace("\"", "\\\""); // escape internal double-quotes
                return $"\"{content}\"";
            });

            // 4. Optionally: handle backtick-wrapped strings as well (optional)
            jsObject = Regex.Replace(jsObject, @"`([^`]*)`", m =>
            {
                var content = m.Groups[1].Value.Replace("\"", "\\\"");
                return $"\"{content}\"";
            });

            return jsObject;
        }
    }
}
