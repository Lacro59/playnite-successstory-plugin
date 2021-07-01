using CommonPlayniteShared.PluginLibrary.PSNLibrary;
using CommonPluginsShared;
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
                string WebResult = DonwloadStringData(Url, GetPsnToken()).GetAwaiter().GetResult();

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
                        "SuccessStory-PSN-NoAuthenticate",
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



        private async Task<string> DonwloadStringData(string UrlAchievements, string token)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:86.0) Gecko/20100101 Firefox/86.0");
                string result = await client.GetStringAsync(UrlAchievements).ConfigureAwait(false);

                return result;
            }
        }
    }


    public class Trophies
    {
        public List<Trophie> trophies { get; set; }
    }

    public class Trophie
    {
        public int trophyId { get; set; }
        public bool trophyHidden { get; set; }
        public string trophyType { get; set; }
        public string trophyName { get; set; }
        public string trophyDetail { get; set; }
        public string trophyIconUrl { get; set; }
        public int trophyRare { get; set; }
        public string trophyEarnedRate { get; set; }
        public FromUser fromUser { get; set; }
    }

    public class FromUser
    {
        public string onlineId { get; set; }
        public bool earned { get; set; }
        public DateTime earnedDate { get; set; }
    }
}
