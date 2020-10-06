using System;
using System.Windows.Media.Imaging;

namespace SuccessStory.Models
{
    /// <summary>
    /// Class for the ListBox achievements
    /// </summary>
    public class ListBoxAchievements
    {
        public FormatConvertedBitmap Icon { get; set; }
        public string Name { get; set; }
        public DateTime? DateUnlock { get; set; }
        public string Description { get; set; }
        public float Percent { get; set; }
    }
}
