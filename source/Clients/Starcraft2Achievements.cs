using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using CommonPluginsShared.Models;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using SuccessStory.Models;
using SuccessStory.Models.StarCraft2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static CommonPluginsShared.PlayniteTools;

namespace SuccessStory.Clients
{
    // TODO KO
    internal class Starcraft2Achievements : BattleNetAchievements
    {
        private static int UserSc2Id { get; set; } = 0;

        private static string UrlStarCraft2 => @"https://starcraft2.com";
        private static string UrlStarCraft2Login => UrlStarCraft2 + @"/login";
        private static string UrlStarCraft2ProfilInfo => UrlStarCraft2 + @"/api/sc2/profile/{2}/1/{0}?locale={1}";
        private static string UrlStarCraft2AchInfo => UrlStarCraft2 + @"/api/sc2/static/profile/2?locale={0}";

        private string UrlProfil { get; set; } = string.Empty;


        public Starcraft2Achievements() : base("Starcraft 2", API.Instance.ApplicationSettings.Language)
        {

        }


        public override GameAchievements GetAchievements(Game game)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Models.Achievement> AllAchievements = new List<Models.Achievement>();
            List<GameStats> AllStats = new List<GameStats>();

            if (IsConnected() && UserSc2Id != 0)
            {
                string region = "0";
                if (UrlProfil.IndexOf("profile/1/1") > -1)
                {
                    region = "1";
                }
                if (UrlProfil.IndexOf("profile/2/1") > -1)
                {
                    region = "2";
                }
                if (UrlProfil.IndexOf("profile/3/1") > -1)
                {
                    region = "3";
                }

                //UserSc2Id = UrlProfil.Split('/').Last();

                string UrlStarCraft2ProfilInfo = string.Format(Starcraft2Achievements.UrlStarCraft2ProfilInfo, UserSc2Id, LocalLang, region);
                string UrlStarCraft2AchInfo = string.Format(Starcraft2Achievements.UrlStarCraft2AchInfo, LocalLang);

                string data = Web.DownloadStringData(UrlStarCraft2ProfilInfo, GetCookies()).GetAwaiter().GetResult();
                BattleNetSc2Profil battleNetSc2Profil = Serialization.FromJson<BattleNetSc2Profil>(data);

                data = Web.DownloadStringData(UrlStarCraft2AchInfo, GetCookies()).GetAwaiter().GetResult();
                BattleNetSc2Ach battleNetSc2Ach = Serialization.FromJson<BattleNetSc2Ach>(data);

                foreach (EarnedAchievement earnedAchievement in battleNetSc2Profil.earnedAchievements)
                {
                    try
                    {
                        string ApiName = earnedAchievement.achievementId;
                        Models.StarCraft2.Achievement achievement = battleNetSc2Ach.Achievements.FirstOrDefault(x => x.Id == ApiName);
                        string Name = achievement.Title;
                        string Description = achievement.Description;
                        string UrlImage = achievement.ImageUrl;

                        int.TryParse(earnedAchievement.completionDate, out int ElpasedTime);

                        DateTime DateUnlocked = (ElpasedTime == 0) ? default : new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(ElpasedTime);

                        Models.StarCraft2.Category cat = battleNetSc2Ach.Categories.FirstOrDefault(x => x.Id == achievement.CategoryId);
                        Models.StarCraft2.Category catParent = battleNetSc2Ach.Categories.FirstOrDefault(x => x.Id == cat.ParentCategoryId);

                        string Category = cat.Name;
                        string ParentCategory = catParent?.Name;


                        AllAchievements.Add(new Models.Achievement
                        {
                            ApiName = ApiName,
                            Name = Name,
                            Description = Description,
                            UrlUnlocked = UrlImage,
                            DateUnlocked = DateUnlocked,

                            ParentCategory = ParentCategory,
                            Category = Category
                        });
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, true, PluginDatabase.PluginName);
                    }
                }
            }
            else
            {
                ShowNotificationPluginNoAuthenticate(ExternalPlugin.BattleNetLibrary);
            }

            gameAchievements.Items = AllAchievements;
            gameAchievements.ItemsStats = AllStats;


            // Set source link
            if (gameAchievements.HasAchievements)
            {
                gameAchievements.SourcesLink = new SourceLink
                {
                    GameName = "StarCraft II",
                    Name = "Battle.net",
                    Url = UrlProfil
                };
            }


            // Set rarity from Exophase
            if (gameAchievements.HasAchievements)
            {
                SuccessStory.ExophaseAchievements.SetRarety(gameAchievements, Services.SuccessStoryDatabase.AchievementSource.Starcraft2);
            }

            gameAchievements.SetRaretyIndicator();
            return gameAchievements;
        }


        #region Configuration
        public override bool ValidateConfiguration()
        {
            return false;
            /*
            if (PlayniteTools.IsDisabledPlaynitePlugins("BattleNetLibrary"))
            {
                ShowNotificationPluginDisable(ResourceProvider.GetString("LOCSuccessStoryNotificationsBattleNetDisabled"));
                return false;
            }

            if (CachedConfigurationValidationResult == null)
            {
                CachedConfigurationValidationResult = IsConnected();

                if (!(bool)CachedConfigurationValidationResult)
                {
                    ShowNotificationPluginNoAuthenticate(ResourceProvider.GetString("LOCSuccessStoryNotificationsBattleNetNoAuthenticate"), ExternalPlugin.BattleNetLibrary);
                }
            }
            else if (!(bool)CachedConfigurationValidationResult)
            {
                ShowNotificationPluginErrorMessage();
            }

            return (bool)CachedConfigurationValidationResult;
            */
        }


        public override bool IsConnected()
        {
            return false;
            /*
            if (CachedIsConnectedResult == null)
            {
                UserSc2Id = 0;
                CachedIsConnectedResult = false;
                string data = string.Empty;
                List<HttpCookie> cookies = null;

                using (IWebView WebViewOffscreen = API.Instance.WebViews.CreateOffscreenView())
                {
                    WebViewOffscreen.NavigateAndWait(UrlStarCraft2Login);
                    _ = WebViewOffscreen.GetPageSource();

                    cookies = WebViewOffscreen.GetCookies().Where(
                        x => x.Domain.Contains("starcraft2")
                                    || x.Domain.Contains("blizzard.com", StringComparison.OrdinalIgnoreCase)
                                    || x.Domain.Contains("battle.net", StringComparison.OrdinalIgnoreCase)
                    ).ToList();

                    data = Web.DownloadStringData("https://starcraft2.blizzard.com/nav/authenticate", cookies).GetAwaiter().GetResult();
                    if (Serialization.TryFromJson(data, out BattleNetUser battleNetSc2User))
                    {
                        UserSc2Id = battleNetSc2User.Id;
                        CachedIsConnectedResult = true;
                        SetCookies(cookies);
                    }
                }
            }

            return (bool)CachedIsConnectedResult;
            */
        }

        public override bool EnabledInSettings()
        {
            return PluginDatabase.PluginSettings.Settings.EnableSc2Achievements;
        }
        #endregion


        #region Errors
        public override void ShowNotificationPluginNoAuthenticate(ExternalPlugin PluginSource)
        {
            string message = string.Format(ResourceProvider.GetString("LOCCommonStoresNoAuthenticate"), ClientName);
            LastErrorId = $"{PluginDatabase.PluginName}-{ClientName.RemoveWhiteSpace()}-noauthenticate";
            LastErrorMessage = message;
            Logger.Warn($"{ClientName} user is not authenticated");

            API.Instance.Notifications.Add(new NotificationMessage(
                $"{PluginDatabase.PluginName}-{ClientName.RemoveWhiteSpace()}-disabled",
                $"{PluginDatabase.PluginName}\r\n{message}",
                NotificationType.Error,
                () =>
                {
                    using (var WebView = API.Instance.WebViews.CreateView(400, 600))
                    {
                        WebView.LoadingChanged += (s, e) =>
                        {
                            string address = WebView.GetCurrentAddress();
                            if (!address.Contains(UrlLogin) && !address.Contains(UrlStarCraft2Login))
                            {
                                ResetCachedConfigurationValidationResult();
                                ResetCachedIsConnectedResult();
                                WebView.Close();
                            }
                        };

                        WebView.Navigate(UrlStarCraft2Login);
                        WebView.OpenDialog();

                        ResetCachedIsConnectedResult();
                        ResetCachedConfigurationValidationResult();
                    }
                }
            ));
        }
        #endregion
    }
}
