﻿using Newtonsoft.Json;
using Playnite.SDK;
using CommonPluginsShared;
using SuccessStory.Models;
using SuccessStory.Services;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows.Documents;

namespace SuccessStory.Views.Interface
{
    /// <summary>
    /// Logique d'interaction pour SuccessStoryAchievementsCompact.xaml
    /// </summary>
    public partial class SuccessStoryAchievementsCompact : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private SuccessStoryDatabase PluginDatabase = SuccessStory.PluginDatabase;

        List<ListBoxAchievements> AchievementsList = new List<ListBoxAchievements>();
        private bool _withUnlocked;


        public SuccessStoryAchievementsCompact(bool withUnlocked = false)
        {
            _withUnlocked = withUnlocked;

            InitializeComponent();

            PluginDatabase.PropertyChanged += OnPropertyChanged;
        }


        protected void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
                if (e.PropertyName == "GameIsLoaded")
                {
                    return;
                }
                if (e.PropertyName == "GameSelectedData" || e.PropertyName == "PluginSettings")
                {
                    SetScData(PluginDatabase.GameSelectedData);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SuccessStory");
            }
        }


        public void SetScData(GameAchievements gameAchievements, bool noControl = false)
        {
            List<Achievements> ListAchievements = gameAchievements.Items;

            Task.Run(() =>
            {
                AchievementsList = new List<ListBoxAchievements>();

                // Select data
                if (_withUnlocked)
                {
                    ListAchievements = ListAchievements.FindAll(x => x.DateUnlocked != default(DateTime));
                    ListAchievements.Sort((x, y) => DateTime.Compare((DateTime)x.DateUnlocked, (DateTime)y.DateUnlocked));
                    ListAchievements.Reverse();
                }
                else
                {
                    ListAchievements = ListAchievements.FindAll(x => x.DateUnlocked == default(DateTime));
                    ListAchievements.Sort((x, y) => string.Compare(x.Name, y.Name));
                }

                // Prepare data
                for (int i = 0; i < ListAchievements.Count; i++)
                {
                    DateTime? dateUnlock = null;
                    BitmapImage iconImage = new BitmapImage();

                    bool IsGray = false;

                    string urlImg = string.Empty;
                    try
                    {
                        if (ListAchievements[i].DateUnlocked == default(DateTime) || ListAchievements[i].DateUnlocked == null)
                        {
                            if (ListAchievements[i].UrlLocked == string.Empty || ListAchievements[i].UrlLocked == ListAchievements[i].UrlUnlocked)
                            {
                                urlImg = ListAchievements[i].ImageUnlocked;
                                IsGray = true;
                            }
                            else
                            {
                                urlImg = ListAchievements[i].ImageLocked;
                            }
                        }
                        else
                        {
                            urlImg = ListAchievements[i].ImageUnlocked;
                            dateUnlock = ListAchievements[i].DateUnlocked;
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, "SuccessStory", "Error on convert bitmap");
                    }

                    string NameAchievement = ListAchievements[i].Name;

                    if (dateUnlock == new DateTime(1982, 12, 15, 0, 0, 0, 0))
                    {
                        dateUnlock = null;
                    }

                    AchievementsList.Add(new ListBoxAchievements()
                    {
                        Name = NameAchievement,
                        DateUnlock = dateUnlock,
                        Icon = urlImg,
                        IconImage = urlImg,
                        IsGray = IsGray,
                        Description = ListAchievements[i].Description,
                        Percent = ListAchievements[i].Percent
                    });

                    iconImage = null;
                }

                if (_withUnlocked)
                {
                    AchievementsList = AchievementsList.OrderByDescending(x => x.DateUnlock).ThenBy(x => x.Name).ToList();
                }
                else
                {
                    AchievementsList = AchievementsList.OrderBy(x => x.Name).ToList();
                }
#if DEBUG
                logger.Debug($"SuccessStory [Ignored] - SuccessStoryAchievementsCompact - ListAchievements({_withUnlocked}) - {JsonConvert.SerializeObject(ListAchievements)}");
#endif

                this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
                {
                    if (!noControl)
                    {
                        if (gameAchievements.Id != SuccessStoryDatabase.GameSelected.Id)
                        {
                            return;
                        }
                    }

                    PART_ScCompactView_IsLoaded(null, null);
                }));
            });
        }


        private void PART_ScCompactView_IsLoaded(object sender, RoutedEventArgs e)
        {
            if (double.IsNaN(PART_ScCompactView.ActualWidth) || PART_ScCompactView.ActualWidth == 0)
            {
                return;
            }

            PART_ScCompactView.Children.Clear();
            PART_ScCompactView.ColumnDefinitions.Clear();

            // Prepare Grid 40x40 & add data
            double actualWidth = PART_ScCompactView.ActualWidth;
            int nbGrid = (int)actualWidth / 52;

#if DEBUG
            logger.Debug($"SuccessStory [Ignored] - SuccessStoryAchievementsCompact - actualWidth: {actualWidth} - nbGrid: {nbGrid} - AchievementsList: {AchievementsList.Count}");
#endif

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
                            tooltip.Inlines.Add(new Run(AchievementsList[i].Name)
                            {
                                FontWeight = FontWeights.Bold
                            });
                            tooltip.Inlines.Add(new LineBreak());
                            tooltip.Inlines.Add(new Run(AchievementsList[i].Description));

                            Image gridImage = new Image();
                            gridImage.Stretch = Stretch.UniformToFill;
                            gridImage.Width = 48;
                            gridImage.Height = 48;
                            gridImage.ToolTip = tooltip;
                            gridImage.SetValue(Grid.ColumnProperty, i);

                            if (_withUnlocked)
                            {
                                var converter = new LocalDateTimeConverter();

                                var nameRun = (Run) tooltip.Inlines.FirstInline;
                                nameRun.Text = AchievementsList[i].NameWithDateUnlock;
                            }

                            if (AchievementsList[i].IsGray)
                            {
                                if (AchievementsList[i].Icon.IsNullOrEmpty() || AchievementsList[i].IconImage.IsNullOrEmpty())
                                {
                                    logger.Warn($"SuccessStory - Empty image");
                                }
                                else
                                {
                                    var tmpImg = new BitmapImage(new Uri(AchievementsList[i].Icon, UriKind.Absolute));
                                    gridImage.Source = ImageTools.ConvertBitmapImage(tmpImg, ImageColor.Gray);

                                    ImageBrush imgB = new ImageBrush
                                    {
                                        ImageSource = new BitmapImage(new Uri(AchievementsList[i].IconImage, UriKind.Absolute))
                                    };
                                    gridImage.OpacityMask = imgB;
                                }
                            }
                            else
                            {
                                if (AchievementsList[i].Icon.IsNullOrEmpty())
                                {
                                    logger.Warn($"SuccessStory - Empty image");
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

                            if (PluginDatabase.PluginSettings.EnableRaretyIndicator)
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
            else
            {
            }
        }

        private void PART_ScCompactView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            PART_ScCompactView_IsLoaded(null, null);
        }
    }
}
