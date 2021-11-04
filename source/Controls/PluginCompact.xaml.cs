using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Interfaces;
using Playnite.SDK.Models;
using Playnite.SDK.Data;
using SuccessStory.Models;
using SuccessStory.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SuccessStory.Controls.Customs;
using CommonPluginsShared.Converters;

namespace SuccessStory.Controls
{
    /// <summary>
    /// Logique d'interaction pour PluginCompact.xaml
    /// </summary>
    public partial class PluginCompact : PluginUserControlExtend
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

        private PluginCompactDataContext ControlDataContext = new PluginCompactDataContext();
        internal override IDataContext _ControlDataContext
        {
            get
            {
                return ControlDataContext;
            }
            set
            {
                ControlDataContext = (PluginCompactDataContext)_ControlDataContext;
            }
        }


        #region Properties
        public bool IsUnlocked
        {
            get { return (bool)GetValue(IsUnlockedProperty); }
            set { SetValue(IsUnlockedProperty, value); }
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
            this.DataContext = ControlDataContext;

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
            bool IsActivated = PluginDatabase.PluginSettings.Settings.EnableIntegrationCompactLocked;
            if (IsUnlocked)
            {
                IsActivated = PluginDatabase.PluginSettings.Settings.EnableIntegrationCompactUnlocked;
            }


            ControlDataContext.IsActivated = IsActivated;
            ControlDataContext.DisplayLastest = PluginDatabase.PluginSettings.Settings.IntegrationCompactPartialDisplayLastest;
            ControlDataContext.OneLine = PluginDatabase.PluginSettings.Settings.IntegrationCompactPartialDisplayLastestOneLine;
            ControlDataContext.Height = PluginDatabase.PluginSettings.Settings.IntegrationCompactPartialHeight;

            ControlDataContext.ItemsSource = new ObservableCollection<Achievements>();
            ControlDataContext.LastestAchievement = new Achievements();
        }


        public override void SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
        {
            if (!IsUnlocked)
            {
                ControlDataContext.DisplayLastest = false;
            }

            if (ControlDataContext.DisplayLastest)
            {
                PART_DisplayLastest.Visibility = Visibility.Visible;
            }
            else
            {
                PART_DisplayLastest.Visibility = Visibility.Collapsed;
            }

            PART_ScCompactView.Children.Clear();
            PART_ScCompactView.ColumnDefinitions.Clear();

            GameAchievements gameAchievements = (GameAchievements)PluginGameData;
            List<Achievements> ListAchievements;

            // Select data
            if (IsUnlocked)
            {
                ListAchievements = gameAchievements.OrderItemsOnlyUnlocked.ToList();
            }
            else
            {
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
                ControlDataContext.LastestAchievement = ListAchievements.Where(x => x.DateUnlocked == ListAchievements.Max(y => y.DateUnlocked)).FirstOrDefault();

                int index = ListAchievements.FindIndex(x => x == ControlDataContext.LastestAchievement);
                ListAchievements.RemoveAt(index);

                AchievementImage achievementImage = new AchievementImage();
                achievementImage.Width = ControlDataContext.Height;
                achievementImage.Height = ControlDataContext.Height;
                achievementImage.IsGray = false;
                achievementImage.Icon = ControlDataContext.LastestAchievement.Icon;
                achievementImage.Percent = ControlDataContext.LastestAchievement.Percent;
                achievementImage.EnableRaretyIndicator = ControlDataContext.LastestAchievement.EnableRaretyIndicator;
                achievementImage.DispalyRaretyValue = ControlDataContext.LastestAchievement.DisplayRaretyValue;

                PART_AchievementImage.Children.Add(achievementImage);


                PART_LastestAchievementName.Text = ControlDataContext.LastestAchievement.Name;
                PART_LastestAchievementNameToolTip.Content = ControlDataContext.LastestAchievement.Name;
                PART_LastestAchievementDescription.Text = ControlDataContext.LastestAchievement.Description;

                LocalDateTimeConverter localDateTimeConverter = new LocalDateTimeConverter();
                PART_LastestAchievemenDateWhenUnlocked.Text = (string)localDateTimeConverter.Convert(ControlDataContext.LastestAchievement.DateWhenUnlocked, null, null, null);
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

            if (formattedText.Width > textBlock.DesiredSize.Width)
            {
                ((ToolTip)((TextBlock)sender).ToolTip).Visibility = Visibility.Visible;
            }
            else
            {
                ((ToolTip)((TextBlock)sender).ToolTip).Visibility = Visibility.Hidden;
            }
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

            if (ControlDataContext.DisplayLastest && !ControlDataContext.OneLine)
            {
                PART_LineSeparator.Visibility = Visibility.Visible;
            }
            else
            {
                PART_LineSeparator.Visibility = Visibility.Collapsed;
            }


            var AchievementsList = ControlDataContext.ItemsSource;

            PART_ScCompactView.Children.Clear();
            PART_ScCompactView.ColumnDefinitions.Clear();

            double actualWidth = PART_ScCompactView.ActualWidth;
            int nbGrid = (int)(actualWidth / (ControlDataContext.Height + 10));

            if (nbGrid > 0)
            {
                for (int i = 0; i < nbGrid; i++)
                {
                    ColumnDefinition gridCol = new ColumnDefinition();
                    gridCol.Width = new GridLength(1, GridUnitType.Star);
                    PART_ScCompactView.ColumnDefinitions.Add(gridCol);

                    if (i < AchievementsList.Count)
                    {
                        if (i < nbGrid - 1)
                        {
                            TextBlock tooltip = new TextBlock();
                            tooltip.Inlines.Add(new Run(AchievementsList[i].NameWithDateUnlock)
                            {
                                FontWeight = FontWeights.Bold
                            });
                            if (PluginDatabase.PluginSettings.Settings.IntegrationCompactPartialShowDescription)
                            {
                                tooltip.Inlines.Add(new LineBreak());
                                tooltip.Inlines.Add(new Run(AchievementsList[i].Description));
                            }

                            AchievementImage achievementImage = new AchievementImage();
                            achievementImage.Width = ControlDataContext.Height;
                            achievementImage.Height = ControlDataContext.Height;
                            achievementImage.ToolTip = tooltip;
                            achievementImage.SetValue(Grid.ColumnProperty, i);
                            achievementImage.IsGray = AchievementsList[i].IsGray;
                            achievementImage.Icon = AchievementsList[i].Icon;
                            achievementImage.Percent = AchievementsList[i].Percent;
                            achievementImage.EnableRaretyIndicator = AchievementsList[i].EnableRaretyIndicator;
                            achievementImage.DispalyRaretyValue = AchievementsList[i].DisplayRaretyValue;

                            PART_ScCompactView.Children.Add(achievementImage);
                        }
                        else
                        {
                            Label lb = new Label();
                            lb.FontSize = 16;
                            lb.Content = $"+{AchievementsList.Count - i}";
                            lb.VerticalAlignment = VerticalAlignment.Center;
                            lb.HorizontalAlignment = HorizontalAlignment.Center;
                            lb.SetValue(Grid.ColumnProperty, i);

                            PART_ScCompactView.Children.Add(lb);
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


    public class PluginCompactDataContext : ObservableObjectExtend, IDataContext
    {
        private bool _IsActivated;
        public bool IsActivated { get => _IsActivated; set => SetValue(ref _IsActivated, value); }

        private double _Height;
        public double Height { get => _Height; set => SetValue(ref _Height, value); }

        private bool _DisplayLastest;
        public bool DisplayLastest { get => _DisplayLastest; set => SetValue(ref _DisplayLastest, value); }

        private bool _OneLine;
        public bool OneLine { get => _OneLine; set => SetValue(ref _OneLine, value); }

        private ObservableCollection<Achievements> _ItemsSource;
        public ObservableCollection<Achievements> ItemsSource { get => _ItemsSource; set => SetValue(ref _ItemsSource, value); }

        private Achievements _LastestAchievement;
        public Achievements LastestAchievement { get => _LastestAchievement; set => SetValue(ref _LastestAchievement, value); }
    }
}
