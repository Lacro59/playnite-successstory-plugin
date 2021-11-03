using CommonPluginsControls.LiveChartsCommon;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Interfaces;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Wpf;
using Playnite.SDK.Models;
using SuccessStory.Models;
using SuccessStory.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace SuccessStory.Controls
{
    /// <summary>
    /// Logique d'interaction pour PluginChart.xaml
    /// </summary>
    public partial class PluginChart : PluginUserControlExtend
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

        private PluginChartDataContext ControlDataContext = new PluginChartDataContext();
        internal override IDataContext _ControlDataContext
        {
            get
            {
                return ControlDataContext;
            }
            set
            {
                ControlDataContext = (PluginChartDataContext)_ControlDataContext;
            }
        }


        #region Properties
        public bool DisableAnimations
        {
            get { return (bool)GetValue(DisableAnimationsProperty); }
            set { SetValue(DisableAnimationsProperty, value); }
        }

        public static readonly DependencyProperty DisableAnimationsProperty = DependencyProperty.Register(
            nameof(DisableAnimations),
            typeof(bool),
            typeof(PluginChart),
            new FrameworkPropertyMetadata(true, ControlsPropertyChangedCallback));


        public int LabelsRotation
        {
            get { return (int)GetValue(LabelsRotationProperty); }
            set { SetValue(LabelsRotationProperty, value); }
        }

        public static readonly DependencyProperty LabelsRotationProperty = DependencyProperty.Register(
            nameof(LabelsRotation),
            typeof(int),
            typeof(PluginChart),
            new FrameworkPropertyMetadata(0, ControlsPropertyChangedCallback));


        public static readonly DependencyProperty AxisLimitProperty;
        public int AxisLimit { get; set; } = 0;
        #endregion


        public PluginChart()
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

            //let create a mapper so LiveCharts know how to plot our CustomerViewModel class
            var customerVmMapper = Mappers.Xy<CustomerForSingle>()
                .X((value, index) => index)
                .Y(value => value.Values);

            //lets save the mapper globally
            Charting.For<CustomerForSingle>(customerVmMapper);
        }


        public override void SetDefaultDataContext()
        {
            bool IsActivated = PluginDatabase.PluginSettings.Settings.EnableIntegrationChart;
            double ChartHeight = PluginDatabase.PluginSettings.Settings.IntegrationChartHeight;
            bool EnableAxisLabel = PluginDatabase.PluginSettings.Settings.EnableIntegrationAxisChart;
            bool EnableOrdinatesLabel = PluginDatabase.PluginSettings.Settings.EnableIntegrationOrdinatesChart;
            int CountAbscissa = PluginDatabase.PluginSettings.Settings.IntegrationChartCountAbscissa;
            if (IgnoreSettings)
            {
                IsActivated = true;
                ChartHeight = double.NaN;
                EnableAxisLabel = true;
                EnableOrdinatesLabel = true;
                CountAbscissa = AxisLimit;
            }
            

            ControlDataContext.IsActivated = IsActivated;
            ControlDataContext.ChartHeight = ChartHeight;
            ControlDataContext.EnableAxisLabel = EnableAxisLabel;
            ControlDataContext.EnableOrdinatesLabel = EnableOrdinatesLabel;
            ControlDataContext.CountAbscissa = CountAbscissa;

            ControlDataContext.HideChartOptions = PluginDatabase.PluginSettings.Settings.EnableIntegrationChartHideOptions;
            ControlDataContext.AllPeriod = PluginDatabase.PluginSettings.Settings.EnableIntegrationChartAllPerdiod;
            ControlDataContext.CutPeriod = PluginDatabase.PluginSettings.Settings.EnableIntegrationChartCutPeriod;
            ControlDataContext.CutEnabled = true;

            ControlDataContext.Series = null;
            ControlDataContext.Labels = null;

            ControlDataContext.LabelsRotation = LabelsRotation;
        }


        public override void SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
        {
            GameAchievements gameAchievements = (GameAchievements)PluginGameData;

            AchievementsGraphicsDataCount GraphicsData = null;
            bool CutPeriod = ControlDataContext.AllPeriod ? ControlDataContext.CutPeriod : false;
            if (ControlDataContext.AllPeriod)
            {
                var DateMin = gameAchievements.Items.Where(x => x.IsUnlock).Select(x => x.DateUnlocked).Min();
                var DateMax = gameAchievements.Items.Where(x => x.IsUnlock).Select(x => x.DateUnlocked).Max();

                if (DateMin != null && DateMax != null)
                {
                    int limit = ((int)((DateTime)DateMax - (DateTime)DateMin).TotalDays) + 1;
                    if (limit > 30)
                    {
                        CutPeriod = true;
                        ControlDataContext.CutPeriod = true;
                        ControlDataContext.CutEnabled = false;
                    }

                    GraphicsData = PluginDatabase.GetCountByDay(newContext.Id, limit, CutPeriod);
                }
                else
                {
                    GraphicsData = PluginDatabase.GetCountByDay(newContext.Id, (ControlDataContext.CountAbscissa - 1), CutPeriod);
                }
            }
            else
            {
                GraphicsData = PluginDatabase.GetCountByDay(newContext.Id, (ControlDataContext.CountAbscissa - 1), CutPeriod);
            }


            string[] StatsGraphicsAchievementsLabels = GraphicsData.Labels;
            SeriesCollection StatsGraphicAchievementsSeries = new SeriesCollection
                    {
                        new LineSeries
                        {
                            Title = string.Empty,
                            Values = GraphicsData.Series
                        }
                    };


            ControlDataContext.Formatter = value => (value < 0) ? string.Empty : value.ToString();

            ControlDataContext.Series = StatsGraphicAchievementsSeries;
            ControlDataContext.Labels = StatsGraphicsAchievementsLabels;


            ControlDataContext.EnableAxisLabel = !(StatsGraphicsAchievementsLabels.Count() > 16 && ControlDataContext.AllPeriod);


            // TODO With OneGameView the GameContext pass at null
            if (this.GameContext == null)
            {
                this.GameContext = newContext;
            }
        }


        private void ToggleButtonAllPeriod_Click(object sender, RoutedEventArgs e)
        {
            if (GameContext != null)
            {
                ControlDataContext.AllPeriod = (bool)((ToggleButton)sender).IsChecked;
                GameAchievements gameAchievements = PluginDatabase.Get(GameContext.Id, true);
                SetData(GameContext, gameAchievements);
            }
        }

        private void ToggleButtonCut_Click(object sender, RoutedEventArgs e)
        {
            if (GameContext != null)
            {
                ControlDataContext.CutPeriod = (bool)((ToggleButton)sender).IsChecked;
                GameAchievements gameAchievements = PluginDatabase.Get(GameContext.Id, true);
                SetData(GameContext, gameAchievements);
            }
        }
    }


    public class PluginChartDataContext : ObservableObject, IDataContext
    {
        private bool _IsActivated;
        public bool IsActivated { get => _IsActivated; set => SetValue(ref _IsActivated, value); }

        private double _ChartHeight;
        public double ChartHeight { get => _ChartHeight; set => SetValue(ref _ChartHeight, value); }

        private bool _EnableAxisLabel;
        public bool EnableAxisLabel { get => _EnableAxisLabel; set => SetValue(ref _EnableAxisLabel, value); }

        private bool _EnableOrdinatesLabel;
        public bool EnableOrdinatesLabel { get => _EnableOrdinatesLabel; set => SetValue(ref _EnableOrdinatesLabel, value); }

        private int _CountAbscissa;
        public int CountAbscissa { get => _CountAbscissa; set => SetValue(ref _CountAbscissa, value); }

        private bool _HideChartOptions;
        public bool HideChartOptions { get => _HideChartOptions; set => SetValue(ref _HideChartOptions, value); }

        private bool _AllPeriod;
        public bool AllPeriod { get => _AllPeriod; set => SetValue(ref _AllPeriod, value); }

        private bool _CutPeriod;
        public bool CutPeriod { get => _CutPeriod; set => SetValue(ref _CutPeriod, value); }

        private bool _CutEnabled;
        public bool CutEnabled { get => _CutEnabled; set => SetValue(ref _CutEnabled, value); }

        private SeriesCollection _Series;
        public SeriesCollection Series { get => _Series; set => SetValue(ref _Series, value); }

        private IList<string> _Labels;
        public IList<string> Labels { get => _Labels; set => SetValue(ref _Labels, value); }

        public int _LabelsRotation;
        public int LabelsRotation { get => _LabelsRotation; set => SetValue(ref _LabelsRotation, value); }

        private Func<double, string> _Formatter;
        public Func<double, string> Formatter { get => _Formatter; set => SetValue(ref _Formatter, value); }
    }
}
