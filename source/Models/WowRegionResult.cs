using System.Collections.Generic;

namespace SuccessStory.Models
{
    public class WowRegionResult
    {
        public Data data { get; set; }
    }

    //public class Type
    //{
    //    public string id { get; set; }
    //    public string name { get; set; }
    //    public string slug { get; set; }
    //    public string @enum { get; set; }
    //    public string __typename { get; set; }
    //}
    //
    public class Population
    {
        public string id { get; set; }
        public string name { get; set; }
        public string slug { get; set; }
        public string @enum { get; set; }
        public string __typename { get; set; }
    }

    public class Realm
    {
        public string name { get; set; }
        public string slug { get; set; }
        public string locale { get; set; }
        public string timezone { get; set; }
        public bool online { get; set; }
        public string category { get; set; }
        //public Type type { get; set; }
        public Population population { get; set; }
        public string __typename { get; set; }
    }

    public class Data
    {
        public List<Realm> Realms { get; set; }
    }
}
