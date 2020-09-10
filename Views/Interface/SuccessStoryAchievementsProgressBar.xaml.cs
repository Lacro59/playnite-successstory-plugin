using Playnite.SDK;
using System;
using System.Windows;
using System.Windows.Controls;

namespace SuccessStory.Views.Interface
{
    /// <summary>
    /// Logique d'interaction pour SuccessStoryAchievementsProgressBar.xaml
    /// </summary>
    public partial class SuccessStoryAchievementsProgressBar : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private bool _withContener;


        public SuccessStoryAchievementsProgressBar(long value, long maxValue, bool showPercent, bool showIndicator, bool withContener = false)
        {
            _withContener = withContener;

            InitializeComponent();

            if (showIndicator)
            {
                AchievementsIndicator.Content = value + "/" + maxValue;
            }
            else
            {
                AchievementsIndicator.Content = string.Empty;
                AchievementsProgressBar.SetValue(Grid.ColumnProperty, 0);
                AchievementsProgressBar.SetValue(Grid.ColumnSpanProperty, 3);
            }

            AchievementsProgressBar.Value = value;
            AchievementsProgressBar.Maximum = maxValue;

            if (showPercent)
            {
                AchievementsPercent.Content = (maxValue != 0) ? (int)Math.Round((double)(value * 100 / maxValue)) + "%" : 0 + "%";
            }
            else
            {
                AchievementsPercent.Content = string.Empty;
            }
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            // Define height & width
            var parent = ((FrameworkElement)((FrameworkElement)((FrameworkElement)sender).Parent).Parent);
            if (_withContener)
            {
                parent = ((FrameworkElement)((FrameworkElement)((FrameworkElement)((FrameworkElement)sender).Parent).Parent).Parent);
            }
#if DEBUG
            logger.Debug($"SuccessStory - SuccessStoryAchievementsProgressBar - parent.name: {parent.Name} - parent.Height: {parent.Height} - parent.Width: {parent.Width}");
#endif

            if (!double.IsNaN(parent.Height))
            {
                ((FrameworkElement)sender).Height = parent.Height;
            }

            if (!double.IsNaN(parent.Width))
            {
                ((FrameworkElement)sender).Width = parent.Width;
            }
        }
    }
}
