using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Models
{
    public class ExophaseSearchResult
    {
        public bool success { get; set; }
        public Games games { get; set; }
    }

    public class ExophasePlatform
    {
        public int termTaxonomyId { get; set; }
        public int termId { get; set; }
        public string name { get; set; }
        public string slug { get; set; }
    }

    public class Images
    {
        public string o { get; set; }
        public string l { get; set; }
        public string m { get; set; }
        public string s { get; set; }
        public string t { get; set; }
    }

    public class List
    {
        public int master_id { get; set; }
        public int? total_exp { get; set; }
        public int? global_players { get; set; }
        public int? global_complete { get; set; }
        public int total_awards { get; set; }
        public int total_points { get; set; }
        public string title { get; set; }
        public string environment_slug { get; set; }
        public List<ExophasePlatform> platforms { get; set; }
        public string endpoint_awards { get; set; }
        public Images images { get; set; }
    }

    public class Paging
    {
        public int current { get; set; }
        public int next { get; set; }
        public int last { get; set; }
        public int total { get; set; }
    }

    public class Games
    {
        public List<object> collection { get; set; }
        public int found { get; set; }
        public object sort { get; set; }
        public object filters { get; set; }
        public int pages { get; set; }
        public List<List> list { get; set; }
        public Paging paging { get; set; }
    }

}
