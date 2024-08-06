using System;
using System.Collections.Generic;
using System.Linq;
using CommonPluginsShared;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using SuccessStory.Models;
using SuccessStory.Models.GenshinImpact;

namespace SuccessStory.Clients
{
    public class GenshinImpactAchievements : GenericAchievements
    {
        private static string Url => @"https://raw.githubusercontent.com/Sycamore0/GenshinData/main/";
        private static string UrlSource => @"https://github.com/Sycamore0/GenshinData";
        private static string UrlTextMap => Url + @"/TextMap/TextMap{0}.json";
        private static string UrlAchievementsCategory => Url + @"/ExcelBinOutput/AchievementGoalExcelConfigData.json";
        private static string UrlAchievements => Url + @"/ExcelBinOutput/AchievementExcelConfigData.json";


        private static string PaimonMoe_Url => "https://raw.githubusercontent.com/MadeBaruna/paimon-moe/main";
        private static string PaimonMoe_UrlSource => "https://github.com//MadeBaruna/paimon-moe";
        private static string PaimonMoe_UrlAchievements => PaimonMoe_Url + "/src/data/achievement/{0}.json";


        public GenshinImpactAchievements() : base("GenshinImpact", CodeLang.GetGenshinLang(API.Instance.ApplicationSettings.Language))
        {

        }


        public override GameAchievements GetAchievements(Game game)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Achievements> AllAchievements = new List<Achievements>();

            try
            {
                string Url = string.Format(PaimonMoe_UrlAchievements, LocalLang.ToLower());
                string TextMapString = Web.DownloadStringData(Url).GetAwaiter().GetResult();
                _ = Serialization.TryFromJson(TextMapString, out dynamic TextMap);
                if (TextMap == null)
                {
                    Url = string.Format(PaimonMoe_UrlAchievements, "en");
                    TextMapString = Web.DownloadStringData(Url).GetAwaiter().GetResult();
                    _ = Serialization.TryFromJson(TextMapString, out TextMap);
                    if (TextMap == null)
                    {
                        throw new Exception($"No data from {Url}");
                    }
                }

                for (int i = 0; i < 80; i++)
                {
                    string map = Serialization.ToJson(TextMap[i.ToString()]);
                    if (Serialization.TryFromJson(map, out Data data) && data != null)
                    {
                        int CategoryOrder = data.Order;
                        string Category = data.Name;
                        string CategoryIcon = string.Format("GenshinImpact\\{0}.png", CategoryOrder - 1);


                        string ach = Serialization.ToJson(data.Achievements);
                        ach = "[" + ach.Replace("[{", "{").Replace("}]", "}") + "]";
                        ach = ach.Replace("[[", "[").Replace("]]", "]");
                        if (Serialization.TryFromJson(ach, out List<GenshinAchievement> genshinAchievements))
                        {
                            genshinAchievements.ForEach(x =>
                            {
                                AllAchievements.Add(new Achievements
                                {
                                    ApiName = x.Id.ToString(),
                                    Name = x.Name,
                                    Description = x.Desc,
                                    UrlUnlocked = "GenshinImpact\\ac.png",

                                    CategoryOrder = CategoryOrder,
                                    CategoryIcon = CategoryIcon,
                                    Category = Category,

                                    GamerScore = x.Reward,

                                    DateUnlocked = default(DateTime)
                                });
                            });
                        }
                    }
                }

                gameAchievements.IsManual = true;
                gameAchievements.SourcesLink = new CommonPluginsShared.Models.SourceLink
                {
                    GameName = "Genshin Impact",
                    Name = "GitHub",
                    Url = PaimonMoe_UrlSource
                };

                /*
                string Url = string.Format(UrlTextMap, LocalLang.ToUpper());
                string TextMapString = Web.DownloadStringData(Url).GetAwaiter().GetResult();
                _ = Serialization.TryFromJson(TextMapString, out dynamic TextMap);
                if (TextMap == null)
                {
                    throw new Exception($"No data from {Url}");
                }

                string AchievementsString = Web.DownloadStringData(UrlAchievements).GetAwaiter().GetResult();
                _ = Serialization.TryFromJson(AchievementsString, out List<GenshinImpactAchievementData> GenshinImpactAchievements);
                if (GenshinImpactAchievements == null)
                {
                    throw new Exception($"No data from {UrlAchievements}");
                }

                string AchievementsCategoryString = Web.DownloadStringData(UrlAchievementsCategory).GetAwaiter().GetResult();
                _ = Serialization.TryFromJson(AchievementsCategoryString, out List<GenshinImpactAchievementsCategory> GenshinImpactAchievementsCategory);
                if (GenshinImpactAchievementsCategory == null)
                {
                    throw new Exception($"No data from {UrlAchievementsCategory}");
                }

                GenshinImpactAchievements.ForEach(x => 
                {
                    GenshinImpactAchievementsCategory giCategory = GenshinImpactAchievementsCategory.Find(y => y.Id != null && (int)y.Id == x.GoalId);
                    int CategoryOrder = giCategory?.OrderId ?? 0;
                    string Category = TextMap[giCategory?.NameTextMapHash?.ToString()]?.Value;
                    string CategoryIcon = string.Format("GenshinImpact\\ac_{0}.png", CategoryOrder);

                    AllAchievements.Add(new Achievements
                    {
                        ApiName = x.Id.ToString(),
                        Name = TextMap[x.TitleTextMapHash?.ToString()]?.Value,
                        Description = TextMap[x.DescTextMapHash?.ToString()]?.Value,
                        UrlUnlocked = "GenshinImpact\\ac.png",

                        CategoryOrder = CategoryOrder,
                        CategoryIcon = CategoryIcon,
                        Category = Category,

                        DateUnlocked = default(DateTime)
                    });

                    gameAchievements.IsManual = true;
                    gameAchievements.SourcesLink = new CommonPluginsShared.Models.SourceLink
                    {
                        GameName = "Genshin Impact",
                        Name = "GitHub",
                        Url = UrlSource
                    };
                });
                */
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            if (AllAchievements.Count > 0)
            {
                AllAchievements = AllAchievements.Where(x => !x.Name.IsNullOrEmpty()).ToList();
            }

            gameAchievements.Items = AllAchievements;
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
            return PluginDatabase.PluginSettings.Settings.EnableGenshinImpact;
        }
        #endregion
    }
}
