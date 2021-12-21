using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Interfaces;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using SuccessStory.Models;
using SuccessStory.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SuccessStory.Controls
{
    /// <summary>
    /// Logique d'interaction pour PluginList.xaml
    /// </summary>
    public partial class PluginList : PluginUserControlExtend
    {
        private SuccessStoryDatabase PluginDatabase = SuccessStory.PluginDatabase;
        internal override IPluginDatabase _PluginDatabase
        {
            get
            {
                return PluginDatabase;
            }
            set
            {
                PluginDatabase = (SuccessStoryDatabase)_PluginDatabase;
            }
        }

        private PluginListDataContext ControlDataContext = new PluginListDataContext();
        internal override IDataContext _ControlDataContext
        {
            get
            {
                return ControlDataContext;
            }
            set
            {
                ControlDataContext = (PluginListDataContext)_ControlDataContext;
            }
        }

        private string NameAsc = "\uea64";
        private string NameDesc = "\uea67";
        private string CalAsc = "\uea65";
        private string CalDesc = "\uea66";
        private string RarityAsc = "\uea68";
        private string RarityDesc = "\uea69";

        private int NameIndex = 1;
        private int CalIndex = 2;
        private int RarityIndex = 3;

        private OrderAchievement orderAchievement;


        #region Properties
        public static readonly DependencyProperty ForceOneColProperty;
        public bool ForceOneCol { get; set; } = false;

        public static readonly DependencyProperty DisplayFilterProperty;
        public bool DisplayFilter { get; set; } = false;
        #endregion


        public PluginList()
        {
            InitializeComponent();
            this.DataContext = ControlDataContext;


            orderAchievement = Serialization.GetClone(PluginDatabase.PluginSettings.Settings.IntegrationListOrderAchievement);
            PART_SortGroupBy.IsChecked = orderAchievement.OrderGroupByUnlocked;

            switch (orderAchievement.OrderAchievementTypeFirst)
            {
                case (OrderAchievementType.AchievementName):
                    NameIndex = 1;
                    PART_SortNameOrder.Content = NameIndex;

                    if (orderAchievement.OrderTypeFirst == OrderType.Ascending)
                    {
                        PART_SortName.Content = NameAsc;
                    }
                    else
                    {
                        PART_SortName.Content = NameDesc;
                    }
                    break;

                case (OrderAchievementType.AchievementDateUnlocked):
                    CalIndex = 1;
                    PART_SortCalOrder.Content = CalIndex;

                    if (orderAchievement.OrderTypeFirst == OrderType.Ascending)
                    {
                        PART_SortCal.Content = CalAsc;
                    }
                    else
                    {
                        PART_SortCal.Content = CalDesc;
                    }
                    break;

                case (OrderAchievementType.AchievementRarety):
                    RarityIndex = 1;
                    PART_SortRarityOrder.Content = RarityIndex;

                    if (orderAchievement.OrderTypeSecond == OrderType.Ascending)
                    {
                        PART_SortRarity.Content = RarityAsc;
                    }
                    else
                    {
                        PART_SortRarity.Content = RarityDesc;
                    }
                    break;
            }

            switch (orderAchievement.OrderAchievementTypeSecond)
            {
                case (OrderAchievementType.AchievementName):
                    NameIndex = 2;
                    PART_SortNameOrder.Content = NameIndex;

                    if (orderAchievement.OrderTypeFirst == OrderType.Ascending)
                    {
                        PART_SortName.Content = NameAsc;
                    }
                    else
                    {
                        PART_SortName.Content = NameDesc;
                    }
                    break;

                case (OrderAchievementType.AchievementDateUnlocked):
                    CalIndex = 2;
                    PART_SortCalOrder.Content = CalIndex;

                    if (orderAchievement.OrderTypeFirst == OrderType.Ascending)
                    {
                        PART_SortCal.Content = CalAsc;
                    }
                    else
                    {
                        PART_SortCal.Content = CalDesc;
                    }
                    break;

                case (OrderAchievementType.AchievementRarety):
                    RarityIndex = 2;
                    PART_SortRarityOrder.Content = RarityIndex;                

                    if (orderAchievement.OrderTypeSecond == OrderType.Ascending)
                    {
                        PART_SortRarity.Content = RarityAsc;
                    }
                    else
                    {
                        PART_SortRarity.Content = RarityDesc;
                    }
                    break;
            }

            switch (orderAchievement.OrderAchievementTypeThird)
            {
                case (OrderAchievementType.AchievementName):
                    NameIndex = 3;
                    PART_SortNameOrder.Content = NameIndex;

                    if (orderAchievement.OrderTypeFirst == OrderType.Ascending)
                    {
                        PART_SortName.Content = NameAsc;
                    }
                    else
                    {
                        PART_SortName.Content = NameDesc;
                    }
                    break;

                case (OrderAchievementType.AchievementDateUnlocked):
                    CalIndex = 3;
                    PART_SortCalOrder.Content = CalIndex;

                    if (orderAchievement.OrderTypeFirst == OrderType.Ascending)
                    {
                        PART_SortCal.Content = CalAsc;
                    }
                    else
                    {
                        PART_SortCal.Content = CalDesc;
                    }
                    break;

                case (OrderAchievementType.AchievementRarety):
                    RarityIndex = 3;
                    PART_SortRarityOrder.Content = RarityIndex;

                    if (orderAchievement.OrderTypeSecond == OrderType.Ascending)
                    {
                        PART_SortRarity.Content = RarityAsc;
                    }
                    else
                    {
                        PART_SortRarity.Content = RarityDesc;
                    }
                    break;
            }



            Task.Run(() =>
            {
                // Wait extension database are loaded
                System.Threading.SpinWait.SpinUntil(() => PluginDatabase.IsLoaded, -1);

                this.Dispatcher?.BeginInvoke((Action)delegate
                {
                    PluginDatabase.PluginSettings.PropertyChanged += PluginSettings_PropertyChanged;
                    PluginDatabase.Database.ItemUpdated += Database_ItemUpdated;
                    PluginDatabase.Database.ItemCollectionChanged += Database_ItemCollectionChanged;
                    PluginDatabase.PlayniteApi.Database.Games.ItemUpdated += Games_ItemUpdated;

                    // Apply settings
                    PluginSettings_PropertyChanged(null, null);
                });
            });
        }


        public override void SetDefaultDataContext()
        {
            bool IsActivated = PluginDatabase.PluginSettings.Settings.EnableIntegrationList;
            double Height = PluginDatabase.PluginSettings.Settings.IntegrationListHeight;
            if (IgnoreSettings)
            {
                IsActivated = true;
                Height = double.NaN;
            }

            int ColDefinied = PluginDatabase.PluginSettings.Settings.IntegrationListColCount;
            if (ForceOneCol)
            {
                ColDefinied = 1;
            }


            ControlDataContext.IsActivated = IsActivated;
            ControlDataContext.Height = Height;

            ControlDataContext.ItemSize = new Size(300, 65);
            ControlDataContext.ColDefinied = ColDefinied;

            ControlDataContext.ItemsSource = new ObservableCollection<Achievements>();


            LbAchievements_SizeChanged(null, null);
        }


        public override void SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
        {
            GameAchievements gameAchievements = (GameAchievements)PluginGameData;
            gameAchievements.orderAchievement = PluginDatabase.PluginSettings.Settings.IntegrationListOrderAchievement;
            ControlDataContext.ItemsSource = gameAchievements.OrderItems;
        }


        #region Events
        /// <summary>
        /// Show or not the ToolTip.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBlock_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            string Text = ((TextBlock)sender).Text;
            TextBlock textBlock = (TextBlock)sender;

            Typeface typeface = new Typeface(
                textBlock.FontFamily,
                textBlock.FontStyle,
                textBlock.FontWeight,
                textBlock.FontStretch);

            FormattedText formattedText = new FormattedText(
                textBlock.Text,
                System.Threading.Thread.CurrentThread.CurrentCulture,
                textBlock.FlowDirection,
                typeface,
                textBlock.FontSize,
                textBlock.Foreground,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            if (formattedText.Width > textBlock.DesiredSize.Width)
            {
                ((ToolTip)((TextBlock)sender).ToolTip).Visibility = Visibility.Visible;
            }
            else
            {
                ((ToolTip)((TextBlock)sender).ToolTip).Visibility = Visibility.Hidden;
            }
        }

        private void LbAchievements_SizeChanged(object sender, SizeChangedEventArgs e)
        {           
            if (ControlDataContext != null)
            {
                double Width = lbAchievements.ActualWidth / ControlDataContext.ColDefinied;
                Width = Width > 10 ? Width : 11;

                ControlDataContext.ItemSize = new Size(Width - 10, 65);
                this.DataContext = null;
                this.DataContext = ControlDataContext;
            }
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

            SetOrder();
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

            SetOrder();
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

            SetOrder();
        }

        private void PART_SortGroupBy_Checked(object sender, RoutedEventArgs e)
        {
            SetOrder();
        }

        private void PART_SortGroupBy_Unchecked(object sender, RoutedEventArgs e)
        {
            SetOrder();
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


        private void SetOrder()
        {
            orderAchievement.OrderGroupByUnlocked = (bool)PART_SortGroupBy.IsChecked;

            switch(NameIndex)
            {
                case 1:
                    orderAchievement.OrderAchievementTypeFirst = OrderAchievementType.AchievementName;
                    if (PART_SortName.Content.ToString() == NameAsc)
                    {
                        orderAchievement.OrderTypeFirst = OrderType.Ascending;
                    }
                    else
                    {
                        orderAchievement.OrderTypeFirst = OrderType.Descending;
                    }
                    break;
                case 2:
                    orderAchievement.OrderAchievementTypeSecond = OrderAchievementType.AchievementName;
                    if (PART_SortName.Content.ToString() == NameAsc)
                    {
                        orderAchievement.OrderTypeSecond = OrderType.Ascending;
                    }
                    else
                    {
                        orderAchievement.OrderTypeSecond = OrderType.Descending;
                    }
                    break;
                case 3:
                    orderAchievement.OrderAchievementTypeThird = OrderAchievementType.AchievementName;
                    if (PART_SortName.Content.ToString() == NameAsc)
                    {
                        orderAchievement.OrderTypeThird = OrderType.Ascending;
                    }
                    else
                    {
                        orderAchievement.OrderTypeThird = OrderType.Descending;
                    }
                    break;
            }

            switch (CalIndex)
            {
                case 1:
                    orderAchievement.OrderAchievementTypeFirst = OrderAchievementType.AchievementDateUnlocked;
                    if (PART_SortCal.Content.ToString() == CalAsc)
                    {
                        orderAchievement.OrderTypeFirst = OrderType.Ascending;
                    }
                    else
                    {
                        orderAchievement.OrderTypeFirst = OrderType.Descending;
                    }
                    break;
                case 2:
                    orderAchievement.OrderAchievementTypeSecond = OrderAchievementType.AchievementDateUnlocked;
                    if (PART_SortCal.Content.ToString() == CalAsc)
                    {
                        orderAchievement.OrderTypeSecond = OrderType.Ascending;
                    }
                    else
                    {
                        orderAchievement.OrderTypeSecond = OrderType.Descending;
                    }
                    break;
                case 3:
                    orderAchievement.OrderAchievementTypeThird = OrderAchievementType.AchievementDateUnlocked;
                    if (PART_SortCal.Content.ToString() == CalAsc)
                    {
                        orderAchievement.OrderTypeThird = OrderType.Ascending;
                    }
                    else
                    {
                        orderAchievement.OrderTypeThird = OrderType.Descending;
                    }
                    break;
            }

            switch (RarityIndex)
            {
                case 1:
                    orderAchievement.OrderAchievementTypeFirst = OrderAchievementType.AchievementRarety;
                    if (PART_SortRarity.Content.ToString() == RarityAsc)
                    {
                        orderAchievement.OrderTypeFirst = OrderType.Ascending;
                    }
                    else
                    {
                        orderAchievement.OrderTypeFirst = OrderType.Descending;
                    }
                    break;
                case 2:
                    orderAchievement.OrderAchievementTypeSecond = OrderAchievementType.AchievementRarety;
                    if (PART_SortRarity.Content.ToString() == RarityAsc)
                    {
                        orderAchievement.OrderTypeSecond = OrderType.Ascending;
                    }
                    else
                    {
                        orderAchievement.OrderTypeSecond = OrderType.Descending;
                    }
                    break;
                case 3:
                    orderAchievement.OrderAchievementTypeThird = OrderAchievementType.AchievementRarety;
                    if (PART_SortRarity.Content.ToString() == RarityAsc)
                    {
                        orderAchievement.OrderTypeThird = OrderType.Ascending;
                    }
                    else
                    {
                        orderAchievement.OrderTypeThird = OrderType.Descending;
                    }
                    break;
            }


            GameAchievements gameAchievements = PluginDatabase.Get(GameContext);
            gameAchievements.orderAchievement = orderAchievement;
            ControlDataContext.ItemsSource = gameAchievements.OrderItems;
        }
        #endregion
    }


    public class PluginListDataContext : ObservableObject, IDataContext
    {
        private bool _IsActivated;
        public bool IsActivated { get => _IsActivated; set => SetValue(ref _IsActivated, value); }

        private double _Height;
        public double Height { get => _Height; set => SetValue(ref _Height, value); }

        private Size _ItemSize;
        public Size ItemSize { get => _ItemSize; set => SetValue(ref _ItemSize, value); }

        private int _ColDefinied;
        public int ColDefinied { get => _ColDefinied; set => SetValue(ref _ColDefinied, value); }

        private ObservableCollection<Achievements> _ItemsSource;
        public ObservableCollection<Achievements> ItemsSource { get => _ItemsSource; set => SetValue(ref _ItemsSource, value); }
    }
}
