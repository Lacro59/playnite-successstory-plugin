using Playnite.SDK.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using CommonPluginsShared.Extensions;

namespace SuccessStory.Services
{
    public static class AchievementImageResolver
    {
        private static readonly ConcurrentDictionary<string, Dictionary<string, string>> ImagesCache = new ConcurrentDictionary<string, Dictionary<string, string>>();

        private static string GetKey(Game game)
        {
            if (game == null) return string.Empty;
            var id = game.GameId;
            if (id != null)
            {
                var idStr = id.ToString();
                if (!string.IsNullOrEmpty(idStr))
                {
                    return idStr;
                }
            }

            return (game.Name ?? string.Empty).ToLowerInvariant().Trim();
        }

        public static bool HasImages(Game game)
        {
            if (game == null) return false;
            return ImagesCache.ContainsKey(GetKey(game));
        }

        public static bool TryGetImages(Game game, out Dictionary<string, string> images)
        {
            images = null;
            if (game == null) return false;
            return ImagesCache.TryGetValue(GetKey(game), out images);
        }

        public static void RegisterImages(Game game, Dictionary<string, string> images)
        {
            if (game == null || images == null || images.Count == 0) return;
            ImagesCache.AddOrUpdate(GetKey(game), images, (k, v) => images);
        }

        public static void Clear() => ImagesCache.Clear();
    }
}
