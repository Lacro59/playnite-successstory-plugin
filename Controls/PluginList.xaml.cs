using CommonPluginsShared;
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
using System.Globalization;
using System.IO;
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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;

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

        private PluginListDataContext ControlDataContext;
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


        #region Properties
        public static readonly DependencyProperty ForceOneColProperty;
        public bool ForceOneCol { get; set; } = false;
        #endregion


        public PluginList()
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

            ControlDataContext = new PluginListDataContext
            {
                IsActivated = IsActivated,
                Height = Height,
                ColDefinied = ColDefinied,

                RowDefinied = 1,
                ItemsSource = new ObservableCollection<Achievements>()
            };

            LbAchievements_SizeChanged(null, null);
        }


        public override Task<bool> SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
        {
            return Task.Run(() =>
            {
                this.Dispatcher.BeginInvoke(DispatcherPriority.Send, new ThreadStart(delegate
                {
                    this.DataContext = null;
                    this.DataContext = ControlDataContext;
                })).Wait();

                GameAchievements gameAchievements = (GameAchievements)PluginGameData;

                List<Achievements> ListAchievements = Serialization.GetClone(gameAchievements.Items);
                ListAchievements = ListAchievements.OrderByDescending(x => x.DateUnlocked).ThenBy(x => x.IsUnlock).ThenBy(x => x.Name).ToList();
                ControlDataContext.ItemsSource = ListAchievements.ToObservable();

                this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                {
                    this.DataContext = null;
                    this.DataContext = ControlDataContext;
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

        private void LbAchievements_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            int RowDefinied = (int)lbAchievements.ActualHeight / 70;
            RowDefinied = (RowDefinied == 0) ? 1 : RowDefinied;
            
            if (ControlDataContext != null)
            {
                ControlDataContext.RowDefinied = RowDefinied;
                this.DataContext = null;
                this.DataContext = ControlDataContext;
            }
        }
        #endregion


        private void Image_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            ((Image)sender).Source = new BitmapImage(new Uri(Path.Combine(PluginDatabase.Paths.PluginPath, "Resources", "default_icon.png")));
        }
    }


    public class PluginListDataContext : IDataContext
    {
        public bool IsActivated { get; set; }
        public double Height { get; set; }
        public int ColDefinied { get; set; }

        public int RowDefinied { get; set; }
        public ObservableCollection<Achievements> ItemsSource { get; set; }
    }

    public class SetColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Color color = Brushes.Transparent.Color;

            if ((float)value <= 10)
            {
                return Brushes.Gold.Color;
            }
            if ((float)value <= 30)
            {
                return Brushes.DarkGray.Color;
            }
            if ((float)value > 30)
            {
                return null;
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
