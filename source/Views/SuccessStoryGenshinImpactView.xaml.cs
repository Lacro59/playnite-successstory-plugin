using CommonPluginsShared;
using CommonPluginsShared.Converters;
using MoreLinq;
using Playnite.SDK;
using Playnite.SDK.Models;
using SuccessStory.Controls;
using SuccessStory.Models;
using SuccessStory.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace SuccessStory.Views
{
    /// <summary>
    /// Logique d'interaction pour SuccessStoryGenshinImpactView.xaml
    /// </summary>
    public partial class SuccessStoryGenshinImpactView : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        private SuccessStoryDatabase PluginDatabase = SuccessStory.PluginDatabase;
        private ControlDataContext ControlDataContext = new ControlDataContext();


        public SuccessStoryGenshinImpactView(Game GameContext)
        {
            InitializeComponent();
            this.DataContext = ControlDataContext;


            // Cover
            if (!GameContext.CoverImage.IsNullOrEmpty())
            {
                string CoverImage = PluginDatabase.PlayniteApi.Database.GetFullFilePath(GameContext.CoverImage);
                PART_ImageCover.Source = BitmapExtensions.BitmapFromFile(CoverImage);
            }

            GameAchievements gameAchievements = PluginDatabase.Get(GameContext, true);

            if (gameAchievements.HasData)
            {
                if (gameAchievements.EstimateTime == null || gameAchievements.EstimateTime.EstimateTimeMin == 0)
                {
                    PART_TimeToUnlockContener.Visibility = Visibility.Collapsed;
                }
                else
                {
                    PART_TimeToUnlock.Text = gameAchievements.EstimateTime.EstimateTime;
                }

                var converter = new LocalDateTimeConverter();
                PART_FirstUnlock.Text = (string)converter.Convert(gameAchievements.Items.Select(x => x.DateWhenUnlocked).Min(), null, null, CultureInfo.CurrentCulture);
                PART_LastUnlock.Text = (string)converter.Convert(gameAchievements.Items.Select(x => x.DateWhenUnlocked).Max(), null, null, CultureInfo.CurrentCulture);


                // Category
                List<CategoryAchievement> categories = gameAchievements.Items
                    .Select(x => new CategoryAchievement { CategoryIcon = x.ImageCategoryIcon, CategoryName = x.Category, CategoryOrder = x.CategoryOrder })
                    .DistinctBy(x => x.CategoryName).OrderBy(x => x.CategoryOrder).ToList();
                PART_ListCategory.ItemsSource = categories;
                PART_ListCategory.SelectedIndex = 0;
            }

            if (gameAchievements.SourcesLink != null)
            {
                PART_SourceLabel.Text = gameAchievements.SourcesLink.GameName + " (" + gameAchievements.SourcesLink.Name + ")";
                PART_SourceLink.Tag = gameAchievements.SourcesLink.Url;
            }

            ControlDataContext.GameContext = GameContext;
            ControlDataContext.Settings = PluginDatabase.PluginSettings.Settings;
        }


        private void PART_SourceLink_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (((Hyperlink)sender).Tag is string)
            {
                if (!((string)((Hyperlink)sender).Tag).IsNullOrEmpty())
                {
                    Process.Start((string)((Hyperlink)sender).Tag);
                }
            }
        }


        private void PART_ListCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                string CategoryName = ((CategoryAchievement)PART_ListCategory.SelectedItem).CategoryName;
                PluginList control = UI.FindVisualChildren<PluginList>(PART_Achievements_List_Contener).First();
                control.SetDataCategory(CategoryName);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, true);
            }
        }
    }


    public class CategoryAchievement
    {
        public string CategoryIcon { get; set; }
        public string CategoryName { get; set; }
        public int CategoryOrder { get; set; }
    }
}
