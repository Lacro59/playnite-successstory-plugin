using CommonPlayniteShared.PluginLibrary.PSNLibrary.Services;
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
        

        private static string urlBase => @"https://m.np.playstation.com/api/trophy/v1";
        private static string UrlTrophiesDetails => urlBase + @"/npCommunicationIds/{0}/trophyGroups/all/trophies";
        private static string UrlTrophies => urlBase + @"/users/me/npCommunicationIds/{0}/trophyGroups/all/trophies";
        private static string UrlAllTrophies => urlBase + @"/users/me/trophyTitles";
        private static string TrophiesWithIdsMobileUrl => urlBase + @"/users/me/titles/trophyTitles?npTitleIds={0}";
        private static string UrlAllTrophyTitles => urlBase + @"/users/me/trophyTitles";


        public PSNAchievements() : base("PSN", CodeLang.GetEpicLang(API.Instance.ApplicationSettings.Language))
        {
            PsnDataPath = PluginDatabase.Paths.PluginUserDataPath + "\\..\\" + GetPluginId(ExternalPlugin.PSNLibrary);
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
                    string[] split = game.GameId.Split('#');
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
                            string UrlTrophiesMobile = string.Format(TrophiesWithIdsMobileUrl, GameId);
                            string WebTrophiesMobileResult = Web.DownloadStringData(UrlTrophiesMobile, PsnAPI.mobileToken.access_token).GetAwaiter().GetResult();
                            TrophyTitlesWithIdsMobile titles_part = Serialization.FromJson<TrophyTitlesWithIdsMobile>(WebTrophiesMobileResult);

                            string TMP_GameId = titles_part?.titles?.FirstOrDefault()?.trophyTitles?.FirstOrDefault()?.npCommunicationId;
                            if (!TMP_GameId.IsNullOrEmpty())
                            {
                                GameId = TMP_GameId;
                            }
                            else
                            {
                                TMP_GameId = GetNPWR_2(game.Name);
                                if (!TMP_GameId.IsNullOrEmpty())
                                {
                                    GameId = TMP_GameId;
                                }
                                else
                                {
                                    TMP_GameId = GetNPWR(game.Name);
                                    if (!TMP_GameId.IsNullOrEmpty())
                                    {
                                        GameId = TMP_GameId;
                                    }
                                    else
                                    {
                                        Logger.Warn($"No trohpies find for {game.Name} - {GameId}");
                                        gameAchievements.Items = AllAchievements;
                                        return gameAchievements;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false, $"Error on PSNAchievements with {GameId}", true, PluginDatabase.PluginName);
                        }
                    }

                    Url = string.Format(UrlTrophies, GameId) + (IsPS5 ? string.Empty : "?npServiceName=trophy");
                    UrlDetails = string.Format(UrlTrophiesDetails, GameId) + (IsPS5 ? string.Empty : "?npServiceName=trophy");

                    string WebResult = string.Empty;
                    Trophies trophies = new Trophies { trophies = new List<Trophie>() };
                    try
                    {
                        WebResult = Web.DownloadStringData(Url, PsnAPI.mobileToken.access_token).GetAwaiter().GetResult();
                        trophies = Serialization.FromJson<Trophies>(WebResult);
                    }
                    catch { }

                    Trophies trophiesDetails = null;
                    try
                    {
                        string WebResultDetails = Web.DownloadStringData(UrlDetails, PsnAPI.mobileToken.access_token, "", LocalLang).GetAwaiter().GetResult();
                        trophiesDetails = Serialization.FromJson<Trophies>(WebResultDetails);
                    }
                    catch
                    {
                        Logger.Warn($"No trophiesDetails find for {game.Name} - {GameId}");
                        gameAchievements.Items = AllAchievements;
                        return gameAchievements;
                    }

                    foreach (Trophie trophie in trophiesDetails?.trophies)
                    {
                        Trophie trophieUser = trophies.trophies.Where(x => x.trophyId == trophie.trophyId).FirstOrDefault();
                        float.TryParse(trophieUser?.trophyEarnedRate, NumberStyles.Float, CultureInfo.InvariantCulture, out float Percent);

                        AllAchievements.Add(new Achievements
                        {
                            Name = trophie.trophyName.IsNullOrEmpty() ? ResourceProvider.GetString("LOCSuccessStoryHiddenTrophy") : trophie.trophyName,
                            Description = trophie.trophyDetail,
                            UrlUnlocked = trophie.trophyIconUrl.IsNullOrEmpty() ? "hidden_trophy.png" : trophie.trophyIconUrl,
                            DateUnlocked = (trophieUser?.earnedDateTime == null) ? default(DateTime) : trophieUser.earnedDateTime,
                            Percent = Percent == 0 ? 100 : Percent
                        });
                    }

                    gameAchievements.CommunicationId = GameId;
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }
            }
            else
            {
                ShowNotificationPluginNoAuthenticate(ResourceProvider.GetString("LOCSuccessStoryNotificationsPsnNoAuthenticate"), ExternalPlugin.PSNLibrary);
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

            gameAchievements.SetRaretyIndicator();
            return gameAchievements;
        }


        #region Configuration
        public override bool ValidateConfiguration()
        {
            if (CommonPluginsShared.PlayniteTools.IsDisabledPlaynitePlugins("PSNLibrary"))
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
                    ShowNotificationPluginNoAuthenticate(ResourceProvider.GetString("LOCSuccessStoryNotificationsPsnNoAuthenticate"), ExternalPlugin.PSNLibrary);
                    return false;
                }

                if (CachedConfigurationValidationResult == null)
                {
                    CachedConfigurationValidationResult = IsConnected();

                    if (!(bool)CachedConfigurationValidationResult)
                    {
                        ShowNotificationPluginNoAuthenticate(ResourceProvider.GetString("LOCSuccessStoryNotificationsPsnNoAuthenticate"), ExternalPlugin.PSNLibrary);
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
                string WebResult = Web.DownloadStringData(UrlAllTrophies, PsnAPI.mobileToken.access_token).GetAwaiter().GetResult();
                psnAllTrophies = Serialization.FromJson<PsnAllTrophies>(WebResult);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            return psnAllTrophies;
        }

        public string GetNPWR(string Name)
        {
            IEnumerable<PSN_NPWR> finded = PSN_NPWR_LIST.NPWR_LIST.Where(x => NormalizeGameName(x.Name).IsEqual(NormalizeGameName(Name)));
            if (finded?.Count() > 0)
            {
                return finded.First().NPWR;
            }

            return string.Empty;
        }

        public string GetNPWR_2(string name)
        {
            try
            {
                string webResult = Web.DownloadStringData(UrlAllTrophyTitles, PsnAPI.mobileToken.access_token).GetAwaiter().GetResult();
                TropyTitlesResponse tropyTitlesResponse = Serialization.FromJson<TropyTitlesResponse>(webResult);
                var finded = tropyTitlesResponse.TrophyTitles.FirstOrDefault(x => NormalizeGameName(x.TrophyTitleName).IsEqual(NormalizeGameName(name)));
                if (finded != null)
                {
                    return finded.NpCommunicationId;
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
