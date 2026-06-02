using System.Collections.Generic;
using UnityEngine;
using BlockPuzzle.Core.Meta.Missions;

namespace BlockPuzzle.UnityAdapter.Meta.UI
{
    /// <summary>
    /// Render owner for mission list rows. Mission state and claimability live in MissionManager / item-level UI.
    /// </summary>
    public class MissionPanel : MonoBehaviour
    {
        [SerializeField] private MissionUIItem missionItemPrefab;
        [SerializeField] private Transform missionListContainer;
        
        private List<MissionUIItem> activeMissionUIs = new List<MissionUIItem>();

        private void OnEnable()
        {
            RefreshUI();
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.OnMissionProgressUpdated += HandleProgressUpdated;
            }
        }

        private void OnDisable()
        {
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.OnMissionProgressUpdated -= HandleProgressUpdated;
            }
        }

        public void RefreshUI()
        {
            ClearMissionItems();
            RebuildMissionItems();
        }

        private void HandleProgressUpdated(MissionProgress progress)
        {
            RefreshUI();
        }

        private void ClearMissionItems()
        {
            foreach (var uiItem in activeMissionUIs)
            {
                Destroy(uiItem.gameObject);
            }

            activeMissionUIs.Clear();
        }

        private void RebuildMissionItems()
        {
            if (MissionManager.Instance == null || missionItemPrefab == null || missionListContainer == null)
                return;

            foreach (var mission in MissionManager.Instance.activeMissions)
            {
                var progress = MissionManager.Instance.GetProgress(mission.id);
                if (progress == null)
                    continue;

                MissionUIItem newItem = Instantiate(missionItemPrefab, missionListContainer);
                newItem.Setup(mission, progress);
                activeMissionUIs.Add(newItem);
            }
        }
    }
}
