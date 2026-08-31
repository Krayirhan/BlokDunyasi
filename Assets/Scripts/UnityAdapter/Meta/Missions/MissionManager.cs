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
            currentAmount = PlayerPrefs.GetInt($"mission_progress_{id}", 0);
            isClaimed = PlayerPrefs.GetInt($"mission_claimed_{id}", 0) == 1;
        }
    }

    public class MissionManager : MonoBehaviour
    {
        private static MissionManager _instance;

        public static MissionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var managerObject = new GameObject("MissionManager");
                    _instance = managerObject.AddComponent<MissionManager>();
                }

                return _instance;
            }
        }

        public List<MissionDefinition> activeMissions = new List<MissionDefinition>();
        private Dictionary<string, MissionProgress> missionProgresses = new Dictionary<string, MissionProgress>();

        public event Action<MissionDefinition> OnMissionCompleted;
        public event Action<MissionProgress> OnMissionProgressUpdated;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeFromResources();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void InitializeMissions(List<MissionDefinition> missions)
        {
            activeMissions = missions ?? new List<MissionDefinition>();
            foreach (var mission in activeMissions)
            {
                if (mission == null || string.IsNullOrWhiteSpace(mission.id))
                    continue;

                if (!missionProgresses.ContainsKey(mission.id))
                {
                    missionProgresses[mission.id] = new MissionProgress(mission.id);
                }
            }
        }

        private void InitializeFromResources()
        {
            var missions = new List<MissionDefinition>(Resources.LoadAll<MissionDefinition>(string.Empty));
            InitializeMissions(missions);
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

                    PlayerPrefs.SetInt($"mission_progress_{mission.id}", progress.currentAmount);
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
                    PlayerPrefs.SetInt($"mission_claimed_{missionId}", 1);
                    PlayerPrefs.Save();
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
