using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Data;

namespace CommonPlayniteShared.Converters
{
    public class TwoBooleanToBooleanConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool a = (bool)values[0];
            bool b = (bool)values[1];

            if (parameter is string)
            {
                if ((string)parameter == "inverted")
                {
                    return !a && !b;
                }

                if ((string)parameter == "firsttrue")
                {
                    return a && !b;
                }
                if ((string)parameter == "secondtrue")
                {
                    return !a && b;
                }
            }

            return a && b;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
