using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonPluginsPlaynite.PluginLibrary.GogLibrary.Models
{
    public class GetOwnedGamesResult
    {
        public class Tag
        {
            public string id;
            public string name;
            public string productCount;
        }

        public class Availability
        {
            public bool isAvailable;
            public bool isAvailableInAccount;
        }

        public class WorksOn
        {
            public bool Windows;
            public bool Mac;
            public bool Linux;
        }

        public class ReleaseDate
        {
            public DateTime? date;
            public int timezone_type;
            public string timezone;
        }

        public class Product
        {
            public bool isGalaxyCompatible;
            public int id;
            public Availability availability;
            public string title;
            public string image;
            public string url;
            public WorksOn worksOn;
            public string category;
            public int rating;
            public bool isComingSoon;
            public bool isMovie;
            public bool isGame;
            public string slug;
            public bool isNew;
            public int dlcCount;
            public ReleaseDate releaseDate;
            public bool isBaseProductMissing;
            public bool isHidingDisabled;
            public bool isInDevelopment;
            public bool isHidden;
        }

        public string sortBy;
        public int page;
        public int totalProducts;
        public int totalPages;
        public int productsPerPage;
        public int moviesCount;
        public List<Tag> tags;
        public List<Product> products;
        public int updatedProductsCount;
        public int hiddenUpdatedProductsCount;
        public bool hasHiddenProducts;
    }
}
