using CommonPluginsControls.LiveChartsCommon;
using CommonPluginsShared;
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
using System.Windows.Shapes;
using System.Windows.Threading;

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

        private PluginChartDataContext ControlDataContext;
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
            

            ControlDataContext = new PluginChartDataContext
            {
                IsActivated = IsActivated,
                ChartHeight = ChartHeight,
                EnableAxisLabel = EnableAxisLabel,
                EnableOrdinatesLabel = EnableOrdinatesLabel,
                CountAbscissa = CountAbscissa,

                Series = null,
                Labels = null,

                LabelsRotation = LabelsRotation
            };
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
                AchievementsGraphicsDataCount GraphicsData = PluginDatabase.GetCountByDay(newContext.Id, (ControlDataContext.CountAbscissa - 1));

                this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                {
                    string[] StatsGraphicsAchievementsLabels = GraphicsData.Labels;
                    SeriesCollection StatsGraphicAchievementsSeries = new SeriesCollection();
                    StatsGraphicAchievementsSeries.Add(new LineSeries
                    {
                        Title = string.Empty,
                        Values = GraphicsData.Series
                    });

                    ControlDataContext.Series = StatsGraphicAchievementsSeries;
                    ControlDataContext.Labels = StatsGraphicsAchievementsLabels;

                    this.DataContext = null;
                    this.DataContext = ControlDataContext;
                }));

                return true;
            });
        }
    }


    public class PluginChartDataContext : IDataContext
    {
        public bool IsActivated { get; set; }
        public double ChartHeight { get; set; }
        public bool EnableAxisLabel { get; set; }
        public bool EnableOrdinatesLabel { get; set; }
        public int CountAbscissa { get; set; }

        public SeriesCollection Series { get; set; }
        public IList<string> Labels { get; set; }

        public int LabelsRotation { get; set; }
    }
}
