using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Models
{
    public class ExophaseSearchResult
    {
        [SerializationPropertyName("success")]
        public bool Success { get; set; }

        [SerializationPropertyName("games")]
        public ExophaseGames Games { get; set; }
    }

    public class ExophasePlatform
    {
        [SerializationPropertyName("termTaxonomyId")]
        public int? TermTaxonomyId { get; set; }

        [SerializationPropertyName("termId")]
        public int? TermId { get; set; }

        [SerializationPropertyName("name")]
        public string Name { get; set; }

        [SerializationPropertyName("slug")]
        public string Slug { get; set; }
    }

    public class Images
    {
        [SerializationPropertyName("t")]
        public string T { get; set; }
        [SerializationPropertyName("o")]
        public string O { get; set; }
        [SerializationPropertyName("s")]
        public string S { get; set; }

        [SerializationPropertyName("m")]
        public string M { get; set; }

        [SerializationPropertyName("l")]
        public string L { get; set; }
    }

    public class List
    {
        [SerializationPropertyName("master_id")]
        public int? MasterId { get; set; }

        [SerializationPropertyName("total_exp")]
        public int? TotalExp { get; set; }

        [SerializationPropertyName("global_players")]
        public int? GlobalPlayers { get; set; }

        [SerializationPropertyName("global_complete")]
        public int? GlobalComplete { get; set; }

        [SerializationPropertyName("total_awards")]
        public int? TotalAwards { get; set; }

        [SerializationPropertyName("total_points")]
        public int? TotalPoints { get; set; }

        [SerializationPropertyName("title")]
        public string Title { get; set; }

        [SerializationPropertyName("environment_slug")]
        public string EnvironmentSlug { get; set; }

        [SerializationPropertyName("platforms")]
        public List<ExophasePlatform> Platforms { get; set; }

        [SerializationPropertyName("regions")]
        public object Regions { get; set; }

        [SerializationPropertyName("endpoint_awards")]
        public string EndpointAwards { get; set; }

        [SerializationPropertyName("images")]
        public Images Images { get; set; }
    }

    public class Paging
    {
        [SerializationPropertyName("current")]
        public int? Current { get; set; }

        [SerializationPropertyName("next")]
        public int? Next { get; set; }

        [SerializationPropertyName("last")]
        public int? Last { get; set; }

        [SerializationPropertyName("total")]
        public int? Total { get; set; }
    }

    public class ExophaseGames
    {
        [SerializationPropertyName("collection")]
        public List<object> Collection { get; set; }

        [SerializationPropertyName("found")]
        public int? Found { get; set; }

        [SerializationPropertyName("sort")]
        public string Sort { get; set; }

        [SerializationPropertyName("filters")]
        public List<object> Filters { get; set; }

        [SerializationPropertyName("pages")]
        public int? Pages { get; set; }

        [SerializationPropertyName("list")]
        public List<List> List { get; set; }

        [SerializationPropertyName("paging")]
        public Paging Paging { get; set; }
    }
}
