using Playnite.SDK.Data;
using System.Collections.Generic;

namespace SuccessStory.Models.Wow
{
    public class Data
    {
        [SerializationPropertyName("Realms")]
        public List<Realm> Realms;
    }

    public class Population
    {
        [SerializationPropertyName("id")]
        public string Id;

        [SerializationPropertyName("name")]
        public string Name;

        [SerializationPropertyName("slug")]
        public string Slug;

        [SerializationPropertyName("enum")]
        public string Enum;

        [SerializationPropertyName("__typename")]
        public string Typename;
    }

    public class Realm
    {
        [SerializationPropertyName("name")]
        public string Name;

        [SerializationPropertyName("slug")]
        public string Slug;

        [SerializationPropertyName("locale")]
        public string Locale;

        [SerializationPropertyName("timezone")]
        public string Timezone;

        [SerializationPropertyName("online")]
        public bool Online;

        [SerializationPropertyName("category")]
        public string Category;

        [SerializationPropertyName("realmLockStatus")]
        public RealmLockStatus RealmLockStatus;

        [SerializationPropertyName("type")]
        public Type Type;

        [SerializationPropertyName("population")]
        public Population Population;

        [SerializationPropertyName("__typename")]
        public string Typename;
    }

    public class RealmLockStatus
    {
        [SerializationPropertyName("isLockedForNewCharacters")]
        public string IsLockedForNewCharacters;

        [SerializationPropertyName("isLockedForPct")]
        public string IsLockedForPct;

        [SerializationPropertyName("__typename")]
        public string Typename;
    }

    public class WowRegionResult
    {
        [SerializationPropertyName("data")]
        public Data Data;
    }

    public class Type
    {
        [SerializationPropertyName("id")]
        public string Id;

        [SerializationPropertyName("name")]
        public string Name;

        [SerializationPropertyName("slug")]
        public string Slug;

        [SerializationPropertyName("enum")]
        public string Enum;

        [SerializationPropertyName("__typename")]
        public string Typename;
    }


}
