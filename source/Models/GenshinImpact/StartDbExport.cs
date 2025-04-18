using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Models.GenshinImpact
{
    public class StartDbExport
    {
        [SerializationPropertyName("user")]
        public User User { get; set; }

        [SerializationPropertyName("signature")]
        public string Signature { get; set; }
    }

    public class Gi
    {
        [SerializationPropertyName("achievements")]
        public List<int> Achievements { get; set; }

        [SerializationPropertyName("uids")]
        public List<object> Uids { get; set; }
    }

    public class Hsr
    {
        [SerializationPropertyName("achievements")]
        public List<int> Achievements { get; set; }

        [SerializationPropertyName("uids")]
        public List<object> Uids { get; set; }
    }

    public class User
    {
        [SerializationPropertyName("username")]
        public string Username { get; set; }

        [SerializationPropertyName("hsr")]
        public Hsr Hsr { get; set; }

        [SerializationPropertyName("zzz")]
        public Zzz Zzz { get; set; }

        [SerializationPropertyName("gi")]
        public Gi Gi { get; set; }
    }

    public class Zzz
    {
        [SerializationPropertyName("achievements")]
        public List<int> Achievements { get; set; }

        [SerializationPropertyName("uids")]
        public List<object> Uids { get; set; }
    }
}
