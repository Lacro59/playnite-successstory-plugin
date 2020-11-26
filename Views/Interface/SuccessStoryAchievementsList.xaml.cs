using Playnite.SDK;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Globalization;
using PluginCommon;
using SuccessStory.Services;
using Newtonsoft.Json;
using System.Windows.Threading;
using System.Threading;

namespace SuccessStory.Views.Interface
{
    /// <summary>
    /// Logique d'interaction pour SuccessStoryAchievementsList.xaml
    /// </summary>
    public partial class SuccessStoryAchievementsList : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private SuccessStoryDatabase PluginDatabase = SuccessStory.PluginDatabase;


        public SuccessStoryAchievementsList()
        {
            InitializeComponent();

            PluginDatabase.PropertyChanged += OnPropertyChanged;
        }


        protected void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
#if DEBUG
                logger.Debug($"SuccessStoryAchievementsList.OnPropertyChanged({e.PropertyName}): {JsonConvert.SerializeObject(PluginDatabase.GameSelectedData)}");
#endif
                if (e.PropertyName == "GameSelectedData" || e.PropertyName == "PluginSettings")
                {
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                    {
                        SetScData(PluginDatabase.GameSelectedData.Items);
                    }));
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SuccessStory");
            }
        }


        public void SetScData(List<Achievements> ListAchievements)
        {
            List<ListBoxAchievements> ListBoxAchievements = new List<ListBoxAchievements>();

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

                // Achievement without unlocktime but achieved = 1
                if (dateUnlock == new DateTime(1982, 12, 15, 0, 0, 0, 0))
                {
                    dateUnlock = null;
                }

                ListBoxAchievements.Add(new ListBoxAchievements()
                {
                    Name = NameAchievement,
                    DateUnlock = dateUnlock,
                    EnableRaretyIndicator = PluginDatabase.PluginSettings.EnableRaretyIndicator,
                    Icon = urlImg,
                    IconImage = urlImg,
                    IsGray = IsGray,
                    Description = ListAchievements[i].Description,
                    Percent = ListAchievements[i].Percent
                });

                iconImage = null;
            }


            // Sorting default.
            lbAchievements.ItemsSource = ListBoxAchievements;
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lbAchievements.ItemsSource);
            view.SortDescriptions.Add(new SortDescription("DateUnlock", ListSortDirection.Descending));
        }


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


        /// <summary>
        /// Resize ListBox on parent.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LbAchievements_Loaded(object sender, RoutedEventArgs e)
        {
            IntegrationUI.SetControlSize((FrameworkElement)sender);
        }
    }

    public class SetColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Color color = Brushes.Transparent.Color;

            if ((float)value > 30)
            {
                return null;
            }

            if ((float)value <= 30)
            {
                color = Brushes.DarkGray.Color;
            }
            if ((float)value <= 10)
            {
                color = Brushes.Gold.Color;
            }

            Color newColor = new Color();
            newColor.ScR = (float)color.R / 255;
            newColor.ScG = (float)color.G / 255;
            newColor.ScB = (float)color.B / 255;

            return newColor;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
