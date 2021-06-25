using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SuccessStory.Models
{
    public class GameStats
    {
        #region Base for Steam
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public double Value { get; set; }
        #endregion

        [DontSerialize]
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

        [DontSerialize]
        public double ValueShow
        {
            get
            {
                return Math.Round(Value, 2, MidpointRounding.AwayFromZero);
            }
        }

        #region More for Battle.net
        public string ImageUrl { get; set; }
        public TimeSpan Time { get; set; }
        public string Color { get; set; }

        public string Mode { get; set; }
        public string CareerType { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        #endregion  
    }
}
