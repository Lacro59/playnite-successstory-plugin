using System.Collections.Generic;

namespace SuccessStory.PlayniteResources.OriginLibrary.Models
{
    public class StorePageMetadata
    {
        public class Components
        {
            public List<Dictionary<string, object>> items;
        }

        public class GameHub
        {
            public string name;
            public string type;
            public string locale;
            public string country;
            public Components components;
        }

        public GameHub gamehub;
    }
}
