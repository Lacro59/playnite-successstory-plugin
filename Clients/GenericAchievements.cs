using Playnite.SDK;
using Playnite.SDK.Models;
using SuccessStory.Models;
using System;

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


        public GameAchievements GetAchievements(Guid Id)
        {
            Game game = _PlayniteApi.Database.Games.Get(Id);
            return GetAchievements(game);
        }

        public abstract GameAchievements GetAchievements(Game game);


        public abstract bool IsConnected();

        public abstract bool IsConfigured();
    }
}
