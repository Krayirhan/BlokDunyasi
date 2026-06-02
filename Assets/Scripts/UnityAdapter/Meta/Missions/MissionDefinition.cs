using System;
using UnityEngine;

namespace BlockPuzzle.Core.Meta.Missions
{
    public enum MissionType
    {
        Daily,
        Weekly
    }

    public enum MissionGoalType
    {
        ClearLines,
        PlaceBlocks,
        EarnScore,
        PlayDaysInARow
    }

    [CreateAssetMenu(fileName = "NewMission", menuName = "BlockPuzzle/Meta/Mission Definition")]
    public class MissionDefinition : ScriptableObject
    {
        public string id;
        public string title;
        public string description;
        public MissionType missionType;
        public MissionGoalType goalType;
        public int targetAmount;
        
        [Header("Reward")]
        public string rewardId; // e.g. "coin", "theme_shard", "continue_token"
        public int rewardAmount;
    }
}