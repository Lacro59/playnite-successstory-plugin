using Newtonsoft.Json;
using Playnite.SDK;
using PluginCommon;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace SuccessStory.Views.Interface
{
    /// <summary>
    /// Logique d'interaction pour SuccessStoryAchievementsCompact.xaml
    /// </summary>
    public partial class SuccessStoryAchievementsCompact : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        List<ListBoxAchievements> AchievementsList = new List<ListBoxAchievements>();
        private bool _withUnlocked;
        private bool _EnableRaretyIndicator;


        public SuccessStoryAchievementsCompact(List<Achievements> ListAchievements, bool withUnlocked = false, bool EnableRaretyIndicator = true)
        {
            _withUnlocked = withUnlocked;
            _EnableRaretyIndicator = EnableRaretyIndicator;

            InitializeComponent();


            // Select data
            if (withUnlocked)
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

                try
                {
                    iconImage.BeginInit();
                    if (ListAchievements[i].DateUnlocked == default(DateTime) || ListAchievements[i].DateUnlocked == null)
                    {
                        if (ListAchievements[i].UrlLocked == string.Empty || ListAchievements[i].UrlLocked == ListAchievements[i].UrlUnlocked)
                        {
                            iconImage.UriSource = new Uri(ListAchievements[i].ImageUnlocked, UriKind.RelativeOrAbsolute);
                            IsGray = true;
                        }
                        else
                        {
                            iconImage.UriSource = new Uri(ListAchievements[i].ImageLocked, UriKind.RelativeOrAbsolute);
                        }
                    }
                    else
                    {
                        iconImage.UriSource = new Uri(ListAchievements[i].ImageUnlocked, UriKind.RelativeOrAbsolute);
                        dateUnlock = ListAchievements[i].DateUnlocked;
                    }
                    iconImage.EndInit();
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
                    Icon = ImageTools.ConvertBitmapImage(iconImage, (IsGray) ? ImageColor.Gray : ImageColor.None),
                    IconImage = ImageTools.ConvertBitmapImage(iconImage, ImageColor.Black),
                    Description = ListAchievements[i].Description,
                    Percent = ListAchievements[i].Percent
                });

                iconImage = null;
            }


#if DEBUG
            logger.Debug($"SuccessStory - SuccessStoryAchievementsCompact - ListAchievements({withUnlocked}) - {JsonConvert.SerializeObject(ListAchievements)}");
#endif
        }

        private void PART_ScCompactView_IsLoaded(object sender, RoutedEventArgs e)
        {
            // Prepare Grid 40x40 & add data
            double actualWidth = PART_ScCompactView.ActualWidth;
            int nbGrid = (int)actualWidth / 52;

#if DEBUG
            logger.Debug($"SuccessStory - SuccessStoryAchievementsCompact - actualWidth: {actualWidth} - nbGrid: {nbGrid}");
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
                            Image gridImage = new Image();
                            gridImage.Stretch = Stretch.UniformToFill;
                            gridImage.Width = 48;
                            gridImage.Height = 48;
                            gridImage.Source = AchievementsList[i].Icon;
                            gridImage.ToolTip = AchievementsList[i].Name;
                            gridImage.SetValue(Grid.ColumnProperty, i);

                            if (_withUnlocked)
                            {
                                var converter = new LocalDateTimeConverter();
                                converter.Convert(AchievementsList[i].DateUnlock, null, null, null);
                                gridImage.ToolTip += " (" + converter.Convert(AchievementsList[i].DateUnlock, null, null, null) +")";
                            }

                            ImageBrush imgB = new ImageBrush
                            {
                                ImageSource = AchievementsList[i].IconImage
                            };
                            gridImage.OpacityMask = imgB;

                            DropShadowEffect myDropShadowEffect = new DropShadowEffect();
                            myDropShadowEffect.ShadowDepth = 0;
                            myDropShadowEffect.BlurRadius = 30;

                            SetColorConverter setColorConverter = new SetColorConverter();
                            var color = setColorConverter.Convert(AchievementsList[i].Percent, null, null, CultureInfo.CurrentCulture);

                            if (color != null)
                            {
                                myDropShadowEffect.Color = (Color)color;
                            }

                            if (_EnableRaretyIndicator)
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

#if DEBUG
                            logger.Debug($"SuccessStory - SuccessStoryAchievementsCompact - AchievementsList.Count: {AchievementsList.Count} - nbGrid: {nbGrid} - i: {i}");
#endif
                        }
                    }
                }
            }
            else
            {
            }
        }
    }
}
