using CommonPluginsShared;
using CommonPluginsShared.Converters;
using CommonPluginsShared.Extensions;
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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace SuccessStory.Views
{
    /// <summary>
    /// Logique d'interaction pour SuccessStoryCategoryView.xaml
    /// </summary>
    public partial class SuccessStoryCategoryView : UserControl
    {
        private SuccessStoryDatabase PluginDatabase => SuccessStory.PluginDatabase;
        private ControlDataContext ControlDataContext { get; set; } = new ControlDataContext();


        public SuccessStoryCategoryView(Game game)
        {
            try
            {
                InitializeComponent();
                DataContext = ControlDataContext;


                // Cover
                if (!game.CoverImage.IsNullOrEmpty())
                {
                    string CoverImage = API.Instance.Database.GetFullFilePath(game.CoverImage);
                    PART_ImageCover.Source = BitmapExtensions.BitmapFromFile(CoverImage);
                }

                GameAchievements gameAchievements = PluginDatabase.Get(game, true);

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

                    LocalDateTimeConverter converter = new LocalDateTimeConverter();
                    PART_FirstUnlock.Text = (string)converter.Convert(gameAchievements.Items.Select(x => x.DateWhenUnlocked).Min(), null, null, CultureInfo.CurrentCulture);
                    PART_LastUnlock.Text = (string)converter.Convert(gameAchievements.Items.Select(x => x.DateWhenUnlocked).Max(), null, null, CultureInfo.CurrentCulture);

                    // Adjustement
                    if (game.PluginId == PlayniteTools.GetPluginId(PlayniteTools.ExternalPlugin.SteamLibrary))
                    {
                        gameAchievements?.Items?.ForEach(x =>
                        {
                            x.Category = x.Category.IsNullOrEmpty() ? ResourceProvider.GetString("LOCSuccessStoryBaseGame") : x.Category;
                        });
                    }

                    // Category
                    List<CategoryAchievement> categories = gameAchievements.Items
                        .DistinctBy(x => x.Category)
                        .Select(x => new CategoryAchievement
                        {
                            Icon100Percent = gameAchievements.Items.Where(y => y.Category.IsEqual(x.Category) && y.IsUnlock).Count() == gameAchievements.Items.Where(y => y.Category.IsEqual(x.Category)).Count() ? Path.Combine(PluginDatabase.Paths.PluginPath, "Resources\\badge.png") : string.Empty,
                            CategoryIcon = x.ImageCategoryIcon.IsNullOrEmpty() ? API.Instance.Database.GetFullFilePath(gameAchievements.Icon) : x.ImageCategoryIcon,
                            CategoryName = x.Category,
                            CategoryOrder = x.CategoryOrder,
                            Maximum = gameAchievements.Items.Where(y => y.Category.IsEqual(x.Category)).Count(),
                            Value = gameAchievements.Items.Where(y => y.Category.IsEqual(x.Category) && y.IsUnlock).Count(),
                            Progression = gameAchievements.Items.Where(y => y.Category.IsEqual(x.Category) && y.IsUnlock).Count() + " / " + gameAchievements.Items.Where(y => y.Category.IsEqual(x.Category)).Count()
                        })
                        .OrderBy(x => x.CategoryOrder).ToList();

                    PART_ListCategory.ItemsSource = game.PluginId == PlayniteTools.GetPluginId(PlayniteTools.ExternalPlugin.SteamLibrary)
                        ? categories
                        : categories.Where(x => !x.CategoryName.IsNullOrEmpty()).ToList();

                    PART_ListCategory.SelectedIndex = 0;
                }

                if (gameAchievements.SourcesLink != null)
                {
                    PART_SourceLabel.Text = gameAchievements.SourcesLink.GameName + " (" + gameAchievements.SourcesLink.Name + ")";
                    PART_SourceLink.Tag = gameAchievements.SourcesLink.Url;
                }

                if (gameAchievements.HasData)
                {
                    ControlDataContext.AchCommon = gameAchievements.Common;
                    ControlDataContext.AchNoCommon = gameAchievements.NoCommon;
                    ControlDataContext.AchRare = gameAchievements.Rare;
                    ControlDataContext.AchUltraRare = gameAchievements.UltraRare;

                    ControlDataContext.TotalGamerScore = gameAchievements.TotalGamerScore;

                    ControlDataContext.EstimateTime = gameAchievements.EstimateTime?.EstimateTime;

                    LocalDateTimeConverter converter = new LocalDateTimeConverter();
                    ControlDataContext.FirstUnlock = (string)converter.Convert(gameAchievements.Items.Select(x => x.DateWhenUnlocked).Min(), null, null, CultureInfo.CurrentCulture);
                    ControlDataContext.LastUnlock = (string)converter.Convert(gameAchievements.Items.Select(x => x.DateWhenUnlocked).Max(), null, null, CultureInfo.CurrentCulture);
                }

                ControlDataContext.GameContext = game;
                ControlDataContext.Settings = PluginDatabase.PluginSettings.Settings;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }


        private void PART_SourceLink_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (((Hyperlink)sender).Tag is string)
            {
                if (!((string)((Hyperlink)sender).Tag).IsNullOrEmpty())
                {
                    _ = Process.Start((string)((Hyperlink)sender).Tag);
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
        public string Icon100Percent { get; set; }
        public string CategoryIcon { get; set; }
        public string CategoryName { get; set; }
        public int CategoryOrder { get; set; }

        public double Value { get; set; }
        public double Maximum { get; set; }
        public string Progression { get; set; }
    }
}
