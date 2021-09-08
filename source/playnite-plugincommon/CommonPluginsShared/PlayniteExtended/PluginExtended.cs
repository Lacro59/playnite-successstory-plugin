using CommonPluginsPlaynite.Common;
using CommonPluginsShared.Interfaces;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace CommonPluginsShared.PlayniteExtended
{
    public abstract class PluginExtended<ISettings> : PlaynitePlugin<ISettings>
    {
        public PluginExtended(IPlayniteAPI api) : base(api)
        {

        }
    }


    public abstract class PluginExtended<ISettings, TPluginDatabase> : PlaynitePlugin<ISettings>
        where TPluginDatabase : IPluginDatabase
    {
        public static TPluginDatabase PluginDatabase { get; set; }


        public PluginExtended(IPlayniteAPI api) : base(api)
        {
            TransfertOldDatabase();
            CleanOldDatabase();

            // Get plugin's database if used
            PluginDatabase = typeof(TPluginDatabase).CrateInstance<TPluginDatabase>(PlayniteApi, PluginSettings, this.GetPluginUserDataPath());
            PluginDatabase.InitializeDatabase();
        }


        // TODO Temp; must be deleted
        #region Transfert database directory
        private void TransfertOldDatabase()
        {
            string OldDirectory = string.Empty;
            string NewDirectory = string.Empty;

            OldDirectory = Path.Combine(this.GetPluginUserDataPath(), "Activity");
            NewDirectory = Path.Combine(this.GetPluginUserDataPath(), "GameActivity");
            if (Directory.Exists(OldDirectory))
            {
                Directory.Move(OldDirectory, NewDirectory);
            }

            OldDirectory = Path.Combine(this.GetPluginUserDataPath(), "Achievements");
            NewDirectory = Path.Combine(this.GetPluginUserDataPath(), "SuccessStory");
            if (Directory.Exists(OldDirectory))
            {
                Directory.Move(OldDirectory, NewDirectory);
            }

            OldDirectory = Path.Combine(this.GetPluginUserDataPath(), "Requierements");
            NewDirectory = Path.Combine(this.GetPluginUserDataPath(), "SystemChecker");
            if (Directory.Exists(OldDirectory))
            {
                Directory.Move(OldDirectory, NewDirectory);
            }
        }

        private void CleanOldDatabase()
        {
            string OldDirectory = string.Empty;

            try
            {
                // Clean old database
                OldDirectory = Path.Combine(this.GetPluginUserDataPath(), "activity_old");
                FileSystem.DeleteDirectory(OldDirectory);

                OldDirectory = Path.Combine(this.GetPluginUserDataPath(), "activityDetails_old");
                FileSystem.DeleteDirectory(OldDirectory);
            }
            catch
            {

            }
        }
        #endregion
    }


    public abstract class PlaynitePlugin<ISettings> : GenericPlugin
    {
        internal static readonly ILogger logger = LogManager.GetLogger();
        internal static IResourceProvider resources = new ResourceProvider();

        public ISettings PluginSettings { get; set; }

        public string PluginFolder { get; set; }


        protected PlaynitePlugin(IPlayniteAPI api) : base(api)
        {
            Properties = new GenericPluginProperties { HasSettings = true };

            // Get plugin's settings 
            PluginSettings = typeof(ISettings).CrateInstance<ISettings>(this);

            // Get plugin's location 
            PluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            LoadCommon();
        }

        protected void LoadCommon()
        {
            // Set the common resourses & event
            Common.Load(PluginFolder, PlayniteApi.ApplicationSettings.Language);
            Common.SetEvent(PlayniteApi);
        }
    }
}
