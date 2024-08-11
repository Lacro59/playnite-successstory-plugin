using CommonPluginsShared;
using CommonPluginsShared.Models;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using SuccessStory.Models;
using SuccessStory.Models.Wow;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SuccessStory.Clients
{
    internal class WowAchievements : BattleNetAchievements
    {
        private static string Sha256Hash { get; set; }

        #region Url
        private static string UrlWowGraphQL => @"https://worldofwarcraft.blizzard.com/graphql";
        private static string UrlWowBase => @"https://worldofwarcraft.blizzard.com/{0}/character/{1}/{2}/{3}/achievements/";
        private static string UrlWowBaseLocalised { get; set; }

        private List<string> Urls { get; set; } = new List<string>();
        private string UrlWowAchCharacter => "characters/model.json";
        private string UrlWowAchPvp => "player-vs-player/model.json";
        private string UrlWowAchQuests => "quests/model.json";
        private string UrlWowAchExploration => "exploration/model.json";
        private string UrlWowAchWorlEvents => "world-events/model.json";
        private string UrlWowAchDungeonsRaids => "dungeons-raids/model.json";
        private string UrlWowAchProfessions => "professions/model.json";
        private string UrlWowAchReputation => "reputation/model.json";
        private string UrlWowAchPetBattles => "pet-battles/model.json";
        private string UrlWowAchCollections => "collections/model.json";
        private string UrlWowAchExpansionFeatures => "expansion-features/model.json";
        private string UrlWowAchFeatsStrength => "feats-of-strength/model.json";
        private string UrlWowAchLegacy => "legacy/model.json";
        #endregion

        public WowAchievements() : base("Wow", CodeLang.GetEpicLang(API.Instance.ApplicationSettings.Language))
        {

        }


        public override GameAchievements GetAchievements(Game game)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Achievements> AllAchievements = new List<Achievements>();
            List<GameStats> AllStats = new List<GameStats>();

            string urlFinal = string.Empty;
            try
            {
                string region = PluginDatabase.PluginSettings.Settings.WowRegions.Find(x => x.IsSelected)?.Name;
                string realm = PluginDatabase.PluginSettings.Settings.WowRealms.Find(x => x.IsSelected)?.Slug;
                string character = PluginDatabase.PluginSettings.Settings.WowCharacter;

                UrlWowBaseLocalised = string.Format(UrlWowBase, LocalLang, region, realm, WebUtility.UrlEncode(character));
                Urls = new List<string>
                {
                    UrlWowAchCharacter, UrlWowAchPvp, UrlWowAchQuests, UrlWowAchExploration, UrlWowAchWorlEvents, UrlWowAchDungeonsRaids,
                    UrlWowAchProfessions, UrlWowAchReputation, UrlWowAchPetBattles, UrlWowAchCollections, UrlWowAchExpansionFeatures, UrlWowAchFeatsStrength,
                    UrlWowAchLegacy
                };

                foreach (string url in Urls)
                {
                    urlFinal = UrlWowBaseLocalised + url;
                    string data = Web.DownloadStringData(urlFinal).GetAwaiter().GetResult();
                    _ = Serialization.TryFromJson(data, out WowAchievementsData wowAchievementsData);
                    _ = Serialization.TryFromJson(Serialization.ToJson(wowAchievementsData?.Subcategories), out dynamic subcategories);

                    if (subcategories != null)
                    {
                        foreach (dynamic subItems in subcategories)
                        {
                            Serialization.TryFromJson(Serialization.ToJson(subItems?.Value), out SubcategoriesItem subcategoriesItem);
                            if (subcategoriesItem?.Achievements != null)
                            {
                                foreach (Achievement achievement in subcategoriesItem.Achievements)
                                {
                                    AllAchievements.Add(new Achievements
                                    {
                                        Name = achievement.Name,
                                        Description = achievement.Description,
                                        UrlUnlocked = achievement.Icon.Url,
                                        UrlLocked = string.Empty,
                                        DateUnlocked = achievement.Time == null ? default : achievement.Time.ToLocalTime(),
                                        GamerScore = achievement.Point
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Error on {urlFinal}", true, PluginDatabase.PluginName);
            }


            gameAchievements.Items = AllAchievements;
            gameAchievements.ItemsStats = AllStats;


            // Set source link
            if (gameAchievements.HasAchievements)
            {
                gameAchievements.SourcesLink = new SourceLink
                {
                    GameName = "Wow",
                    Name = "Battle.net",
                    Url = UrlWowBaseLocalised
                };
            }

            // Set rarity from Exophase
            if (gameAchievements.HasAchievements)
            {
                ExophaseAchievements exophaseAchievements = new ExophaseAchievements();
                exophaseAchievements.SetRarety(gameAchievements, Services.SuccessStoryDatabase.AchievementSource.Overwatch);
            }

            gameAchievements.SetRaretyIndicator();
            return gameAchievements;
        }


        #region Configuration
        public override bool ValidateConfiguration()
        {
            return true;
        }

        public override bool EnabledInSettings()
        {
            string region = PluginDatabase.PluginSettings.Settings.WowRegions.Find(x => x.IsSelected)?.Name;
            string realm = PluginDatabase.PluginSettings.Settings.WowRealms.Find(x => x.IsSelected)?.Slug;
            string character = PluginDatabase.PluginSettings.Settings.WowCharacter;

            return PluginDatabase.PluginSettings.Settings.EnableWowAchievements
                && !region.IsNullOrEmpty() && !realm.IsNullOrEmpty() && !character.IsNullOrEmpty();
        }
        #endregion


        #region Wow
        public static List<CbData> GetRealm(string Region)
        {
            List<CbData> CbDatas = new List<CbData>();

            try
            {
                string payload = "{\"operationName\":\"GetRealmStatusData\",\"variables\":{\"input\":{\"compoundRegionGameVersionSlug\":\"" + Region + "\"}},\"extensions\":{\"persistedQuery\":{\"version\":1,\"sha256Hash\":\"" + GetSha256Hash() + "\"}}}";
                string result = Web.PostStringDataPayload(UrlWowGraphQL, payload).GetAwaiter().GetResult();
                WowRegionResult wowRegionResult = Serialization.FromJson<WowRegionResult>(result);
                foreach (Realm realm in wowRegionResult.Data.Realms)
                {
                    CbDatas.Add(new CbData { Name = realm.Name, Slug = realm.Slug });
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            return CbDatas;
        }

        private static string GetSha256Hash()
        {
            if (!Sha256Hash.IsNullOrEmpty())
            {
                return Sha256Hash;
            }

            try
            {
                string url = string.Format("https://worldofwarcraft.blizzard.com/game/status");
                string response = Web.DownloadStringData(url).GetAwaiter().GetResult();

                string js = Regex.Match(response, @"realm-status.\w*.js").Value;
                if (!js.IsNullOrEmpty())
                {
                    url = $"https://assets.worldofwarcraft.blizzard.com/static/{js}";
                    response = Web.DownloadStringData(url).GetAwaiter().GetResult();
                    Sha256Hash = Regex.Match(response, "\"GetRealmStatusData\"[)],a[.]documentId=\"(\\w*)\"}").Groups[1].Value;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            return Sha256Hash;
        }
        #endregion
    }
}
