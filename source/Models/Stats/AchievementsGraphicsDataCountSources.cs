using CommonPluginsControls.LiveChartsCommon;
using LiveCharts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Models.Stats
{
    public class AchievementsGraphicsDataCountSources
    {
        public string[] Labels { get; set; }
        public ChartValues<CustomerForSingle> SeriesUnlocked { get; set; }
        public ChartValues<CustomerForSingle> SeriesLocked { get; set; }
        public ChartValues<CustomerForSingle> SeriesTotal { get; set; }
    }
}
