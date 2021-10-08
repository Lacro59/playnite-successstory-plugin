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

namespace SuccessStory.Clients
{
    class PSNAchievements : GenericAchievements
    {
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

        private const string UrlAchievementsDetails = @"https://m.np.playstation.net/api/trophy/v1/npCommunicationIds/{0}/trophyGroups/all/trophies";
        private const string UrlAchievements = @"https://m.np.playstation.net/api/trophy/v1/users/me/npCommunicationIds/{0}/trophyGroups/all/trophies";


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

                    var split = game.GameId.Split('#');
                    string GameId = split.Count() < 3 ? game.GameId : game.GameId.Split('#')[2];

                    Url = string.Format(UrlAchievements, GameId) + "?npServiceName=trophy";                 // all without ps5
                    UrlDetails = string.Format(UrlAchievementsDetails, GameId) + "?npServiceName=trophy";   // all without ps5
                    string WebResult = Web.DownloadStringData(Url, PsnAPI.mobileToken.access_token).GetAwaiter().GetResult();
                    string WebResultDetails = Web.DownloadStringData(UrlDetails, PsnAPI.mobileToken.access_token, "", LocalLang).GetAwaiter().GetResult();

                    Trophies trophies = Serialization.FromJson<Trophies>(WebResult);
                    Trophies trophiesDetails = Serialization.FromJson<Trophies>(WebResultDetails);
                    foreach (Trophie trophie in trophies?.trophies)
                    {
                        Trophie trophieDetails = trophiesDetails.trophies.Where(x => x.trophyId == trophie.trophyId).FirstOrDefault();

                        float.TryParse(trophie.trophyEarnedRate.Replace(".", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator).Replace(",", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator), out float Percent);

                        AllAchievements.Add(new Achievements
                        {
                            Name = (trophieDetails.trophyName.IsNullOrEmpty()) ? resources.GetString("LOCSuccessStoryHiddenTrophy") : trophieDetails.trophyName,
                            Description = trophieDetails.trophyDetail,
                            UrlUnlocked = (trophieDetails.trophyIconUrl.IsNullOrEmpty()) ? "hidden_trophy.png" : trophieDetails.trophyIconUrl,
                            DateUnlocked = (trophie.earnedDateTime == null) ? default(DateTime) : trophie.earnedDateTime,
                            Percent = Percent
                        });
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false);
                }
            }
            else
            {
                ShowNotificationPluginNoAuthenticate(resources.GetString("LOCSuccessStoryNotificationsPsnNoAuthenticate"));
            }


            gameAchievements.Items = AllAchievements;


            // Set source link
            if (gameAchievements.HasAchivements)
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
                if (CachedConfigurationValidationResult == null)
                {
                    CachedConfigurationValidationResult = IsConnected();

                    if (!(bool)CachedConfigurationValidationResult)
                    {
                        ShowNotificationPluginNoAuthenticate(resources.GetString("LOCSuccessStoryNotificationsPsnNoAuthenticate"));
                    }
                }

                if (!(bool)CachedConfigurationValidationResult)
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
    }
}
