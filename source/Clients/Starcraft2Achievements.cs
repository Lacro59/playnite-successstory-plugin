using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using CommonPluginsShared.Models;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SuccessStory.Clients
{
    internal class Starcraft2Achievements : BattleNetAchievements
    {
        private string UserSc2Id = string.Empty;

        private const string UrlStarCraft2 = @"https://starcraft2.com/";
        private const string UrlStarCraft2Login = @"https://starcraft2.com/login";
        private const string UrlStarCraft2ProfilInfo = @"https://starcraft2.com/fr-fr/api/sc2/profile/2/1/{0}?locale={1}";
        private const string UrlStarCraft2AchInfo = @"https://starcraft2.com/fr-fr/api/sc2/static/profile/2?locale={0}";

        private string UrlProfil = string.Empty;


        public Starcraft2Achievements() : base("Starcraft 2", PluginDatabase.PlayniteApi.ApplicationSettings.Language)
        {

        }


        public override GameAchievements GetAchievements(Game game)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Achievements> AllAchievements = new List<Achievements>();
            List<GameStats> AllStats = new List<GameStats>();

            if (IsConnected())
            {
                if (!UrlProfil.IsNullOrEmpty())
                {
                    UserSc2Id = UrlProfil.Split('/').Last();

                    string UrlStarCraft2ProfilInfo = string.Format(Starcraft2Achievements.UrlStarCraft2ProfilInfo, UserSc2Id, LocalLang);
                    string UrlStarCraft2AchInfo = string.Format(Starcraft2Achievements.UrlStarCraft2AchInfo, LocalLang);

                    string data = Web.DownloadStringData(UrlStarCraft2ProfilInfo, GetCookies()).GetAwaiter().GetResult();
                    BattleNetSc2Profil battleNetSc2Profil = Serialization.FromJson<BattleNetSc2Profil>(data);

                    data = Web.DownloadStringData(UrlStarCraft2AchInfo, GetCookies()).GetAwaiter().GetResult();
                    BattleNetSc2Ach battleNetSc2Ach = Serialization.FromJson<BattleNetSc2Ach>(data);

                    foreach (var earnedAchievement in battleNetSc2Profil.earnedAchievements)
                    {
                        try
                        {
                            string ApiName = earnedAchievement.achievementId;

                            var achievement = battleNetSc2Ach.achievements.Where(x => x.id == ApiName).FirstOrDefault();

                            string Name = achievement.title;
                            string Description = achievement.description;
                            string UrlImage = achievement.imageUrl;

                            int.TryParse(earnedAchievement.completionDate, out int ElpasedTime);

                            DateTime DateUnlocked = (ElpasedTime == 0) ? default(DateTime) : new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(ElpasedTime).ToLocalTime();

                            var cat = battleNetSc2Ach.categories.Where(x => x.id == achievement.categoryId).FirstOrDefault();
                            var catParent = battleNetSc2Ach.categories.Where(x => x.id == cat.parentCategoryId).FirstOrDefault();

                            string Category = cat.name;
                            string ParentCategory = catParent?.name;


                            AllAchievements.Add(new Achievements
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
                            Common.LogError(ex, false, true, "SuccessStory");
                        }
                    }
                }
                else
                {
                    ShowNotificationPluginNoAuthenticate(resources.GetString("LOCSuccessStoryNotificationsBattleNetNoAuthenticateSc2"));
                }
            }
            else
            {
                ShowNotificationPluginNoAuthenticate(resources.GetString("LOCSuccessStoryNotificationsBattleNetNoAuthenticate"));
            }

            gameAchievements.Items = AllAchievements;
            gameAchievements.ItemsStats = AllStats;


            // Set source link
            if (gameAchievements.HasAchivements)
            {
                gameAchievements.SourcesLink = new SourceLink
                {
                    GameName = "StarCraft II",
                    Name = "Battle.net",
                    Url = UrlProfil
                };
            }


            // Set rarity from Exophase
            if (gameAchievements.HasAchivements)
            {
                ExophaseAchievements exophaseAchievements = new ExophaseAchievements();
                exophaseAchievements.SetRarety(gameAchievements, Services.SuccessStoryDatabase.AchievementSource.Starcraft2);
            }


            return gameAchievements;
        }


        #region Configuration
        public override bool ValidateConfiguration()
        {
            if (PlayniteTools.IsDisabledPlaynitePlugins("BattleNetLibrary"))
            {
                ShowNotificationPluginDisable(resources.GetString("LOCSuccessStoryNotificationsBattleNetDisabled"));
                return false;
            }

            if (CachedConfigurationValidationResult == null)
            {
                CachedConfigurationValidationResult = IsConnected();

                if (!(bool)CachedConfigurationValidationResult)
                {
                    ShowNotificationPluginNoAuthenticate(resources.GetString("LOCSuccessStoryNotificationsBattleNetNoAuthenticate"));
                }
            }
            else if (!(bool)CachedConfigurationValidationResult)
            {
                ShowNotificationPluginErrorMessage();
            }

            return (bool)CachedConfigurationValidationResult;
        }


        public override bool IsConnected()
        {
            if (CachedIsConnectedResult == null)
            {
                CachedIsConnectedResult = false;
                string data = string.Empty;
                List<HttpCookie> cookies = null;
                using (var WebViewOffscreen = PluginDatabase.PlayniteApi.WebViews.CreateOffscreenView())
                {
                    WebViewOffscreen.NavigateAndWait(UrlStarCraft2Login);
                    data = WebViewOffscreen.GetPageSource();

                    HtmlParser parser = new HtmlParser();
                    IHtmlDocument htmlDocument = parser.Parse(data);

                    foreach (var SearchElement in htmlDocument.QuerySelectorAll("a.ProfilePill"))
                    {
                        UrlProfil = SearchElement.GetAttribute("href");
                    }

                    if (!UrlProfil.IsNullOrEmpty())
                    {
                        CachedIsConnectedResult = true;

                        cookies = WebViewOffscreen.GetCookies().Where(
                            x => x.Domain.Contains("starcraft2")
                                        || x.Domain.Contains("blizzard.com", StringComparison.OrdinalIgnoreCase)
                                        || x.Domain.Contains("battle.net", StringComparison.OrdinalIgnoreCase)
                        ).ToList();
                        SetCookies(cookies);
                    }
                }
            }

            return (bool)CachedIsConnectedResult;
        }

        public override bool EnabledInSettings()
        {
            return PluginDatabase.PluginSettings.Settings.EnableSc2Achievements;
        }
        #endregion


        #region Errors
        public override void ShowNotificationPluginNoAuthenticate(string Message)
        {
            LastErrorId = $"successStory-{ClientName.RemoveWhiteSpace().ToLower()}-noauthenticate";
            LastErrorMessage = Message;
            logger.Warn($"{ClientName} user is not authenticated");

            PluginDatabase.PlayniteApi.Notifications.Add(new NotificationMessage(
                $"successStory-{ClientName.RemoveWhiteSpace().ToLower()}-disabled",
                $"SuccessStory\r\n{Message}",
                NotificationType.Error,
                () =>
                {
                    using (var WebView = PluginDatabase.PlayniteApi.WebViews.CreateView(400, 600))
                    {
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
