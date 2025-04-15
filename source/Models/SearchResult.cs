using System.Collections.Generic;
using System.Linq;

namespace SuccessStory.Models
{
    public class SearchResult
    {
        public string Url { get; set; }
        public string Name { get; set; }
        public string UrlImage { get; set; }
        public List<string> Platforms { get; set; }
        public int AchievementsCount { get; set; }

        public uint AppId { get; set; }

        public string PlatformsFirst => Platforms?.FirstOrDefault();

        public string PlatformsFirstColor
        {
            get
            {
                switch (PlatformsFirst)
                {
                    case "macOS":
                    case "iOS":
                        return "#060606";
                    case "Google Play":
                        return "#6bad50";
                    case "Steam":
                        return "#1b2838";
                    case "PS1":
                    case "PS2":
                    case "PS3":
                    case "PS4":
                    case "PS5":
                    case "PS6":
                    case "PS Vita":
                    case "PSP":
                    case "PS VR":
                    case "PS VR2":
                        return "#296cc8";
                    case "Retro":
                        return "#296cc8";
                    case "GFWL":
                    case "Xbox Live":
                    case "Xbox One":
                    case "Xbox 360":
                    case "Xbox Series":
                    case "Windows 8":
                    case "Windows 10":
                    case "Windows 11":
                    case "WP":
                        return "#107c10";
                    case "Stadia":
                        return "#cd2640";
                    case "EA app":
                    case "Electronic Arts":
                        return "##9f050b";
                    case "Blizzard":
                        return "#01b2f1";
                    case "GOG":
                        return "#5c2f74";
                    case "Ubisoft":
                        return "#0070ff";
                    case "Epic":
                        return "#333333";
                    case "Switch":
                        return "#b50613";
                    case "32X":
                    case "3DO":
                    case "3DS":
                    case "Amiga":
                    case "Amstrad CPC":
                    case "Apple II":
                    case "Arcade":
                    case "Arcadia 2001":
                    case "Arduboy":
                    case "Atari 2600":
                    case "Atari 5200":
                    case "Atari 7800":
                    case "Atari Jaguar":
                    case "Atari Jaguar CD":
                    case "Atari Lynx":
                    case "Atari ST":
                    case "Cassette Vision":
                    case "ColecoVision":
                    case "Commodore 64":
                    case "DOS":
                    case "Dreamcast":
                    case "Elektor TV Games Computer":
                    case "Events":
                    case "Fairchild Channel F":
                    case "FM Towns":
                    case "Game & Watch":
                    case "Game Gear":
                    case "GameCube":
                    case "GB":
                    case "GBA":
                    case "GBC":
                    case "Genesis":
                    case "Hubs":
                    case "Intellivision":
                    case "Interton VC 4000":
                    case "Magnavox Odyssey 2":
                    case "Master System":
                    case "Mega Duck":
                    case "MSX":
                    case "N64":
                    case "NDS":
                    case "Neo Geo CD":
                    case "Neo Geo Pocket":
                    case "NES":
                    case "Nintendo DSi":
                    case "Nokia N-Gage":
                    case "Oric":
                    case "PC Engine":
                    case "PC Engine CD":
                    case "PC-6000":
                    case "PC-8000/8800":
                    case "PC-9800":
                    case "PC-FX":
                    case "Philips CD-i":
                    case "Pokemon Mini":
                    case "Saturn":
                    case "Sega CD":
                    case "Sega Pico":
                    case "SG-1000":
                    case "Sharp X1":
                    case "SNES":
                    case "Standalone":
                    case "Super Cassette Vision":
                    case "Thomson TO8":
                    case "TIC-80":
                    case "Uzebox":
                    case "Vectrex":
                    case "VIC-20":
                    case "Virtual Boy":
                    case "WASM-4":
                    case "Watara Supervision":
                    case "Wii":
                    case "Wii U":
                    case "WonderSwan":
                    case "X68K":
                    case "Xbox":
                    case "Zeebo":
                    case "ZX Spectrum":
                    case "ZX81":
                        return "#1553ae";

                    default:
                        return null;
                }
            }
        }
    }
}
