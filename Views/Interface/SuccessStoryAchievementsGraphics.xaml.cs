using LiveCharts;
using Playnite.SDK;
using PluginCommon;
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

        private dynamic Win;

        public SuccessStoryAchievementsGraphics(SeriesCollection StatsGraphicAchievementsSeries, IList<string> StatsGraphicsAchievementsLabels, dynamic Win = null)
        {
            InitializeComponent();

            this.Win = Win;

            StatsGraphicAchievements.Series = StatsGraphicAchievementsSeries;
            StatsGraphicAchievementsX.Labels = StatsGraphicsAchievementsLabels;
        }

        private void StatsGraphicAchievements_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (Win == null)
            {
                foreach (StackPanel sp in Tools.FindVisualChildren<StackPanel>(Application.Current.MainWindow))
                {
                    if (sp.Name == "PART_Achievements_Graphics")
                    {
                        StatsGraphicAchievements.Height = sp.MaxHeight + 20;
                    }
                }
            }
            else
            {
                foreach (StackPanel sp in Tools.FindVisualChildren<StackPanel>(Win))
                {
                    if (sp.Name == "SuccessStory_Achievements_Graphics")
                    {
                        StatsGraphicAchievements.Height = sp.MaxHeight + 7;
                    }
                }
            }
        }
    }
}
