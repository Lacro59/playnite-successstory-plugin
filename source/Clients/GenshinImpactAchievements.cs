using System;
using System.Collections.Generic;
using System.Linq;
using CommonPluginsShared;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using SuccessStory.Models;

namespace SuccessStory.Clients
{
    class GenshinImpactAchievements : GenericAchievements
    {
        private static string UrlTextMap                => @"https://raw.githubusercontent.com/theBowja/GenshinData-1/master/TextMap/TextMap{0}.json";
        private static string UrlAchievementsCategory   => @"https://raw.githubusercontent.com/theBowja/GenshinData-1/master/ExcelBinOutput/AchievementGoalExcelConfigData.json";
        private static string UrlAchievements           => @"https://raw.githubusercontent.com/theBowja/GenshinData-1/master/ExcelBinOutput/AchievementExcelConfigData.json";

        public GenshinImpactAchievements() : base("GenshinImpact", CodeLang.GetGenshinLang(PluginDatabase.PlayniteApi.ApplicationSettings.Language))
        {

        }


        public override GameAchievements GetAchievements(Game game)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Achievements> AllAchievements = new List<Achievements>();

            try
            {
                string Url = string.Format(UrlTextMap, LocalLang.ToUpper());
                string TextMapString = Web.DownloadStringData(Url).GetAwaiter().GetResult();
                Serialization.TryFromJson(TextMapString, out dynamic TextMap);
                if (TextMap == null)
                {
                    throw new Exception($"No data from {Url}");
                }

                string AchievementsString = Web.DownloadStringData(UrlAchievements).GetAwaiter().GetResult();
                Serialization.TryFromJson(AchievementsString, out List<GenshinImpactAchievementData> GenshinImpactAchievements);
                if (GenshinImpactAchievements == null)
                {
                    throw new Exception($"No data from {UrlAchievements}");
                }

                string AchievementsCategoryString = Web.DownloadStringData(UrlAchievementsCategory).GetAwaiter().GetResult();
                Serialization.TryFromJson(AchievementsCategoryString, out List<GenshinImpactAchievementsCategory> GenshinImpactAchievementsCategory);
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
                        Url = "https://github.com/theBowja/GenshinData-1"
                    };
                });
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
