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
using System.Windows.Shapes;
using System.Windows.Threading;
using SuccessStory.Models;

namespace SuccessStory.Controls.Customs
{
    /// <summary>
    /// Logique d'interaction pour AchievementImage.xaml
    /// </summary>
    public partial class AchievementImage : UserControl
    {
        #region Properties
        public string Icon
        {
            get { return (string)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
            nameof(Icon),
            typeof(string),
            typeof(AchievementImage),
            new FrameworkPropertyMetadata(string.Empty)
        );

        //private static void IconPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        //{
        //    if (sender is AchievementImage obj && e.NewValue != e.OldValue)
        //    {
        //        obj.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
        //        {
        //            //obj.LoadIcon(e.NewValue);
        //        }));
        //    }
        //}
        //
        //private async void LoadIcon(object AchievementContext)
        //{
        //    var IconSource = await Task.Factory.StartNew(() =>
        //    {
        //        if (AchievementContext is Achievements achievements)
        //        {
        //            return BitmapExtensions.BitmapFromFile(achievements.UrlUnlocked);
        //        }
        //        else
        //        {
        //            return null;
        //        }
        //    });
        //
        //    PART_Icon.Source = IconSource;
        //
        //    if (AchievementContext != null)
        //    {
        //        this.DataContext = new
        //        {
        //            IsGray = ((Achievements)AchievementContext).IsGray,
        //            EnableRaretyIndicator = ((Achievements)AchievementContext).EnableRaretyIndicator,
        //            Percent = ((Achievements)AchievementContext).Percent
        //        };
        //    }
        //}


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
        }
    }
}
