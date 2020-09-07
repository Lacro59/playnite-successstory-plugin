using Playnite.SDK;
using Playnite.SDK.Models;
using PluginCommon;
using SuccessStory.Database;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Clients
{
    abstract class GenericAchievements
    {
        internal static readonly ILogger logger = LogManager.GetLogger();
        internal static readonly IResourceProvider resources = new ResourceProvider();

        internal readonly IPlayniteAPI _PlayniteApi;
        internal readonly SuccessStorySettings _settings;
        internal readonly string _PluginUserDataPath;

        internal string LocalLang { get; set; } = string.Empty;


        public GenericAchievements(IPlayniteAPI PlayniteApi, SuccessStorySettings settings, string PluginUserDataPath)
        {
            _PlayniteApi = PlayniteApi;
            _settings = settings;
            _PluginUserDataPath = PluginUserDataPath;
        }

        public abstract GameAchievements GetAchievements(Game game);
    }
}
