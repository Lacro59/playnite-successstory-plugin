using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using SuccessStory.Services;
using System.IO;
using System.Windows.Media.Animation;
using CommonPlayniteShared;
using CommonPluginsShared;

namespace SuccessStory.Controls.Customs
{
    /// <summary>
    /// Logique d'interaction pour AchievementImage.xaml
    /// </summary>
    public partial class AchievementImage : UserControl
    {
        private static SuccessStoryDatabase PluginDatabase = SuccessStory.PluginDatabase;

        internal Storyboard PART_ColorEffect;
        internal Storyboard PART_ColorEffectUltraRare;

        internal object CurrentIcon { get; set; }


        #region Properties
        public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
            nameof(Icon),
            typeof(string),
            typeof(AchievementImage),
            new FrameworkPropertyMetadata(string.Empty, IconChanged)
        );
        public string Icon
        {
            get => (string)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }
        private static void IconChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            try
            {
                AchievementImage control = (AchievementImage)obj;
                control.LoadNewIcon(args.NewValue, args.OldValue);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        public static readonly DependencyProperty IsGrayProperty = DependencyProperty.Register(
            nameof(IsGray),
            typeof(bool),
            typeof(AchievementImage),
            new FrameworkPropertyMetadata(false)
        );
        public bool IsGray
        {
            get => (bool)GetValue(IsGrayProperty);
            set => SetValue(IsGrayProperty, value);
        }

        public static readonly DependencyProperty EnableRaretyIndicatorProperty = DependencyProperty.Register(
            nameof(EnableRaretyIndicator),
            typeof(bool),
            typeof(AchievementImage),
            new FrameworkPropertyMetadata(true)
        );
        public bool EnableRaretyIndicator
        {
            get => (bool)GetValue(EnableRaretyIndicatorProperty);
            set => SetValue(EnableRaretyIndicatorProperty, value);
        }

        public static readonly DependencyProperty DisplayRaretyValueProperty = DependencyProperty.Register(
            nameof(DisplayRaretyValue),
            typeof(bool),
            typeof(AchievementImage),
            new FrameworkPropertyMetadata(true)
        );
        public bool DisplayRaretyValue
        {
            get => (bool)GetValue(DisplayRaretyValueProperty);
            set => SetValue(DisplayRaretyValueProperty, value);
        }

        public static readonly DependencyProperty PercentProperty = DependencyProperty.Register(
            nameof(Percent),
            typeof(float),
            typeof(AchievementImage),
            new FrameworkPropertyMetadata(default(float))
        );
        public float Percent
        {
            get => (float)GetValue(PercentProperty);
            set => SetValue(PercentProperty, value);
        }


        public static readonly DependencyProperty IsLockedProperty = DependencyProperty.Register(
            nameof(IsLocked),
            typeof(bool),
            typeof(AchievementImage),
            new FrameworkPropertyMetadata(false, PropertyChanged)
        );
        public bool IsLocked
        {
            get => (bool)GetValue(IsLockedProperty);
            set => SetValue(IsLockedProperty, value);
        }

        public static readonly DependencyProperty IconTextProperty = DependencyProperty.Register(
            nameof(IconText),
            typeof(string),
            typeof(AchievementImage),
            new FrameworkPropertyMetadata(string.Empty, PropertyChanged)
        );        
        public string IconText
        {
            get => (string)GetValue(IconTextProperty);
            set => SetValue(IconTextProperty, value);
        }

        public static readonly DependencyProperty IconCustomProperty = DependencyProperty.Register(
            nameof(IconCustom),
            typeof(string),
            typeof(AchievementImage),
            new FrameworkPropertyMetadata(string.Empty, PropertyChanged)
        );        
        public string IconCustom
        {
            get => (string)GetValue(IconCustomProperty);
            set => SetValue(IconCustomProperty, value);
        }


        private static void PropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            try
            {
                AchievementImage control = (AchievementImage)obj;
                control.NewProperty();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }
        #endregion


        public AchievementImage()
        {
            InitializeComponent();

            PART_ColorEffect = (Storyboard)TryFindResource("PART_ColorEffect");
            PART_ColorEffectUltraRare = (Storyboard)TryFindResource("PART_ColorEffectUltraRare");

            NewProperty();
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


        private async void LoadNewIcon(object newSource, object oldSource)
        {
            dynamic image = null;
            try
            {
                if (newSource?.Equals(CurrentIcon) == true)
                {
                    return;
                }

                CurrentIcon = newSource;
                bool IsGray = this.IsGray;

                if (newSource != null)
                {
                    image = await Task.Factory.StartNew(() =>
                    {
                        if (newSource is string str)
                        {
                            dynamic tmpImage = ImageSourceManagerPlugin.GetImage(str, false);

                            if (tmpImage == null)
                            {
                                tmpImage = new BitmapImage(new Uri(Path.Combine(PluginDatabase.Paths.PluginPath, "Resources", "default_icon.png")));
                                ((BitmapImage)tmpImage).Freeze();
                            }

                            if (IsGray)
                            {
                                return ImageTools.ConvertBitmapImage(tmpImage, ImageColor.Gray);
                            }

                            return tmpImage;
                        }
                        else
                        {
                            return null;
                        }
                    });
                }
            }
            catch
            {
                image = null;
            }

            PART_Image.Source = image;
        }


        private void NewProperty()
        {
            PART_IconText.Visibility = Visibility.Collapsed;
            if (IsLocked)
            {
                PART_IconText.Visibility = Visibility.Visible;
                if (File.Exists(IconCustom))
                {
                    Icon = IconCustom;
                }
            }
        }
    }
}
