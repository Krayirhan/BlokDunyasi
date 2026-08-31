using System;
using System.Threading.Tasks;
using BlockPuzzle.Core.Common;
using BlockPuzzle.Core.Engine;
using BlockPuzzle.Core.Game;
using BlockPuzzle.Core.Persistence;
using BlockPuzzle.Core.Shapes;
using BlockPuzzle.UnityAdapter.Analytics;
using BlockPuzzle.UnityAdapter.UI.Localization;

namespace BlockPuzzle.UnityAdapter.Boot
{
    public sealed class TutorialService
    {
        public event Action<TutorialStepPayload> StepChanged;

        public bool IsActive => _isActive;
        public int StepIndex => _stepIndex;

        private bool _isActive;
        private int _stepIndex;
        private bool _pendingThreeByThreeSet;
        private bool _threeByThreeSetApplied;
        private bool _pendingActivation;

        private GameEngine _gameEngine;
        private readonly ISettingsPersistence _settingsPersistence;
        private readonly bool _isEnabled;
        private readonly Func<bool> _canLog;
        private GameSettings _settingsCache;

        private static readonly ShapeId[] OpeningSet =
        {
            ShapeLibrary.Single,
            new ShapeId(3),
            new ShapeId(8)
        };

        private static readonly ShapeId[] ThreeByThreeSet =
        {
            new ShapeId(9),
            ShapeLibrary.Single,
            new ShapeId(5)
        };

        public TutorialService(ISettingsPersistence settingsPersistence, bool isEnabled, Func<bool> canLog)
        {
            _settingsPersistence = settingsPersistence;
            _isEnabled = isEnabled;
            _canLog = canLog;
        }

        public void SetEngine(GameEngine engine)
        {
            _gameEngine = engine;
        }

        public void SetSettingsCache(GameSettings settings)
        {
            _settingsCache = settings;
        }

        public void MarkForActivation(GameMode currentMode)
        {
            _pendingActivation = _isEnabled
                && currentMode == GameMode.Classic
                && (GameLaunchState.ForceTutorialReplay || _settingsCache == null || !_settingsCache.TutorialCompleted);
        }

        public void ActivateIfPending()
        {
            if (!_pendingActivation)
                return;

            _pendingActivation = false;
            _isActive = true;
            _stepIndex = 1;
            _pendingThreeByThreeSet = false;
            _threeByThreeSetApplied = false;

            AppAnalytics.TrackTutorialStarted("first_session");
            ApplyBlockSet(OpeningSet);
        }

        public void UpdateProgress(MoveResult moveResult, ShapeId placedShapeId)
        {
            if (!_isActive || moveResult == null || !moveResult.Success)
                return;

            if (_stepIndex <= 1)
            {
                _stepIndex = 2;
                EmitStepState();
                return;
            }

            if (_stepIndex == 2 && moveResult.LinesCleared > 0)
            {
                _stepIndex = 3;
                _pendingThreeByThreeSet = true;

                if (moveResult.TriggersSpawn)
                    ApplyPendingSpawnOverrideIfNeeded();

                EmitStepState();
                return;
            }

            if (_stepIndex == 3)
            {
                if (_pendingThreeByThreeSet && moveResult.TriggersSpawn)
                    ApplyPendingSpawnOverrideIfNeeded();

                if (placedShapeId.Equals(new ShapeId(9)))
                    CompleteRun("first_session");
            }
        }

        public void SkipActiveTutorial()
        {
            if (!_isActive)
                return;

            CompleteRun("first_session_skipped");
        }

        public void ResetRuntimeState(bool notify)
        {
            _isActive = false;
            _stepIndex = 0;
            _pendingThreeByThreeSet = false;
            _threeByThreeSetApplied = false;

            if (notify)
                EmitStepState();
        }

        public string GetGuidanceCode()
        {
            if (!_isActive)
                return string.Empty;

            return _stepIndex switch
            {
                <= 1 => "tutorial_place",
                2 => "tutorial_clear",
                3 => "tutorial_3x3",
                _ => "tutorial_generic"
            };
        }

        private void ApplyBlockSet(ShapeId[] shapeIds)
        {
            if (_gameEngine == null || shapeIds == null || shapeIds.Length == 0)
                return;

            _gameEngine.OverrideActiveBlocks(shapeIds);
        }

        private void ApplyPendingSpawnOverrideIfNeeded()
        {
            if (!_isActive || !_pendingThreeByThreeSet || _threeByThreeSetApplied)
                return;

            ApplyBlockSet(ThreeByThreeSet);
            _pendingThreeByThreeSet = false;
            _threeByThreeSetApplied = true;
        }

        private async void CompleteRun(string analyticsContext)
        {
            if (!_isActive)
                return;

            _isActive = false;
            _pendingThreeByThreeSet = false;
            _threeByThreeSetApplied = false;

            AppAnalytics.TrackTutorialCompleted(string.IsNullOrWhiteSpace(analyticsContext) ? "first_session" : analyticsContext);

            _settingsCache ??= GameSettings.CreateDefault();
            _settingsCache.TutorialCompleted = true;

            if (_settingsPersistence != null)
            {
                try
                {
                    await _settingsPersistence.SaveSettingsAsync(_settingsCache);
                }
                catch (Exception ex)
                {
                    if (_canLog())
                        GameLogger.LogWarning($"[TutorialService] Tutorial completion could not be persisted: {ex.Message}");
                }
            }

            EmitStepState();
        }

        public void RefreshStepState()
        {
            EmitStepState();
        }

        private void EmitStepState()
        {
            if (!_isActive)
            {
                StepChanged?.Invoke(new TutorialStepPayload(false, 0, 3, string.Empty, string.Empty));
                return;
            }

            string title;
            string description;

            var lang = LanguageManager.Instance != null
                ? LanguageManager.Instance.CurrentLanguage
                : LanguageManager.Language.Turkish;

            if (lang == LanguageManager.Language.Korean)
            {
                switch (_stepIndex)
                {
                    case 1:
                        title = "첫 번째 블록 배치하기";
                        description = "블록을 드래그하여 그리드에 올려놓으세요. 첫 번째 목표는 블록 배치를 익히는 것입니다.";
                        break;
                    case 2:
                        title = "가로 줄 또는 세로 줄 지우기";
                        description = "가로 또는 세로 한 줄을 가득 채우면 자동으로 지워집니다. 블록을 채워 줄을 지워보세요.";
                        break;
                    case 3:
                        title = "3x3 공간 확보하기";
                        description = "큰 3x3 블록이 들어갈 수 있는 빈 공간을 남겨두는 것이 중요합니다. 항상 충분한 공간을 유지하세요.";
                        break;
                    default:
                        title = string.Empty;
                        description = string.Empty;
                        break;
                }
            }
            else if (lang == LanguageManager.Language.English)
            {
                switch (_stepIndex)
                {
                    case 1:
                        title = "Place the First Block";
                        description = "Drag any block and drop it on the grid. The first goal is simply to get a feel for placing blocks.";
                        break;
                    case 2:
                        title = "Clear a Row or Column";
                        description = "A fully filled row or column is automatically cleared. Place your blocks to trigger a clear.";
                        break;
                    case 3:
                        title = "Keep 3x3 Space Free";
                        description = "Large block shapes require a 3x3 empty area. Keep enough open space to accommodate future 3x3 blocks.";
                        break;
                    default:
                        title = string.Empty;
                        description = string.Empty;
                        break;
                }
            }
            else
            {
                switch (_stepIndex)
                {
                    case 1:
                        title = "İlk Bloğu Yerleştir";
                        description = "Herhangi bir bloğu sürükleyip grid üzerine bırak. İlk hedef sadece yerleştirme mantığını hissetmek.";
                        break;
                    case 2:
                        title = "Bir Satır Veya Sütun Temizle";
                        description = "Tam dolu bir satır ya da sütun otomatik temizlenir. Bloklarını bu temizlemeyi kuracak şekilde kullan.";
                        break;
                    case 3:
                        title = "3x3 Alan Açık Tut";
                        description = "Büyük kare bloklar için 3x3 boşluk lazım. Sıradaki 3x3 bloğu yerleştirebileceğin kadar alan koru.";
                        break;
                    default:
                        title = string.Empty;
                        description = string.Empty;
                        break;
                }
            }

            StepChanged?.Invoke(new TutorialStepPayload(true, _stepIndex, 3, title, description));
        }
    }
}
