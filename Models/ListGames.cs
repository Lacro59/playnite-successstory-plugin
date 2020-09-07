using System;
using System.Windows.Media.Imaging;

namespace SuccessStory.Models
{
    /// <summary>
    /// Class for the ListView games
    /// </summary>
    public class ListViewGames
    {
        public string Id { get; set; }
        public BitmapImage Icon { get; set; }
        public string Name { get; set; }
        public DateTime? LastActivity { get; set; }
        public string SourceName { get; set; }
        public string SourceIcon { get; set; }
        public int ProgressionValue { get; set; }
        public int Total { get; set; }
        public string TotalPercent { get; set; }
        public int Unlocked { get; set; }
    }
}
