using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Commons
{
    /// <summary>
    /// Get icon from name for "font.ttf".
    /// </summary>
    class TransformIcon
    {
        private static string steam = "";
        private static string gog = "";
        private static string battleNET = "";
        private static string origin = "";
        private static string xbox = "";
        private static string uplay = "";
        private static string epic = "";
        private static string playnite = "";
        private static string bethesda = "";
        private static string humble = "";
        private static string twitch = "";


        public static string Get(string Name)
        {
            string stringReturn;
            switch (Name.ToLower())
            {
                case "steam":
                    stringReturn = steam;
                    break;
                case "gog":
                    stringReturn = gog;
                    break;
                case "battle.net":
                    stringReturn = battleNET;
                    break;
                case "origin":
                    stringReturn = origin;
                    break;
                case "xbox":
                    stringReturn = xbox;
                    break;
                case "uplay":
                    stringReturn = uplay;
                    break;
                case "epic":
                    stringReturn = epic;
                    break;
                case "playnite":
                    stringReturn = playnite;
                    break;
                case "bethesda":
                    stringReturn = bethesda;
                    break;
                case "humble":
                    stringReturn = humble;
                    break;
                case "twitch":
                    stringReturn = twitch;
                    break;
                default:
                    stringReturn = Name;
                    break;
            }
            return stringReturn;
        }
    }
}
