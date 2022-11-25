using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SuccessStory.Services
{
    public class SuccessStorySearch : SearchContext
    {
        private static IResourceProvider resources = new ResourceProvider();
        private readonly SuccessStoryDatabase PluginDatabase = SuccessStory.PluginDatabase;


        public SuccessStorySearch()
        {
            Description = resources.GetString("LOCSuccessStorySearchDescription");
            Label = PluginDatabase.PluginName;
            Hint = resources.GetString("LOCSuccessStorySearchHint");
            Delay = 500;
        }

        public override IEnumerable<SearchItem> GetSearchResults(GetSearchResultsArgs args)
        {
            List<SearchItem> searchItems = new List<SearchItem>();

            try
            {
                // Parameters
                bool hasNp = false;
                bool hasFav = false;
                bool hasPercent = false;
                string paramsPercent = string.Empty;
                bool hasTime = false;
                string paramsTime = string.Empty;
                List<string> stores = new List<string>();

                args.SearchTerm.Split(' ').ForEach(x =>
                {
                    if (x.Contains("-percent=", StringComparison.InvariantCultureIgnoreCase))
                    {
                        hasPercent = true;
                        paramsPercent = x.Replace("-percent=", string.Empty);
                    }

                    if (x.Contains("-time=", StringComparison.InvariantCultureIgnoreCase))
                    {
                        hasTime = true;
                        paramsTime = x.Replace("-time=", string.Empty);
                    }

                    if (!hasNp) hasNp = x.IsEqual("-np");
                    if (!hasFav) hasFav = x.IsEqual("-fav");

                    if (x.Contains("-stores=", StringComparison.InvariantCultureIgnoreCase))
                    {
                        stores = x.Replace("-stores=", string.Empty, StringComparison.InvariantCultureIgnoreCase).Split(',').ToList();
                    }
                });

                string SearchTerm = Regex.Replace(args.SearchTerm, @"-stores=(\w*,)*\w*", string.Empty, RegexOptions.IgnoreCase).Trim();
                SearchTerm = Regex.Replace(SearchTerm, @"-percent=(<|>|\w*<>)*\w*", string.Empty, RegexOptions.IgnoreCase).Trim();
                SearchTerm = Regex.Replace(SearchTerm, @"-time=(<|>|\w*<>)*\w*", string.Empty, RegexOptions.IgnoreCase).Trim();
                SearchTerm = Regex.Replace(SearchTerm, @"-\w*", string.Empty, RegexOptions.IgnoreCase).Trim();


                // Search
                PluginDatabase.Database.Where(x => x.Name.Contains(SearchTerm, StringComparison.InvariantCultureIgnoreCase)
                                && !x.IsDeleted
                                && (args.GameFilterSettings.Uninstalled || x.IsInstalled)
                                && (args.GameFilterSettings.Hidden || !x.Hidden)
                                && (!hasNp || x.Playtime == 0)
                                && (!hasFav || x.Favorite)
                                && (!hasPercent || SearchByPercent(x, paramsPercent))
                                && (!hasTime || SearchByTime(x, paramsTime))
                                && (stores.Count == 0 || stores.Any(y => x.Source?.Name?.Contains(y, StringComparison.InvariantCultureIgnoreCase) ?? false))
                                )
                    .ForEach(x =>
                    {
                        bool isOK = true;

                        if (isOK)
                        {
                            searchItems.Add(new GameSearchItem(x.Game, resources.GetString("LOCGameSearchItemActionSwitchTo"), () => API.Instance.MainView.SelectGame(x.Game.Id)));
                        }
                    });

                if (args.CancelToken.IsCancellationRequested)
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return null;
            }

            return searchItems;
        }


        public bool SearchByTime(GameAchievements x, string query)
        {
            if (query.Contains("<>"))
            {
                string[] data = query.Replace("<>", "#").Split('#');
                if (data.Count() == 2)
                {
                    double timeMin = Tools.GetElapsedSeconde(data[0]);
                    double timeMax = Tools.GetElapsedSeconde(data[1]);

                    if (timeMin > -1 && timeMax > -1)
                    {
                        return x.EstimateTime?.EstimateTimeMax == 0 ? false : (x.EstimateTime?.EstimateTimeMax * 3600) >= timeMin && (x.EstimateTime?.EstimateTimeMax * 3600) <= timeMax;
                    }
                }
            }
            else if (query.Contains("<"))
            {
                double time = Tools.GetElapsedSeconde(query.Replace("<", string.Empty));

                if (time > -1)
                {
                    return x.EstimateTime?.EstimateTimeMax == 0 ? false : (x.EstimateTime?.EstimateTimeMax * 3600) <= time;
                }
            }
            else if (query.Contains(">"))
            {
                double time = Tools.GetElapsedSeconde(query.Replace(">", string.Empty));

                if (time > -1)
                {
                    return x.EstimateTime?.EstimateTimeMax == 0 ? false : (x.EstimateTime?.EstimateTimeMax * 3600) >= time;
                }
            }
            else
            {
                double time = Tools.GetElapsedSeconde(query);

                if (time > -1)
                {
                    return x.EstimateTime?.EstimateTimeMax == 0 ? false : (x.EstimateTime?.EstimateTimeMax * 3600) == time;
                }
            }

            return false;
        }


        private bool SearchByPercent(GameAchievements x, string query)
        {
            if (query.Contains("<>"))
            {
                string[] data = query.Replace("<>", "#").Split('#');
                if (data.Count() == 2)
                {
                    if (int.TryParse(data[0], out int percentMin) && int.TryParse(data[1], out int percentMax))
                    {
                        return x.Progression >= percentMin && x.Progression <= percentMax;
                    }
                }
            }
            else if (query.Contains("<"))
            {
                if (int.TryParse(query.Replace("<", string.Empty), out int percent))
                {
                    return x.Progression <= percent;
                }
            }
            else if (query.Contains(">"))
            {
                if (int.TryParse(query.Replace(">", string.Empty), out int percent))
                {
                    return x.Progression >= percent;
                }
            }
            else
            {
                if (int.TryParse(query, out int percent))
                {
                    return x.Progression == percent;
                }
            }

            return false;
        }
    }
}
