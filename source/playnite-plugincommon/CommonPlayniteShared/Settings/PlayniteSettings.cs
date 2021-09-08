using System.IO;

namespace CommonPluginsPlaynite
{
    public class PlayniteSettings
    {
        public static bool IsPortable
        {
            get
            {
                return !File.Exists(PlaynitePaths.UninstallerPath);
            }
        }
    }
}
