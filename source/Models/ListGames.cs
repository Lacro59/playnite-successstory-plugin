using Playnite.SDK;
using Playnite.SDK.Data;
using SuccessStory.Services;
using System;
using System.Windows.Media.Imaging;

namespace SuccessStory.Models
{
    /// <summary>
    /// Class for the ListView games
    /// </summary>
    public class ListViewGames
    {
        private SuccessStoryDatabase PluginDatabase = SuccessStory.PluginDatabase;


        public string Icon100Percent { get; set; }
        public string Id { get; set; }
        public string Icon { get; set; }
        public string Name { get; set; }
        public DateTime? LastActivity { get; set; }
        public string SourceName { get; set; }
        public string SourceIcon { get; set; }
        public int ProgressionValue { get; set; }
        public int Total { get; set; }
        public string TotalPercent { get; set; }
        public int Unlocked { get; set; }
        public bool IsManual { get; set; }

        public AchRaretyStats Common { get; set; }
        public AchRaretyStats NoCommon { get; set; }
        public AchRaretyStats Rare { get; set; }

        [DontSerialize]
        public Guid GameId
        {
            get
            {
                Guid.TryParse(Id, out Guid result);
                return result;
            }
        }

        [DontSerialize]
        public RelayCommand<Guid> GoToGame
        {
            get
            {
                return PluginDatabase.GoToGame;
            }
        }

        [DontSerialize]
        public bool GameExist
        {
            get
            {
                return PluginDatabase.PlayniteApi.Database.Games.Get(GameId) != null;
            }
        }
    }
}
