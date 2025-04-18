using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Models.GenshinImpact
{
    public class SeelieMeExport
    {
        [SerializationPropertyName("goals")]
        public List<object> Goals { get; set; }

        [SerializationPropertyName("inactive")]
        public object Inactive { get; set; }

        [SerializationPropertyName("notes")]
        public object Notes { get; set; }

        [SerializationPropertyName("custom_items")]
        public List<object> CustomItems { get; set; }

        [SerializationPropertyName("customs")]
        public object Customs { get; set; }

        [SerializationPropertyName("inventory")]
        public List<object> Inventory { get; set; }

        [SerializationPropertyName("tasks")]
        public List<object> Tasks { get; set; }

        [SerializationPropertyName("achievements")]
        public Dictionary<string, AchievementItem> Achievements { get; set; }

        [SerializationPropertyName("ar")]
        public int Ar { get; set; }

        [SerializationPropertyName("wl")]
        public int Wl { get; set; }

        [SerializationPropertyName("server")]
        public string Server { get; set; }
    }

    public class AchievementItem
    {
        [SerializationPropertyName("done")]
        public bool Done { get; set; }
    }
}
