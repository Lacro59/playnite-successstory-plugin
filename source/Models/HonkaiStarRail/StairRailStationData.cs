using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Models.HonkaiStarRail
{
    public class StairRailStationData
    {
        [SerializationPropertyName("series")]
        public List<Series> Series { get; set; }

        [SerializationPropertyName("achievements")]
        public List<Achievement> Achievements { get; set; }
    }

    public class Achievement
    {
        [SerializationPropertyName("id")]
        public int Id { get; set; }

        [SerializationPropertyName("seriesId")]
        public string SeriesId { get; set; }

        [SerializationPropertyName("title")]
        public string Title { get; set; }
    }

    public class Series
    {
        [SerializationPropertyName("title")]
        public string Title { get; set; }

        [SerializationPropertyName("id")]
        public int Id { get; set; }
    }
}
