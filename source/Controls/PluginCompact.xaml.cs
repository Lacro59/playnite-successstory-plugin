using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Interfaces;
using Playnite.SDK.Models;
using SuccessStory.Models;
using SuccessStory.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SuccessStory.Controls.Customs;
using CommonPluginsShared.Converters;
using System.Globalization;
using CommonPluginsShared.Extensions;
using Playnite.SDK;
using System.Windows.Media.Effects;

namespace SuccessStory.Controls
{
    /// <summary>
    /// Logique d'interaction pour PluginCompact.xaml
    /// </summary>
    public partial class PluginCompact : PluginUserControlExtend
    {
        private SuccessStoryDatabase PluginDatabase => SuccessStory.PluginDatabase;
        internal override IPluginDatabase pluginDatabase => PluginDatabase;

        private PluginCompactDataContext ControlDataContext = new PluginCompactDataContext();
        internal override IDataContext controlDataContext
        {
            get => ControlDataContext;
            set => ControlDataContext = (PluginCompactDataContext)controlDataContext;
        }


        #region Properties
        public bool IsUnlocked
        {
            get => (bool)GetValue(IsUnlockedProperty);
            set => SetValue(IsUnlockedProperty, value);
        }

        public static readonly DependencyProperty IsUnlockedProperty = DependencyProperty.Register(
            nameof(IsUnlocked),
            typeof(bool),
            typeof(PluginCompact),
            new FrameworkPropertyMetadata(false, ControlsPropertyChangedCallback));
        #endregion


        public PluginCompact()
        {
            InitializeComponent();
            DataContext = ControlDataContext;

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
            bool IsActivated = PluginDatabase.PluginSettings.Settings.EnableIntegrationCompactLocked;
            if (IsUnlocked)
            {
                IsActivated = PluginDatabase.PluginSettings.Settings.EnableIntegrationCompactUnlocked;
            }

            ControlDataContext.IsActivated = IsActivated;
            ControlDataContext.ShowHiddenIcon = PluginDatabase.PluginSettings.Settings.ShowHiddenIcon;
            ControlDataContext.DisplayLastest = PluginDatabase.PluginSettings.Settings.IntegrationCompactPartialDisplayLastest;
            ControlDataContext.OneLine = PluginDatabase.PluginSettings.Settings.IntegrationCompactPartialDisplayLastestOneLine;
            ControlDataContext.Height = PluginDatabase.PluginSettings.Settings.IntegrationCompactPartialHeight;

            ControlDataContext.ItemsSource = new ObservableCollection<Achievements>();
            ControlDataContext.LastestAchievement = new Achievements();


            // With PlayerActivities
            if (Tag is DateTime)
            {
                ControlDataContext.DisplayLastest = false;
                ControlDataContext.Height = 48;
            }
        }


        public override void SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
        {
            if (!IsUnlocked)
            {
                ControlDataContext.DisplayLastest = false;
            }

            PART_DisplayLastest.Visibility = ControlDataContext.DisplayLastest ? Visibility.Visible : Visibility.Collapsed;

            PART_ScCompactView.Children.Clear();
            PART_ScCompactView.ColumnDefinitions.Clear();

            GameAchievements gameAchievements = (GameAchievements)PluginGameData;
            List<Achievements> ListAchievements;

            // Select data
            if (IsUnlocked)
            {
                gameAchievements.orderAchievement = PluginDatabase.PluginSettings.Settings.IntegrationCompactUnlockedOrderAchievement;
                ListAchievements = gameAchievements.OrderItemsOnlyUnlocked.ToList();
            }
            else
            {
                gameAchievements.orderAchievement = PluginDatabase.PluginSettings.Settings.IntegrationCompactLockedOrderAchievement;
                ListAchievements = gameAchievements.OrderItemsOnlyLocked.ToList();
            }

            if (ListAchievements.Count == 0)
            {
                MustDisplay = false;
                return;
            }

            PART_AchievementImage.Children.Clear();
            if (IsUnlocked && ListAchievements.Count > 0 && ControlDataContext.DisplayLastest)
            {
                ControlDataContext.LastestAchievement = ListAchievements.FirstOrDefault(x => x.DateWhenUnlocked == ListAchievements.Max(y => y.DateWhenUnlocked));

                int index = ListAchievements.FindIndex(x => x == ControlDataContext.LastestAchievement);
                ListAchievements.RemoveAt(index);

                AchievementImage achievementImage = new AchievementImage
                {
                    Width = ControlDataContext.Height,
                    Height = ControlDataContext.Height,
                    IsGray = false,
                    Icon = ControlDataContext.LastestAchievement.Icon,
                    Percent = ControlDataContext.LastestAchievement.Percent,
                    EnableRaretyIndicator = ControlDataContext.LastestAchievement.EnableRaretyIndicator,
                    DisplayRaretyValue = ControlDataContext.LastestAchievement.DisplayRaretyValue
                };

                _ = PART_AchievementImage.Children.Add(achievementImage);

                PART_LastestAchievementName.Text = ControlDataContext.LastestAchievement.Name;
                PART_LastestAchievementNameToolTip.Content = ControlDataContext.LastestAchievement.Name;
                PART_LastestAchievementDescription.Text = ControlDataContext.LastestAchievement.Description;

                LocalDateTimeConverter localDateTimeConverter = new LocalDateTimeConverter();
                PART_LastestAchievemenDateWhenUnlocked.Text = (string)localDateTimeConverter.Convert(ControlDataContext.LastestAchievement.DateWhenUnlocked, null, null, CultureInfo.CurrentCulture);
            }

            ControlDataContext.ItemsSource = ListAchievements.ToObservable();

            PART_ScCompactView_IsLoaded(null, null);
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

            ((ToolTip)((TextBlock)sender).ToolTip).Visibility = formattedText.Width > textBlock.DesiredSize.Width
                ? Visibility.Visible 
                : Visibility.Hidden;
        }

        private void PART_ScCompactView_IsLoaded(object sender, RoutedEventArgs e)
        {
            if (double.IsNaN(PART_ScCompactView.ActualWidth) || PART_ScCompactView.ActualWidth == 0)
            {
                return;
            }

            if (ControlDataContext.ItemsSource == null)
            {
                return;
            }


            if (ControlDataContext.OneLine)
            {
                PART_GridAchContener.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);

                Grid.SetColumn(PART_DisplayLastest, 0);
                Grid.SetRow(PART_DisplayLastest, 2);
            }
            else
            {
                PART_GridAchContener.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Auto);

                Grid.SetColumn(PART_DisplayLastest, 1);
                Grid.SetRow(PART_DisplayLastest, 0);
            }

            PART_LineSeparator.Visibility = ControlDataContext.DisplayLastest && !ControlDataContext.OneLine 
                ? Visibility.Visible 
                : Visibility.Collapsed;


            ObservableCollection<Achievements> AchievementsList = ControlDataContext.ItemsSource;

            // With PlayerActivities
            if (Tag is DateTime)
            {
                AchievementsList = AchievementsList
                    .Where(x => x.DateWhenUnlocked?.ToString("yyyy-MM-dd").IsEqual(((DateTime)Tag).ToString("yyyy-MM-dd")) ?? false)
                    .ToObservable();
            }



            PART_ScCompactView.Children.Clear();
            PART_ScCompactView.ColumnDefinitions.Clear();

            double actualWidth = PART_ScCompactView.ActualWidth;
            int nbGrid = (int)(actualWidth / (ControlDataContext.Height + 10));

            if (nbGrid > 0)
            {
                for (int i = 0; i < nbGrid; i++)
                {
                    ColumnDefinition gridCol = new ColumnDefinition
                    {
                        Width = new GridLength(1, GridUnitType.Star)
                    };
                    PART_ScCompactView.ColumnDefinitions.Add(gridCol);

                    if (i < AchievementsList.Count)
                    {
                        if (i < nbGrid - 1)
                        {
                            AchievementImage achievementImage = new AchievementImage
                            {
                                Width = ControlDataContext.Height,
                                Height = ControlDataContext.Height,
                                ToolTip = AchievementsList[i].AchToolTipCompactPartial,
                                IsGray = AchievementsList[i].IsGray,
                                Icon = AchievementsList[i].Icon,
                                Percent = AchievementsList[i].Percent,
                                EnableRaretyIndicator = AchievementsList[i].EnableRaretyIndicator,
                                DisplayRaretyValue = AchievementsList[i].DisplayRaretyValue,
                                IsLocked = !AchievementsList[i].IsUnlock,
                                IconCustom = AchievementsList[i].IconCustom,
                                IconText = AchievementsList[i].IconText
                            };

                            if (!AchievementsList[i].IsUnlock && AchievementsList[i].IsHidden && !PluginDatabase.PluginSettings.Settings.ShowHiddenIcon)
                            {
                                StackPanel stackPanel = new StackPanel
                                {
                                    Effect = new BlurEffect
                                    {
                                        Radius = 4,
                                        KernelType = KernelType.Box
                                    }
                                };
                                _ = stackPanel.Children.Add(achievementImage);
                                stackPanel.SetValue(Grid.ColumnProperty, i);
                                _ = PART_ScCompactView.Children.Add(stackPanel);
                            }
                            else
                            {
                                achievementImage.SetValue(Grid.ColumnProperty, i);
                                _ = PART_ScCompactView.Children.Add(achievementImage);
                            }
                        }
                        else
                        {
                            Label lb = new Label
                            {
                                FontSize = 16,
                                Content = $"+{AchievementsList.Count - i}",
                                VerticalAlignment = VerticalAlignment.Center,
                                HorizontalAlignment = HorizontalAlignment.Center
                            };
                            lb.SetValue(Grid.ColumnProperty, i);

                            _ = PART_ScCompactView.Children.Add(lb);
                        }
                    }
                }
            }
        }

        private void PART_ScCompactView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            PART_ScCompactView_IsLoaded(null, null);
        }
        #endregion


        private void Image_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            ((Image)sender).Source = new BitmapImage(new Uri(Path.Combine(PluginDatabase.Paths.PluginPath, "Resources", "default_icon.png")));
        }
    }


    public class PluginCompactDataContext : ObservableObject, IDataContext
    {
        private bool isActivated;
        public bool IsActivated { get => isActivated; set => SetValue(ref isActivated, value); }

        private bool showHiddenIcon;
        public bool ShowHiddenIcon { get => showHiddenIcon; set => SetValue(ref showHiddenIcon, value); }

        private double height;
        public double Height { get => height; set => SetValue(ref height, value); }

        private bool displayLastest;
        public bool DisplayLastest { get => displayLastest; set => SetValue(ref displayLastest, value); }

        private bool oneLine;
        public bool OneLine { get => oneLine; set => SetValue(ref oneLine, value); }

        private ObservableCollection<Achievements> itemsSource;
        public ObservableCollection<Achievements> ItemsSource { get => itemsSource; set => SetValue(ref itemsSource, value); }

        private Achievements lastestAchievement;
        public Achievements LastestAchievement { get => lastestAchievement; set => SetValue(ref lastestAchievement, value); }
    }
}
