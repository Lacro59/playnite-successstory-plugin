using CommonPluginsShared;
using CommonPluginsStores.GameJolt;
using CommonPluginsStores.Models;
using Playnite.SDK;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Clients
{
    public class GameJoltAchievements : GenericAchievements
    {
        protected static readonly Lazy<GameJoltApi> gameJoltApi = new Lazy<GameJoltApi>(() => new GameJoltApi(PluginDatabase.PluginName, PlayniteTools.ExternalPlugin.SuccessStory));
        internal static GameJoltApi GameJoltApi => gameJoltApi.Value;


        public GameJoltAchievements() : base("Game Jolt")
        {

        }

        public override GameAchievements GetAchievements(Playnite.SDK.Models.Game game)
        {
            GameAchievements gameAchievements = SuccessStory.PluginDatabase.GetDefault(game);
            List<Achievement> AllAchievements = new List<Achievement>();

            if (IsConfigured())
            {
                try
                {
                    string id = game.GameId;
                    id = game.Name;

                    string user = PluginDatabase.PluginSettings.Settings.GameJoltUser;
                    if (!user.StartsWith("@"))
                    {
                        user = "@" + user;
                    }
                    GameJoltApi.CurrentAccountInfos.Pseudo = user;

                    ObservableCollection<GameAchievement> gogAchievements = GameJoltApi.GetAchievements(id, GameJoltApi.CurrentAccountInfos);
                    if (gogAchievements?.Count > 0)
                    {
                        AllAchievements = gogAchievements.Select(x => new Achievement
                        {
                            ApiName = x.Id,
                            Name = x.Name,
                            Description = x.Description,
                            UrlUnlocked = x.UrlUnlocked,
                            UrlLocked = x.UrlLocked,
                            DateUnlocked = x.DateUnlocked.ToString().Contains(default(DateTime).ToString()) ? (DateTime?)null : x.DateUnlocked,
                            Percent = x.Percent,
                            GamerScore = x.GamerScore,
                            IsHidden = x.IsHidden
                        }).ToList();
                        gameAchievements.Items = AllAchievements;
                    }

                    // Set source link
                    if (gameAchievements.HasAchievements)
                    {
                        gameAchievements.SourcesLink = GameJoltApi.GetAchievementsSourceLink(game.Name, id, GameJoltApi.CurrentAccountInfos);
                    }
                }
                catch (Exception ex)
                {
                    ShowNotificationPluginError(ex);
                    return gameAchievements;
                }
            }
            else
            {
                ShowNotificationPluginNoConfiguration();
            }

            gameAchievements.SetRaretyIndicator();
            return gameAchievements;
        }

        #region Configuration
        public override bool ValidateConfiguration()
        {
            if (!PluginDatabase.PluginSettings.Settings.PluginState.GameJoltIsEnabled)
            {
                ShowNotificationPluginDisable(ResourceProvider.GetString("LOCSuccessStoryNotificationsEpicDisabled"));
                return false;
            }
            else
            {
                if (CachedConfigurationValidationResult == null)
                {
                    CachedConfigurationValidationResult = IsConfigured();

                    if (!(bool)CachedConfigurationValidationResult)
                    {
                        ShowNotificationPluginNoConfiguration();
                    }
                }
                return (bool)CachedConfigurationValidationResult;
            }
        }


        public override bool IsConfigured()
        {
            return !PluginDatabase.PluginSettings.Settings.GameJoltUser.IsNullOrEmpty();
        }

        public override bool EnabledInSettings()
        {
            return PluginDatabase.PluginSettings.Settings.EnableGameJolt;
        }
        #endregion
    }
}
