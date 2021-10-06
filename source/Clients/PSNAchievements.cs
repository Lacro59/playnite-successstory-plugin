using CommonPlayniteShared.PluginLibrary.PSNLibrary;
using CommonPluginsShared;
using CommonPluginsShared.Models;
using CommonPluginsStores;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace SuccessStory.Clients
{
    class PSNAchievements : GenericAchievements
    {
        private const string UrlAchievements = @"https://us-tpy.np.community.playstation.net/trophy/v1/trophyTitles/{0}/trophyGroups/all/trophies?fields=@default,trophyRare,trophyEarnedRate&npLanguage={1}&sortKey=trophyId&iconSize=m";


        public PSNAchievements() : base("PSN")
        {

        }


        public override GameAchievements GetAchievements(Game game)
        {
            string GameName = game.Name;
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Achievements> AllAchievements = new List<Achievements>();
            int Total = 0;
            int Unlocked = 0;
            int Locked = 0;

            string Url = string.Empty;


            if (IsConnected())
            {
                try
                {
                    string Lang = CodeLang.GetGogLang(PluginDatabase.PlayniteApi.ApplicationSettings.Language);
                    string GameId = game.GameId.Split('#')[2];
                    Url = string.Format(UrlAchievements, GameId, Lang);
                    string WebResult = Web.DownloadStringData(Url, GetPsnToken()).GetAwaiter().GetResult();

                    Trophies trophies = Serialization.FromJson<Trophies>(WebResult);
                    foreach (Trophie trophie in trophies.trophies)
                    {
                        float.TryParse(trophie.trophyEarnedRate.Replace(".", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator).Replace(",", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator), out float Percent);

                        AllAchievements.Add(new Achievements
                        {
                            Name = (trophie.trophyName.IsNullOrEmpty()) ? resources.GetString("LOCSuccessStoryHiddenTrophy") : trophie.trophyName,
                            Description = trophie.trophyDetail,
                            UrlUnlocked = (trophie.trophyIconUrl.IsNullOrEmpty()) ? "hidden_trophy.png" : trophie.trophyIconUrl,
                            DateUnlocked = (trophie.fromUser.earnedDate == null) ? default(DateTime) : trophie.fromUser.earnedDate,
                            Percent = Percent
                        });

                        Total++;

                        if (trophie.fromUser.earned)
                        {
                            Unlocked++;
                        }
                        else
                        {
                            Locked++;
                        }
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
                    GameName = GameName,
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
                CachedIsConnectedResult = !GetPsnToken().IsNullOrEmpty();
            }

            return (bool)CachedIsConnectedResult;
        }

        public override bool EnabledInSettings()
        {
            return PluginDatabase.PluginSettings.Settings.EnablePsn;
        }
        #endregion


        #region PSN
        public string GetPsnToken()
        {
            string PSNDataPath = PluginDatabase.Paths.PluginUserDataPath + "\\..\\e4ac81cb-1b1a-4ec9-8639-9a9633989a71";

            if (Directory.Exists(PSNDataPath))
            {
                // TODO Edit this when is fixed in Playnite
                PSNAccountClient pSNAccountClient = new PSNAccountClient(PluginDatabase.PlayniteApi, PSNDataPath);
                if (pSNAccountClient.GetIsUserLoggedIn().GetAwaiter().GetResult())
                {
                    return pSNAccountClient.GetStoredToken();
                }
                else
                {
                    ShowNotificationPluginNoAuthenticate(resources.GetString("LOCSuccessStoryNotificationsPsnNoAuthenticate"));
                }
            }

            return null;
        }
        #endregion
    }
}
