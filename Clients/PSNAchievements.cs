using CommonPlayniteShared.PluginLibrary.PSNLibrary;
using CommonPluginsShared;
using CommonPluginsStores;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Clients
{
    class PSNAchievements : GenericAchievements
    {
        private IWebView WebViewOffscreen;

        private const string UrlAchievements = @"https://us-tpy.np.community.playstation.net/trophy/v1/trophyTitles/{0}/trophyGroups/all/trophies?fields=@default,trophyRare,trophyEarnedRate&npLanguage={1}&sortKey=trophyId&iconSize=m";


        public PSNAchievements() : base()
        {
            WebViewOffscreen = PluginDatabase.PlayniteApi.WebViews.CreateOffscreenView();
        }


        public override GameAchievements GetAchievements(Game game)
        {
            List<Achievements> AllAchievements = new List<Achievements>();
            string GameName = game.Name;
            bool HaveAchivements = false;
            int Total = 0;
            int Unlocked = 0;
            int Locked = 0;

            GameAchievements Result = SuccessStory.PluginDatabase.GetDefault(game);

            try
            {
                string Lang = CodeLang.GetGogLang(PluginDatabase.PlayniteApi.ApplicationSettings.Language);
                string GameId = game.GameId.Split('#')[2];
                string Url = string.Format(UrlAchievements, GameId, Lang);
                string WebResult = Web.DownloadStringData(Url, GetPsnToken()).GetAwaiter().GetResult();

                Trophies trophies = Serialization.FromJson<Trophies>(WebResult);
                foreach(Trophie trophie in trophies.trophies)
                {
                    HaveAchivements = true;
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


            Result.Items = AllAchievements;
            Result.Name = GameName;
            Result.HaveAchivements = HaveAchivements;
            Result.Total = Total;
            Result.Unlocked = Unlocked;
            Result.Locked = Locked;
            Result.Progression = (Total != 0) ? (int)Math.Ceiling((double)(Unlocked * 100 / Total)) : 0;
            Result.Items = AllAchievements;

            return Result;
        }


        public string GetPsnToken()
        {
            string PSNDataPath = PluginDatabase.Paths.PluginUserDataPath + "\\..\\e4ac81cb-1b1a-4ec9-8639-9a9633989a71";

            if (Directory.Exists(PSNDataPath))
            {
                PSNAccountClient pSNAccountClient = new PSNAccountClient(PluginDatabase.PlayniteApi, PSNDataPath);
                if (pSNAccountClient.GetIsUserLoggedIn().GetAwaiter().GetResult())
                {
                    return pSNAccountClient.GetStoredToken();
                }
                else
                {
                    PluginDatabase.PlayniteApi.Notifications.Add(new NotificationMessage(
                        "SuccessStory-Psn-NoAuthenticate",
                        $"SuccessStory\r\n{resources.GetString("LOCSuccessStoryNotificationsPsnNoAuthenticate")}",
                        NotificationType.Error
                    ));
                    logger.Warn("PSN user is not authenticate");
                }
            }

            return null;
        }


        public override bool IsConnected()
        {
            return !GetPsnToken().IsNullOrEmpty();
        }

        public override bool IsConfigured()
        {
            return !GetPsnToken().IsNullOrEmpty();
        }
    }
}
