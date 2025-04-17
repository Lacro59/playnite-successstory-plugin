using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Models.HonkaiStarRail
{
    public class AchievementSeries
    {
        [SerializationPropertyName("SeriesID")]
        public int SeriesID { get; set; }

        [SerializationPropertyName("SeriesTitle")]
        public SeriesTitle SeriesTitle { get; set; }

        [SerializationPropertyName("MainIconPath")]
        public string MainIconPath { get; set; }

        [SerializationPropertyName("IconPath")]
        public string IconPath { get; set; }

        [SerializationPropertyName("GoldIconPath")]
        public string GoldIconPath { get; set; }

        [SerializationPropertyName("SilverIconPath")]
        public string SilverIconPath { get; set; }

        [SerializationPropertyName("CopperIconPath")]
        public string CopperIconPath { get; set; }

        [SerializationPropertyName("Priority")]
        public int Priority { get; set; }
    }

    public class SeriesTitle
    {
        [SerializationPropertyName("Hash")]
        public string Hash { get; set; }
    }
}
