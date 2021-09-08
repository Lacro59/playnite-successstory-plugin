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
    public class LocalDateYMConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value != null && (DateTime)value != default(DateTime))
                {
                    string tmpDate = ((DateTime)value).ToString(CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern);
                    string tmpDay = string.Empty;
                    string tmpDateShort = string.Empty;

                    tmpDay = ((DateTime)value).ToString("d");
                    Regex rgx = new Regex(@"[/\.\- ]" + tmpDay + "|" + tmpDay + @"[/\.\- ]");
                    tmpDateShort = rgx.Replace(tmpDate, string.Empty, 1);

                    if (tmpDateShort.Length == tmpDate.Length)
                    {
                        tmpDay = ((DateTime)value).ToString("dd");
                        rgx = new Regex(@"[/\.\- ]" + tmpDay + "|" + tmpDay + @"[/\.\- ]");
                        tmpDateShort = rgx.Replace(tmpDate, string.Empty, 1);
                    }

                    return tmpDateShort;
                }
                else
                {
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
