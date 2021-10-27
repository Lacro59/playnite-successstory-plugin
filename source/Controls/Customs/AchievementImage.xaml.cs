using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Imaging;
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
using SuccessStory.Models;
using SuccessStory.Services;
using System.IO;
using System.Windows.Media.Effects;
using SuccessStory.Converters;
using System.Globalization;
using System.Windows.Media.Animation;

namespace SuccessStory.Controls.Customs
{
    /// <summary>
    /// Logique d'interaction pour AchievementImage.xaml
    /// </summary>
    public partial class AchievementImage : UserControl
    {
        private SuccessStoryDatabase PluginDatabase = SuccessStory.PluginDatabase;

        internal Storyboard PART_ColorEffect;
        internal Storyboard PART_ColorEffectUltraRare;


        #region Properties
        public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
            nameof(Icon),
            typeof(string),
            typeof(AchievementImage),
            new FrameworkPropertyMetadata(string.Empty)
        );
        public string Icon
        {
            get { return (string)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        public static readonly DependencyProperty IsGrayProperty = DependencyProperty.Register(
            nameof(IsGray),
            typeof(bool),
            typeof(AchievementImage),
            new FrameworkPropertyMetadata(false)
        );
        public bool IsGray
        {
            get { return (bool)GetValue(IsGrayProperty); }
            set { SetValue(IsGrayProperty, value); }
        }

        public static readonly DependencyProperty EnableRaretyIndicatorProperty = DependencyProperty.Register(
            nameof(EnableRaretyIndicator),
            typeof(bool),
            typeof(AchievementImage),
            new FrameworkPropertyMetadata(true)
        );
        public bool EnableRaretyIndicator
        {
            get { return (bool)GetValue(EnableRaretyIndicatorProperty); }
            set { SetValue(EnableRaretyIndicatorProperty, value); }
        }

        public static readonly DependencyProperty DispalyRaretyValueProperty = DependencyProperty.Register(
            nameof(DispalyRaretyValue),
            typeof(bool),
            typeof(AchievementImage),
            new FrameworkPropertyMetadata(true)
        );
        public bool DispalyRaretyValue
        {
            get { return (bool)GetValue(DispalyRaretyValueProperty); }
            set { SetValue(DispalyRaretyValueProperty, value); }
        }

        public static readonly DependencyProperty PercentProperty = DependencyProperty.Register(
            nameof(Percent),
            typeof(float),
            typeof(AchievementImage),
            new FrameworkPropertyMetadata(default(float))
        );
        public float Percent
        {
            get { return (float)GetValue(PercentProperty); }
            set { SetValue(PercentProperty, value); }
        }
        #endregion


        public AchievementImage()
        {
            InitializeComponent();

            PART_ColorEffect = (Storyboard)TryFindResource("PART_ColorEffect");
            PART_ColorEffectUltraRare = (Storyboard)TryFindResource("PART_ColorEffectUltraRare");
        }


        private void Image_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            ((Image)sender).Source = new BitmapImage(new Uri(Path.Combine(PluginDatabase.Paths.PluginPath, "Resources", "default_icon.png")));
        }

        private void Image_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                PART_Label.Content = Math.Round(Percent, 1);
                PART_ProgressBar.Value = Math.Round(Percent, 1);

                if (Percent > PluginDatabase.PluginSettings.Settings.RarityUltraRare)
                {
                    //PART_ColorEffect.Begin();
                    //PART_ColorEffectUltraRare.Stop();
                }
                else
                {
                    //PART_ColorEffect.Stop();
                    //PART_ColorEffectUltraRare.Begin();
                }
            }
            catch { }
        }
    }
}
