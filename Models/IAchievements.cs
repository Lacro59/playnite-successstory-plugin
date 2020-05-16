using Playnite.SDK;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SuccessStory.Models
{
    interface IAchievements
    {
        //ConcurrentDictionary<Guid, List<Achievements>> Database { get; set; }
        //string DatabasePath { get; set; }

        /// <summary>
        /// Load the database achievements.
        /// </summary>
        /// <param name="Path"></param>
        void Load(string Path);

        /// <summary>
        /// Add / Update list achievements for a game.
        /// </summary>
        /// <param name="gameId"></param>
        /// <param name="data"></param>
        void AddAchievements(Guid gameId, string data);

        /// <summary>
        /// Get list achievements for a game.
        /// </summary>
        /// <param name="gameId"></param>
        List<Achievements>GetAchievementsList(Guid gameId);

        /// <summary>
        /// Get list achievements for a game from the WEB.
        /// </summary>
        /// <param name="gameId"></param>
        /// <returns></returns>
        List<Achievements> GetAchievementsListWEB(Guid gameId, IPlayniteAPI PlayniteApi, string PluginUserDataPath = "");

        /// <summary>
        /// Save the database achievements. 
        /// </summary>
        void Save();
    }
}
