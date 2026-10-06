using CommonPlayniteShared.Common;
using CommonPlayniteShared.PluginLibrary.PSNLibrary.Services;
using CommonPluginsShared;
using CommonPluginsShared.Models;
using CommonPluginsStores;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using SuccessStory.Models;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using CommonPluginsShared.Extensions;
using CommonPlayniteShared.PluginLibrary.PSNLibrary.Models;
using static CommonPluginsShared.PlayniteTools;
using Playnite.SDK;
using SuccessStory.Models.PSN;

namespace SuccessStory.Clients
{
    // https://andshrew.github.io/PlayStation-Trophies/#/APIv2
    public class PSNAchievements : GenericAchievements
    {
        protected static PsnAllTrophies psnAllTrophies;
        internal static PsnAllTrophies PsnAllTrophies
        {
            get
            {
                if (psnAllTrophies == null)
                {
                    psnAllTrophies = GetAllTrophies();
                }
                return psnAllTrophies;
            }

            set => psnAllTrophies = value;
        }

        protected static PSNClient psnAPI;
        internal static PSNClient PsnAPI
        {
            get
            {
                if (psnAPI == null)
                {
                    psnAPI = new PSNClient(PsnDataPath);
                }
                return psnAPI;
            }

            set => psnAPI = value;
        }

        private static string PsnDataPath { get; set; }

        public string CommunicationId { get; set; }
        

        private static string UrlBase => @"https://m.np.playstation.com/api/trophy/v1";
        private static string UrlTrophyGroups => UrlBase + @"/npCommunicationIds/{0}/trophyGroups";
        private static string UrlTrophiesDetails => UrlBase + @"/npCommunicationIds/{0}/trophyGroups/all/trophies";
        private static string UrlTrophies => UrlBase + @"/users/me/npCommunicationIds/{0}/trophyGroups/all/trophies";
        private static string UrlAllTrophies => UrlBase + @"/users/me/trophyTitles";
        private static string TrophiesWithIdsMobileUrl => UrlBase + @"/users/me/titles/trophyTitles?npTitleIds={0}";
        private static string UrlAllTrophyTitles => UrlBase + @"/users/me/trophyTitles";


        public PSNAchievements() : base("PSN", CodeLang.GetEpicLang(API.Instance.ApplicationSettings.Language))
        {
            PsnDataPath = PluginDatabase.Paths.PluginUserDataPath + "\\..\\" + GetPluginId(ExternalPlugin.PSNLibrary);
        }


        public override GameAchievements GetAchievements(Game game)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Achievement> allAchievements = new List<Achievement>();

            string url = string.Empty;
            string urlDetails = string.Empty;


            if (IsConnected())
            {
                try
                {
                    PsnAPI.CheckAuthentication().GetAwaiter().GetResult();

                    // TODO Old plugin, still useful?
                    string[] split = game.GameId.Split('#');
                    string gameId = split.Count() < 3 ? game.GameId : game.GameId.Split('#')[2];

                    bool isPS5 = game.Platforms?.Where(x => x.Name.Contains("5")).Count() > 0;

                    if (!CommunicationId.IsNullOrEmpty())
                    {
                        gameId = CommunicationId;
                    }

                    if (!gameId.Contains("NPWR", StringComparison.InvariantCultureIgnoreCase))
                    {
                        try
                        {
                            string urlTrophiesMobile = string.Format(TrophiesWithIdsMobileUrl, gameId);
                            string webTrophiesMobileResult = Web.DownloadStringData(urlTrophiesMobile, PsnAPI.mobileToken.access_token).GetAwaiter().GetResult();
                            TrophyTitlesWithIdsMobile titles_part = Serialization.FromJson<TrophyTitlesWithIdsMobile>(webTrophiesMobileResult);

                            string tmp_GameId = titles_part?.titles?.FirstOrDefault()?.trophyTitles?.FirstOrDefault()?.npCommunicationId;
                            if (!tmp_GameId.IsNullOrEmpty())
                            {
                                gameId = tmp_GameId;
                            }
                            else
                            {
                                tmp_GameId = GetNPWR_2(game.Name);
                                if (!tmp_GameId.IsNullOrEmpty())
                                {
                                    gameId = tmp_GameId;
                                }
                                else
                                {
                                    tmp_GameId = GetNPWR(game.Name);
                                    if (!tmp_GameId.IsNullOrEmpty())
                                    {
                                        gameId = tmp_GameId;
                                    }
                                    else
                                    {
                                        Logger.Warn($"No trophies found for {game.Name} - {gameId}");
                                        gameAchievements.Items = allAchievements;
                                        return gameAchievements;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false, $"Error on PSNAchievements with {gameId}", true, PluginDatabase.PluginName);
                        }
                    }

                    url = string.Format(UrlTrophies, gameId) + (isPS5 ? string.Empty : "?npServiceName=trophy");
                    urlDetails = string.Format(UrlTrophiesDetails, gameId) + (isPS5 ? string.Empty : "?npServiceName=trophy");

                    string response = string.Empty;
                    TrophiesResponse trophies = new TrophiesResponse();
                    try
                    {
                        response = Web.DownloadStringData(url, PsnAPI.mobileToken.access_token).GetAwaiter().GetResult();
                        trophies = Serialization.FromJson<TrophiesResponse>(response);
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, true, PluginDatabase.PluginName);
                    }

                    TrophiesResponse trophiesDetails = null;
                    try
                    {
                        string responseDetails = Web.DownloadStringData(urlDetails, PsnAPI.mobileToken.access_token, "", LocalLang).GetAwaiter().GetResult();
                        trophiesDetails = Serialization.FromJson<TrophiesResponse>(responseDetails);
                    }
                    catch
                    {
                        Logger.Warn($"No trophiesDetails found for {game.Name} - {gameId}");
                        gameAchievements.Items = allAchievements;
                        return gameAchievements;
                    }

                    if (trophiesDetails?.Trophies == null)
                    {
                        Logger.Warn($"No trophiesDetails found for {game.Name} - {gameId}");
                        gameAchievements.Items = allAchievements;
                        return gameAchievements;
                    }

                    Dictionary<string, TrophyGroup> trophyGroups = new Dictionary<string, TrophyGroup>();
                    if (trophiesDetails.HasTrophyGroups)
                    {
                        HashSet<string> requiredGroupIds = new HashSet<string>(
                            trophiesDetails.Trophies
                                .Select(x => x.TrophyGroupId)
                                .Where(g => !g.IsNullOrEmpty())
                        );

                        trophyGroups = GetTrophyGroups(gameId, isPS5, requiredGroupIds);
                    }

                    foreach (Trophy trophie in trophiesDetails.Trophies)
                    {
                        Trophy trophieUser = trophies?.Trophies?.FirstOrDefault(x => x.TrophyId == trophie.TrophyId);
                        _ = float.TryParse(trophieUser?.TrophyEarnedRate, NumberStyles.Float, CultureInfo.InvariantCulture, out float Percent);

                        float GamerScore = 0;
                        switch (trophie.TrophyType)
                        {
                            case "bronze":
                                GamerScore = 15;
                                break;
                            case "silver":
                                GamerScore = 30;
                                break;
                            case "gold":
                                GamerScore = 90;
                                break;
                            case "platinum":
                                GamerScore = 300;
                                break;
                            default:
                                GamerScore = 15;
                                break;
                        }

                        string category = string.Empty;
                        int categoryOrder = 0;
                        string categoryIcon = string.Empty;

                        if (trophiesDetails.HasTrophyGroups && !trophie.TrophyGroupId.IsNullOrEmpty())
                        {
                            category = trophie.TrophyGroupId;
                            categoryOrder = int.TryParse(trophie.TrophyGroupId, out int order) ? order : 0;

                            if (trophyGroups.TryGetValue(trophie.TrophyGroupId, out TrophyGroup group))
                            {
                                categoryIcon = group.TrophyGroupIconUrl ?? string.Empty;
                                if (!group.TrophyGroupName.IsNullOrEmpty())
                                {
                                    category = group.TrophyGroupName;
                                }
                            }
                        }

                        allAchievements.Add(new Achievement
                        {
                            ApiName = trophie.TrophyId.ToString(),
                            Name = trophie.TrophyName.IsNullOrEmpty() ? ResourceProvider.GetString("LOCSuccessStoryHiddenTrophy") : trophie.TrophyName,
                            Description = trophie.TrophyDetail,
                            UrlUnlocked = trophie.TrophyIconUrl.IsNullOrEmpty() ? "hidden_trophy.png" : trophie.TrophyIconUrl,
                            DateUnlocked = trophieUser?.EarnedDateTime,
                            Percent = Percent == 0 ? 100 : Percent,
                            GamerScore = GamerScore,
                            IsHidden = trophie.TrophyHidden,
                            Category = category,
                            CategoryOrder = categoryOrder,
                            CategoryIcon = categoryIcon
                        });
                    }

                    gameAchievements.CommunicationId = gameId;
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }
            }
            else
            {
                ShowNotificationPluginNoAuthenticate(ExternalPlugin.PSNLibrary);
            }


            gameAchievements.Items = allAchievements;

            // Set source link
            if (gameAchievements.HasAchievements)
            {
                gameAchievements.SourcesLink = new SourceLink
                {
                    GameName = gameAchievements.Name,
                    Name = "PSN",
                    Url = url
                };
            }

            gameAchievements.SetRaretyIndicator();
            PluginDatabase.AddOrUpdate(gameAchievements);
            return gameAchievements;
        }


        private Dictionary<string, TrophyGroup> GetTrophyGroups(string gameId, bool isPS5, HashSet<string> requiredGroupIds)
        {
            Dictionary<string, TrophyGroup> groupsDict = new Dictionary<string, TrophyGroup>(StringComparer.InvariantCultureIgnoreCase);
            string lang = LocalLang.IsNullOrEmpty() ? "default" : LocalLang;
            string cacheFolder = Path.Combine(PluginDatabase.Paths.PluginCachePath, "PSN", "TrophyGroups", lang);
            string cacheFile = Path.Combine(cacheFolder, $"{gameId}.json");

            bool cacheValid = false;

            if (File.Exists(cacheFile))
            {
                if (Serialization.TryFromJsonFile(cacheFile, out TrophyGroupsResponse cachedResponse) && cachedResponse?.TrophyGroups != null)
                {
                    foreach (TrophyGroup group in cachedResponse.TrophyGroups)
                    {
                        if (!group.TrophyGroupId.IsNullOrEmpty())
                        {
                            groupsDict[group.TrophyGroupId] = group;
                        }
                    }

                    if (requiredGroupIds.All(id => groupsDict.ContainsKey(id)))
                    {
                        cacheValid = true;
                    }
                }
            }

            if (!cacheValid)
            {
                try
                {
                    string urlGroups = string.Format(UrlTrophyGroups, gameId) + (isPS5 ? string.Empty : "?npServiceName=trophy");
                    string responseGroups = Web.DownloadStringData(urlGroups, PsnAPI.mobileToken.access_token, string.Empty, LocalLang).GetAwaiter().GetResult();
                    if (!responseGroups.IsNullOrEmpty() && Serialization.TryFromJson(responseGroups, out TrophyGroupsResponse apiResponse) && apiResponse?.TrophyGroups != null)
                    {
                        groupsDict.Clear();
                        foreach (TrophyGroup group in apiResponse.TrophyGroups)
                        {
                            if (!group.TrophyGroupId.IsNullOrEmpty())
                            {
                                groupsDict[group.TrophyGroupId] = group;
                            }
                        }

                        try
                        {
                            FileSystem.WriteStringToFileSafe(cacheFile, responseGroups);
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false, $"Error saving PSN trophy groups cache for {gameId}", false, PluginDatabase.PluginName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Error fetching PSN trophy groups for {gameId}", false, PluginDatabase.PluginName);
                }
            }

            return groupsDict;
        }


        #region Configuration
        public override bool ValidateConfiguration()
        {
            if (!PluginDatabase.PluginSettings.Settings.PluginState.PsnIsEnabled)
            {
                ShowNotificationPluginDisable(ResourceProvider.GetString("LOCSuccessStoryNotificationsPsnDisabled"));
                return false;
            }
            else
            {
                try
                {
                    PsnAPI.CheckAuthentication().GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, true);
                    ShowNotificationPluginNoAuthenticate(ExternalPlugin.PSNLibrary);
                    return false;
                }

                if (CachedConfigurationValidationResult == null)
                {
                    CachedConfigurationValidationResult = IsConnected();

                    if (!(bool)CachedConfigurationValidationResult)
                    {
                        ShowNotificationPluginNoAuthenticate(ExternalPlugin.PSNLibrary);
                    }
                }
                else if (!(bool)CachedConfigurationValidationResult)
                {
                    ShowNotificationPluginErrorMessage(ExternalPlugin.PSNLibrary);
                }

                return (bool)CachedConfigurationValidationResult;
            }
        }


        public override bool IsConnected()
        {
            if (CachedIsConnectedResult == null)
            {
                CachedIsConnectedResult = PsnAPI.GetIsUserLoggedIn().GetAwaiter().GetResult();
            }

            return (bool)CachedIsConnectedResult;
        }

        public override bool EnabledInSettings()
        {
            return PluginDatabase.PluginSettings.Settings.EnablePsn;
        }
        #endregion


        #region PSN
        internal static PsnAllTrophies GetAllTrophies()
        {
            PsnAllTrophies psnAllTrophies = null;

            try
            {
                string response = Web.DownloadStringData(UrlAllTrophies, PsnAPI.mobileToken.access_token).GetAwaiter().GetResult();
                psnAllTrophies = Serialization.FromJson<PsnAllTrophies>(response);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            return psnAllTrophies;
        }

        public string GetNPWR(string Name)
        {
            IEnumerable<PSN_NPWR> found = PSN_NPWR_LIST.NPWR_LIST.Where(x => NormalizeGameName(x.Name).IsEqual(NormalizeGameName(Name)));
            return found?.Count() > 0 ? found.First().NPWR : string.Empty;
        }

        public string GetNPWR_2(string name)
        {
            try
            {
                string response = Web.DownloadStringData(UrlAllTrophyTitles, PsnAPI.mobileToken.access_token).GetAwaiter().GetResult();
                TropyTitlesResponse tropyTitlesResponse = Serialization.FromJson<TropyTitlesResponse>(response);
                TrophyTitle found = tropyTitlesResponse.TrophyTitles.FirstOrDefault(x => NormalizeGameName(x.TrophyTitleName).IsEqual(NormalizeGameName(name)));
                if (found != null)
                {
                    return found.NpCommunicationId;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            return string.Empty;
        }
        #endregion
    }
}
