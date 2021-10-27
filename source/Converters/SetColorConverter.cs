using SuccessStory.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace SuccessStory.Converters
{
    public class SetColorConverter : IValueConverter
    {
        private SuccessStoryDatabase PluginDatabase = SuccessStory.PluginDatabase;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Color color = Brushes.Transparent.Color;

            if (value == null)
            {
                return null;
            }

            double.TryParse(value.ToString(), out double valueDouble);

            if (valueDouble <= PluginDatabase.PluginSettings.Settings.RarityUltraRare && PluginDatabase.PluginSettings.Settings.UseUltraRare)
            {
                return PluginDatabase.PluginSettings.Settings.RarityUltraRareColor.Color;
            }

            if (valueDouble <= PluginDatabase.PluginSettings.Settings.RarityRare)
            {
                return PluginDatabase.PluginSettings.Settings.RarityRareColor.Color;
            }
            if (valueDouble <= PluginDatabase.PluginSettings.Settings.RarityUncommon)
            {
                return PluginDatabase.PluginSettings.Settings.RarityUncommonColor.Color;
            }
            if (valueDouble > PluginDatabase.PluginSettings.Settings.RarityUncommon)
            {
                return null;
            }

            Color newColor = new Color();
            newColor.ScR = (float)color.R / 255;
            newColor.ScG = (float)color.G / 255;
            newColor.ScB = (float)color.B / 255;

            return newColor;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
