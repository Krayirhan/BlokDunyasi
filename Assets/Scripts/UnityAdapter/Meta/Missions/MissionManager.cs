using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlockPuzzle.Core.Meta.Missions
{
    public class MissionProgress
    {
        public string missionId;
        public int currentAmount;
        public bool isClaimed;

        public MissionProgress(string id)
        {
            missionId = id;
            currentAmount = 0;
            isClaimed = false;
        }
    }

    public class MissionManager : MonoBehaviour
    {
        public static MissionManager Instance { get; private set; }

        public List<MissionDefinition> activeMissions = new List<MissionDefinition>();
        private Dictionary<string, MissionProgress> missionProgresses = new Dictionary<string, MissionProgress>();

        public event Action<MissionDefinition> OnMissionCompleted;
        public event Action<MissionProgress> OnMissionProgressUpdated;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void InitializeMissions(List<MissionDefinition> missions)
        {
            activeMissions = missions;
            foreach (var mission in activeMissions)
            {
                if (!missionProgresses.ContainsKey(mission.id))
                {
                    missionProgresses[mission.id] = new MissionProgress(mission.id);
                }
            }
        }

        public void ReportProgress(MissionGoalType goalType, int amount)
        {
            foreach (var mission in activeMissions)
            {
                if (mission.goalType == goalType)
                {
                    var progress = missionProgresses[mission.id];
                    if (progress.isClaimed || progress.currentAmount >= mission.targetAmount) continue;

                    progress.currentAmount += amount;
                    if (progress.currentAmount >= mission.targetAmount)
                    {
                        progress.currentAmount = mission.targetAmount;
                        OnMissionCompleted?.Invoke(mission);
                    }
                    OnMissionProgressUpdated?.Invoke(progress);
                }
            }
        }

        public bool ClaimReward(string missionId)
        {
            if (missionProgresses.TryGetValue(missionId, out var progress))
            {
                var missionDef = activeMissions.Find(m => m.id == missionId);
                if (missionDef != null && progress.currentAmount >= missionDef.targetAmount && !progress.isClaimed)
                {
                    progress.isClaimed = true;
                    RewardInventory.Instance.AddReward(missionDef.rewardId, missionDef.rewardAmount);
                    return true;
                }
            }
            return false;
        }

        public MissionProgress GetProgress(string missionId)
        {
            missionProgresses.TryGetValue(missionId, out var progress);
            return progress;
        }
    }
}