using Playnite.SDK;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CommonPluginsShared
{
    public class Tools
    {
        private static readonly ILogger logger = LogManager.GetLogger();


        /// <summary>
        /// 
        /// </summary>
        /// <param name="Value"></param>
        /// <param name="WithoutDouble"></param>
        /// <returns></returns>
        public static string SizeSuffix(double Value, bool WithoutDouble = false)
        {
            string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

            if (Value < 0) { return "-" + SizeSuffix(-Value); }
            if (Value == 0) { return "0" + CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator + "0 bytes"; }

            int mag = (int)Math.Log(Value, 1024);
            decimal adjustedSize = (decimal)Value / (1L << (mag * 10));

            if (WithoutDouble)
            {
                return string.Format("{0} {1}", adjustedSize.ToString("0", CultureInfo.CurrentUICulture), SizeSuffixes[mag]);
            }
            return string.Format("{0} {1}", adjustedSize.ToString("0.0", CultureInfo.CurrentUICulture), SizeSuffixes[mag]);
        }


        // TODO Used?
        // https://stackoverflow.com/a/48735199
        //public static JArray RemoveValue(JArray oldArray, dynamic obj)
        //{
        //    List<string> newArray = oldArray.ToObject<List<string>>();
        //    newArray.Remove(obj);
        //    return JArray.FromObject(newArray);
        //}


        #region Date manipulations
        /// <summary>
        /// Get number week from a DateTime
        /// </summary>
        /// <param name="Date"></param>
        /// <returns></returns>
        public static int WeekOfYearISO8601(DateTime Date)
        {
            var day = (int)CultureInfo.CurrentCulture.Calendar.GetDayOfWeek(Date);
            return CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(Date.AddDays(4 - (day == 0 ? 7 : day)), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        /// <summary>
        /// Get a Datetime from a week number
        /// </summary>
        /// <param name="Year"></param>
        /// <param name="Day"></param>
        /// <param name="Week"></param>
        /// <returns></returns>
        public static DateTime YearWeekDayToDateTime(int Year, DayOfWeek Day, int Week)
        {
            DateTime startOfYear = new DateTime(Year, 1, 1);

            // The +7 and %7 stuff is to avoid negative numbers etc.
            int daysToFirstCorrectDay = (((int)Day - (int)startOfYear.DayOfWeek) + 7) % 7;

            return startOfYear.AddDays(7 * (Week - 1) + daysToFirstCorrectDay);
        }

        /// <summary>
        /// Get week count from a year
        /// </summary>
        /// <param name="Year"></param>
        /// <returns></returns>
        public static int GetWeeksInYear(int Year)
        {
            DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
            DateTime date1 = new DateTime(Year, 12, 31);
            System.Globalization.Calendar cal = dfi.Calendar;
            return cal.GetWeekOfYear(date1, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);
        }
        #endregion


        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <remarks>https://stackoverflow.com/a/3974535</remarks>
        /// <returns></returns>
        public static string ToHex(byte[] bytes)
        {
            char[] c = new char[bytes.Length * 2];

            byte b;

            for (int bx = 0, cx = 0; bx < bytes.Length; ++bx, ++cx)
            {
                b = ((byte)(bytes[bx] >> 4));
                c[cx] = (char)(b > 9 ? b + 0x37 + 0x20 : b + 0x30);

                b = ((byte)(bytes[bx] & 0x0F));
                c[++cx] = (char)(b > 9 ? b + 0x37 + 0x20 : b + 0x30);
            }

            return new string(c);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte[] HexToBytes(string str)
        {
            if (str.Length == 0 || str.Length % 2 != 0)
                return new byte[0];

            byte[] buffer = new byte[str.Length / 2];
            char c;
            for (int bx = 0, sx = 0; bx < buffer.Length; ++bx, ++sx)
            {
                // Convert first half of byte
                c = str[sx];
                buffer[bx] = (byte)((c > '9' ? (c > 'Z' ? (c - 'a' + 10) : (c - 'A' + 10)) : (c - '0')) << 4);

                // Convert second half of byte
                c = str[++sx];
                buffer[bx] |= (byte)(c > '9' ? (c > 'Z' ? (c - 'a' + 10) : (c - 'A' + 10)) : (c - '0'));
            }

            return buffer;
        }
    }
}
