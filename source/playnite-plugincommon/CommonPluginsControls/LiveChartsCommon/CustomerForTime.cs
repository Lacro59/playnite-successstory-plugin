using CommonPluginsControls.Controls;
using System;

namespace CommonPluginsControls.LiveChartsCommon
{
    public class CustomerForTime
    {
        public string Icon { get; set; }
        public string IconText { get; set; }

        public string Name { get; set; }
        public long Values { get; set; }
        public string ValuesFormat => (int)TimeSpan.FromSeconds(Values).TotalHours + "h " + TimeSpan.FromSeconds(Values).ToString(@"mm") + "min";
    }
}
