﻿using CommonPlayniteShared.PluginLibrary.PSNLibrary.Services;
using CommonPluginsShared;
using CommonPluginsShared.Models;
using CommonPluginsStores;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using SuccessStory.Models;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using CommonPluginsShared.Extensions;
using CommonPlayniteShared.PluginLibrary.PSNLibrary.Models;
using static CommonPluginsShared.PlayniteTools;
using Playnite.SDK;
using SuccessStory.Models.PSN;
using CommonPluginsStores.Psn.Models;

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
                    Trophies trophies = new Trophies { trophies = new List<Trophie>() };
                    try
                    {
                        response = Web.DownloadStringData(url, PsnAPI.mobileToken.access_token).GetAwaiter().GetResult();
                        trophies = Serialization.FromJson<Trophies>(response);
                    }
                    catch { }

                    Trophies trophiesDetails = null;
                    try
                    {
                        string responseDetails = Web.DownloadStringData(urlDetails, PsnAPI.mobileToken.access_token, "", LocalLang).GetAwaiter().GetResult();
                        trophiesDetails = Serialization.FromJson<Trophies>(responseDetails);
                    }
                    catch
                    {
                        Logger.Warn($"No trophiesDetails found for {game.Name} - {gameId}");
                        gameAchievements.Items = allAchievements;
                        return gameAchievements;
                    }

                    foreach (Trophie trophie in trophiesDetails?.trophies)
                    {
                        Trophie trophieUser = trophies.trophies.FirstOrDefault(x => x.trophyId == trophie.trophyId);
                        _ = float.TryParse(trophieUser?.trophyEarnedRate, NumberStyles.Float, CultureInfo.InvariantCulture, out float Percent);

                        float GamerScore = 0;
                        switch (trophie.trophyType)
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

                        allAchievements.Add(new Achievement
                        {
                            Name = trophie.trophyName.IsNullOrEmpty() ? ResourceProvider.GetString("LOCSuccessStoryHiddenTrophy") : trophie.trophyName,
                            Description = trophie.trophyDetail,
                            UrlUnlocked = trophie.trophyIconUrl.IsNullOrEmpty() ? "hidden_trophy.png" : trophie.trophyIconUrl,
                            DateUnlocked = (trophieUser?.earnedDateTime == null) ? null : trophieUser.earnedDateTime,
                            Percent = Percent == 0 ? 100 : Percent,
                            GamerScore = GamerScore
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
            return gameAchievements;
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
