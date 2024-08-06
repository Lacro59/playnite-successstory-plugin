using CommonPluginsShared.Converters;
using Playnite.SDK;
using Playnite.SDK.Data;
using SuccessStory.Services;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace SuccessStory.Models
{
    /// <summary>
    /// Class for the ListAll
    /// </summary>
    public class ListAll
    {
        private SuccessStoryDatabase PluginDatabase => SuccessStory.PluginDatabase;


        public string Id { get; set; }
        public string Icon { get; set; }
        public string Name { get; set; }
        public DateTime? LastActivity { get; set; }
        public string SourceName { get; set; }
        public string SourceIcon { get; set; }
        public bool IsManual { get; set; }


        public DateTime? FirstUnlock { get; set; }
        public DateTime? LastUnlock { get; set; }
        public List<DateTime> DatesUnlock { get; set; }

        public float Gamerscore { get; set; }


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
        public RelayCommand<Guid> GoToGame => PluginDatabase.GoToGame;

        [DontSerialize]
        public bool GameExist => API.Instance.Database.Games.Get(GameId) != null;


        public string AchIcon { get; set; }
        public bool AchIsGray { get; set; }
        public bool AchEnableRaretyIndicator { get; set; }
        public bool AchDisplayRaretyValue { get; set; }
        public string AchName { get; set; }
        public DateTime? AchDateUnlock { get; set; }
        public string AchDescription { get; set; }
        public float AchPercent { get; set; }
        public string AchNameWithDateUnlock
        {
            get
            {
                string NameWithDateUnlock = AchName;

                if (AchDateUnlock != null && AchDateUnlock != default(DateTime) && AchDateUnlock != new DateTime(1982, 12, 15, 0, 0, 0))
                {
                    LocalDateTimeConverter converter = new LocalDateTimeConverter();
                    NameWithDateUnlock += " (" + converter.Convert(AchDateUnlock, null, null, CultureInfo.CurrentCulture) + ")";
                }

                return NameWithDateUnlock;
            }
        }

        public bool AchIsUnlock { get; set; }
        public string AchIconImageUnlocked { get; set; }
        public string AchIconImageLocked { get; set; }
    }
}
