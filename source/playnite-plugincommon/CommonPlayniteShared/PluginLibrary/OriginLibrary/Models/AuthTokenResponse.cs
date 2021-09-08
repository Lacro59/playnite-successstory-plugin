using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonPluginsPlaynite.PluginLibrary.OriginLibrary.Models
{
    public class AuthTokenResponse
    {
        public string error;
        public string expires_in;
        public string token_type;
        public string access_token;
    }
}
