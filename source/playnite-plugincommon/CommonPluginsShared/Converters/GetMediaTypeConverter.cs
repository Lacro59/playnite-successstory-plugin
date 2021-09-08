using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace CommonPluginsShared.Converters
{
    public class GetMediaTypeConverter : IValueConverter
    {
        private static ILogger logger = LogManager.GetLogger();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is string)
                {
                    if (System.IO.Path.GetExtension((string)value).ToLower().Contains("mp4"))
                    {
                        return "\ueb13";
                    }
                    if (System.IO.Path.GetExtension((string)value).ToLower().Contains("avi"))
                    {
                        return "\ueb13";
                    }

                    if (System.IO.Path.GetExtension((string)value).ToLower().Contains("webp"))
                    {
                        return "\ueb16 \ueb13";
                    }

                    return "\ueb16";
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
