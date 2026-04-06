using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using CommonPluginsShared;
using CommonPluginsShared.Collections;
using SuccessStory.Clients;
using SuccessStory.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using static SuccessStory.Clients.TrueAchievements;
using System.Windows;
using SuccessStory.Views;
using System.Diagnostics;
using System.Text.RegularExpressions;
using static CommonPluginsShared.PlayniteTools;
using CommonPluginsShared.Extensions;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using FuzzySharp;

namespace SuccessStory.Services
{
    public class SuccessStoryDatabase : PluginDatabaseObject<SuccessStorySettingsViewModel, SuccessStoryCollection, GameAchievements, Achievement>
    {
        public SuccessStory Plugin { get; set; }

        private static readonly Lazy<Dictionary<AchievementSource, GenericAchievements>> achievementProviders = new Lazy<Dictionary<AchievementSource, GenericAchievements>>(() =>
        {
            return new Dictionary<AchievementSource, GenericAchievements>
            {
                { AchievementSource.GOG, new GogAchievements() },
                { AchievementSource.Epic, new EpicAchievements() },
                { AchievementSource.EA, new EaAchievements() },
                { AchievementSource.Overwatch, new OverwatchAchievements() },
                { AchievementSource.Wow, new WowAchievements() },
                { AchievementSource.Playstation, new PSNAchievements() },
                { AchievementSource.RetroAchievements, new RetroAchievements() },
                { AchievementSource.RPCS3, new Rpcs3Achievements() },
                { AchievementSource.ShadPS4, new ShadPS4Achievements() },
                { AchievementSource.Xbox360, new Xbox360Achievements() },
                { AchievementSource.Starcraft2, new Starcraft2Achievements() },
                { AchievementSource.Steam, new SteamAchievements() },
                { AchievementSource.Xbox, new XboxAchievements() },
                { AchievementSource.GenshinImpact, new GenshinImpactAchievements() },
                { AchievementSource.WutheringWaves, new WutheringWavesAchievements() },
                { AchievementSource.HonkaiStarRail, new HonkaiStarRailAchievements() },
                { AchievementSource.ZenlessZoneZero, new ZenlessZoneZeroAchievements() },
                { AchievementSource.GuildWars2, new GuildWars2Achievements() },
                { AchievementSource.GameJolt, new GameJoltAchievements() },
                { AchievementSource.Local, SteamAchievements.GetLocalSteamAchievementsProvider() }
            };
        });

        public static IReadOnlyDictionary<AchievementSource, GenericAchievements> AchievementProviders => achievementProviders.Value;

        public SuccessStoryDatabase(SuccessStorySettingsViewModel pluginSettings, string pluginUserDataPath) : base(pluginSettings, "SuccessStory", pluginUserDataPath)
        {
             TagBefore = "[SS]";
        }


        public void InitializeClient(SuccessStory plugin)
        {
            Plugin = plugin;
        }

        public void GetManual(Game game)
        {
            try
            {
                GameAchievements gameAchievements = GetDefault(game);

                SuccessStoreGameSelection ViewExtension = new SuccessStoreGameSelection(game);
                Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(ResourceProvider.GetString("LOCSuccessStory"), ViewExtension);
                _ = windowExtension.ShowDialog();

                if (ViewExtension.GameAchievements != null)
                {
                    gameAchievements = ViewExtension.GameAchievements;
                    gameAchievements.IsManual = true;
                }

                gameAchievements = SetEstimateTimeToUnlock(game, gameAchievements);
                AddOrUpdate(gameAchievements);

                Common.LogDebug(true, $"GetManual({game.Id}) - gameAchievements: {Serialization.ToJson(gameAchievements)}");
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }
        }

        public GameAchievements RefreshManual(Game game)
        {
            Logger.Info($"RefreshManual({game?.Name} - {game?.Id} - {game?.Source?.Name})");
            GameAchievements gameAchievements = null;

            try
            {
                gameAchievements = Get(game, true);
                if (gameAchievements != null && gameAchievements.HasData)
                {
					// eFMann - added Xbox360/Xenia
					if (PlayniteTools.GameUseXbox360(game) && PluginSettings.Settings.EnableXbox360Achievements)
					{
						Xbox360Achievements xbox360Achievements = new Xbox360Achievements();
						if (xbox360Achievements.IsConfigured())
						{
							gameAchievements = xbox360Achievements.GetAchievements(game);
							if (gameAchievements?.HasAchievements ?? false)
							{
								return gameAchievements;
							}
						}
					}

					if (gameAchievements.SourcesLink?.Name.IsEqual("steam") ?? false)
                    {
                        string str = gameAchievements.SourcesLink?.Url.Replace("https://steamcommunity.com/stats/", string.Empty).Replace("/achievements", string.Empty);
                        if (uint.TryParse(str, out uint AppId))
                        {
                            SteamAchievements steamAchievements = new SteamAchievements();
                            steamAchievements.SetLocal();
                            steamAchievements.SetManual();
                            gameAchievements = steamAchievements.GetAchievements(game, AppId);
                        }
                    }
                    else if (gameAchievements.SourcesLink?.Name.IsEqual("exophase") ?? false)
                    {
                        SearchResult searchResult = new SearchResult
                        {
                            Name = gameAchievements.SourcesLink?.GameName,
                            Url = gameAchievements.SourcesLink?.Url
                        };

                        gameAchievements = SuccessStory.ExophaseAchievements.GetAchievements(game, searchResult);
                    }

                    Common.LogDebug(true, $"RefreshManual({game.Id}) - gameAchievements: {Serialization.ToJson(gameAchievements)}");
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return gameAchievements;
        }


        public void GetGenshinImpact(Game game)
        {
            try
            {
                GenshinImpactAchievements genshinImpactAchievements = new GenshinImpactAchievements();
                GameAchievements gameAchievements = genshinImpactAchievements.GetAchievements(game);
                AddOrUpdate(gameAchievements);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }
        }

        public GameAchievements RefreshGenshinImpact(Game game)
        {
            Logger.Info($"RefreshGenshinImpact({game?.Name} - {game?.Id})");
            GameAchievements gameAchievements = null;

            try
            {
                GenshinImpactAchievements genshinImpactAchievements = new GenshinImpactAchievements();
                gameAchievements = genshinImpactAchievements.GetAchievements(game);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return gameAchievements;
        }


        public void GetWutheringWaves(Game game)
        {
            try
            {
                WutheringWavesAchievements wutheringWavesAchievements = new WutheringWavesAchievements();
                GameAchievements gameAchievements = wutheringWavesAchievements.GetAchievements(game);
                AddOrUpdate(gameAchievements);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }
        }

        public GameAchievements RefreshWutheringWaves(Game game)
        {
            Logger.Info($"RefreshWutheringWaves({game?.Name} - {game?.Id})");
            GameAchievements gameAchievements = null;

            try
            {
                WutheringWavesAchievements wutheringWavesAchievements = new WutheringWavesAchievements();
                gameAchievements = wutheringWavesAchievements.GetAchievements(game);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return gameAchievements;
        }


        public void GetHonkaiStarRail(Game game)
        {
            try
            {
                HonkaiStarRailAchievements honkaiStarRailAchievements = new HonkaiStarRailAchievements();
                GameAchievements gameAchievements = honkaiStarRailAchievements.GetAchievements(game);
                AddOrUpdate(gameAchievements);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }
        }

        public GameAchievements RefreshHonkaiStarRail(Game game)
        {
            Logger.Info($"RefreshHonkaiStarRail({game?.Name} - {game?.Id})");
            GameAchievements gameAchievements = null;

            try
            {
                HonkaiStarRailAchievements honkaiStarRailAchievements = new HonkaiStarRailAchievements();
                gameAchievements = honkaiStarRailAchievements.GetAchievements(game);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return gameAchievements;
        }


        public void GetZenlessZoneZero(Game game)
        {
            try
            {
                ZenlessZoneZeroAchievements zenlessZoneZeroAchievements = new ZenlessZoneZeroAchievements();
                GameAchievements gameAchievements = zenlessZoneZeroAchievements.GetAchievements(game);
                AddOrUpdate(gameAchievements);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }
        }

        public GameAchievements RefreshZenlessZoneZero(Game game)
        {
            Logger.Info($"RefreshZenlessZoneZero({game?.Name} - {game?.Id})");
            GameAchievements gameAchievements = null;

            try
            {
                ZenlessZoneZeroAchievements zenlessZoneZeroAchievements = new ZenlessZoneZeroAchievements();
                gameAchievements = zenlessZoneZeroAchievements.GetAchievements(game);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }

            return gameAchievements;
        }


        public override GameAchievements Get(Guid id, bool onlyCache = false, bool force = false)
        {
            GameAchievements gameAchievements = base.GetOnlyCache(id);
            Game game = API.Instance.Database.Games.Get(id);

            // Get from web
            if ((gameAchievements == null && !onlyCache) || force)
            {
                gameAchievements = GetWeb(id);
                AddOrUpdate(gameAchievements);
            }
            else if (gameAchievements == null && game != null)
            {
                gameAchievements = GetDefault(game);
                //Add(gameAchievements);
            }

            return gameAchievements;
        }

        /// <summary>
        /// Generate database achivements for the game if achievement exist and game not exist in database.
        /// </summary>
        /// <param name="game"></param>
        public override GameAchievements GetWeb(Guid id)
        {
            Game game = API.Instance.Database.Games.Get(id);
            GameAchievements gameAchievements = GetDefault(game);
            AchievementSource achievementSource = GetAchievementSource(PluginSettings.Settings, game);

            if (achievementSource == AchievementSource.None)
            {
                Logger.Warn($"No provider find for {game.Name} - {achievementSource} - {game.Source?.Name} - {game?.Platforms?.FirstOrDefault()?.Name}");
            }

            // Generate database only this source
            if (VerifToAddOrShow(PluginSettings.Settings, game))
            {
                GenericAchievements achievementProvider = AchievementProviders[achievementSource];
                RetroAchievements retroAchievementsProvider = achievementProvider as RetroAchievements;
                PSNAchievements psnAchievementsProvider = achievementProvider as PSNAchievements;

                Logger.Info($"Used {achievementProvider} for {game.Name} - {achievementSource}/{game.Source?.Name}/{game?.Platforms?.FirstOrDefault()?.Name}");

                GameAchievements TEMPgameAchievements = Get(game, true);

                if (retroAchievementsProvider != null && (!SuccessStory.IsFromMenu || TEMPgameAchievements.RAgameID != 0))
                {
                    ((RetroAchievements)achievementProvider).GameId = TEMPgameAchievements.RAgameID;
                }
                else if (retroAchievementsProvider != null)
                {
                    ((RetroAchievements)achievementProvider).GameId = 0;
                }


                if (psnAchievementsProvider != null && (!SuccessStory.IsFromMenu || !TEMPgameAchievements.CommunicationId.IsNullOrEmpty()))
                {
                    ((PSNAchievements)achievementProvider).CommunicationId = TEMPgameAchievements.CommunicationId;
                }
                else if (psnAchievementsProvider != null)
                {
                    ((PSNAchievements)achievementProvider).CommunicationId = null;
                }


                gameAchievements = achievementProvider.GetAchievements(game);

                if (retroAchievementsProvider != null)
                {
                    gameAchievements.RAgameID = retroAchievementsProvider.GameId;
                }

                if (!(gameAchievements?.HasAchievements ?? false))
                {
                    Logger.Warn($"No achievements found for {game.Name}");
                }
                else
                {
                    //gameAchievements = SetEstimateTimeToUnlock(game, gameAchievements);
                    Logger.Info($"{gameAchievements.Unlocked}/{gameAchievements.Total} achievements found for {game.Name}");
                }

                Common.LogDebug(true, $"Achievements for {game.Name} - {achievementSource} - {Serialization.ToJson(gameAchievements)}");
            }

            return gameAchievements;
        }


        private async Task<GameAchievements> SetEstimateTimeToUnlockAsync(Game game, GameAchievements gameAchievements, CancellationToken cancellationToken = default)
        {
            if (game != null && (gameAchievements?.HasAchievements ?? false))
            {
                try
                {
                    EstimateTimeToUnlock estimateTimeSteam = new EstimateTimeToUnlock();
                    EstimateTimeToUnlock estimateTimeXbox = new EstimateTimeToUnlock();

                    bool isSteam = game.Source?.Name?.IsEqual("Steam") ?? false;
                    bool isXbox = game.Source?.Name?.IsEqual("Xbox") ?? false;

                    using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                    {
                        cts.CancelAfter(TimeSpan.FromSeconds(10));
                        var token = cts.Token;

                        var searchTask = Task.Run(async () =>
                        {
                            try
                            {
                                // Define searches to perform
                                var searches = new List<OriginData>();
                                if (isSteam)
                                {
                                    searches.Add(OriginData.Steam);
                                    searches.Add(OriginData.Xbox);
                                }
                                else if (isXbox)
                                {
                                    searches.Add(OriginData.Xbox);
                                    searches.Add(OriginData.Steam);
                                }
                                else
                                {
                                    searches.Add(OriginData.Xbox);
                                    searches.Add(OriginData.Steam);
                                }

                                foreach (var origin in searches)
                                {
                                    if (token.IsCancellationRequested)
                                    {
                                        Logger.Debug($"SetEstimateTimeToUnlock cancelled for {game.Name}");
                                        break;
                                    }

                                    string originName = origin == OriginData.Steam ? "Steam" : "Xbox";
                                    try
                                    {
                                        List<TrueAchievementSearch> listGames = TrueAchievements.SearchGame(game, origin);
                                        if (listGames.Count > 0)
                                        {
                                            var fuzzList = listGames.Select(x => new { MatchPercent = Fuzz.Ratio(game.Name, x.GameName), Data = x })
                                                .OrderByDescending(x => x.MatchPercent)
                                                .ToList();

                                            var bestMatch = fuzzList.FirstOrDefault();
                                            if (bestMatch != null && bestMatch.MatchPercent > 60)
                                            {
                                                if (!bestMatch.Data.GameUrl.IsNullOrEmpty())
                                                {
                                                    EstimateTimeToUnlock estimateTime = TrueAchievements.GetEstimateTimeToUnlock(bestMatch.Data.GameUrl);
                                                    if (estimateTime != null)
                                                    {
                                                        if (origin == OriginData.Steam) estimateTimeSteam = estimateTime;
                                                        else estimateTimeXbox = estimateTime;
                                                    }

                                                    // If we found a very good match for our native platform, we can stop here
                                                    if (estimateTime != null && ((isSteam && origin == OriginData.Steam && bestMatch.MatchPercent > 90) ||
                                                        (isXbox && origin == OriginData.Xbox && bestMatch.MatchPercent > 90)))
                                                    {
                                                        Logger.Debug($"Found excellent native match ({bestMatch.MatchPercent}%) for {game.Name} on {originName}. Skipping second search.");
                                                        break;
                                                    }
                                                }
                                                else
                                                {
                                                    Logger.Debug($"No TrueAchievements ({originName}) URL for best match of {game.Name}");
                                                }
                                            }
                                            else if (bestMatch != null)
                                            {
                                                Logger.Debug($"Best match for {game.Name} on {originName} only had {bestMatch.MatchPercent}% similarity. Skipping.");
                                            }
                                        }
                                        else
                                        {
                                            Logger.Debug($"Game not found on TrueAchievements ({originName}) for {game.Name}");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Error(ex, $"Error searching TrueAchievements ({originName}) for {game.Name}");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error(ex, "Error in SetEstimateTimeToUnlock search task");
                            }
                        }, cts.Token);



                        try
                        {
                            var delayTask = Task.Delay(10000, cts.Token);
                            var completedTask = await Task.WhenAny(searchTask, delayTask).ConfigureAwait(false);

                            if (completedTask == searchTask)
                            {
                                await searchTask.ConfigureAwait(false); // Propagate exceptions
                            }
                            else
                            {
                                Logger.Debug($"SetEstimateTimeToUnlockAsync timed out for {game.Name}");
                            }
                        }
                        catch (Exception ex)
                        {
                            if (ex is OperationCanceledException)
                            {
                                Logger.Debug($"SetEstimateTimeToUnlockAsync timed out or cancelled for {game.Name}");
                            }
                            else
                            {
                                Logger.Error(ex, $"Error waiting for search task for {game.Name}");
                            }
                        }

                        if (estimateTimeSteam.DataCount >= estimateTimeXbox.DataCount && estimateTimeSteam.DataCount > 0)
                        {
                            Common.LogDebug(true, $"Using EstimateTime (Steam) for {game.Name}");
                            gameAchievements.EstimateTime = estimateTimeSteam;
                        }
                        else if (estimateTimeXbox.DataCount > 0)
                        {
                            Common.LogDebug(true, $"Using EstimateTime (Xbox) for {game.Name}");
                            gameAchievements.EstimateTime = estimateTimeXbox;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginName);
                }
            }

            return gameAchievements;
        }

        /// <summary>
        /// Synchronous wrapper for SetEstimateTimeToUnlockAsync.
        /// WARNING: Uses Task.Run to avoid deadlock on UI threads.
        /// Prefer calling SetEstimateTimeToUnlockAsync directly when possible.
        /// </summary>
        private GameAchievements SetEstimateTimeToUnlock(Game game, GameAchievements gameAchievements, CancellationToken cancellationToken = default)
        {
            // Use Task.Run to offload to thread pool and avoid capturing sync context
            return Task.Run(() => SetEstimateTimeToUnlockAsync(game, gameAchievements, cancellationToken)).GetAwaiter().GetResult();
        }

        public enum AchievementSource
        {
            None,
            Local,
            Playstation,
            Steam,
            GOG,
            Epic,
            EA,
            Xbox,
            RetroAchievements,
            RPCS3,
            Overwatch,
            Starcraft2,
            Wow,
            GenshinImpact,
            GuildWars2,
            GameJolt,
            WutheringWaves,
            HonkaiStarRail,
            ZenlessZoneZero,
			ShadPS4,
			Xbox360
		}

        private static AchievementSource GetAchievementSourceFromLibraryPlugin(SuccessStorySettings settings, Game game)
        {
            ExternalPlugin pluginType = PlayniteTools.GetPluginType(game.PluginId);
            if (pluginType == ExternalPlugin.None)
			{
				if (settings.EnableXbox360Achievements && GameUseXbox360(game)) // eFMann - added Xbox350 source
				{
					return AchievementSource.Xbox360;
				}
				if (game.Source?.Name?.Contains("Xbox Game Pass", StringComparison.OrdinalIgnoreCase) ?? false)
                {
                    return AchievementSource.Xbox;
                }
                if (game.Source?.Name?.Contains("Microsoft Store", StringComparison.OrdinalIgnoreCase) ?? false)
                {
                    return AchievementSource.Xbox;
                }

                return AchievementSource.None;
            }

            switch (pluginType)
            {
                case ExternalPlugin.BattleNetLibrary:
                    switch (game.Name.ToLowerInvariant())
                    {
                        case "overwatch":
                        case "overwatch 2":
                            if (settings.EnableOverwatchAchievements)
                            {
                                return AchievementSource.None;
                                //return AchievementSource.Overwatch;
                            }
                            break;

                        case "starcraft 2":
                        case "starcraft ii":
                            if (settings.EnableSc2Achievements)
                            {
                                return AchievementSource.None;
                                //return AchievementSource.Starcraft2;
                            }
                            break;

                        case "wow":
                        case "world of warcraft":
                            if (settings.EnableWowAchievements)
                            {
                                return AchievementSource.Wow;
                            }
                            break;

                        default:
                            break;
                    }
                    break;

                case ExternalPlugin.GogLibrary:
                case ExternalPlugin.GogOssLibrary:
                    if (settings.EnableGog)
                    {
                        return AchievementSource.GOG;
                    }
                    break;

                case ExternalPlugin.EpicLibrary:
                case ExternalPlugin.LegendaryLibrary:
                    if (settings.EnableEpic)
                    {
                        return AchievementSource.Epic;
                    }
                    break;

                case ExternalPlugin.OriginLibrary:
                    if (settings.EnableOrigin)
                    {
                        return AchievementSource.EA;
                    }
                    break;

                case ExternalPlugin.PSNLibrary:
                    if (settings.EnablePsn)
                    {
                        return AchievementSource.Playstation;
                    }
                    break;

                case ExternalPlugin.SteamLibrary:
                    if (settings.EnableSteam)
                    {
                        return AchievementSource.Steam;
                    }
                    break;

                case ExternalPlugin.XboxLibrary:
                case ExternalPlugin.XCloud:
					if (settings.EnableXbox360Achievements && GameUseXbox360(game)) // eFMann
					{
						return AchievementSource.Xbox360;
					}
					if (settings.EnableXbox)
                    {
                        return AchievementSource.Xbox;
                    }
                    break;

                case ExternalPlugin.GameJoltLibrary:
                    if (settings.EnableGameJolt)
                    {
                        return AchievementSource.GameJolt;
                    }
                    break;

                case ExternalPlugin.None:
                    break;
                case ExternalPlugin.IndiegalaLibrary:
                    break;
                case ExternalPlugin.AmazonGamesLibrary:
                    break;
                case ExternalPlugin.BethesdaLibrary:
                    break;
                case ExternalPlugin.HumbleLibrary:
                    break;
                case ExternalPlugin.ItchioLibrary:
                    break;
                case ExternalPlugin.RockstarLibrary:
                    break;
                case ExternalPlugin.TwitchLibrary:
                    break;
                case ExternalPlugin.OculusLibrary:
                    break;
                case ExternalPlugin.RiotLibrary:
                    break;
                case ExternalPlugin.UplayLibrary:
                    break;
                case ExternalPlugin.SuccessStory:
                    break;
                case ExternalPlugin.CheckDlc:
                    break;
                case ExternalPlugin.EmuLibrary:
                    break;

                default:
                    break;
            }

            return AchievementSource.None;
        }

        private static AchievementSource GetAchievementSourceFromEmulator(SuccessStorySettings settings, Game game)
        {
			// Priority 1: Check for RPCS3 or ShadPS4 or Xenia
			if (settings.EnableRpcs3Achievements && PlayniteTools.GameUseRpcs3(game))
            {
                return AchievementSource.RPCS3;
            }
			if (settings.EnableShadPS4Achievements && PlayniteTools.GameUseShadPS4(game))
            {
                return AchievementSource.ShadPS4;
			}
			if (settings.EnableXbox360Achievements && PlayniteTools.GameUseXbox360(game) )
			{
				return AchievementSource.Xbox360;
			}

			// Priority 2: Check if any game action is an emulator
			bool hasEmulatorAction = game.GameActions != null && game.GameActions.Any(action => action.Type == GameActionType.Emulator);
            if (settings.EnableRetroAchievements && hasEmulatorAction)
            {
                return AchievementSource.RetroAchievements;
            }

            // Priority 3: Check for RetroAchievements via platform
            if (settings.EnableRetroAchievements && game.Platforms?.Count > 0)
            {
                var platform = game.Platforms.FirstOrDefault();
                if (platform != null)
                {
                    int consoleID = settings.RaConsoleAssociateds
                        .FirstOrDefault(x => x.Platforms.Any(y => y.Id == platform.Id))
                        ?.RaConsoleId ?? 0;

                    if (consoleID != 0)
                    {
                        return AchievementSource.RetroAchievements;
                    }
                }
            }
            else if (settings.EnableRetroAchievements && (game.Platforms == null || game.Platforms.Count == 0))
            {
                Logger.Warn($"GetAchievementSourceFromEmulator: No platform for {game.Name}");
            }

            return AchievementSource.None;
        }

        public static AchievementSource GetAchievementSource(SuccessStorySettings settings, Game game, bool ignoreSpecial = false)
        {
            if (game.Name.IsEqual("Genshin Impact") && !ignoreSpecial)
            {
                return AchievementSource.GenshinImpact;
            }
            if (game.Name.IsEqual("Wuthering Waves") && !ignoreSpecial)
            {
                return AchievementSource.WutheringWaves;
            }
            if (game.Name.IsEqual("Honkai: Star Rail") && !ignoreSpecial)
            {
                return AchievementSource.HonkaiStarRail;
            }
            if (game.Name.IsEqual("Zenless Zone Zero") && !ignoreSpecial)
            {
                return AchievementSource.ZenlessZoneZero;
            }

            if (game.Name.IsEqual("Guild Wars 2"))
            {
                return AchievementSource.GuildWars2;
            }

            AchievementSource source = GetAchievementSourceFromLibraryPlugin(settings, game);
            if (source != AchievementSource.None)
            {
                return source;
            }

            source = GetAchievementSourceFromEmulator(settings, game);
            if (source != AchievementSource.None)
            {
                return source;
            }

            //any game can still get local achievements when that's enabled
            return settings.EnableLocal ? AchievementSource.Local : AchievementSource.None;
        }


        /// <summary>
        /// Validate achievement configuration for the service this game is linked to
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="game"></param>
        /// <returns>true when achievements can be retrieved for the supplied game</returns>
        public static bool VerifToAddOrShow(SuccessStorySettings settings, Game game)
        {
            AchievementSource achievementSource = GetAchievementSource(settings, game);
            if (!AchievementProviders.TryGetValue(achievementSource, out GenericAchievements achievementProvider))
            {
                return false;
            }

            if (achievementProvider.EnabledInSettings())
            {
                return achievementProvider.ValidateConfiguration();
            }

            Logger.Warn($"VerifToAddOrShow() found no action for {game.Name} - {achievementSource} - {game.Source?.Name} - {game?.Platforms?.FirstOrDefault()?.Name}");
            return false;
        }
        
        public bool VerifAchievementsLoad(Guid gameId)
        {
            return GetOnlyCache(gameId) != null;
        }


        public override void SetThemesResources(Game game)
        {
            if (game == null)
            {
                Logger.Warn("game null in SetThemesResources()");
                return;
            }

            GameAchievements gameAchievements = Get(game, true);

            if (gameAchievements == null || !gameAchievements.HasData)
            {
                PluginSettings.Settings.HasData = false;

                PluginSettings.Settings.Is100Percent = false;
                PluginSettings.Settings.Common = new AchRaretyStats();
                PluginSettings.Settings.NoCommon = new AchRaretyStats();
                PluginSettings.Settings.Rare = new AchRaretyStats();
                PluginSettings.Settings.UltraRare = new AchRaretyStats();
                PluginSettings.Settings.Unlocked = 0;
                PluginSettings.Settings.Locked = 0;
                PluginSettings.Settings.Total = 0;
                PluginSettings.Settings.TotalGamerScore = 0;
                PluginSettings.Settings.Percent = 0;
                PluginSettings.Settings.EstimateTimeToUnlock = string.Empty;

                PluginSettings.Settings.ListAchievements = new List<Achievement>();
                PluginSettings.Settings.ListAchUnlockDateAsc = new List<Achievement>();
                PluginSettings.Settings.ListAchUnlockDateDesc = new List<Achievement>();
            }
            else
            {
                PluginSettings.Settings.HasData = gameAchievements.HasData;

                PluginSettings.Settings.Is100Percent = gameAchievements.Is100Percent;
                PluginSettings.Settings.Common = gameAchievements.Common;
                PluginSettings.Settings.NoCommon = gameAchievements.UnCommon;
                PluginSettings.Settings.Rare = gameAchievements.Rare;
                PluginSettings.Settings.UltraRare = gameAchievements.UltraRare;
                PluginSettings.Settings.Unlocked = gameAchievements.Unlocked;
                PluginSettings.Settings.Locked = gameAchievements.Locked;
                PluginSettings.Settings.Total = gameAchievements.Total;
                PluginSettings.Settings.TotalGamerScore = (int)gameAchievements.TotalGamerScore;
                PluginSettings.Settings.Percent = gameAchievements.Progression;
                PluginSettings.Settings.EstimateTimeToUnlock = gameAchievements.EstimateTime?.EstimateTime;

                PluginSettings.Settings.ListAchievements = gameAchievements.Items;
                PluginSettings.Settings.ListAchUnlockDateAsc = gameAchievements.Items?.OrderBy(x => x.DateUnlocked).ThenBy(x => x.Name).ToList();
                PluginSettings.Settings.ListAchUnlockDateDesc = gameAchievements.Items?.OrderByDescending(x => x.DateUnlocked).ThenBy(x => x.Name).ToList();
            }
        }


        public async Task RefreshData(Game game)
        {
            await Task.Run(() =>
            {
                string SourceName = GetSourceName(game);
                string GameName = game.Name;
                bool VerifToAddOrShow = SuccessStoryDatabase.VerifToAddOrShow(PluginSettings.Settings, game);
                GameAchievements gameAchievements = Get(game, true);

                if (!gameAchievements.IsIgnored && VerifToAddOrShow)
                {
                    RefreshNoLoader(game.Id);
                }

                // refresh themes resources
                if (game.Id == GameContext.Id)
                {
                    SetThemesResources(GameContext);
                }
            });
        }

        public override void RefreshNoLoader(Guid id, CancellationToken cancellationToken = default)
        {
            Game game = null;
            GameAchievements loadedItem = null;

            if (Application.Current.Dispatcher.CheckAccess())
            {
                game = API.Instance.Database.Games.Get(id);
                loadedItem = Get(id, true);
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    game = API.Instance.Database.Games.Get(id);
                    loadedItem = Get(id, true);
                });
            }

            GameAchievements webItem = null;

            if (loadedItem?.IsIgnored == true)
            {
                Logger.Info($"RefreshNoLoader: {game?.Name} is ignored");
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();
            Logger.Info($"RefreshNoLoader({game?.Name} - {game?.Id}) - IsIgnored: {loadedItem?.IsIgnored}");

            if (loadedItem?.IsManual == true)
            {
                webItem = game.Name.IsEqual("Genshin Impact") ? RefreshGenshinImpact(game) : game.Name.IsEqual("Wuthering Waves") ? RefreshWutheringWaves(game) : game.Name.IsEqual("Honkai: Star Rail") ? RefreshHonkaiStarRail(game) : game.Name.IsEqual("Zenless Zone Zero") ? RefreshZenlessZoneZero(game) : RefreshManual(game);

                if (webItem != null)
                {
                    webItem.IsManual = true;
                    for (int i = 0; i < webItem.Items.Count; i++)
                    {
                        Achievement found = loadedItem.Items.Find(x => (x.ApiName.IsNullOrEmpty() || x.ApiName.IsEqual(webItem.Items[i].ApiName)) && x.Name.IsEqual(webItem.Items[i].Name));
                        if (found != null)
                        {
                            webItem.Items[i].DateUnlocked = found.DateUnlocked;
                        }
                    }
                    // Check is ok
                    if (loadedItem.Unlocked != webItem.Unlocked && loadedItem.Items?.Count != webItem.Items?.Count)
                    {
                        Logger.Warn($"Unlocked data does not match for {game?.Name}");
                        webItem = loadedItem;
                    }
                }
            }
            else
            {
                webItem = GetWeb(id);
            }

            cancellationToken.ThrowIfCancellationRequested();

            bool mustUpdate = true;
            if (webItem != null && !webItem.HasAchievements)
            {
                mustUpdate = loadedItem != null && !loadedItem.HasAchievements;
            }

            if (webItem != null && !ReferenceEquals(loadedItem, webItem) && mustUpdate)
            {
                try
                {
                    if (webItem.HasAchievements)
                    {
                        webItem = SetEstimateTimeToUnlock(game, webItem, cancellationToken);
                    }
                    Update(webItem);
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginName);
                }
            }
            else
            {
                webItem = loadedItem;
            }

            if (Application.Current.Dispatcher.CheckAccess())
            {
                ActionAfterRefresh(webItem);
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() => ActionAfterRefresh(webItem));
            }
        }

        public override void Refresh(IEnumerable<Guid> ids, string message)
        {
            GlobalProgressOptions options = new GlobalProgressOptions($"{PluginName} - {message}")
            {
                Cancelable = true,
                IsIndeterminate = false
            };

            _ = API.Instance.Dialogs.ActivateGlobalProgress((a) =>
            {
                API.Instance.Database.BeginBufferUpdate();
                Database.BeginBufferUpdate();

                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                a.ProgressMaxValue = ids.Count();

                // Use bounded concurrency to refresh multiple games in parallel while limiting resource usage.
                int maxConcurrency = 4; // tuning: reduce/increase as needed
                var concurrency = new System.Threading.SemaphoreSlim(maxConcurrency);
                try
                {
                    var tasks = new List<Task>();
                    int progressCounter = 0;

                    foreach (Guid id in ids)
                    {
                        try
                        {
                            // Respect cancellation while waiting for a slot
                            concurrency.Wait(a.CancelToken);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }

                        if (a.CancelToken.IsCancellationRequested)
                        {
                            concurrency.Release();
                            break;
                        }

                        // Capture id for closure
                        Guid gameId = id;

                        var task = Task.Run(() =>
                        {
                            try
                            {
                                // Marshal UI/DB access through main dispatcher
                                Game game = null;
                                API.Instance.MainView.UIDispatcher.Invoke(() =>
                                {
                                    game = API.Instance.Database.Games.Get(gameId);
                                });

                                if (game == null)
                                {
                                    return;
                                }

                                // Update progress text (non-blocking)
                                API.Instance.MainView.UIDispatcher.BeginInvoke((Action)(() =>
                                {
                                    a.Text = ResourceProvider.GetString("LOCCommonProcessing")
                                        + "\n" + game.Name + (game.Source == null ? string.Empty : $" ({game.Source.Name})");
                                }));

                                if (a.CancelToken.IsCancellationRequested)
                                {
                                    return;
                                }

                                string sourceName = PlayniteTools.GetSourceName(game);
                                AchievementSource achievementSource = GetAchievementSource(PluginSettings.Settings, game);
                                string gameName = game.Name;
                                bool verifToAddOrShow = VerifToAddOrShow(PluginSettings.Settings, game);
                                GameAchievements gameAchievements = Get(game, true);

                                if (!gameAchievements.IsIgnored && verifToAddOrShow && !gameAchievements.IsManual)
                                {
                                    try
                                    {
                                        RefreshNoLoader(gameId, a.CancelToken);
                                    }
                                    catch (Exception ex)
                                    {
                                        Common.LogError(ex, false, true, PluginName);
                                    }
                                }
                            }
                            finally
                            {
                                int progress = System.Threading.Interlocked.Increment(ref progressCounter);
                                // Marshal progress update to UI thread
                                API.Instance.MainView.UIDispatcher.Invoke(() =>
                                {
                                    a.CurrentProgressValue = progress;
                                });
                                concurrency.Release();
                            }
                        }, a.CancelToken);

                        tasks.Add(task);
                    }

                    // Wait for all scheduled tasks to complete WITHOUT cancellation token
                    // to ensure all tasks finish before we dispose semaphore and end buffer updates
                    try
                    {
                        Task.WhenAll(tasks).Wait();
                    }
                    catch (AggregateException)
                    {
                        // Tasks may have thrown exceptions, but we still need to clean up properly
                    }
                }
                finally
                {
                    // Dispose semaphore only after all tasks have completed
                    concurrency.Dispose();
                }

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                Logger.Info($"Task Refresh(){(a.CancelToken.IsCancellationRequested ? " canceled" : string.Empty)} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {a.CurrentProgressValue}/{ids.Count()} items");

                // End buffer updates only after all tasks have completed
                Database.EndBufferUpdate();
                API.Instance.Database.EndBufferUpdate();
            }, options);
        }

        public override void ActionAfterRefresh(GameAchievements item)
        {
            Game game = API.Instance.Database.Games.Get(item.Id);
            // Add feature
            if ((item?.HasAchievements ?? false) && PluginSettings.Settings.AchievementFeature != null)
            {
                if (game.FeatureIds != null)
                {
                    _ = game.FeatureIds.AddMissing(PluginSettings.Settings.AchievementFeature.Id);
                }
                else
                {
                    game.FeatureIds = new List<Guid> { PluginSettings.Settings.AchievementFeature.Id };
                }
            }

            ChangeCompletionStatus(game);

            API.Instance.Database.Games.Update(game);
        }

        public void ChangeCompletionStatus(Game game)
        {
            if (PluginSettings.Settings.CompletionStatus100Percent != null && PluginSettings.Settings.Auto100PercentCompleted)
            {
                GameAchievements gameAchievements = Get(game, true);
                if ((gameAchievements?.HasAchievements ?? false) && (gameAchievements?.Is100Percent ?? false))
                {
                    game.CompletionStatusId = PluginSettings.Settings.CompletionStatus100Percent.Id;
                }
            }
            API.Instance.Database.Games.Update(game);
        }


        public void RefreshRarety()
        {
            GlobalProgressOptions options = new GlobalProgressOptions($"{PluginName} - {ResourceProvider.GetString("LOCCommonProcessing")}")
            {
                Cancelable = true,
                IsIndeterminate = false
            };

            _ = API.Instance.Dialogs.ActivateGlobalProgress((a) =>
            {
                Logger.Info($"RefreshRarety() started");
                API.Instance.Database.BeginBufferUpdate();
                Database.BeginBufferUpdate();

                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                IEnumerable<GameAchievements> db = Database.Where(x => x.IsManual && x.HasAchievements);
                a.ProgressMaxValue = (double)db.Count();

                SteamAchievements steamAchievements = new SteamAchievements();
                bool SteamConfig = steamAchievements.IsConfigured();

                foreach (GameAchievements gameAchievements in db)
                {
                    a.Text = $"{PluginName} - {ResourceProvider.GetString("LOCCommonProcessing")}"
                        + "\n\n" + $"{a.CurrentProgressValue}/{a.ProgressMaxValue}"
                        + "\n" + gameAchievements.Name + (gameAchievements.Source == null ? string.Empty : $" ({gameAchievements.Source.Name})");

                    if (a.CancelToken.IsCancellationRequested)
                    {
                        break;
                    }

                    try
                    {
                        string sourceName = gameAchievements.SourcesLink?.Name?.ToLower();
                        switch (sourceName)
                        {
                            case "steam":
                                if (uint.TryParse(Regex.Match(gameAchievements.SourcesLink.Url, @"\d+").Value, out uint appId))
                                {
                                    steamAchievements.SetRarity(appId, gameAchievements);
                                }
                                else
                                {
                                    Logger.Warn($"No Steam appId");
                                }
                                break;

                            case "exophase":
                                SuccessStory.ExophaseAchievements.SetRarety(gameAchievements, AchievementSource.Local, a.CancelToken);
                                break;

                            default:
                                Logger.Warn($"No sourcesLink for {gameAchievements.Name} with {sourceName}");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, true, PluginName);
                    }

                    a.CurrentProgressValue++;
                }

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                Logger.Info($"Task RefreshRarety(){(a.CancelToken.IsCancellationRequested ? " canceled" : string.Empty)} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {a.CurrentProgressValue}/{(double)db.Count()} items");

                Database.EndBufferUpdate();
                API.Instance.Database.EndBufferUpdate();
            }, options);
        }

        public void RefreshEstimateTime()
        {
            GlobalProgressOptions options = new GlobalProgressOptions($"{PluginName} - {ResourceProvider.GetString("LOCCommonProcessing")}")
            {
                Cancelable = true,
                IsIndeterminate = false
            };

            _ = API.Instance.Dialogs.ActivateGlobalProgress((a) =>
            {
                Logger.Info($"RefreshEstimateTime() started");
                API.Instance.Database.BeginBufferUpdate();
                Database.BeginBufferUpdate();

                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                IEnumerable<GameAchievements> db = Database.Where(x => x.IsManual && x.HasAchievements);
                a.ProgressMaxValue = db.Count();

                SteamAchievements steamAchievements = new SteamAchievements();
                bool SteamConfig = steamAchievements.IsConfigured();

                foreach (GameAchievements gameAchievements in db)
                {
                    a.Text = $"{PluginName} - {ResourceProvider.GetString("LOCCommonProcessing")}"
                        + "\n\n" + $"{a.CurrentProgressValue}/{a.ProgressMaxValue}"
                        + "\n" + gameAchievements.Name + (gameAchievements.Source == null ? string.Empty : $" ({gameAchievements.Source.Name})");

                    if (a.CancelToken.IsCancellationRequested)
                    {
                        break;
                    }

                    try
                    {
                        Game game = API.Instance.Database.Games.Get(gameAchievements.Id);
                        GameAchievements gameAchievementsNew = Serialization.GetClone(gameAchievements);
                        gameAchievementsNew = SetEstimateTimeToUnlock(game, gameAchievements, a.CancelToken);
                        AddOrUpdate(gameAchievementsNew);
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, true, PluginName);
                    }

                    a.CurrentProgressValue++;
                }

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                Logger.Info($"Task RefreshEstimateTime(){(a.CancelToken.IsCancellationRequested ? " canceled" : string.Empty)} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {a.CurrentProgressValue}/{(double)db.Count()} items");

                Database.EndBufferUpdate();
                API.Instance.Database.EndBufferUpdate();
            }, options);
        }


        #region Tag system
        public override void AddTag(Game game)
        {
            GameAchievements item = Get(game, true);
            if (item.HasAchievements)
            {
                try
                {
                    if (item.EstimateTime == null)
                    {
                        return;
                    }

                    Guid? TagId = FindGoodPluginTags(item.EstimateTime.EstimateTimeMax.ToString());
                    if (TagId != null)
                    {
                        if (game.TagIds != null)
                        {
                            game.TagIds.Add((Guid)TagId);
                        }
                        else
                        {
                            game.TagIds = new List<Guid> { (Guid)TagId };
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Tag insert error with {game.Name}", true, PluginName, string.Format(ResourceProvider.GetString("LOCCommonNotificationTagError"), game.Name));
                    return;
                }
            }
            else if (TagMissing)
            {
                if (game.TagIds != null)
                {
                    game.TagIds.Add((Guid)AddNoDataTag());
                }
                else
                {
                    game.TagIds = new List<Guid> { (Guid)AddNoDataTag() };
                }
            }

            API.Instance.MainView.UIDispatcher?.Invoke(() =>
            {
                API.Instance.Database.Games.Update(game);
                game.OnPropertyChanged();
            });
        }

        internal override Guid? FindGoodPluginTags(string tagName)
        {
            if(int.TryParse(tagName, out int estimateTimeMax))
            {
                return null;
            }

            // Add tag
            if (estimateTimeMax != 0)
            {
                if (estimateTimeMax <= 1)
                {
                    return CheckTagExist($"{ResourceProvider.GetString("LOCCommon0to1")}");
                }
                if (estimateTimeMax <= 6)
                {
                    return CheckTagExist($"{ResourceProvider.GetString("LOCCommon1to5")}");
                }
                if (estimateTimeMax <= 10)
                {
                    return CheckTagExist($"{ResourceProvider.GetString("LOCCommon5to10")}");
                }
                if (estimateTimeMax <= 20)
                {
                    return CheckTagExist($"{ResourceProvider.GetString("LOCCommon10to20")}");
                }
                if (estimateTimeMax <= 30)
                {
                    return CheckTagExist($"{ResourceProvider.GetString("LOCCommon20to30")}");
                }
                if (estimateTimeMax <= 40)
                {
                    return CheckTagExist($"{ResourceProvider.GetString("LOCCommon30to40")}");
                }
                if (estimateTimeMax <= 50)
                {
                    return CheckTagExist($"{ResourceProvider.GetString("LOCCommon40to50")}");
                }
                if (estimateTimeMax <= 60)
                {
                    return CheckTagExist($"{ResourceProvider.GetString("LOCCommon50to60")}");
                }
                if (estimateTimeMax <= 70)
                {
                    return CheckTagExist($"{ResourceProvider.GetString("LOCCommon60to70")}");
                }
                if (estimateTimeMax <= 80)
                {
                    return CheckTagExist($"{ResourceProvider.GetString("LOCCommon70to80")}");
                }
                if (estimateTimeMax <= 90)
                {
                    return CheckTagExist($"{ResourceProvider.GetString("LOCCommon80to90")}");
                }
                if (estimateTimeMax <= 100)
                {
                    return CheckTagExist($"{ResourceProvider.GetString("LOCCommon90to100")}");
                }
                if (estimateTimeMax > 100)
                {
                    return CheckTagExist($"{ResourceProvider.GetString("LOCCommon100plus")}");
                }
            }

            return null;
        }
        #endregion


        public void SetIgnored(GameAchievements gameAchievements)
        {
            if (!gameAchievements.IsIgnored)
            {
                _ = Remove(gameAchievements.Id);
                GameAchievements pluginData = Get(gameAchievements.Id, true);
                pluginData.IsIgnored = true;
                AddOrUpdate(pluginData);
            }
            else
            {
                gameAchievements.IsIgnored = false;
                AddOrUpdate(gameAchievements);
                Refresh(gameAchievements.Id);
            }
        }


        internal override string GetCsvData(GlobalProgressActionArgs a, bool minimum)
        {
            string csvData = string.Empty;

            a.ProgressMaxValue = minimum
                ? Database.Items?.Where(x => x.Value.HasAchievements)?.Count() ?? 0
                : Database.Items?.Where(x => x.Value.HasAchievements)?.Sum(x => x.Value.Items.Count()) ?? 0;

            Database.Items?.Where(x => x.Value.HasAchievements)?.ForEach(x =>
            {
                // Header
                if (csvData.IsNullOrEmpty())
                {
                    csvData = minimum
                        ? "\"Game name\";\"Platform\";\"Achievement link\";\"Achievements\";\"Date last unlock\";\"Gamerscore\";\"100%\";"
                        : "\"Game name\";\"Platform\";\"Achievement link\";\"Achievement name\";\"Description\";\"Date unlock\";\"Is hidden\";\"Percent\";\"Gamerscore\";";
                }

                if (a.CancelToken.IsCancellationRequested)
                {
                    return;
                }

                if (minimum)
                {
                    a.Text = $"{PluginName} - {ResourceProvider.GetString("LOCCommonExtracting")}"
                       + "\n\n" + $"{a.CurrentProgressValue}/{a.ProgressMaxValue}"
                       + "\n" + x.Value.Game?.Name + (x.Value.Game?.Source == null ? string.Empty : $" ({x.Value.Game?.Source.Name})");

                    csvData += Environment.NewLine;
                    csvData += $"\"{x.Value.Name}\";\"{x.Value.Source?.Name ?? x.Value.Platforms?.First()?.Name ?? "Playnite"}\";\"{x.Value.SourcesLink?.Url}\";\"{x.Value.Unlocked + " // " + x.Value.Items.Count}\";\"{x.Value.LastUnlock?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty}\";\"{x.Value.TotalGamerScore}\";\"{(x.Value.Is100Percent ? "X" : string.Empty)}\";";
                    a.CurrentProgressValue++;
                }
                else
                {
                    x.Value.Items.ForEach(y =>
                    {
                        if (a.CancelToken.IsCancellationRequested)
                        {
                            return;
                        }

                        a.Text = $"{PluginName} - {ResourceProvider.GetString("LOCCommonExtracting")}"
                            + "\n\n" + $"{a.CurrentProgressValue}/{a.ProgressMaxValue}"
                            + "\n" + x.Value.Game?.Name + (x.Value.Game?.Source == null ? string.Empty : $" ({x.Value.Game?.Source.Name})");

                        csvData += Environment.NewLine;
                        csvData += $"\"{x.Value.Name}\";\"{x.Value.Source?.Name ?? x.Value.Platforms?.First()?.Name ?? "Playnite"}\";\"{x.Value.SourcesLink?.Url}\";\"{y.Name}\";\"{y.Description}\";\"{y.DateWhenUnlocked?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty}\";\"{(y.IsHidden ? "X" : string.Empty)}\";\"{y.Percent}\";\"{y.GamerScore}\";";
                        a.CurrentProgressValue++;
                    });
                }
            });
            return csvData;
        }
    }
}
