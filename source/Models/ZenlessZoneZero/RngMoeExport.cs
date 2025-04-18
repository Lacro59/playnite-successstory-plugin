using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Models.ZenlessZoneZero
{
    public class RngMoeExport
    {
        [SerializationPropertyName("version")]
        public int Version { get; set; }

        [SerializationPropertyName("game")]
        public string Game { get; set; }

        [SerializationPropertyName("data")]
        public Data Data { get; set; }
    }

    public class ProfileData
    {
        [SerializationPropertyName("name")]
        public string Name { get; set; }

        [SerializationPropertyName("id")]
        public int Id { get; set; }

        [SerializationPropertyName("bindUid")]
        public object BindUid { get; set; }

        [SerializationPropertyName("stores")]
        public Stores Stores { get; set; }

        [SerializationPropertyName("version")]
        public int Version { get; set; }
    }

    public class _0
    {
        [SerializationPropertyName("identityHash")]
        public string IdentityHash { get; set; }

        [SerializationPropertyName("gachaBanners")]
        public GachaBanners GachaBanners { get; set; }

        [SerializationPropertyName("gachaTypes")]
        public GachaTypes GachaTypes { get; set; }

        [SerializationPropertyName("items")]
        public Items Items { get; set; }

        [SerializationPropertyName("lastManualImportUid")]
        public int LastManualImportUid { get; set; }

        [SerializationPropertyName("share")]
        public Share Share { get; set; }

        [SerializationPropertyName("itemAppend")]
        public ItemAppend ItemAppend { get; set; }

        [SerializationPropertyName("flags")]
        public Flags Flags { get; set; }
    }

    public class _1
    {
        [SerializationPropertyName("itemList")]
        public List<object> ItemList { get; set; }
    }

    public class _2
    {
        [SerializationPropertyName("version")]
        public int Version { get; set; }

        [SerializationPropertyName("enabled")]
        public Dictionary<string, bool> Enabled { get; set; }

        [SerializationPropertyName("arcadeEnabled")]
        public ArcadeEnabled ArcadeEnabled { get; set; }

        [SerializationPropertyName("poEnabled")]
        public PoEnabled PoEnabled { get; set; }
    }

    public class _3
    {
        [SerializationPropertyName("version")]
        public int Version { get; set; }

        [SerializationPropertyName("settings")]
        public Settings Settings { get; set; }
    }

    public class ArcadeEnabled
    {
    }

    public class Data
    {
        [SerializationPropertyName("actionIdx")]
        public long ActionIdx { get; set; }

        [SerializationPropertyName("profileIdx")]
        public int ProfileIdx { get; set; }

        [SerializationPropertyName("profiles")]
        public Dictionary<string, ProfileData> Profiles { get; set; }

        [SerializationPropertyName("curProfileId")]
        public int CurProfileId { get; set; }
    }

    public class Flags
    {
        [SerializationPropertyName("recalc")]
        public bool Recalc { get; set; }
    }

    public class GachaBanners
    {
    }

    public class GachaTypes
    {
    }

    public class ItemAppend
    {
        [SerializationPropertyName("1011")]
        public int _1011 { get; set; }

        [SerializationPropertyName("1031")]
        public int _1031 { get; set; }

        [SerializationPropertyName("1081")]
        public int _1081 { get; set; }
    }

    public class Items
    {
    }

    public class PoEnabled
    {
    }

    public class Settings
    {
    }

    public class Share
    {
        [SerializationPropertyName("name")]
        public string Name { get; set; }

        [SerializationPropertyName("profile")]
        public int Profile { get; set; }
    }

    public class Stores
    {
        [SerializationPropertyName("0")]
        public _0 _0 { get; set; }

        [SerializationPropertyName("1")]
        public _1 _1 { get; set; }

        [SerializationPropertyName("2")]
        public _2 _2 { get; set; }

        [SerializationPropertyName("3")]
        public _3 _3 { get; set; }
    }
}
