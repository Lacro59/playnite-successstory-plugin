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
    public class CompareValueConverter : IMultiValueConverter
    {
        public static IResourceProvider resources = new ResourceProvider();


        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                int.TryParse(((string)values[0]).Replace("%", string.Empty).Replace("°", string.Empty), out int ValueData);
                int.TryParse((string)values[1], out int ValueControl);

                bool Enable = (bool)values[2];

                if (Enable)
                {
                    int.TryParse((string)parameter, out int parameterValue);
                    if (parameterValue == 0)
                    {
                        if (ValueData > ValueControl)
                        {
                            return resources.GetResource("TextBrush");
                        }
                        else
                        {
                            return Brushes.Orange;
                        }
                    }
                    else
                    {
                        if (ValueData < ValueControl)
                        {
                            return resources.GetResource("TextBrush");
                        }
                        else
                        {
                            return Brushes.Orange;
                        }
                    }
                }
                else
                {
                    return resources.GetResource("TextBrush");
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return (Brushes)resources.GetResource("TextBrush");
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
