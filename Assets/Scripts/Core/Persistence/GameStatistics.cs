// File: Core/Persistence/GameStatistics.cs
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlockPuzzle.Core.Persistence
{
    /// <summary>
    /// Player statistics and achievements tracking.
    /// </summary>
    [Serializable]
    public class GameStatistics
    {
        /// <summary>
        /// All-time high score.
        /// </summary>
        public int HighScore { get; set; }
        
        /// <summary>
        /// Total number of games played.
        /// </summary>
        public int GamesPlayed { get; set; }
        
        /// <summary>
        /// Total number of games completed.
        /// </summary>
        public int GamesCompleted { get; set; }
        
        /// <summary>
        /// Total playing time across all sessions.
        /// </summary>
        public TimeSpan TotalPlayTime { get; set; }
        
        /// <summary>
        /// Total blocks placed.
        /// </summary>
        public int TotalBlocksPlaced { get; set; }
        
        /// <summary>
        /// Total lines cleared.
        /// </summary>
        public int TotalLinesCleared { get; set; }
        
        /// <summary>
        /// Highest combo streak achieved.
        /// </summary>
        public int HighestCombo { get; set; }
        
        /// <summary>
        /// Longest game session (by time).
        /// </summary>
        public TimeSpan LongestSession { get; set; }
        
        /// <summary>
        /// Highest score in a single move.
        /// </summary>
        public int HighestSingleMoveScore { get; set; }
        
        /// <summary>
        /// Most lines cleared in a single move.
        /// </summary>
        public int MostLinesClearedAtOnce { get; set; }
        
        /// <summary>
        /// Recent scores (last 10 games).
        /// </summary>
        public List<int> RecentScores { get; set; }

        /// <summary>
        /// Top scores (highest scores across all games).
        /// </summary>
        public List<int> TopScores { get; set; }
        
        /// <summary>
        /// Achievement unlocks by ID.
        /// </summary>
        public HashSet<string> UnlockedAchievements { get; set; }
        
        /// <summary>
        /// Daily challenge completions by date.
        /// </summary>
        public Dictionary<DateTime, int> DailyChallengeCompletions { get; set; }
        
        /// <summary>
        /// Current streak of consecutive days played.
        /// </summary>
        public int ConsecutiveDaysStreak { get; set; }
        
        /// <summary>
        /// Last day the game was played.
        /// </summary>
        public DateTime LastPlayDate { get; set; }
        
        /// <summary>
        /// Total score across all games.
        /// </summary>
        public long TotalScore { get; set; }
        
        /// <summary>
        /// Perfect games (no mistakes/optimal play).
        /// </summary>
        public int PerfectGames { get; set; }
        
        public GameStatistics()
        {
            RecentScores = new List<int>();
            TopScores = new List<int>();
            UnlockedAchievements = new HashSet<string>();
            DailyChallengeCompletions = new Dictionary<DateTime, int>();
            LastPlayDate = DateTime.MinValue;
        }
        
        /// <summary>
        /// Records a completed game session.
        /// </summary>
        /// <param name="score">Final score</param>
        /// <param name="sessionTime">Session duration</param>
        /// <param name="blocksPlaced">Blocks placed this session</param>
        /// <param name="linesCleared">Lines cleared this session</param>
        /// <param name="highestCombo">Highest combo this session</param>
        public void RecordGameSession(int score, TimeSpan sessionTime, int blocksPlaced, 
            int linesCleared, int highestCombo)
        {
            GamesPlayed++;
            TotalPlayTime += sessionTime;
            TotalBlocksPlaced += blocksPlaced;
            TotalLinesCleared += linesCleared;
            TotalScore += score;
            
            if (score > HighScore)
                HighScore = score;
                
            if (highestCombo > HighestCombo)
                HighestCombo = highestCombo;
                
            if (sessionTime > LongestSession)
                LongestSession = sessionTime;
            
            // Track recent scores
            RecentScores.Add(score);
            if (RecentScores.Count > 10)
                RecentScores.RemoveAt(0);

            // Track top scores
            AddTopScore(score, 5);
                
            // Update daily streak
            UpdateDailyStreak();
        }
        
        /// <summary>
        /// Records a single move's score.
        /// </summary>
        /// <param name="moveScore">Score from this move</param>
        /// <param name="linesCleared">Lines cleared in this move</param>
        public void RecordMove(int moveScore, int linesCleared)
        {
            if (moveScore > HighestSingleMoveScore)
                HighestSingleMoveScore = moveScore;
                
            if (linesCleared > MostLinesClearedAtOnce)
                MostLinesClearedAtOnce = linesCleared;
        }
        
        /// <summary>
        /// Unlocks an achievement.
        /// </summary>
        /// <param name="achievementId">Achievement identifier</param>
        /// <returns>True if achievement was newly unlocked</returns>
        public bool UnlockAchievement(string achievementId)
        {
            return UnlockedAchievements.Add(achievementId);
        }
        
        /// <summary>
        /// Checks if an achievement is unlocked.
        /// </summary>
        /// <param name="achievementId">Achievement identifier</param>
        /// <returns>True if unlocked</returns>
        public bool IsAchievementUnlocked(string achievementId)
        {
            return UnlockedAchievements.Contains(achievementId);
        }
        
        /// <summary>
        /// Gets the average score across all games.
        /// </summary>
        /// <returns>Average score</returns>
        public float GetAverageScore()
        {
            return GamesPlayed > 0 ? (float)TotalScore / GamesPlayed : 0f;
        }
        
        /// <summary>
        /// Gets the average score of recent games.
        /// </summary>
        /// <returns>Average of last 10 games</returns>
        public float GetRecentAverageScore()
        {
            return RecentScores.Count > 0 ? (float)RecentScores.Average() : 0f;
        }

        /// <summary>
        /// Adds a score to the top scores list (sorted descending).
        /// </summary>
        /// <param name="score">Final score</param>
        /// <param name="maxCount">Max number of top scores to keep</param>
        public void AddTopScore(int score, int maxCount)
        {
            if (TopScores == null)
                TopScores = new List<int>();

            TopScores.Add(score);
            TopScores.Sort((a, b) => b.CompareTo(a));

            if (TopScores.Count > maxCount)
                TopScores.RemoveRange(maxCount, TopScores.Count - maxCount);
        }

        /// <summary>
        /// Returns the top scores list (highest first).
        /// </summary>
        public List<int> GetTopScores(int count = 5)
        {
            if (TopScores == null || TopScores.Count == 0)
                return new List<int>();

            var resultCount = count < 0 ? 0 : Math.Min(count, TopScores.Count);
            return TopScores.Take(resultCount).ToList();
        }
        
        /// <summary>
        /// Gets blocks placed per hour rate.
        /// </summary>
        /// <returns>Blocks per hour</returns>
        public float GetBlocksPerHour()
        {
            return TotalPlayTime.TotalHours > 0 ? (float)(TotalBlocksPlaced / TotalPlayTime.TotalHours) : 0f;
        }
        
        /// <summary>
        /// Gets lines cleared per hour rate.
        /// </summary>
        /// <returns>Lines per hour</returns>
        public float GetLinesPerHour()
        {
            return TotalPlayTime.TotalHours > 0 ? (float)(TotalLinesCleared / TotalPlayTime.TotalHours) : 0f;
        }
        
        /// <summary>
        /// Gets completion rate (completed vs started games).
        /// </summary>
        /// <returns>Completion rate as percentage</returns>
        public float GetCompletionRate()
        {
            return GamesPlayed > 0 ? (float)GamesCompleted / GamesPlayed * 100f : 0f;
        }
        
        private void UpdateDailyStreak()
        {
            var today = DateTime.Today;
            
            if (LastPlayDate == DateTime.MinValue)
            {
                // First time playing
                ConsecutiveDaysStreak = 1;
            }
            else if (LastPlayDate.Date == today.AddDays(-1))
            {
                // Consecutive day
                ConsecutiveDaysStreak++;
            }
            else if (LastPlayDate.Date != today)
            {
                // Gap in playing, reset streak
                ConsecutiveDaysStreak = 1;
            }
            // If LastPlayDate == today, don't change streak
            
            LastPlayDate = today;
        }
        
        /// <summary>
        /// Creates default statistics instance.
        /// </summary>
        /// <returns>Default statistics</returns>
        public static GameStatistics CreateDefault()
        {
            return new GameStatistics();
        }
    }
}
