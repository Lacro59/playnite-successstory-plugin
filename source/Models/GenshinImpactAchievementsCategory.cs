using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Models
{
    public class GenshinImpactAchievementsCategory
    {
        public int OrderId { get; set; }
        public object NameTextMapHash { get; set; }
        public string IconPath { get; set; }
        public int? Id { get; set; } = 0;
        public int? FinishRewardId { get; set; }
    }
}
