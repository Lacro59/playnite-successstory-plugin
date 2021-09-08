using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonPluginsPlaynite.PluginLibrary.SteamLibrary.SteamShared
{
    public class AppReviewsResult
    {
        public class QuerySummary
        {
            public int num_reviews { get; set; }
            public int review_score { get; set; }
            public string review_score_desc { get; set; }
            public int total_positive { get; set; }
            public int total_negative { get; set; }
            public int total_reviews { get; set; }
        }

        public int success { get; set; }
        public QuerySummary query_summary { get; set; }
    }

    public class StoreSearchResult : GenericItemOption
    {
        public uint GameId { get; set; }
    }

    public class StoreAppDetailsResult
    {
        public class AppDetails
        {
            public class Category
            {
                public int id;
                public string description;
            }

            public class Genre
            {
                public string id;
                public string description;
            }

            public class ReleaseDate
            {
                public bool comming_soon;
                public string date;
            }

            public class Requirement
            {
                public string minimum;
                public string recommended;
            }

            public class Platforms
            {
                public bool windows;
                public bool mac;
                public bool linux;
            }

            public class Metacritic
            {
                public int score;
                public string url;
            }

            public class Screenshot
            {
                public int id;
                public string path_thumbnail;
                public string path_full;
            }

            public class Movie
            {
                public int id;
                public string name;
                public string thumbnail;
                public bool highlight;
                public Dictionary<string, string> webm;
            }

            public class Support
            {
                public string url;
                public string email;
            }

            public string type;
            public string name;
            public int steam_appid;
            public int required_age;
            public bool is_free;
            public List<int> dlc;
            public string header_image;
            public string background;
            public string detailed_description;
            public string about_the_game;
            public string short_description;
            public string supported_languages;
            public string website;
            public object pc_requirements;
            public object mac_requirements;
            public object linux_requirements;
            public List<Genre> genres;
            public ReleaseDate release_date;
            public List<string> developers;
            public List<string> publishers;
            public Platforms platforms;
            public Metacritic metacritic;
            public List<Category> categories;
            public List<Screenshot> screenshots;
            public List<Movie> movies;
            public Support support_info;
        }

        public bool success
        {
            get; set;
        }

        public AppDetails data
        {
            get; set;
        }
    }
}
