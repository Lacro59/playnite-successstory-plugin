using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Models
{
    public class GameStats
    {
        #region Base for Steam
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public double Value { get; set; }

        [JsonIgnore]
        public string NameShow
        {
            get
            {
                if (DisplayName.IsNullOrEmpty())
                {
                    return Name;
                }

                return DisplayName;
            }
        }

        [JsonIgnore]
        public double ValueShow
        {
            get
            {
                return Math.Round(Value, 2, MidpointRounding.AwayFromZero);
            }
        }
        #endregion


        #region More for Battle.net
        public string ImageUrl { get; set; }
        public TimeSpan Time { get; set; }

        public string Mode { get; set; }
        public string CareerType { get; set; }
        public string Category { get; set; }
        #endregion  
    }
}
