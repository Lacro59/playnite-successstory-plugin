using Playnite.SDK;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;

namespace SuccessStory.Views.Interfaces
{
    /// <summary>
    /// Logique d'interaction pour SettingsOrderAchievement.xaml
    /// </summary>
    public partial class SettingsOrderAchievement : UserControl
    {
        #region
        public OrderAchievement OrderAchievement
        {
            get { return (OrderAchievement)GetValue(OrderAchievementProperty); }
            set { SetValue(OrderAchievementProperty, value); }
        }
        
        public static readonly DependencyProperty OrderAchievementProperty = DependencyProperty.Register(
            nameof(OrderAchievement),
            typeof(OrderAchievement),
            typeof(SettingsOrderAchievement));

        public bool UsedGroupBy
        {
            get { return (bool)GetValue(UsedGroupByProperty); }
            set { SetValue(UsedGroupByProperty, value); }
        }
        
        public static readonly DependencyProperty UsedGroupByProperty = DependencyProperty.Register(
            nameof(UsedGroupBy),
            typeof(bool),
            typeof(SettingsOrderAchievement));
        #endregion  


        public SettingsOrderAchievement()
        {
            InitializeComponent();
        }
    }


    public class OrderAchievementTypeToStringConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var source = (OrderAchievementType)value;
            switch (source)
            {
                case OrderAchievementType.AchievementName:
                    return ResourceProvider.GetString("LOCGameNameTitle");
                case OrderAchievementType.AchievementDateUnlocked:
                    return ResourceProvider.GetString("LOCSuccessStoryDateUnlocked");
                case OrderAchievementType.AchievementRarety:
                    return ResourceProvider.GetString("LOCSuccessStoryRarety");
                default:
                    return string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return new NotSupportedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }

    public class OrderTypeToStringConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var source = (OrderType)value;
            switch (source)
            {
                case OrderType.Ascending:
                    return ResourceProvider.GetString("LOCMenuSortAscending");
                case OrderType.Descending:
                    return ResourceProvider.GetString("LOCMenuSortDescending");
                default:
                    return string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return new NotSupportedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}
