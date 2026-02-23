// =============================================================================
// BLOK DÜNYASI - UNITY ADAPTERS
// Bridges pure C# engine to Unity systems
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using BlockPuzzle.Core.Board;
using BlockPuzzle.Core.Common;
using BlockPuzzle.Core.Persistence;
using BlockPuzzle.Core.RNG;
using BlockPuzzle.Core.Shapes;

namespace BlockPuzzle.UnityAdapter
{

    /// <summary>
    /// PlayerPrefs-based storage provider.
    /// </summary>
    public sealed class PlayerPrefsStorage : IStorageProvider
    {
        public string LoadString(string key)
        {
            return PlayerPrefs.GetString(key, string.Empty);
        }

        public void SaveString(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
        }

        public int LoadInt(string key, int defaultValue = 0)
        {
            return PlayerPrefs.GetInt(key, defaultValue);
        }

        public void SaveInt(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
        }

        public bool HasKey(string key)
        {
            return PlayerPrefs.HasKey(key);
        }

        public void DeleteKey(string key)
        {
            PlayerPrefs.DeleteKey(key);
        }

        public void Save()
        {
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// JsonUtility-based serializer with DTO mappings for Unity-compatible types.
    /// </summary>
    public sealed class UnityJsonSerializer : IJsonSerializer
    {
        public string Serialize<T>(T obj)
        {
            if (obj == null)
                return string.Empty;

            if (obj is GameData gameData)
                return JsonUtility.ToJson(GameDataDto.FromGameData(gameData), false);

            if (obj is GameSettings gameSettings)
                return JsonUtility.ToJson(GameSettingsDto.FromGameSettings(gameSettings), false);

            if (obj is GameStatistics gameStatistics)
                return JsonUtility.ToJson(GameStatisticsDto.FromGameStatistics(gameStatistics), false);

            return JsonUtility.ToJson(obj, false);
        }

        public T Deserialize<T>(string json)
        {
            if (string.IsNullOrEmpty(json))
                return default;

            var targetType = typeof(T);

            if (targetType == typeof(GameData))
            {
                var dto = JsonUtility.FromJson<GameDataDto>(json);
                return (T)(object)dto.ToGameData();
            }

            if (targetType == typeof(GameSettings))
            {
                var dto = JsonUtility.FromJson<GameSettingsDto>(json);
                return (T)(object)dto.ToGameSettings();
            }

            if (targetType == typeof(GameStatistics))
            {
                var dto = JsonUtility.FromJson<GameStatisticsDto>(json);
                return (T)(object)dto.ToGameStatistics();
            }

            return JsonUtility.FromJson<T>(json);
        }
    }

    [Serializable]
    internal class GameSettingsDto
    {
        public float MasterVolume;
        public float SfxVolume;
        public float MusicVolume;
        public bool SfxEnabled;
        public bool MusicEnabled;
        public bool VibrationEnabled;
        public bool ShowPlacementHints;
        public bool ShowValidPlacements;
        public bool AutoDarkMode;
        public bool DarkMode;
        public float AnimationSpeed;
        public bool ParticleEffects;
        public float ScreenShakeIntensity;
        public bool ShowFpsCounter;
        public string Language;
        public string BoardTheme;
        public string BlockTheme;
        public bool TutorialCompleted;
        public string LastPlayedVersion;

        public static GameSettingsDto FromGameSettings(GameSettings settings)
        {
            return new GameSettingsDto
            {
                MasterVolume = settings.MasterVolume,
                SfxVolume = settings.SfxVolume,
                MusicVolume = settings.MusicVolume,
                SfxEnabled = settings.SfxEnabled,
                MusicEnabled = settings.MusicEnabled,
                VibrationEnabled = settings.VibrationEnabled,
                ShowPlacementHints = settings.ShowPlacementHints,
                ShowValidPlacements = settings.ShowValidPlacements,
                AutoDarkMode = settings.AutoDarkMode,
                DarkMode = settings.DarkMode,
                AnimationSpeed = settings.AnimationSpeed,
                ParticleEffects = settings.ParticleEffects,
                ScreenShakeIntensity = settings.ScreenShakeIntensity,
                ShowFpsCounter = settings.ShowFpsCounter,
                Language = settings.Language,
                BoardTheme = settings.BoardTheme,
                BlockTheme = settings.BlockTheme,
                TutorialCompleted = settings.TutorialCompleted,
                LastPlayedVersion = settings.LastPlayedVersion
            };
        }

        public GameSettings ToGameSettings()
        {
            return new GameSettings
            {
                MasterVolume = MasterVolume,
                SfxVolume = SfxVolume,
                MusicVolume = MusicVolume,
                SfxEnabled = SfxEnabled,
                MusicEnabled = MusicEnabled,
                VibrationEnabled = VibrationEnabled,
                ShowPlacementHints = ShowPlacementHints,
                ShowValidPlacements = ShowValidPlacements,
                AutoDarkMode = AutoDarkMode,
                DarkMode = DarkMode,
                AnimationSpeed = AnimationSpeed,
                ParticleEffects = ParticleEffects,
                ScreenShakeIntensity = ScreenShakeIntensity,
                ShowFpsCounter = ShowFpsCounter,
                Language = Language,
                BoardTheme = BoardTheme,
                BlockTheme = BlockTheme,
                TutorialCompleted = TutorialCompleted,
                LastPlayedVersion = LastPlayedVersion
            };
        }
    }

    [Serializable]
    internal class GameDataDto
    {
        public int SaveVersion;
        public long SaveTimeBinary;
        public int[] BoardBlockIds;
        public int[] BoardColorIds;
        public int BoardWidth;
        public int BoardHeight;
        public int Score;
        public int ComboStreak;
        public int[] ActiveBlockIds;
        public int[] ActiveBlockSlots;
        public int MoveCount;
        public int TotalLinesCleared;
        public long GameStartTimeBinary;
        public long LastMoveTimeBinary;
        public bool IsGameOver;
        public int RandomSeed;
        public float DifficultyLevel;
        public SpawnerSaveDataDto SpawnerData;

        public static GameDataDto FromGameData(GameData data)
        {
            var boardCells = data.BoardCells ?? Array.Empty<CellState>();
            var blockIds = new int[boardCells.Length];
            var colorIds = new int[boardCells.Length];
            for (int i = 0; i < boardCells.Length; i++)
            {
                blockIds[i] = boardCells[i].BlockId;
                colorIds[i] = boardCells[i].ColorId;
            }

            return new GameDataDto
            {
                SaveVersion = data.SaveVersion,
                SaveTimeBinary = data.SaveTime.ToBinary(),
                BoardBlockIds = blockIds,
                BoardColorIds = colorIds,
                BoardWidth = data.BoardWidth,
                BoardHeight = data.BoardHeight,
                Score = data.Score,
                ComboStreak = data.ComboStreak,
                ActiveBlockIds = ShapeIdsToInts(data.ActiveBlocks),
                ActiveBlockSlots = data.ActiveBlockSlots,
                MoveCount = data.MoveCount,
                TotalLinesCleared = data.TotalLinesCleared,
                GameStartTimeBinary = data.GameStartTime.ToBinary(),
                LastMoveTimeBinary = data.LastMoveTime.ToBinary(),
                IsGameOver = data.IsGameOver,
                RandomSeed = data.RandomSeed,
                DifficultyLevel = data.DifficultyLevel,
                SpawnerData = SpawnerSaveDataDto.FromSpawnerSaveData(data.SpawnerData)
            };
        }

        public GameData ToGameData()
        {
            var data = new GameData
            {
                SaveVersion = SaveVersion,
                SaveTime = DateTime.FromBinary(SaveTimeBinary),
                BoardWidth = BoardWidth,
                BoardHeight = BoardHeight,
                Score = Score,
                ComboStreak = ComboStreak,
                ActiveBlocks = IntsToShapeIds(ActiveBlockIds),
                ActiveBlockSlots = ActiveBlockSlots,
                MoveCount = MoveCount,
                TotalLinesCleared = TotalLinesCleared,
                GameStartTime = DateTime.FromBinary(GameStartTimeBinary),
                LastMoveTime = DateTime.FromBinary(LastMoveTimeBinary),
                IsGameOver = IsGameOver,
                RandomSeed = RandomSeed,
                DifficultyLevel = DifficultyLevel,
                SpawnerData = SpawnerData != null ? SpawnerData.ToSpawnerSaveData() : new SpawnerSaveData()
            };

            data.BoardCells = BuildBoardCells(BoardBlockIds, BoardColorIds);
            return data;
        }

        private static CellState[] BuildBoardCells(int[] blockIds, int[] colorIds)
        {
            if (blockIds == null || colorIds == null || blockIds.Length != colorIds.Length)
                return Array.Empty<CellState>();

            var cells = new CellState[blockIds.Length];
            for (int i = 0; i < blockIds.Length; i++)
            {
                int blockId = blockIds[i];
                int colorId = colorIds[i];
                cells[i] = blockId > 0 && colorId > 0 ? new CellState(blockId, colorId) : CellState.Empty;
            }
            return cells;
        }

        private static int[] ShapeIdsToInts(ShapeId[] shapeIds)
        {
            if (shapeIds == null)
                return Array.Empty<int>();

            var ids = new int[shapeIds.Length];
            for (int i = 0; i < shapeIds.Length; i++)
            {
                ids[i] = shapeIds[i].Value;
            }
            return ids;
        }

        private static ShapeId[] IntsToShapeIds(int[] ids)
        {
            if (ids == null)
                return Array.Empty<ShapeId>();

            var shapes = new ShapeId[ids.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                shapes[i] = new ShapeId(ids[i]);
            }
            return shapes;
        }
    }

    [Serializable]
    internal class SpawnerSaveDataDto
    {
        public int TotalPlacements;
        public float RecentSuccessRate;
        public float OverallSuccessRate;
        public bool[] RecentPlacementHistory;

        public static SpawnerSaveDataDto FromSpawnerSaveData(SpawnerSaveData data)
        {
            if (data == null)
                return new SpawnerSaveDataDto();

            return new SpawnerSaveDataDto
            {
                TotalPlacements = data.TotalPlacements,
                RecentSuccessRate = data.RecentSuccessRate,
                OverallSuccessRate = data.OverallSuccessRate,
                RecentPlacementHistory = data.RecentPlacementHistory
            };
        }

        public SpawnerSaveData ToSpawnerSaveData()
        {
            return new SpawnerSaveData
            {
                TotalPlacements = TotalPlacements,
                RecentSuccessRate = RecentSuccessRate,
                OverallSuccessRate = OverallSuccessRate,
                RecentPlacementHistory = RecentPlacementHistory
            };
        }
    }

    [Serializable]
    internal class GameStatisticsDto
    {
        public int HighScore;
        public int GamesPlayed;
        public int GamesCompleted;
        public long TotalPlayTimeTicks;
        public int TotalBlocksPlaced;
        public int TotalLinesCleared;
        public int HighestCombo;
        public long LongestSessionTicks;
        public int HighestSingleMoveScore;
        public int MostLinesClearedAtOnce;
        public List<int> RecentScores;
        public List<int> TopScores;
        public List<string> UnlockedAchievements;
        public List<DailyCompletionDto> DailyChallengeCompletions;
        public int ConsecutiveDaysStreak;
        public long LastPlayDateBinary;
        public long TotalScore;
        public int PerfectGames;

        public static GameStatisticsDto FromGameStatistics(GameStatistics stats)
        {
            var dto = new GameStatisticsDto
            {
                HighScore = stats.HighScore,
                GamesPlayed = stats.GamesPlayed,
                GamesCompleted = stats.GamesCompleted,
                TotalPlayTimeTicks = stats.TotalPlayTime.Ticks,
                TotalBlocksPlaced = stats.TotalBlocksPlaced,
                TotalLinesCleared = stats.TotalLinesCleared,
                HighestCombo = stats.HighestCombo,
                LongestSessionTicks = stats.LongestSession.Ticks,
                HighestSingleMoveScore = stats.HighestSingleMoveScore,
                MostLinesClearedAtOnce = stats.MostLinesClearedAtOnce,
                RecentScores = stats.RecentScores != null ? new List<int>(stats.RecentScores) : new List<int>(),
                TopScores = stats.TopScores != null ? new List<int>(stats.TopScores) : new List<int>(),
                UnlockedAchievements = stats.UnlockedAchievements != null ? new List<string>(stats.UnlockedAchievements) : new List<string>(),
                DailyChallengeCompletions = new List<DailyCompletionDto>(),
                ConsecutiveDaysStreak = stats.ConsecutiveDaysStreak,
                LastPlayDateBinary = stats.LastPlayDate.ToBinary(),
                TotalScore = stats.TotalScore,
                PerfectGames = stats.PerfectGames
            };

            if (stats.DailyChallengeCompletions != null)
            {
                foreach (var pair in stats.DailyChallengeCompletions)
                {
                    dto.DailyChallengeCompletions.Add(new DailyCompletionDto
                    {
                        DateBinary = pair.Key.ToBinary(),
                        Count = pair.Value
                    });
                }
            }

            return dto;
        }

        public GameStatistics ToGameStatistics()
        {
            var stats = new GameStatistics
            {
                HighScore = HighScore,
                GamesPlayed = GamesPlayed,
                GamesCompleted = GamesCompleted,
                TotalBlocksPlaced = TotalBlocksPlaced,
                TotalLinesCleared = TotalLinesCleared,
                HighestCombo = HighestCombo,
                HighestSingleMoveScore = HighestSingleMoveScore,
                MostLinesClearedAtOnce = MostLinesClearedAtOnce,
                ConsecutiveDaysStreak = ConsecutiveDaysStreak,
                TotalScore = TotalScore,
                PerfectGames = PerfectGames
            };

            stats.TotalPlayTime = new TimeSpan(TotalPlayTimeTicks);
            stats.LongestSession = new TimeSpan(LongestSessionTicks);
            stats.LastPlayDate = DateTime.FromBinary(LastPlayDateBinary);

            if (RecentScores != null)
                stats.RecentScores = new List<int>(RecentScores);

            if (TopScores != null)
                stats.TopScores = new List<int>(TopScores);

            if (UnlockedAchievements != null)
                stats.UnlockedAchievements = new HashSet<string>(UnlockedAchievements);

            if (DailyChallengeCompletions != null)
            {
                stats.DailyChallengeCompletions = new Dictionary<DateTime, int>();
                foreach (var entry in DailyChallengeCompletions)
                {
                    stats.DailyChallengeCompletions[DateTime.FromBinary(entry.DateBinary)] = entry.Count;
                }
            }

            return stats;
        }
    }

    [Serializable]
    internal struct DailyCompletionDto
    {
        public long DateBinary;
        public int Count;
    }

    /// <summary>
    /// Vector conversion utilities.
    /// </summary>
    public static class VectorConversions
    {
        public static Vector2Int ToVector2Int(Int2 coord)
        {
            return new Vector2Int(coord.X, coord.Y);
        }

        public static Int2 ToInt2(Vector2Int vec)
        {
            return new Int2(vec.x, vec.y);
        }

        public static Vector2 ToVector2(Int2 coord)
        {
            return new Vector2(coord.X, coord.Y);
        }
    }
}
