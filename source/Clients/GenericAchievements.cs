using Playnite.SDK;
using Playnite.SDK.Models;
using SuccessStory.Models;
using SuccessStory.Services;
using System;

namespace SuccessStory.Clients
{
    abstract class GenericAchievements
    {
        internal static readonly ILogger logger = LogManager.GetLogger();
        internal static readonly IResourceProvider resources = new ResourceProvider();

        internal SuccessStoryDatabase PluginDatabase = SuccessStory.PluginDatabase;

        internal string LocalLang { get; set; } = string.Empty;


        public GenericAchievements()
        {

        }


        public GameAchievements GetAchievements(Guid Id)
        {
            Game game = PluginDatabase.PlayniteApi.Database.Games.Get(Id);
            return GetAchievements(game);
        }

        public abstract GameAchievements GetAchievements(Game game);


        public abstract bool IsConnected();

        public abstract bool IsConfigured();
    }
}
