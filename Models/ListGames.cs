using System;
using System.Windows.Media.Imaging;

namespace SuccessStory.Models
{
    /// <summary>
    /// Class for the ListView games
    /// </summary>
    public class ListViewGames
    {
        public string Icon100Percent { get; set; }
        public string Id { get; set; }
        public string Icon { get; set; }
        public string Name { get; set; }
        public DateTime? LastActivity { get; set; }
        public string SourceName { get; set; }
        public string SourceIcon { get; set; }
        public int ProgressionValue { get; set; }
        public int Total { get; set; }
        public string TotalPercent { get; set; }
        public int Unlocked { get; set; }
        public bool IsManual { get; set; }
    }
}
