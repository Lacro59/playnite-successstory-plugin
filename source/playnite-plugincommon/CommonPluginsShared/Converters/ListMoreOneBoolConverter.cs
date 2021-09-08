using CommonPlayniteShared;
using Playnite.SDK;
using System;
using System.Collections;
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
    public class ListMoreOneBoolConverter : IValueConverter
    {
        public object Convert(object value, Type TargetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is IList)
                {
                    return ((IList)value).Count > 1;
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
