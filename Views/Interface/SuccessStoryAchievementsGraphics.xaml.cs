using LiveCharts;
using LiveCharts.Configurations;
using Playnite.SDK;
using PluginCommon;
using PluginCommon.LiveChartsCommon;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace SuccessStory.Views.Interface
{
    /// <summary>
    /// Logique d'interaction pour SuccessStoryAchievementsGraphics.xaml
    /// </summary>
    public partial class SuccessStoryAchievementsGraphics : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        public SuccessStoryAchievementsGraphics(SeriesCollection StatsGraphicAchievementsSeries, IList<string> StatsGraphicsAchievementsLabels)
        {
            InitializeComponent();

            //let create a mapper so LiveCharts know how to plot our CustomerViewModel class
            var customerVmMapper = Mappers.Xy<CustomerForSingle>()
                .X((value, index) => index)
                .Y(value => value.Values);

            //lets save the mapper globally
            Charting.For<CustomerForSingle>(customerVmMapper);

            StatsGraphicAchievements.Series = StatsGraphicAchievementsSeries;
            StatsGraphicAchievementsX.Labels = StatsGraphicsAchievementsLabels;
            //StatsGraphicAchievementsY.MinValue = -1;
        }

        private void StatsGraphicAchievements_Loaded(object sender, RoutedEventArgs e)
        {
            var parent = ((FrameworkElement)((FrameworkElement)StatsGraphicAchievements.Parent).Parent);
            StatsGraphicAchievements.Height = parent.ActualHeight +18;
        }
    }
}
