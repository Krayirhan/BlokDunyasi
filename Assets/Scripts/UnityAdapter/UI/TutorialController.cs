using System;
using UnityEngine;
using BlockPuzzle.UnityAdapter.Boot;

namespace BlockPuzzle.UnityAdapter.UI
{
    /// <summary>
    /// Tutorial state facade for UI and launch flows.
    /// Keeps tutorial orchestration in one place while GameBootstrap owns the gameplay-side execution.
    /// </summary>
    public sealed class TutorialController : MonoBehaviour
    {
        public static TutorialController Instance { get; private set; }

        public event Action<TutorialStepPayload> TutorialStepChanged;

        public bool IsTutorialVisible { get; private set; }
        public int CurrentStepIndex { get; private set; }
        public int TotalSteps { get; private set; }
        public string CurrentTitle { get; private set; } = string.Empty;
        public string CurrentDescription { get; private set; } = string.Empty;

        private static void Bootstrap()
        {
            return;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            GameBootstrap.OnTutorialStepChanged += HandleTutorialStepChanged;
        }

        private void OnDisable()
        {
            GameBootstrap.OnTutorialStepChanged -= HandleTutorialStepChanged;
        }

        public void SkipActiveTutorial()
        {
            Debug.Log("[TutorialController] Tutorial is disabled.");
        }

        public void RequestReplay()
        {
            Debug.Log("[TutorialController] Tutorial replay is disabled.");
        }

        private void HandleTutorialStepChanged(TutorialStepPayload payload)
        {
            IsTutorialVisible = payload.Visible;
            CurrentStepIndex = payload.StepIndex;
            TotalSteps = payload.TotalSteps;
            CurrentTitle = payload.Title ?? string.Empty;
            CurrentDescription = payload.Description ?? string.Empty;
            TutorialStepChanged?.Invoke(payload);
        }
    }
}
