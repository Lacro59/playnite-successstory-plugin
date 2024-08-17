using CommonPluginsShared;
using Playnite.SDK;
using Playnite.SDK.Data;
using SuccessStory.Services;
using System;
using System.Collections.Generic;

namespace SuccessStory.Models
{
    /// <summary>
    /// Class for the ListView games
    /// </summary>
    public class ListViewGames
    {
        public string Icon100Percent { get; set; }
        public string Id { get; set; }
        public string Icon { get; set; }
        public string Name { get; set; }
        public string CompletionStatus { get; set; }
        public DateTime? LastActivity { get; set; }
        public string SourceName { get; set; }
        public string SourceIcon { get; set; }
        public int ProgressionValue { get; set; }
        public int Total { get; set; }
        public string TotalPercent { get; set; }
        public float TotalGamerScore { get; set; }
        public int Unlocked { get; set; }
        public bool IsManual { get; set; }


        public DateTime? FirstUnlock { get; set; }
        public DateTime? LastUnlock { get; set; }
        public List<DateTime> DatesUnlock { get; set; }


        public AchRaretyStats Common { get; set; }
        public AchRaretyStats NoCommon { get; set; }
        public AchRaretyStats Rare { get; set; }
        public AchRaretyStats UltraRare { get; set; }

        [DontSerialize]
        public Guid GameId
        {
            get
            {
                _ = Guid.TryParse(Id, out Guid result);
                return result;
            }
        }

        [DontSerialize]
        public bool GameExist => API.Instance.Database.Games.Get(GameId) != null;
    }
}
