using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace CommonPluginsControls.Controls
{
    /// <summary>
    /// Logique d'interaction pour MediaElementExtend.xaml
    /// </summary>
    public partial class MediaElementExtend : UserControl
    {
        private DispatcherTimer timer;
        private bool isSeekingMedia = false;


        #region Properties
        public Uri Source
        {
            get { return (Uri)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public static DependencyProperty SourceProperty = DependencyProperty.Register(
            nameof(Source),
            typeof(Uri),
            typeof(MediaElementExtend),
            new FrameworkPropertyMetadata(default(Uri)));


        public MediaState LoadedBehavior
        {
            get { return (MediaState)GetValue(LoadedBehaviorProperty); }
            set { SetValue(LoadedBehaviorProperty, value); }
        }

        public static DependencyProperty LoadedBehaviorProperty = DependencyProperty.Register(
            nameof(LoadedBehavior),
            typeof(MediaState),
            typeof(MediaElementExtend),
            new FrameworkPropertyMetadata(MediaState.Stop));
        #endregion


        public MediaElementExtend()
        {
            InitializeComponent();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += timer_Tick;
            timer.Start();
        }


        private void timer_Tick(object sender, EventArgs e)
        {
            if (PART_Video.Source != null && PART_Video.NaturalDuration.HasTimeSpan && PART_Video.LoadedBehavior == MediaState.Play)
            {
                if (!isSeekingMedia)
                {
                    timelineSlider.Value = PART_Video.Position.TotalSeconds;
                }

                lblStatus.Content = PART_Video.Position.ToString(@"hh\:mm\:ss") + " / " + PART_Video.NaturalDuration.TimeSpan.ToString(@"hh\:mm\:ss");
            }
        }


        #region Controls
        // Play the media.
        private void OnMouseDownPlayMedia(object sender, RoutedEventArgs e)
        {
            PART_Video.LoadedBehavior = MediaState.Play;
            timer.Start();
        }

        // Pause the media.
        private void OnMouseDownPauseMedia(object sender, RoutedEventArgs e)
        {
            PART_Video.LoadedBehavior = MediaState.Pause;
            timer.Stop();
        }

        // Change the volume of the media.
        private void ChangeMediaVolume(object sender, RoutedPropertyChangedEventArgs<double> args)
        {
            PART_Video.Volume = (double)volumeSlider.Value;
        }

        // When the media opens, initialize the "Seek To" slider maximum value
        // to the total number of miliseconds in the length of the media clip.
        private void PART_Video_MediaOpened(object sender, EventArgs e)
        {
            if (PART_Video.Source != null && PART_Video.NaturalDuration.HasTimeSpan && PART_Video.LoadedBehavior == MediaState.Play)
            {
                timelineSlider.Maximum = PART_Video.NaturalDuration.TimeSpan.TotalSeconds;
            }
        }

        private void PART_Video_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                if (PART_Video.LoadedBehavior == MediaState.Pause)
                {
                    PART_Video.LoadedBehavior = MediaState.Play;
                }
                else
                {
                    PART_Video.LoadedBehavior = MediaState.Pause;
                }
            }
        }
        #endregion


        private void Grid_Unloaded(object sender, RoutedEventArgs e)
        {
            if (timer != null)
            {
                timer.Stop();
                timer = null;
            }

            PART_Video.Source = null;
        }


        private void TimelineSlider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            isSeekingMedia = true;
        }

        private void TimelineSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            int SliderValue = (int)timelineSlider.Value;

            // Overloaded constructor takes the arguments days, hours, minutes, seconds, milliseconds.
            // Create a TimeSpan with miliseconds equal to the slider value.
            TimeSpan ts = new TimeSpan(0, 0, 0, SliderValue, 0);
            PART_Video.Position = ts;

            isSeekingMedia = false;
        }
    }
}
