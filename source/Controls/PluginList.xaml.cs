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


        #region Properties
        public static readonly DependencyProperty ForceOneColProperty;
        public bool ForceOneCol { get; set; } = false;
        #endregion


        public PluginList()
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

            List<Achievements> ListAchievements = Serialization.GetClone(gameAchievements.Items);
            ListAchievements = ListAchievements.OrderByDescending(x => x.DateUnlocked).ThenBy(x => x.IsUnlock).ThenBy(x => x.Name).ToList();
            ControlDataContext.ItemsSource = ListAchievements.ToObservable();
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
    }


    public class PluginListDataContext : ObservableObject, IDataContext
    {
        private bool _IsActivated { get; set; }
        public bool IsActivated
        {
            get => _IsActivated;
            set
            {
                if (value.Equals(_IsActivated) == true)
                {
                    return;
                }

                _IsActivated = value;
                OnPropertyChanged();
            }
        }

        private double _Height { get; set; }
        public double Height
        {
            get => _Height;
            set
            {
                if (value.Equals(_Height) == true)
                {
                    return;
                }

                _Height = value;
                OnPropertyChanged();
            }
        }

        private Size _ItemSize { get; set; }
        public Size ItemSize
        {
            get => _ItemSize;
            set
            {
                if (value.Equals(_ItemSize) == true)
                {
                    return;
                }

                _ItemSize = value;
                OnPropertyChanged();
            }
        }

        private int _ColDefinied { get; set; }
        public int ColDefinied
        {
            get => _ColDefinied;
            set
            {
                if (value.Equals(_ColDefinied) == true)
                {
                    return;
                }

                _ColDefinied = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<Achievements> _ItemsSource { get; set; }
        public ObservableCollection<Achievements> ItemsSource
        {
            get => _ItemsSource;
            set
            {
                if (value?.Equals(_ItemsSource) == true)
                {
                    return;
                }

                _ItemsSource = value;
                OnPropertyChanged();
            }
        }
    }
}
