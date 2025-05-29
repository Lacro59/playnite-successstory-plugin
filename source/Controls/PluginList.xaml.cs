using CommonPluginsControls.Controls;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Extensions;
using CommonPluginsShared.Interfaces;
using MoreLinq;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using SuccessStory.Models;
using SuccessStory.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SuccessStory.Controls
{
    /// <summary>
    /// Logique d'interaction pour PluginList.xaml
    /// </summary>
    public partial class PluginList : PluginUserControlExtend
    {
        private SuccessStoryDatabase PluginDatabase => SuccessStory.PluginDatabase;
        protected override IPluginDatabase pluginDatabase => PluginDatabase;

        private PluginListDataContext ControlDataContext = new PluginListDataContext();
        protected override IDataContext controlDataContext
        {
            get => ControlDataContext;
            set => ControlDataContext = (PluginListDataContext)controlDataContext;
        }

        private string NameAsc => "\uea64";
        private string NameDesc => "\uea67";
        private string CalAsc => "\uea65";
        private string CalDesc => "\uea66";
        private string RarityAsc => "\uea68";
        private string RarityDesc => "\uea69";

        private int NameIndex { get; set; } = 1;
        private int CalIndex { get; set; } = 2;
        private int RarityIndex { get; set; } = 3;

        private string GameName { get; set; } = string.Empty;
        private OrderAchievement OrderAchievement { get; set; }

        private string CategoryName { get; set; }


        #region Properties
        public static readonly DependencyProperty ForceOneColProperty;
        public bool ForceOneCol { get; set; } = false;

        public static readonly DependencyProperty UsedCategoryProperty;
        public bool UsedCategory { get; set; } = false;

        public static readonly DependencyProperty DisplayFilterProperty;
        public bool DisplayFilter { get; set; } = false;
        #endregion


        public PluginList()
        {
            InitializeComponent();
            DataContext = ControlDataContext;

            OrderAchievement = Serialization.GetClone(PluginDatabase.PluginSettings.Settings.IntegrationListOrderAchievement);
            PART_SortGroupBy.IsChecked = OrderAchievement.OrderGroupByUnlocked;

            switch (OrderAchievement.OrderAchievementTypeFirst)
            {
                case OrderAchievementType.AchievementName:
                    NameIndex = 1;
                    PART_SortNameOrder.Content = NameIndex;
                    PART_SortName.Content = OrderAchievement.OrderTypeFirst == OrderType.Ascending ? NameAsc : NameDesc;
                    break;

                case OrderAchievementType.AchievementDateUnlocked:
                    CalIndex = 1;
                    PART_SortCalOrder.Content = CalIndex;
                    PART_SortCal.Content = OrderAchievement.OrderTypeFirst == OrderType.Ascending ? CalAsc : CalDesc;
                    break;

                case OrderAchievementType.AchievementRarety:
                    RarityIndex = 1;
                    PART_SortRarityOrder.Content = RarityIndex;
                    PART_SortRarity.Content = OrderAchievement.OrderTypeSecond == OrderType.Ascending ? RarityAsc : RarityDesc;
                    break;

                default:
                    break;
            }

            switch (OrderAchievement.OrderAchievementTypeSecond)
            {
                case OrderAchievementType.AchievementName:
                    NameIndex = 2;
                    PART_SortNameOrder.Content = NameIndex;
                    PART_SortName.Content = OrderAchievement.OrderTypeFirst == OrderType.Ascending ? NameAsc : NameDesc;
                    break;

                case OrderAchievementType.AchievementDateUnlocked:
                    CalIndex = 2;
                    PART_SortCalOrder.Content = CalIndex;
                    PART_SortCal.Content = OrderAchievement.OrderTypeFirst == OrderType.Ascending ? CalAsc : CalDesc;
                    break;

                case OrderAchievementType.AchievementRarety:
                    RarityIndex = 2;
                    PART_SortRarityOrder.Content = RarityIndex;
                    PART_SortRarity.Content = OrderAchievement.OrderTypeSecond == OrderType.Ascending ? RarityAsc : RarityDesc;
                    break;

                default:
                    break;
            }

            switch (OrderAchievement.OrderAchievementTypeThird)
            {
                case OrderAchievementType.AchievementName:
                    NameIndex = 3;
                    PART_SortNameOrder.Content = NameIndex;
                    PART_SortName.Content = OrderAchievement.OrderTypeFirst == OrderType.Ascending ? NameAsc : NameDesc;
                    break;

                case OrderAchievementType.AchievementDateUnlocked:
                    CalIndex = 3;
                    PART_SortCalOrder.Content = CalIndex;
                    PART_SortCal.Content = OrderAchievement.OrderTypeFirst == OrderType.Ascending ? CalAsc : CalDesc;
                    break;

                case OrderAchievementType.AchievementRarety:
                    RarityIndex = 3;
                    PART_SortRarityOrder.Content = RarityIndex;
                    PART_SortRarity.Content = OrderAchievement.OrderTypeSecond == OrderType.Ascending ? RarityAsc : RarityDesc;
                    break;

                default:
                    break;
            }


            _ = Task.Run(() =>
            {
                // Wait extension database are loaded
                _ = System.Threading.SpinWait.SpinUntil(() => PluginDatabase.IsLoaded, -1);

                _ = Dispatcher?.BeginInvoke((Action)delegate
                {
                    PluginDatabase.PluginSettings.PropertyChanged += PluginSettings_PropertyChanged;
                    PluginDatabase.Database.ItemUpdated += Database_ItemUpdated;
                    PluginDatabase.Database.ItemCollectionChanged += Database_ItemCollectionChanged;
                    API.Instance.Database.Games.ItemUpdated += Games_ItemUpdated;

                    // Apply settings
                    PluginSettings_PropertyChanged(null, null);
                });
            });
        }


        public override void SetDefaultDataContext()
        {
            ControlDataContext.Settings = PluginDatabase.PluginSettings.Settings;
            bool IsActivated = PluginDatabase.PluginSettings.Settings.EnableIntegrationList;

            bool ShowHiddenIcon = PluginDatabase.PluginSettings.Settings.ShowHiddenIcon;
            bool ShowHiddenTitle = PluginDatabase.PluginSettings.Settings.ShowHiddenTitle;
            bool ShowHiddenDescription = PluginDatabase.PluginSettings.Settings.ShowHiddenDescription;
            double Height = PluginDatabase.PluginSettings.Settings.IntegrationListHeight;
            double IconHeight = PluginDatabase.PluginSettings.Settings.IntegrationListIconHeight;
            if (IgnoreSettings)
            {
                IsActivated = true;
                Height = double.NaN;
                IconHeight = 48;
            }

            int ColDefinied = PluginDatabase.PluginSettings.Settings.IntegrationListColCount;
            if (ForceOneCol)
            {
                ColDefinied = 1;
            }


            ControlDataContext.IsActivated = IsActivated;
            ControlDataContext.ShowHiddenIcon = ShowHiddenIcon;
            ControlDataContext.ShowHiddenTitle = ShowHiddenTitle;
            ControlDataContext.ShowHiddenDescription = ShowHiddenDescription;
            ControlDataContext.Height = Height;
            ControlDataContext.IconHeight = IconHeight;

            ControlDataContext.ItemSize = new Size(300, 65);
            ControlDataContext.ColDefinied = ColDefinied;

            ControlDataContext.ItemsSource = new ObservableCollection<Achievement>();


            foreach (object item in PART_TabControl.Items)
            {
                ((TabItem)item).Visibility = Visibility.Collapsed;
                ((TabItem)item).Header = string.Empty;
            }


            LbAchievements_SizeChanged(null, null);
        }


        public override void SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
        {
            if (UsedCategory)
            {
                SetDataCategory(CategoryName);
                return;
            }

            GameAchievements gameAchievements = (GameAchievements)PluginGameData;
            gameAchievements.OrderAchievement = PluginDatabase.PluginSettings.Settings.IntegrationListOrderAchievement;

            if (!gameAchievements.Items.FirstOrDefault().CategoryRpcs3.IsNullOrEmpty())
            {
                List<string> Categories = gameAchievements.Items.Select(x => x.CategoryRpcs3).Distinct().ToList();
                Categories.ForEach((x, idx) =>
                {
                    ((TabItem)PART_TabControl.Items[idx]).Header = new TextBlockTrimmed { Text = x };
                    ((TabItem)PART_TabControl.Items[idx]).Visibility = Visibility.Visible;
                    ((TabItem)PART_TabControl.Items[idx]).Tag = x;
                });

                PART_TabControl.SelectedIndex = 0;
                PART_TabControl_SelectionChanged(PART_TabControl, null);
            }
            else
            {
                ControlDataContext.ItemsSource = gameAchievements.OrderItems;
            }
        }


        public void SetDataCategory(string CategoryName)
        {
            this.CategoryName = CategoryName;
            if (GameContext == null)
            {
                return;
            }

            GameAchievements gameAchievements = PluginDatabase.Get(GameContext, true);
            gameAchievements.OrderAchievement = OrderAchievement;

            ObservableCollection<Achievement> achievements = gameAchievements.OrderItems.Where(x => x.Category.IsEqual(CategoryName)).ToObservable();
            ControlDataContext.ItemsSource = achievements;
        }


        #region Events
        private void LbAchievements_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ControlDataContext != null)
            {
                double Width = lbAchievements.ActualWidth / ControlDataContext.ColDefinied;
                Width = Width > 10 ? Width : 11;

                ControlDataContext.ItemSize = new Size(Width - 10, 65);
                DataContext = null;
                DataContext = ControlDataContext;
            }
        }


        private void PART_TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                TabControl tb = sender as TabControl;
                if (tb.SelectedIndex > -1)
                {
                    TabItem ti = (TabItem)tb.Items[tb.SelectedIndex];
                    if (ti.Tag != null)
                    {
                        GameName = ti.Tag.ToString();
                        SetOrder(GameName);
                    }
                }
            }
            catch { }
        }
        #endregion


        #region Filter
        private void PART_SortName_Click(object sender, RoutedEventArgs e)
        {
            if (PART_SortName.Content.ToString() == NameAsc)
            {
                PART_SortName.Content = NameDesc;
            }
            else
            {
                ChangeIndex();
                PART_SortName.Content = NameAsc;
            }

            SetOrder(GameName);
        }

        private void PART_SortCal_Click(object sender, RoutedEventArgs e)
        {
            if (PART_SortCal.Content.ToString() == CalAsc)
            {
                PART_SortCal.Content = CalDesc;
            }
            else
            {
                ChangeIndex();
                PART_SortCal.Content = CalAsc;
            }

            SetOrder(GameName);
        }

        private void PART_SortRarity_Click(object sender, RoutedEventArgs e)
        {
            if (PART_SortRarity.Content.ToString() == RarityAsc)
            {
                PART_SortRarity.Content = RarityDesc;
            }
            else
            {
                ChangeIndex();
                PART_SortRarity.Content = RarityAsc;
            }

            SetOrder(GameName);
        }

        private void PART_SortGroupBy_Checked(object sender, RoutedEventArgs e)
        {
            SetOrder(GameName);
        }

        private void PART_SortGroupBy_Unchecked(object sender, RoutedEventArgs e)
        {
            SetOrder(GameName);
        }

    
        private void ChangeIndex()
        {
            NameIndex++;
            if (NameIndex == 4)
            {
                NameIndex = 1;
            }
            PART_SortNameOrder.Content = NameIndex;

            CalIndex++;
            if (CalIndex == 4)
            {
                CalIndex = 1;
            }
            PART_SortCalOrder.Content = CalIndex;

            RarityIndex++;
            if (RarityIndex == 4)
            {
                RarityIndex = 1;
            }
            PART_SortRarityOrder.Content = RarityIndex;
        }


        private void SetOrder(string GameName = "")
        {
            OrderAchievement.OrderGroupByUnlocked = (bool)PART_SortGroupBy.IsChecked;

            switch (NameIndex)
            {
                case 1:
                    OrderAchievement.OrderAchievementTypeFirst = OrderAchievementType.AchievementName;
                    OrderAchievement.OrderTypeFirst = PART_SortName.Content.ToString() == NameAsc ? OrderType.Ascending : OrderType.Descending;
                    break;

                case 2:
                    OrderAchievement.OrderAchievementTypeSecond = OrderAchievementType.AchievementName;
                    OrderAchievement.OrderTypeSecond = PART_SortName.Content.ToString() == NameAsc ? OrderType.Ascending : OrderType.Descending;
                    break;

                case 3:
                    OrderAchievement.OrderAchievementTypeThird = OrderAchievementType.AchievementName;
                    OrderAchievement.OrderTypeThird = PART_SortName.Content.ToString() == NameAsc ? OrderType.Ascending : OrderType.Descending;
                    break;

                default:
                    break;
            }

            switch (CalIndex)
            {
                case 1:
                    OrderAchievement.OrderAchievementTypeFirst = OrderAchievementType.AchievementDateUnlocked;
                    OrderAchievement.OrderTypeFirst = PART_SortCal.Content.ToString() == CalAsc ? OrderType.Ascending : OrderType.Descending;
                    break;

                case 2:
                    OrderAchievement.OrderAchievementTypeSecond = OrderAchievementType.AchievementDateUnlocked;
                    OrderAchievement.OrderTypeSecond = PART_SortCal.Content.ToString() == CalAsc ? OrderType.Ascending : OrderType.Descending;
                    break;

                case 3:
                    OrderAchievement.OrderAchievementTypeThird = OrderAchievementType.AchievementDateUnlocked;
                    OrderAchievement.OrderTypeThird = PART_SortCal.Content.ToString() == CalAsc ? OrderType.Ascending : OrderType.Descending;
                    break;

                default:
                    break;
            }

            switch (RarityIndex)
            {
                case 1:
                    OrderAchievement.OrderAchievementTypeFirst = OrderAchievementType.AchievementRarety;
                    OrderAchievement.OrderTypeFirst = PART_SortRarity.Content.ToString() == RarityAsc ? OrderType.Ascending : OrderType.Descending;
                    break;

                case 2:
                    OrderAchievement.OrderAchievementTypeSecond = OrderAchievementType.AchievementRarety;
                    OrderAchievement.OrderTypeSecond = PART_SortRarity.Content.ToString() == RarityAsc ? OrderType.Ascending : OrderType.Descending;
                    break;

                case 3:
                    OrderAchievement.OrderAchievementTypeThird = OrderAchievementType.AchievementRarety;
                    OrderAchievement.OrderTypeThird = PART_SortRarity.Content.ToString() == RarityAsc ? OrderType.Ascending : OrderType.Descending;
                    break;

                default:
                    break;
            }

            if (GameContext != null)
            {
                GameAchievements gameAchievements = PluginDatabase.Get(GameContext, true);
                gameAchievements.OrderAchievement = OrderAchievement;

                ObservableCollection<Achievement> achievements = gameAchievements.OrderItems;
                if (!CategoryName.IsNullOrEmpty())
                {
                    achievements = achievements.Where(x => x.Category.IsEqual(CategoryName)).ToObservable();
                }

                ControlDataContext.ItemsSource = GameName.IsNullOrEmpty()
                    ? achievements
                    : achievements.Where(x => x.CategoryRpcs3.IsEqual(GameName)).ToObservable();
            }
        }
        #endregion
    }


    public class PluginListDataContext : ObservableObject, IDataContext
    {
        private SuccessStorySettings settings;
        public SuccessStorySettings Settings { get => settings; set => SetValue(ref settings, value); }

        private bool isActivated;
        public bool IsActivated { get => isActivated; set => SetValue(ref isActivated, value); }

        private bool showHiddenIcon;
        public bool ShowHiddenIcon { get => showHiddenIcon; set => SetValue(ref showHiddenIcon, value); }

        private bool showHiddenTitle;
        public bool ShowHiddenTitle { get => showHiddenTitle; set => SetValue(ref showHiddenTitle, value); }

        private bool showHiddenDescription;
        public bool ShowHiddenDescription { get => showHiddenDescription; set => SetValue(ref showHiddenDescription, value); }

        private double height;
        public double Height { get => height; set => SetValue(ref height, value); }

        private double iconHeight;
        public double IconHeight { get => iconHeight; set => SetValue(ref iconHeight, value); }

        private Size itemSize;
        public Size ItemSize { get => itemSize; set => SetValue(ref itemSize, value); }

        private int colDefinied;
        public int ColDefinied { get => colDefinied; set => SetValue(ref colDefinied, value); }

        private ObservableCollection<Achievement> itemsSource;
        public ObservableCollection<Achievement> ItemsSource { get => itemsSource; set => SetValue(ref itemsSource, value); }
    }
}
