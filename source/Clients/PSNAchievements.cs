using CommonPlayniteShared.PluginLibrary.PSNLibrary;
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
using CommonPlayniteShared.PluginLibrary.PSNLibrary.Models;
using static CommonPluginsShared.PlayniteTools;

namespace SuccessStory.Clients
{
    // https://andshrew.github.io/PlayStation-Trophies/#/APIv2
    class PSNAchievements : GenericAchievements
    {
        protected static PsnAllTrophies _PsnAllTrophies;
        internal static PsnAllTrophies PsnAllTrophies
        {
            get
            {
                if (_PsnAllTrophies == null)
                {
                    _PsnAllTrophies = GetAllTrophies();
                }
                return _PsnAllTrophies;
            }

            set
            {
                _PsnAllTrophies = value;
            }
        }

        protected static PSNAccountClient _PsnAPI;
        internal static PSNAccountClient PsnAPI
        {
            get
            {
                if (_PsnAPI == null)
                {
                    _PsnAPI = new PSNAccountClient(PluginDatabase.PlayniteApi, PsnDataPath);
                }
                return _PsnAPI;
            }

            set
            {
                _PsnAPI = value;
            }
        }

        private static string PsnDataPath;

        public string CommunicationId { get; set; }

        private const string UrlTrophiesDetails = @"https://m.np.playstation.net/api/trophy/v1/npCommunicationIds/{0}/trophyGroups/all/trophies";
        private const string UrlTrophies = @"https://m.np.playstation.net/api/trophy/v1/users/me/npCommunicationIds/{0}/trophyGroups/all/trophies";

        private const string urlAllTrophies = @"https://m.np.playstation.net/api/trophy/v1/users/me/trophyTitles";

        private const string trophiesWithIdsMobileUrl = @"https://m.np.playstation.net/api/trophy/v1/users/me/titles/trophyTitles?npTitleIds={0}";

        public PSNAchievements() : base("PSN", CodeLang.GetEpicLang(PluginDatabase.PlayniteApi.ApplicationSettings.Language))
        {
            PsnDataPath = PluginDatabase.Paths.PluginUserDataPath + "\\..\\e4ac81cb-1b1a-4ec9-8639-9a9633989a71";
        }


        public override GameAchievements GetAchievements(Game game)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Achievements> AllAchievements = new List<Achievements>();

            string Url = string.Empty;
            string UrlDetails = string.Empty;


            if (IsConnected())
            {
                try
                {
                    PsnAPI.CheckAuthentication().GetAwaiter().GetResult();

                    // TODO Old plugin, still useful?
                    var split = game.GameId.Split('#');
                    string GameId = split.Count() < 3 ? game.GameId : game.GameId.Split('#')[2];

                    bool IsPS5 = game.Platforms.Where(x => x.Name.Contains("5")).Count() > 0;

                    if (!CommunicationId.IsNullOrEmpty())
                    {
                        GameId = CommunicationId;
                    }

                    if (!GameId.Contains("NPWR", StringComparison.InvariantCultureIgnoreCase))
                    {
                        try
                        {
                            string UrlTrophiesMobile = string.Format(trophiesWithIdsMobileUrl, GameId);
                            string WebTrophiesMobileResult = Web.DownloadStringData(UrlTrophiesMobile, PsnAPI.mobileToken.access_token).GetAwaiter().GetResult();
                            var titles_part = Serialization.FromJson<TrophyTitlesWithIdsMobile>(WebTrophiesMobileResult);

                            string TMP_GameId = titles_part?.titles?.FirstOrDefault()?.trophyTitles?.FirstOrDefault()?.npCommunicationId;
                            if (!TMP_GameId.IsNullOrEmpty())
                            {
                                GameId = TMP_GameId;
                            }
                            else
                            {
                                logger.Warn($"No trohpies find for {game.Name} - {GameId}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false, true, "SuccessStory");
                        }
                    }

                    Url = string.Format(UrlTrophies, GameId) + (IsPS5 ? string.Empty : "?npServiceName=trophy");
                    UrlDetails = string.Format(UrlTrophiesDetails, GameId) + (IsPS5 ? string.Empty : "?npServiceName=trophy");

                    string WebResult = Web.DownloadStringData(Url, PsnAPI.mobileToken.access_token).GetAwaiter().GetResult();
                    string WebResultDetails = Web.DownloadStringData(UrlDetails, PsnAPI.mobileToken.access_token, "", LocalLang).GetAwaiter().GetResult();

                    Trophies trophies = Serialization.FromJson<Trophies>(WebResult);
                    Trophies trophiesDetails = Serialization.FromJson<Trophies>(WebResultDetails);
                    foreach (Trophie trophie in trophies?.trophies)
                    {
                        Trophie trophieDetails = trophiesDetails.trophies.Where(x => x.trophyId == trophie.trophyId).FirstOrDefault();

                        float.TryParse(trophie.trophyEarnedRate.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator).Replace(",", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator), out float Percent);

                        AllAchievements.Add(new Achievements
                        {
                            Name = (trophieDetails.trophyName.IsNullOrEmpty()) ? resources.GetString("LOCSuccessStoryHiddenTrophy") : trophieDetails.trophyName,
                            Description = trophieDetails.trophyDetail,
                            UrlUnlocked = (trophieDetails.trophyIconUrl.IsNullOrEmpty()) ? "hidden_trophy.png" : trophieDetails.trophyIconUrl,
                            DateUnlocked = (trophie.earnedDateTime == null) ? default(DateTime) : trophie.earnedDateTime,
                            Percent = Percent
                        });
                    }

                    gameAchievements.CommunicationId = GameId;
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, "SuccessStory");
                }
            }
            else
            {
                ShowNotificationPluginNoAuthenticate(resources.GetString("LOCSuccessStoryNotificationsPsnNoAuthenticate"), ExternalPlugin.PSNLibrary);
            }


            gameAchievements.Items = AllAchievements;


            // Set source link
            if (gameAchievements.HasAchievements)
            {
                gameAchievements.SourcesLink = new SourceLink
                {
                    GameName = gameAchievements.Name,
                    Name = "PSN",
                    Url = Url
                };
            }


            return gameAchievements;
        }


        #region Configuration
        public override bool ValidateConfiguration()
        {
            if (CommonPluginsShared.PlayniteTools.IsDisabledPlaynitePlugins("PSNLibrary"))
            {
                ShowNotificationPluginDisable(resources.GetString("LOCSuccessStoryNotificationsPsnDisabled"));
                return false;
            }
            else
            {
                PsnAPI.CheckAuthentication().GetAwaiter().GetResult();

                if (CachedConfigurationValidationResult == null)
                {
                    CachedConfigurationValidationResult = IsConnected();

                    if (!(bool)CachedConfigurationValidationResult)
                    {
                        ShowNotificationPluginNoAuthenticate(resources.GetString("LOCSuccessStoryNotificationsPsnNoAuthenticate"), ExternalPlugin.PSNLibrary);
                    }
                }
                else if (!(bool)CachedConfigurationValidationResult)
                {
                    ShowNotificationPluginErrorMessage();
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
                string WebResult = Web.DownloadStringData(urlAllTrophies, PsnAPI.mobileToken.access_token).GetAwaiter().GetResult();
                psnAllTrophies = Serialization.FromJson<PsnAllTrophies>(WebResult);
            }
            catch(Exception ex)
            {
                Common.LogError(ex, false, true, "SuccessStory");
            }

            return psnAllTrophies;
        }
        #endregion
    }


    public class PsnAllTrophies
    {
        public List<TrophyTitle> trophyTitles { get; set; }
        public int totalItemCount { get; set; }
    }

    public class DefinedTrophies
    {
        public int bronze { get; set; }
        public int silver { get; set; }
        public int gold { get; set; }
        public int platinum { get; set; }
    }

    public class EarnedTrophies
    {
        public int bronze { get; set; }
        public int silver { get; set; }
        public int gold { get; set; }
        public int platinum { get; set; }
    }

    public class TrophyTitle
    {
        public string npServiceName { get; set; }
        public string npCommunicationId { get; set; }
        public string trophySetVersion { get; set; }
        public string trophyTitleName { get; set; }
        public string trophyTitleDetail { get; set; }
        public string trophyTitleIconUrl { get; set; }
        public string trophyTitlePlatform { get; set; }
        public bool hasTrophyGroups { get; set; }
        public DefinedTrophies definedTrophies { get; set; }
        public int progress { get; set; }
        public EarnedTrophies earnedTrophies { get; set; }
        public bool hiddenFlag { get; set; }
        public DateTime lastUpdatedDateTime { get; set; }
    }
}
