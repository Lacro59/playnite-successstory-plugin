using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Models.Xbox
{
    public class Xbox360Achievement
    {
        public int id { get; set; }
        public int titleId { get; set; }
        public string name { get; set; }
        public long sequence { get; set; }
        public int flags { get; set; }
        public bool unlockedOnline { get; set; }
        public bool unlocked { get; set; }
        public bool isSecret { get; set; }
        public int platform { get; set; }
        public int gamerscore { get; set; }
        public int imageId { get; set; }
        public string description { get; set; }
        public string lockedDescription { get; set; }
        public int type { get; set; }
        public bool isRevoked { get; set; }
        public DateTime timeUnlocked { get; set; }
    }

    public class Xbox360AchievementResponse
    {
        public List<Xbox360Achievement> achievements { get; set; }
        public PagingInfo pagingInfo { get; set; }
        public DateTime version { get; set; }
    }

    public class PagingInfo
    {
        public string continuationToken { get; set; }
        public int totalRecords { get; set; }
    }
}
