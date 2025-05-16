using CommonPluginsControls.LiveChartsCommon;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Interfaces;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Wpf;
using Playnite.SDK;
using Playnite.SDK.Models;
using SuccessStory.Models;
using SuccessStory.Models.Stats;
using SuccessStory.Services;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private SuccessStoryDatabase PluginDatabase => SuccessStory.PluginDatabase;
        internal override IPluginDatabase pluginDatabase => PluginDatabase;

        private PluginChartDataContext ControlDataContext = new PluginChartDataContext();
        internal override IDataContext controlDataContext
        {
            get => ControlDataContext;
            set => ControlDataContext = (PluginChartDataContext)controlDataContext;
        }


        #region Properties
        public bool DisableAnimations
        {
            get => (bool)GetValue(DisableAnimationsProperty);
            set => SetValue(DisableAnimationsProperty, value);
        }

        public static readonly DependencyProperty DisableAnimationsProperty = DependencyProperty.Register(
            nameof(DisableAnimations),
            typeof(bool),
            typeof(PluginChart),
            new FrameworkPropertyMetadata(true, ControlsPropertyChangedCallback));


        public int LabelsRotation
        {
            get => (int)GetValue(LabelsRotationProperty);
            set => SetValue(LabelsRotationProperty, value);
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

            //let create a mapper so LiveCharts know how to plot our CustomerViewModel class
            CartesianMapper<CustomerForSingle> customerVmMapper = Mappers.Xy<CustomerForSingle>()
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

            AchGraphicsDataCount GraphicsData = null;
            bool CutPeriod = ControlDataContext.AllPeriod && ControlDataContext.CutPeriod;
            if (ControlDataContext.AllPeriod)
            {
                DateTime? DateMin = gameAchievements.Items.Where(x => x.IsUnlock).Select(x => x.DateWhenUnlocked).Min();
                DateTime? DateMax = gameAchievements.Items.Where(x => x.IsUnlock).Select(x => x.DateWhenUnlocked).Max();

                if (DateMin != null && DateMax != null)
                {
                    int limit = ((int)((DateTime)DateMax - (DateTime)DateMin).TotalDays) + 1;
                    if (limit > 30)
                    {
                        CutPeriod = true;
                        ControlDataContext.CutPeriod = true;
                        ControlDataContext.CutEnabled = false;
                    }

                    GraphicsData = SuccessStoryStats.GetCountByDay(newContext.Id, limit, CutPeriod);
                }
                else
                {
                    GraphicsData = SuccessStoryStats.GetCountByDay(newContext.Id, (ControlDataContext.CountAbscissa - 1), CutPeriod);
                }
            }
            else
            {
                GraphicsData = SuccessStoryStats.GetCountByDay(newContext.Id, (ControlDataContext.CountAbscissa - 1), CutPeriod);
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
            if (GameContext == null)
            {
                GameContext = newContext;
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
        private bool isActivated;
        public bool IsActivated { get => isActivated; set => SetValue(ref isActivated, value); }

        private double chartHeight;
        public double ChartHeight { get => chartHeight; set => SetValue(ref chartHeight, value); }

        private bool enableAxisLabel;
        public bool EnableAxisLabel { get => enableAxisLabel; set => SetValue(ref enableAxisLabel, value); }

        private bool enableOrdinatesLabel;
        public bool EnableOrdinatesLabel { get => enableOrdinatesLabel; set => SetValue(ref enableOrdinatesLabel, value); }

        private int countAbscissa;
        public int CountAbscissa { get => countAbscissa; set => SetValue(ref countAbscissa, value); }

        private bool hideChartOptions;
        public bool HideChartOptions { get => hideChartOptions; set => SetValue(ref hideChartOptions, value); }

        private bool allPeriod;
        public bool AllPeriod { get => allPeriod; set => SetValue(ref allPeriod, value); }

        private bool cutPeriod;
        public bool CutPeriod { get => cutPeriod; set => SetValue(ref cutPeriod, value); }

        private bool cutEnabled;
        public bool CutEnabled { get => cutEnabled; set => SetValue(ref cutEnabled, value); }

        private SeriesCollection series;
        public SeriesCollection Series { get => series; set => SetValue(ref series, value); }

        private IList<string> labels;
        public IList<string> Labels { get => labels; set => SetValue(ref labels, value); }

        public int labelsRotation;
        public int LabelsRotation { get => labelsRotation; set => SetValue(ref labelsRotation, value); }

        private Func<double, string> formatter;
        public Func<double, string> Formatter { get => formatter; set => SetValue(ref formatter, value); }
    }
}
