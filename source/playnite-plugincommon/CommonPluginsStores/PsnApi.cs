using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores
{
    public class PsnApi
    {
    }


    public class Trophies
    {
        public List<Trophie> trophies { get; set; }
    }

    public class Trophie
    {
        public int trophyId { get; set; }
        public bool trophyHidden { get; set; }
        public string trophyType { get; set; }
        public string trophyName { get; set; }
        public string trophyDetail { get; set; }
        public string trophyIconUrl { get; set; }
        public int trophyRare { get; set; }
        public string trophyEarnedRate { get; set; }
        public FromUser fromUser { get; set; }
    }

    public class FromUser
    {
        public string onlineId { get; set; }
        public bool earned { get; set; }
        public DateTime earnedDate { get; set; }
    }
}
