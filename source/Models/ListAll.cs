using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;

namespace SuccessStory.Models
{
    /// <summary>
    /// Class for the ListAll
    /// </summary>
    public class ListAll
    {
        public string Id { get; set; }
        public string Icon { get; set; }
        public string Name { get; set; }
        public DateTime? LastActivity { get; set; }
        public string SourceName { get; set; }
        public string SourceIcon { get; set; }
        public bool IsManual { get; set; }


        public DateTime? FirstUnlock { get; set; }
        public DateTime? LastUnlock { get; set; }


        [DontSerialize]
        public Guid GameId => Guid.TryParse(Id, out Guid result) ? result : Guid.Empty;

        [DontSerialize]
        public bool GameExist => API.Instance.Database.Games.Get(GameId) != null;


        public bool AchEnableRaretyIndicator { get; set; }
        public bool AchDisplayRaretyValue { get; set; }
        public Achievement Achievement { get; set; }
    }
}
