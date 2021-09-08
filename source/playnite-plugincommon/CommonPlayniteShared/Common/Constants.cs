using System;
using System.Collections.Generic;
using System.Globalization;

namespace CommonPluginsPlaynite.Common
{
    public static class Constants
    {
        public static string DateUiFormat
        {
            get;
        } = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;

        public static string TimeUiFormat
        {
            get;
        } = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern;


        public static char[] ListSeparators
        {
            get;
        } = new char[] { ListSeparator };

        public const char ListSeparator = ',';
    }
}
