using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Models.GenshinImpact
{
    public class PaimonMoeExport
    {
        [SerializationPropertyName("achievement")]
        public Dictionary<string, Dictionary<string, bool>> Achievement { get; set; }

        [SerializationPropertyName("achievement-checklist")]
        public object AchievementChecklist { get; set; }

        [SerializationPropertyName("ar")]
        public int Ar { get; set; }

        [SerializationPropertyName("converted")]
        public DateTime Converted { get; set; }

        [SerializationPropertyName("locale")]
        public string Locale { get; set; }

        [SerializationPropertyName("server")]
        public string Server { get; set; }

        [SerializationPropertyName("update-time")]
        public DateTime UpdateTime { get; set; }

        [SerializationPropertyName("wish-counter-setting")]
        public WishCounterSetting WishCounterSetting { get; set; }

        [SerializationPropertyName("wl")]
        public int Wl { get; set; }
    }

    public class WishCounterSetting
    {
        [SerializationPropertyName("firstTime")]
        public bool FirstTime { get; set; }

        [SerializationPropertyName("manualInput")]
        public bool ManualInput { get; set; }
    }
}
