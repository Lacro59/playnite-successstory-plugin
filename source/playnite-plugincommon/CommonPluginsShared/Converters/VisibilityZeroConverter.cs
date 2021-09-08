using CommonPlayniteShared;
using Playnite.SDK;
using System;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CommonPluginsShared.Converters
{
    public class VisibilityZeroConverter : IValueConverter
    {
        private static ILogger logger = LogManager.GetLogger();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value.ToString() == "0")
                {
                    if (parameter.ToString() == "1")
                    {
                        return Visibility.Collapsed;
                    }
                    else
                    {
                        return Visibility.Hidden;
                    }
                }
                else
                {
                    return Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
