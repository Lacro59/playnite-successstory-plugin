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

        /// <summary>
        /// Override to validate service-specific config and display error messages to the user
        /// </summary>
        /// <param name="playniteAPI"></param>
        /// <param name="plugin"></param>
        /// <returns>false when there are errors, true if everything's good</returns>
        public abstract bool ValidateConfiguration(IPlayniteAPI playniteAPI, Playnite.SDK.Plugins.Plugin plugin, SuccessStorySettings settings);

        protected bool? CachedConfigurationValidationResult { get; set; }

        public abstract bool EnabledInSettings(SuccessStorySettings settings);

        public virtual void ResetCachedConfigurationValidationResult()
        {
            CachedConfigurationValidationResult = null;
        }
    }
}
