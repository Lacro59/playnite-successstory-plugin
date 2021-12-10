using CommonPluginsShared;
using CommonPluginsShared.Converters;
using CommonPluginsShared.Extensions;
using Playnite.SDK;
using QuickSearch.SearchItems;
using SuccessStory.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuccessStory.Models
{
    class QuickSearchItemSource : ISearchSubItemSource<string>
    {
        private SuccessStoryDatabase PluginDatabase = SuccessStory.PluginDatabase;


        public string Prefix => "SuccessStory";

        public bool DisplayAllIfQueryIsEmpty => true;

        public string Icon
        {
            get
            {
                return Path.Combine(PluginDatabase.Paths.PluginPath, "Resources", "command-line.png");
            }
        }


        public IEnumerable<ISearchItem<string>> GetItems()
        {
            return null;
        }

        public IEnumerable<ISearchItem<string>> GetItems(string query)
        {
            if (query.IsEqual("time"))
            {
                return new List<ISearchItem<string>>
                {
                    new CommandItem("<", new List<CommandAction>(), "example: time < 30 s", Icon),
                    new CommandItem("<>", new List<CommandAction>(), "example: time 30 min <> 1 h", Icon),
                    new CommandItem(">", new List<CommandAction>(), "example: time > 2 h", Icon),
                }.AsEnumerable();
            }

            if (query.IsEqual("percent"))
            {
                return new List<ISearchItem<string>>
                {
                    new CommandItem("<", new List<CommandAction>(), "example: percent < 50", Icon),
                    new CommandItem("<>", new List<CommandAction>(), "example: percent 10 <> 30", Icon),
                    new CommandItem(">", new List<CommandAction>(), "example: percent > 90", Icon),
                }.AsEnumerable();
            }

            return new List<ISearchItem<string>>
            {
                new CommandItem("time", new List<CommandAction>(), ResourceProvider.GetString("LOCSsQuickSearchByTime"), Icon),
                new CommandItem("percent", new List<CommandAction>(), ResourceProvider.GetString("LOCSsQuickSearchByPercent"), Icon),
            }.AsEnumerable();
        }

        public Task<IEnumerable<ISearchItem<string>>> GetItemsTask(string query, IReadOnlyList<Candidate> addedItems)
        {
            var parameters = GetParameters(query);
            if (parameters.Count > 0)
            {
                switch (parameters[0].ToLower())
                {
                    case "time":
                        return SearchByTime(query);

                    case "percent":
                        return SearchByPercent(query);
                }
            }

            return null;
        }


        private List<string> GetParameters(string query)
        {
            List<string> parameters = query.Split(' ').ToList();
            if (parameters.Count > 1 && parameters[0].IsNullOrEmpty())
            {
                parameters.RemoveAt(0);
            }
            return parameters;
        }

        private CommandItem GetCommandItem(GameAchievements data, string query)
        {
            DefaultIconConverter defaultIconConverter = new DefaultIconConverter();

            LocalDateTimeConverter localDateTimeConverter = new LocalDateTimeConverter();

            var title = data.Name;
            var icon = defaultIconConverter.Convert(data.Icon, null, null, null).ToString();
            var dateSession = localDateTimeConverter.Convert(data.LastActivity, null, null, CultureInfo.CurrentCulture).ToString();
            var LastSession = data.LastActivity == null ? string.Empty : ResourceProvider.GetString("LOCLastPlayedLabel")
                    + " " + dateSession;
            var infoEstimate = data.EstimateTime?.EstimateTime.IsNullOrEmpty() ?? true ? string.Empty : "  -  [" + data.EstimateTime?.EstimateTime + "]";


            var item = new CommandItem(title, () => PluginDatabase.PlayniteApi.MainView.SelectGame(data.Id), "", null, icon)
            {
                IconChar = null,
                BottomLeft = PlayniteTools.GetSourceName(data.Id),
                BottomCenter = null,
                BottomRight = data.Unlocked + " / " + data.Total + "  -  (" + data.Progression + " %)" + infoEstimate,
                TopLeft = title,
                TopRight = LastSession,
                Keys = new List<ISearchKey<string>>() { new CommandItemKey() { Key = query, Weight = 1 } }
            };

            return item;
        }

        private double GetElapsedSeconde(string value, string type)
        {
            switch (type.ToLower())
            {
                case "h":
                    double h = double.Parse(value);
                    return h * 3600;

                case "min":
                    double m = double.Parse(value);
                    return m * 60;


                case "s":
                    return double.Parse(value);
            }

            return 0;
        }

        private List<KeyValuePair<Guid, GameAchievements>> GetDb(ConcurrentDictionary<Guid, GameAchievements> db)
        {
            return db.Where(x => PluginDatabase.PlayniteApi.Database.Games.Get(x.Key) != null).ToList();
        }


        private Task<IEnumerable<ISearchItem<string>>> SearchByTime(string query)
        {
            var parameters = GetParameters(query);
            var db = GetDb(PluginDatabase.Database.Items);

            if (parameters.Count == 4)
            {
                return Task.Run(() =>
                {
                    var search = new List<ISearchItem<string>>();

                    switch (parameters[1])
                    {
                        case ">":
                            try
                            {
                                double s = GetElapsedSeconde(parameters[2], parameters[3]);
                                foreach (var data in db)
                                {
                                    if (data.Value.EstimateTime?.EstimateTimeMax >= s)
                                    {
                                        search.Add(GetCommandItem(data.Value, query));
                                    }
                                }
                            }
                            catch { }
                            break;


                        case "<":
                            try
                            {
                                double s = GetElapsedSeconde(parameters[2], parameters[3]);
                                foreach (var data in db)
                                {
                                    if (data.Value.EstimateTime?.EstimateTimeMax <= s)
                                    {
                                        search.Add(GetCommandItem(data.Value, query));
                                    }
                                }
                            }
                            catch { }
                            break;
                    }

                    return search.AsEnumerable();
                });
            }

            if (parameters.Count == 6)
            {
                return Task.Run(() =>
                {
                    var search = new List<ISearchItem<string>>();
                    switch (parameters[3])
                    {
                        case "<>":
                            try
                            {
                                double sMin = GetElapsedSeconde(parameters[1], parameters[2]);
                                double sMax = GetElapsedSeconde(parameters[4], parameters[5]);
                                foreach (var data in db)
                                {
                                    if (data.Value.EstimateTime?.EstimateTimeMax >= sMin && data.Value.EstimateTime?.EstimateTimeMax <= sMax)
                                    {
                                        search.Add(GetCommandItem(data.Value, query));
                                    }
                                }
                            }
                            catch { }
                            break;
                    }

                    return search.AsEnumerable();
                });
            }

            return null;
        }

        private Task<IEnumerable<ISearchItem<string>>> SearchByPercent(string query)
        {
            var parameters = GetParameters(query);
            var db = GetDb(PluginDatabase.Database.Items);

            if (parameters.Count == 3)
            {
                return Task.Run(() =>
                {
                    var search = new List<ISearchItem<string>>();
                    switch (parameters[1])
                    {
                        case ">":
                            try
                            {
                                int.TryParse(parameters[2], out int percent);
                                foreach (var data in db)
                                {
                                    if (data.Value.Progression >= percent)
                                    {
                                        search.Add(GetCommandItem(data.Value, query));
                                    }
                                }
                            }
                            catch { }
                            break;


                        case "<":
                            try
                            {
                                int.TryParse(parameters[2], out int percent);
                                foreach (var data in db)
                                {
                                    if (data.Value.Progression <= percent)
                                    {
                                        search.Add(GetCommandItem(data.Value, query));
                                    }
                                }
                            }
                            catch { }
                            break;
                    }

                    return search.AsEnumerable();
                });
            }

            if (parameters.Count == 4)
            {
                return Task.Run(() =>
                {
                    var search = new List<ISearchItem<string>>();
                    switch (parameters[2])
                    {
                        case "<>":
                            try
                            {
                                int.TryParse(parameters[1], out int percentMin);
                                int.TryParse(parameters[3], out int percentMax);
                                foreach (var data in db)
                                {
                                    if (data.Value.Progression >= percentMin && data.Value.Progression <= percentMax)
                                    {
                                        search.Add(GetCommandItem(data.Value, query));
                                    }
                                }
                            }
                            catch { }
                            break;
                    }

                    return search.AsEnumerable();
                });
            }

            return null;
        }
    }
}
