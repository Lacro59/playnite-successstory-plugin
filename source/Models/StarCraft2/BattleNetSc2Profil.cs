using Playnite.SDK.Data;
using System.Collections.Generic;

namespace SuccessStory.Models
{
    public class BattleNetSc2Profil
    {
        public Summary summary { get; set; }
        public Snapshot snapshot { get; set; }
        public Career career { get; set; }
        public SwarmLevels swarmLevels { get; set; }
        public Campaign campaign { get; set; }
        public List<CategoryPointProgress> categoryPointProgress { get; set; }
        public List<object> achievementShowcase { get; set; }
        public List<EarnedReward> earnedRewards { get; set; }
        public List<EarnedAchievement> earnedAchievements { get; set; }
    }

    public class Summary
    {
        public string id { get; set; }
        public int realm { get; set; }
        public string displayName { get; set; }
        public string portrait { get; set; }
        public string decalTerran { get; set; }
        public string decalProtoss { get; set; }
        public string decalZerg { get; set; }
        public int totalSwarmLevel { get; set; }
        public int totalAchievementPoints { get; set; }
    }

    public class _1v1
    {
        public int rank { get; set; }
        public object leagueName { get; set; }
        public int totalGames { get; set; }
        public int totalWins { get; set; }
    }

    public class _2v2
    {
        public int rank { get; set; }
        public object leagueName { get; set; }
        public int totalGames { get; set; }
        public int totalWins { get; set; }
    }

    public class _3v3
    {
        public int rank { get; set; }
        public object leagueName { get; set; }
        public int totalGames { get; set; }
        public int totalWins { get; set; }
    }

    public class _4v4
    {
        public int rank { get; set; }
        public object leagueName { get; set; }
        public int totalGames { get; set; }
        public int totalWins { get; set; }
    }

    public class Archon
    {
        public int rank { get; set; }
        public object leagueName { get; set; }
        public int totalGames { get; set; }
        public int totalWins { get; set; }
    }

    public class SeasonSnapshot
    {
        public _1v1 _1v1 { get; set; }
        public _2v2 _2v2 { get; set; }
        public _3v3 _3v3 { get; set; }
        public _4v4 _4v4 { get; set; }
        public Archon Archon { get; set; }
    }

    public class Snapshot
    {
        public SeasonSnapshot seasonSnapshot { get; set; }
        public int totalRankedSeasonGamesPlayed { get; set; }
    }

    public class Best1v1Finish
    {
        public object leagueName { get; set; }
        public int timesAchieved { get; set; }
    }

    public class BestTeamFinish
    {
        public object leagueName { get; set; }
        public int timesAchieved { get; set; }
    }

    public class Career
    {
        public int terranWins { get; set; }
        public int zergWins { get; set; }
        public int protossWins { get; set; }
        public int totalCareerGames { get; set; }
        public int totalGamesThisSeason { get; set; }
        public object current1v1LeagueName { get; set; }
        public object currentBestTeamLeagueName { get; set; }
        public Best1v1Finish best1v1Finish { get; set; }
        public BestTeamFinish bestTeamFinish { get; set; }
    }

    public class Terran
    {
        public int level { get; set; }
        public int maxLevelPoints { get; set; }
        public int currentLevelPoints { get; set; }
    }

    public class Zerg
    {
        public int level { get; set; }
        public int maxLevelPoints { get; set; }
        public int currentLevelPoints { get; set; }
    }

    public class Protoss
    {
        public int level { get; set; }
        public int maxLevelPoints { get; set; }
        public int currentLevelPoints { get; set; }
    }

    public class SwarmLevels
    {
        public int level { get; set; }
        public Terran terran { get; set; }
        public Zerg zerg { get; set; }
        public Protoss protoss { get; set; }
    }

    public class DifficultyCompleted
    {
        [SerializationPropertyName("wings-of-liberty")]
        public string WingsOfLiberty { get; set; }

        [SerializationPropertyName("heart-of-the-swarm")]
        public string HeartOfTheSwarm { get; set; }
    }

    public class Campaign
    {
        public DifficultyCompleted difficultyCompleted { get; set; }
    }

    public class CategoryPointProgress
    {
        public string categoryId { get; set; }
        public int pointsEarned { get; set; }
    }

    public class EarnedReward
    {
        public string rewardId { get; set; }
        public bool selected { get; set; }
        public string achievementId { get; set; }
        public string category { get; set; }
    }

    public class Earned
    {
        public int quantity { get; set; }
        public int startTime { get; set; }
    }

    public class Criterion
    {
        public string criterionId { get; set; }
        public Earned earned { get; set; }
    }

    public class EarnedAchievement
    {
        public string achievementId { get; set; }
        public string completionDate { get; set; }
        public int numCompletedAchievementsInSeries { get; set; }
        public int totalAchievementsInSeries { get; set; }
        public bool isComplete { get; set; }
        public bool inProgress { get; set; }
        public List<Criterion> criteria { get; set; }
        public int nextProgressEarnedQuantity { get; set; }
        public int nextProgressRequiredQuantity { get; set; }
    }
}
