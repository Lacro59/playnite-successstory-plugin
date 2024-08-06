using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Models
{
    public class Account
    {
        [SerializationPropertyName("battleTag")]
        public BattleTag BattleTag { get; set; }
    }

    public class BattleTag
    {
        [SerializationPropertyName("name")]
        public string Name { get; set; }

        [SerializationPropertyName("code")]
        public string Code { get; set; }
    }

    public class Flags
    {
        [SerializationPropertyName("employee")]
        public bool Employee { get; set; }
    }

    public class BattleNetUser
    {
        [SerializationPropertyName("id")]
        public int Id { get; set; }

        [SerializationPropertyName("battletag")]
        public string Battletag { get; set; }

        [SerializationPropertyName("email")]
        public string Email { get; set; }

        [SerializationPropertyName("mobile_number")]
        public string MobileNumber { get; set; }

        [SerializationPropertyName("account_identifier")]
        public string AccountIdentifier { get; set; }

        [SerializationPropertyName("verified_email_address_flag")]
        public bool VerifiedEmailAddressFlag { get; set; }

        [SerializationPropertyName("provider")]
        public string Provider { get; set; }

        [SerializationPropertyName("token")]
        public string Token { get; set; }

        [SerializationPropertyName("updated")]
        public long Updated { get; set; }

        [SerializationPropertyName("name")]
        public string Name { get; set; }

        [SerializationPropertyName("country")]
        public string Country { get; set; }

        [SerializationPropertyName("flags")]
        public Flags Flags { get; set; }

        [SerializationPropertyName("account")]
        public Account Account { get; set; }
    }
}
