using System;
using System.Windows.Media.Imaging;

namespace SuccessStory.Models
{
    /// <summary>
    /// Class for the listbox achievements
    /// </summary>
    public class listAchievements
    {
        //public BitmapImage Icon { get; set; }
        public FormatConvertedBitmap Icon { get; set; }
        public string Name { get; set; }
        public DateTime? DateUnlock { get; set; }
        public string Description { get; set; }
        public float Percent { get; set; }
    }
}
