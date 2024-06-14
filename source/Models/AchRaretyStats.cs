namespace SuccessStory.Models
{
    public class AchRaretyStats
    {
        public int Locked { get; set; }
        public int UnLocked { get; set; }
        public int Total { get; set; }

        public string Stats => UnLocked + " / " + Total;
    }
}
