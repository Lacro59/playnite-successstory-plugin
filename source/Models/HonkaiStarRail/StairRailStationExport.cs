using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Models.HonkaiStarRail
{
    public class StairRailStationExport
    {
        [SerializationPropertyName("version")]
        public int Version { get; set; }

        [SerializationPropertyName("profiles")]
        public object Profiles { get; set; }

        [SerializationPropertyName("storageActionIdx")]
        public long StorageActionIdx { get; set; }

        [SerializationPropertyName("nextIdx")]
        public int NextIdx { get; set; }

        [SerializationPropertyName("data")]
        public Data Data { get; set; }

        [SerializationPropertyName("curAccountIdx")]
        public string CurAccountIdx { get; set; }
    }

    public class _1Achieve
    {
        [SerializationPropertyName("completeState")]
        public CompleteState CompleteState { get; set; }
    }

    public class CompleteState
    {
        [SerializationPropertyName("29325")]
        public object _29325 { get; set; }

        [SerializationPropertyName("29327")]
        public object _29327 { get; set; }

        [SerializationPropertyName("29323")]
        public object _29323 { get; set; }

        [SerializationPropertyName("29328")]
        public object _29328 { get; set; }

        [SerializationPropertyName("29333")]
        public object _29333 { get; set; }

        [SerializationPropertyName("29324")]
        public object _29324 { get; set; }

        [SerializationPropertyName("29334")]
        public object _29334 { get; set; }

        [SerializationPropertyName("29321")]
        public object _29321 { get; set; }

        [SerializationPropertyName("29322")]
        public object _29322 { get; set; }

    }

    public class Data
    {
        [SerializationPropertyName("stores")]
        public Stores Stores { get; set; }
    }

    public class Stores
    {
        [SerializationPropertyName("1_achieve")]
        public _1Achieve _1Achieve { get; set; }
    }
}
