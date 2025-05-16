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
using System.Threading.Tasks;
using FuzzySharp;

namespace SuccessStory.Services
{
    public class SuccessStoryDatabase : PluginDatabaseObject<SuccessStorySettingsViewModel, SuccessStoryCollection, GameAchievements, Achievement>
    {
        public SuccessStory Plugin { get; set; }

        private static object AchievementProvidersLock => new object();
        private static Dictionary<AchievementSource, GenericAchievements> achievementProviders;
        internal static Dictionary<AchievementSource, GenericAchievements> AchievementProviders
        {
            get
            {
                lock (AchievementProvidersLock)
                {
                    if (achievementProviders == null)
                    {
                        achievementProviders = new Dictionary<AchievementSource, GenericAchievements>();

                        // Local method to secure the creation of each provider
                        void TryAddProvider(AchievementSource source, Func<GenericAchievements> factory)
                        {
                            try
                            {
                                Common.LogDebug(true, $"[AchievementsFactory] Creating provider for: {source}");
                                var provider = factory();
                                achievementProviders[source] = provider;
                                Common.LogDebug(true, $"[AchievementsFactory] Successfully created provider for: {source}");
                            }
                            catch (Exception ex)
                            {
                                Common.LogError(ex, false, $"[AchievementsFactory] Error creating provider for {source}", true, PluginDatabase.PluginName);
                            }
                        }

                        // Secure addition of providers
                        TryAddProvider(AchievementSource.GOG, () => new GogAchievements());
                        TryAddProvider(AchievementSource.Epic, () => new EpicAchievements());
                        TryAddProvider(AchievementSource.Origin, () => new OriginAchievements());
                        TryAddProvider(AchievementSource.Overwatch, () => new OverwatchAchievements());
                        TryAddProvider(AchievementSource.Wow, () => new WowAchievements());
                        TryAddProvider(AchievementSource.Playstation, () => new PSNAchievements());
                        TryAddProvider(AchievementSource.RetroAchievements, () => new RetroAchievements());
                        TryAddProvider(AchievementSource.RPCS3, () => new Rpcs3Achievements());
                        TryAddProvider(AchievementSource.Starcraft2, () => new Starcraft2Achievements());
                        TryAddProvider(AchievementSource.Steam, () => new SteamAchievements());
                        TryAddProvider(AchievementSource.Xbox, () => new XboxAchievements());
                        TryAddProvider(AchievementSource.GenshinImpact, () => new GenshinImpactAchievements());
                        TryAddProvider(AchievementSource.WutheringWaves, () => new WutheringWavesAchievements());
                        TryAddProvider(AchievementSource.HonkaiStarRail, () => new HonkaiStarRailAchievements());
                        TryAddProvider(AchievementSource.ZenlessZoneZero, () => new ZenlessZoneZeroAchievements());
                        TryAddProvider(AchievementSource.GuildWars2, () => new GuildWars2Achievements());
                        TryAddProvider(AchievementSource.GameJolt, () => new GameJoltAchievements());
                        TryAddProvider(AchievementSource.Local, () => SteamAchievements.GetLocalSteamAchievementsProvider());
                    }
                }
                return achievementProviders;
            }
        }

        public SuccessStoryDatabase(SuccessStorySettingsViewModel pluginSettings, string pluginUserDataPath) : base(pluginSettings, "SuccessStory", pluginUserDataPath)
        {
            TagBefore = "[SS]";
        }


        public void InitializeClient(SuccessStory plugin)
        {
            Plugin = plugin;
        }


        protected override bool LoadDatabase()
        {
            try
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                Database = new SuccessStoryCollection(Paths.PluginDatabasePath);
                Database.SetGameInfo<Achievement>();

                DeleteDataWithDeletedGame();

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                Logger.Info($"LoadDatabase with {Database.Count} items - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)}");
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
                return false;
            }

            return true;
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

                        ExophaseAchievements exophaseAchievements = new ExophaseAchievements();
                        gameAchievements = exophaseAchievements.GetAchievements(game, searchResult);
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
                Add(gameAchievements);
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


        private GameAchievements SetEstimateTimeToUnlock(Game game, GameAchievements gameAchievements)
        {
            if (game != null && (gameAchievements?.HasAchievements ?? false))
            {
                try
                {
                    EstimateTimeToUnlock estimateTimeSteam = new EstimateTimeToUnlock();
                    EstimateTimeToUnlock estimateTimeXbox = new EstimateTimeToUnlock();

                    List<TrueAchievementSearch> listGames = TrueAchievements.SearchGame(game, OriginData.Steam);
                    if (listGames.Count > 0)
                    {
                        var fuzzList = listGames.Select(x => new { MatchPercent = Fuzz.Ratio(game.Name, x.GameName), Data = x })
                            .OrderByDescending(x => x.MatchPercent)
                            .ToList();

                        if (fuzzList.First().Data.GameUrl.IsNullOrEmpty())
                        {
                            Logger.Warn($"No TrueAchievements (Steam) url for {game.Name}");
                        }
                        else
                        {
                            estimateTimeSteam = TrueAchievements.GetEstimateTimeToUnlock(fuzzList.First().Data.GameUrl);
                        }
                    }
                    else
                    {
                        Logger.Warn($"Game not found on TrueSteamAchivements (Steam) for {game.Name}");
                    }

                    listGames = TrueAchievements.SearchGame(game, OriginData.Xbox);
                    if (listGames.Count > 0)
                    {
                        var fuzzList = listGames.Select(x => new { MatchPercent = Fuzz.Ratio(game.Name, x.GameName), Data = x })
                            .OrderByDescending(x => x.MatchPercent)
                            .ToList();

                        if (fuzzList.First().Data.GameUrl.IsNullOrEmpty())
                        {
                            Logger.Warn($"No TrueAchievements (Xbox) url for {game.Name}");
                        }
                        else
                        {
                            estimateTimeXbox = TrueAchievements.GetEstimateTimeToUnlock(fuzzList.First().Data.GameUrl);
                        }
                    }
                    else
                    {
                        Logger.Warn($"Game not found on TrueAchivements (Xbox) for {game.Name}");
                    }

                    if (estimateTimeSteam.DataCount >= estimateTimeXbox.DataCount)
                    {
                        Common.LogDebug(true, $"Get EstimateTime (Steam) for {game.Name}");
                        gameAchievements.EstimateTime = estimateTimeSteam;
                    }
                    else
                    {
                        Common.LogDebug(true, $"Get EstimateTime (Xbox) for {game.Name}");
                        gameAchievements.EstimateTime = estimateTimeXbox;
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginName);
                }
            }

            return gameAchievements;
        }

        public enum AchievementSource
        {
            None,
            Local,
            Playstation,
            Steam,
            GOG,
            Epic,
            Origin,
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
            ZenlessZoneZero
        }

        private static AchievementSource GetAchievementSourceFromLibraryPlugin(SuccessStorySettings settings, Game game)
        {
            ExternalPlugin pluginType = PlayniteTools.GetPluginType(game.PluginId);
            if (pluginType == ExternalPlugin.None)
            {
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
                        return AchievementSource.Origin;
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
            AchievementSource achievementSource = AchievementSource.None;

            if (game.GameActions == null)
            {
                return achievementSource;
            }

            foreach (GameAction action in game.GameActions)
            {
                if (action.Type != GameActionType.Emulator)
                {
                    continue;
                }
                else
                {
                    achievementSource = AchievementSource.RetroAchievements;
                }

                if (PlayniteTools.GameUseRpcs3(game) && settings.EnableRpcs3Achievements)
                {
                    return AchievementSource.RPCS3;
                }

                // TODO With the emulator migration problem emulator.BuiltInConfigId is null
                // TODO emulator.BuiltInConfigId = "retroarch" is limited; other emulators has RA
                if (game.Platforms?.Count > 0)
                {
                    string PlatformName = game.Platforms.FirstOrDefault().Name;
                    Guid PlatformId = game.Platforms.FirstOrDefault().Id;
                    int consoleID = settings.RaConsoleAssociateds.Find(x => x.Platforms.Find(y => y.Id == PlatformId) != null)?.RaConsoleId ?? 0;
                    if (settings.EnableRetroAchievements && consoleID != 0)
                    {
                        return AchievementSource.RetroAchievements;
                    }
                }
                else
                {
                    Logger.Warn($"No platform for {game.Name}");
                }
            }

            return achievementSource;
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

        public override void RefreshNoLoader(Guid id)
        {
            Game game = API.Instance.Database.Games.Get(id);
            GameAchievements loadedItem = Get(id, true);
            GameAchievements webItem = null;

            if (loadedItem?.IsIgnored ?? true)
            {
                return;
            }

            Logger.Info($"RefreshNoLoader({game?.Name} - {game?.Id})");

            if (loadedItem.IsManual)
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

            bool mustUpdate = true;
            if (webItem != null && !webItem.HasAchievements)
            {
                mustUpdate = !loadedItem.HasAchievements;
            }

            if (webItem != null && !ReferenceEquals(loadedItem, webItem) && mustUpdate)
            {
                if (webItem.HasAchievements)
                {
                    webItem = SetEstimateTimeToUnlock(game, webItem);
                }
                Update(webItem);
            }
            else
            {
                webItem = loadedItem;
            }

            ActionAfterRefresh(webItem);
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

                foreach (Guid id in ids)
                {
                    Game game = API.Instance.Database.Games.Get(id);
                    a.Text = $"{PluginName} - {message}"
                        + "\n\n" + $"{a.CurrentProgressValue}/{a.ProgressMaxValue}"
                        + "\n" + game.Name + (game.Source == null ? string.Empty : $" ({game.Source.Name})");

                    if (a.CancelToken.IsCancellationRequested)
                    {
                        break;
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
                            RefreshNoLoader(id);
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false, true, PluginName);
                        }
                    }

                    a.CurrentProgressValue++;
                }
                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                Logger.Info($"Task Refresh(){(a.CancelToken.IsCancellationRequested ? " canceled" : string.Empty)} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {a.CurrentProgressValue}/{ids.Count()} items");

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

                ExophaseAchievements exophaseAchievements = new ExophaseAchievements();
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
                                exophaseAchievements.SetRarety(gameAchievements, AchievementSource.Local);
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

                ExophaseAchievements exophaseAchievements = new ExophaseAchievements();
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
                        gameAchievementsNew = SetEstimateTimeToUnlock(game, gameAchievements);
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
