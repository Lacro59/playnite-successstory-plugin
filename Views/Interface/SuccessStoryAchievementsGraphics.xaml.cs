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

        private bool _withContener;


        public SuccessStoryAchievementsGraphics(SeriesCollection StatsGraphicAchievementsSeries, IList<string> StatsGraphicsAchievementsLabels, SuccessStorySettings settings, bool withContener = false)
        {
            _withContener = withContener;

            InitializeComponent();

            //let create a mapper so LiveCharts know how to plot our CustomerViewModel class
            var customerVmMapper = Mappers.Xy<CustomerForSingle>()
                .X((value, index) => index)
                .Y(value => value.Values);

            //lets save the mapper globally
            Charting.For<CustomerForSingle>(customerVmMapper);

            StatsGraphicAchievements.Series = StatsGraphicAchievementsSeries;
            StatsGraphicAchievementsX.Labels = StatsGraphicsAchievementsLabels;

            if (!settings.IgnoreSettings)
            {
                StatsGraphicAchievementsX.ShowLabels = settings.EnableIntegrationAxisGraphic;
                StatsGraphicAchievementsY.ShowLabels = settings.EnableIntegrationOrdinatesGraphic;
            }
        }

        private void StatsGraphicAchievements_Loaded(object sender, RoutedEventArgs e)
        {
            // Define height & width
            var parent = ((FrameworkElement)((FrameworkElement)((FrameworkElement)sender).Parent).Parent);
            if (_withContener)
            {
                parent = ((FrameworkElement)((FrameworkElement)((FrameworkElement)((FrameworkElement)((FrameworkElement)sender).Parent).Parent).Parent).Parent);
            }

#if DEBUG
            logger.Debug($"SuccessStory - SuccessStoryAchievementsGraphics({_withContener}) - parent.name: {parent.Name} - parent.Height: {parent.Height} - parent.Width: {parent.Width}");
#endif

            if (!double.IsNaN(parent.Height))
            {
                ((FrameworkElement)sender).Height = parent.Height;
            }
            ((FrameworkElement)((FrameworkElement)sender).Parent).Height = ((FrameworkElement)sender).Height;
            ((FrameworkElement)sender).Height = ((FrameworkElement)sender).Height + 18;
            
            if (!double.IsNaN(parent.Width))
            {
                ((FrameworkElement)sender).Width = parent.Width;
            }
        }
    }
}
