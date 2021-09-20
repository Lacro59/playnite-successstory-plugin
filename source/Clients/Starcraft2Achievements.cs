using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using CommonPluginsShared;
using CommonPluginsShared.Models;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
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
        private string UrlStarCraft2 = @"https://starcraft2.com/";
        private string UrlStarCraft2Login = @"login";
        private string UrlStarCraft2ProfilInfo = @"https://starcraft2.com/fr-fr/api/sc2/profile/2/1/{0}?locale={1}";
        private string UrlStarCraft2AchInfo = @"https://starcraft2.com/fr-fr/api/sc2/static/profile/2?locale={0}";


        public Starcraft2Achievements() : base()
        {
            UrlStarCraft2Login = UrlStarCraft2 + UrlStarCraft2Login;
        }


        public override bool IsConnected()
        {
            var ApiStatus = base.GetApiStatus();

            if (ApiStatus == null)
            {
                return false;
            }

            if (ApiStatus.authenticated)
            {
                Task.Run(() =>
                {
                    using (var WebView = PluginDatabase.PlayniteApi.WebViews.CreateOffscreenView())
                    {
                        WebView.Navigate(UrlStarCraft2);
                    }
                });
            }

            return ApiStatus.authenticated;
        }

        public override GameAchievements GetAchievements(Game game)
        {
            List<Achievements> AllAchievements = new List<Achievements>();
            List<GameStats> AllStats = new List<GameStats>();
            string GameName = game.Name;
            int Total = 0;
            int Unlocked = 0;
            int Locked = 0;

            GameAchievements Result = PluginDatabase.GetDefault(game);
            Result.Items = AllAchievements;


            string UrlProfil = string.Empty;
            if (IsConnected())
            {
                WebViewOffscreen.NavigateAndWait(UrlStarCraft2);
                string data = WebViewOffscreen.GetPageSource();

                HtmlParser parser = new HtmlParser();
                IHtmlDocument htmlDocument = parser.Parse(data);

                foreach (var SearchElement in htmlDocument.QuerySelectorAll("a.ProfilePill"))
                {
                    UrlProfil = SearchElement.GetAttribute("href");
                }

                if (!UrlProfil.IsNullOrEmpty())
                {
                    string Lang = PluginDatabase.PlayniteApi.ApplicationSettings.Language;

                    UserSc2Id = UrlProfil.Split('/').Last();

                    UrlStarCraft2ProfilInfo = string.Format(UrlStarCraft2ProfilInfo, UserSc2Id, Lang);
                    UrlStarCraft2AchInfo = string.Format(UrlStarCraft2AchInfo, Lang);

                    WebViewOffscreen.NavigateAndWait(UrlStarCraft2ProfilInfo);
                    data = WebViewOffscreen.GetPageText();
                    BattleNetSc2Profil battleNetSc2Profil = Serialization.FromJson<BattleNetSc2Profil>(data);

                    WebViewOffscreen.NavigateAndWait(UrlStarCraft2AchInfo);
                    data = WebViewOffscreen.GetPageText();
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

                            DateTime DateUnlocked = (ElpasedTime == 0) ? default(DateTime) : new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(ElpasedTime);

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

                            Total++;
                            if (ElpasedTime > 0)
                            {
                                Unlocked++;
                            }
                            else
                            {
                                Locked++;
                            }
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false);
                        }
                    }


                    //try
                    //{
                    //    AllStats = GetUsersStats(game, htmlDocument);
                    //}
                    //catch (Exception ex)
                    //{
                    //    Common.LogError(ex, false, $"Error on GetUsersStats({game.Name})");
                    //}
                }
                else
                {
                    logger.Error($"No StarCraft II profil connected");
                    PluginDatabase.PlayniteApi.Notifications.Add(new NotificationMessage(
                         "SuccessStory-BattleNet-NoAuthenticateSc2",
                         $"SuccessStory\r\n{resources.GetString("LOCSuccessStoryNotificationsBattleNetNoAuthenticateSc2")}",
                         NotificationType.Error,
                         () =>
                         {
                             using (var WebView = PluginDatabase.PlayniteApi.WebViews.CreateView(400, 600))
                             {
                                 WebView.Navigate(UrlStarCraft2Login);
                                 WebView.OpenDialog();
                             }
                         }
                     ));
                }
            }

            Result.Name = GameName;
            Result.HaveAchivements = Total > 0;
            Result.Total = Total;
            Result.Unlocked = Unlocked;
            Result.Locked = Locked;
            Result.Progression = (Total != 0) ? (int)Math.Ceiling((double)(Unlocked * 100 / Total)) : 0;
            Result.Items = AllAchievements;
            Result.ItemsStats = AllStats;

            if (Result.HaveAchivements)
            {
                ExophaseAchievements exophaseAchievements = new ExophaseAchievements();
                exophaseAchievements.SetRarety(Result);


                Result.SourcesLink = new SourceLink
                {
                    GameName = "StarCraft II",
                    Name = "Battle.Net",
                    Url = UrlProfil
                };
            }

            return Result;
        }

        public override bool ValidateConfiguration(IPlayniteAPI playniteAPI, Plugin plugin, SuccessStorySettings settings)
        {
            if (!settings.EnableSc2Achievements)
                return true;

            if (PlayniteTools.IsDisabledPlaynitePlugins("BattleNetLibrary"))
            {
                logger.Warn("Battle.net is enable then disabled");
                playniteAPI.Notifications.Add(new NotificationMessage(
                    "SuccessStory-BattleNet-disabled",
                    $"SuccessStory\r\n{resources.GetString("LOCSuccessStoryNotificationsBattleNetDisabled")}",
                    NotificationType.Error,
                    () => plugin.OpenSettingsView()
                ));
                return false;
            }
            else
            {
                Common.LogDebug(true, $"VerifToAddOrShowSc2: {CachedConfigurationValidationResult}");

                if (CachedConfigurationValidationResult == null)
                {
                    CachedConfigurationValidationResult = IsConnected();
                }

                Common.LogDebug(true, $"VerifToAddOrShowSc2: {CachedConfigurationValidationResult}");

                if (!(bool)CachedConfigurationValidationResult)
                {
                    logger.Warn("Battle.net user is not authenticated");
                    playniteAPI.Notifications.Add(new NotificationMessage(
                        "SuccessStory-BattleNet-NoAuthenticate",
                        $"SuccessStory\r\n{resources.GetString("LOCSuccessStoryNotificationsBattleNetNoAuthenticate")}",
                        NotificationType.Error,
                        () => plugin.OpenSettingsView()
                    ));
                    return false;
                }
            }
            return true;
        }
        public override bool EnabledInSettings(SuccessStorySettings settings)
        {
            return settings.EnableSc2Achievements;
        }
    }
}
