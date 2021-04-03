using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Interfaces;
using Playnite.SDK.Models;
using SuccessStory.Models;
using SuccessStory.Services;
using SuccessStory.Views.Interface;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

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

        private PluginCompactDataContext ControlDataContext;
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

            Task.Run(() =>
            {
                // Wait extension database are loaded
                System.Threading.SpinWait.SpinUntil(() => PluginDatabase.IsLoaded, -1);

                this.Dispatcher.BeginInvoke((Action)delegate
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

            ControlDataContext = new PluginCompactDataContext
            {
                IsActivated = IsActivated,
                DisplayLastest = PluginDatabase.PluginSettings.Settings.IntegrationCompactPartialDisplayLastest,
                Height = PluginDatabase.PluginSettings.Settings.IntegrationCompactPartialHeight,

                ItemsSource = new ObservableCollection<Achievements>(),
                LastestAchievement = new Achievements()
            };
        }


        public override Task<bool> SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
        {
            bool IsUnlocked = this.IsUnlocked;

            return Task.Run(() =>
            {
                if (!IsUnlocked)
                {
                    ControlDataContext.DisplayLastest = false;
                }

                this.Dispatcher.BeginInvoke(DispatcherPriority.Send, new ThreadStart(delegate
                {
                    PART_ScCompactView.Children.Clear();
                    PART_ScCompactView.ColumnDefinitions.Clear();

                    this.DataContext = null;
                    this.DataContext = ControlDataContext;
                })).Wait();

                GameAchievements gameAchievements = (GameAchievements)PluginGameData;
                List<Achievements> ListAchievements = gameAchievements.Items.GetClone();

                // Select data
                if (IsUnlocked)
                {
                    ListAchievements = ListAchievements.FindAll(x => x.DateUnlocked != default(DateTime));
                }
                else
                {
                    ListAchievements = ListAchievements.FindAll(x => x.DateUnlocked == default(DateTime));
                }

                ListAchievements = ListAchievements.OrderByDescending(x => x.DateUnlocked).ThenBy(x => x.IsUnlock).ThenBy(x => x.Name).ToList();

                if (IsUnlocked)
                {
                    ControlDataContext.LastestAchievement = ListAchievements[0];
                    ListAchievements.RemoveAt(0);
                }

                ControlDataContext.ItemsSource = ListAchievements.ToObservable();

                this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                {
                    this.DataContext = null;
                    this.DataContext = ControlDataContext;

                    PART_ScCompactView_IsLoaded(null, null);
                }));

                return true;
            });
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

            var AchievementsList = ControlDataContext.ItemsSource;

            PART_ScCompactView.Children.Clear();
            PART_ScCompactView.ColumnDefinitions.Clear();

            double actualWidth = PART_ScCompactView.ActualWidth;
            int nbGrid = (int)(actualWidth / (ControlDataContext.Height + 10));

            Common.LogDebug(true, $"actualWidth: {actualWidth} - nbGrid: {nbGrid} - AchievementsList: {AchievementsList.Count}");

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
                            Image gridImage = new Image();
                            gridImage.Stretch = Stretch.UniformToFill;
                            gridImage.Width = ControlDataContext.Height;
                            gridImage.Height = ControlDataContext.Height;
                            gridImage.ToolTip = AchievementsList[i].Name;
                            gridImage.SetValue(Grid.ColumnProperty, i);

                            if (IsUnlocked)
                            {
                                var converter = new LocalDateTimeConverter();
                                gridImage.ToolTip = AchievementsList[i].NameWithDateUnlock;
                            }

                            if (AchievementsList[i].IsGray)
                            {
                                if (AchievementsList[i].Icon.IsNullOrEmpty() || AchievementsList[i].Icon.IsNullOrEmpty())
                                {
                                    logger.Warn($"Empty image");
                                }
                                else
                                {
                                    var tmpImg = new BitmapImage(new Uri(AchievementsList[i].Icon, UriKind.Absolute));
                                    gridImage.Source = ImageTools.ConvertBitmapImage(tmpImg, ImageColor.Gray);

                                    ImageBrush imgB = new ImageBrush
                                    {
                                        ImageSource = new BitmapImage(new Uri(AchievementsList[i].Icon, UriKind.Absolute))
                                    };
                                    gridImage.OpacityMask = imgB;
                                }
                            }
                            else
                            {
                                if (AchievementsList[i].Icon.IsNullOrEmpty())
                                {
                                    logger.Warn($"Empty image");
                                }
                                else
                                {
                                    gridImage.Source = new BitmapImage(new Uri(AchievementsList[i].Icon, UriKind.Absolute));
                                }
                            }

                            DropShadowEffect myDropShadowEffect = new DropShadowEffect();
                            myDropShadowEffect.ShadowDepth = 0;
                            myDropShadowEffect.BlurRadius = 15;

                            SetColorConverter setColorConverter = new SetColorConverter();
                            var color = setColorConverter.Convert(AchievementsList[i].Percent, null, null, CultureInfo.CurrentCulture);

                            if (color != null)
                            {
                                myDropShadowEffect.Color = (Color)color;
                            }

                            if (PluginDatabase.PluginSettings.Settings.EnableRaretyIndicator)
                            {
                                gridImage.Effect = myDropShadowEffect;
                            }

                            PART_ScCompactView.Children.Add(gridImage);
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
    }


    public class PluginCompactDataContext : IDataContext
    {
        public bool IsActivated { get; set; }
        public bool DisplayLastest { get; set; }
        public double Height { get; set; }

        public ObservableCollection<Achievements> ItemsSource { get; set; }
        public Achievements LastestAchievement { get; set; }
    }
}
