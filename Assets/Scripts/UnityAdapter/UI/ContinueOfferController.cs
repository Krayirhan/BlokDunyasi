using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BlockPuzzle.UnityAdapter.Boot;
using BlockPuzzle.UnityAdapter.Grid;
using BlockPuzzle.UnityAdapter.UI.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Debug = BlockPuzzle.Core.Common.GameLogger;

namespace BlockPuzzle.UnityAdapter.UI
{
    public sealed class ContinueOfferController
    {
        public event Action<int> FinalGameOverRequested;
        public event Action HideRequested;
        public event Action ContinueSucceeded;

        public bool IsOfferActive => _isActive;
        public bool IsWaitingRewardedResult => _waitingRewardedResult;
        public int PendingFinalScore => _pendingFinalScore;
        public int ContinuesUsedThisRun { get; private set; }

        private readonly MonoBehaviour _coroutineHost;
        private readonly GameBootstrap _gameBootstrap;
        private readonly SimpleGridView _gridView;

        private readonly GameObject _continueOfferPanel;
        private readonly TextMeshProUGUI _noMovesLabel;
        private readonly TextMeshProUGUI _continueCountdownText;
        private readonly Button _continueButton;

        private readonly bool _enableContinueOffer;
        private readonly float _continueCountdownSeconds;
        private readonly float _rewardedLoadTimeoutSeconds;

        private readonly string _rewardedLoadingMessage;
        private readonly string _rewardedOpeningMessage;
        private readonly string _rewardedLoadFailedMessage;
        private readonly string _noMovesMessage;
        private readonly string _rewardedLoadingMessageEnglish;
        private readonly string _rewardedOpeningMessageEnglish;
        private readonly string _rewardedLoadFailedMessageEnglish;
        private readonly string _noMovesMessageEnglish;

        private readonly Func<bool> _canLog;

        private bool _isActive;
        private bool _waitingRewardedResult;
        private bool _queuedRewardedAdShow;
        private bool _rewardEarned;
        private int _pendingFinalScore;
        private Coroutine _continueCountdownRoutine;
        private Coroutine _rewardedLoadTimeoutRoutine;
        private float _continueCountdownRemaining;
        private bool _adCallbacksHooked;

        public ContinueOfferController(
            MonoBehaviour coroutineHost,
            GameBootstrap gameBootstrap,
            SimpleGridView gridView,
            GameObject continueOfferPanel,
            TextMeshProUGUI noMovesLabel,
            TextMeshProUGUI continueCountdownText,
            Button continueButton,
            bool enableContinueOffer,
            float continueCountdownSeconds,
            float rewardedLoadTimeoutSeconds,
            string rewardedLoadingMessage,
            string rewardedOpeningMessage,
            string rewardedLoadFailedMessage,
            string noMovesMessage,
            string rewardedLoadingMessageEnglish,
            string rewardedOpeningMessageEnglish,
            string rewardedLoadFailedMessageEnglish,
            string noMovesMessageEnglish,
            Func<bool> canLog)
        {
            _coroutineHost = coroutineHost;
            _gameBootstrap = gameBootstrap;
            _gridView = gridView;
            _continueOfferPanel = continueOfferPanel;
            _noMovesLabel = noMovesLabel;
            _continueCountdownText = continueCountdownText;
            _continueButton = continueButton;
            _enableContinueOffer = enableContinueOffer;
            _continueCountdownSeconds = continueCountdownSeconds;
            _rewardedLoadTimeoutSeconds = rewardedLoadTimeoutSeconds;
            _rewardedLoadingMessage = rewardedLoadingMessage;
            _rewardedOpeningMessage = rewardedOpeningMessage;
            _rewardedLoadFailedMessage = rewardedLoadFailedMessage;
            _noMovesMessage = noMovesMessage;
            _rewardedLoadingMessageEnglish = rewardedLoadingMessageEnglish;
            _rewardedOpeningMessageEnglish = rewardedOpeningMessageEnglish;
            _rewardedLoadFailedMessageEnglish = rewardedLoadFailedMessageEnglish;
            _noMovesMessageEnglish = noMovesMessageEnglish;
            _canLog = canLog;
        }

        public bool TryStart(int finalScore)
        {
            if (!_enableContinueOffer)
                return false;

            if (!HasContinueQuotaRemaining())
                return false;

            if (_continueOfferPanel == null)
                return false;

            _pendingFinalScore = finalScore;
            _isActive = true;
            _waitingRewardedResult = false;
            _queuedRewardedAdShow = false;
            _rewardEarned = false;
            _continueCountdownRemaining = Mathf.Max(1f, _continueCountdownSeconds);
            SetContinueOfferAdUiVisible(true);

            if (_noMovesLabel != null)
                _noMovesLabel.text = GetNoMovesMessage();

            UpdateContinueCountdownText(_continueCountdownRemaining);
            SetContinueOfferVisible(true);
            EmitContinueTelemetry("continue_offer_shown");

            if (_continueButton != null)
                _continueButton.interactable = true;

            if (_noMovesLabel != null)
                _noMovesLabel.raycastTarget = false;

            if (_continueCountdownText != null)
                _continueCountdownText.raycastTarget = false;

            StopCountdownRoutine();
            StopTimeoutRoutine();
            _continueCountdownRoutine = _coroutineHost.StartCoroutine(ContinueCountdownRoutine());
            return true;
        }

        public bool TryFinalizeWhenUnavailable(int finalScore)
        {
            if (!_enableContinueOffer)
                return false;

            if (HasContinueQuotaRemaining())
                return false;

            _pendingFinalScore = finalScore;
            EmitContinueTelemetry("continue_offer_exhausted");
            FinalGameOverRequested?.Invoke(finalScore);
            return true;
        }

        public void HandleContinueButtonClicked()
        {
            if (!_isActive || _waitingRewardedResult)
                return;

            if (!IsRewardedAdReady())
            {
                if (_canLog())
                    Debug.LogWarning("[ContinueOffer] Rewarded ad not ready yet. Waiting for load...");

                EmitContinueTelemetry("continue_clicked_waiting_ad");
                _waitingRewardedResult = true;
                _queuedRewardedAdShow = true;
                _rewardEarned = false;

                if (_continueButton != null)
                    _continueButton.interactable = false;

                StopCountdownRoutine();

                if (_noMovesLabel != null)
                    _noMovesLabel.text = GetRewardedLoadingMessage();

                StopTimeoutRoutine();
                _rewardedLoadTimeoutRoutine = _coroutineHost.StartCoroutine(RewardedLoadTimeoutRoutine());
                HookAdCallbacks();
                return;
            }

            _waitingRewardedResult = true;
            _queuedRewardedAdShow = false;
            _rewardEarned = false;
            EmitContinueTelemetry("continue_clicked");

            if (_continueButton != null)
                _continueButton.interactable = false;

            StopCountdownRoutine();

            if (_noMovesLabel != null)
                _noMovesLabel.text = GetRewardedOpeningMessage();

            StopTimeoutRoutine();
            ShowRewardedAd();
        }

        public void HandleRescueButtonClicked()
        {
            if (_gameBootstrap == null)
                return;

            bool rescued = _gameBootstrap.TryUseRescueToken();
            if (rescued)
            {
                EmitContinueTelemetry("rescue_token_success");
                HideRequested?.Invoke();
                return;
            }

            EmitContinueTelemetry("rescue_token_failed");
        }

        public void Reset()
        {
            _isActive = false;
            _waitingRewardedResult = false;
            _queuedRewardedAdShow = false;
            _rewardEarned = false;
            _continueCountdownRemaining = 0f;
            StopCountdownRoutine();
            StopTimeoutRoutine();
            SetContinueOfferVisible(false);
        }

        public void Cleanup()
        {
            UnhookAdCallbacks();
        }

        private bool HasContinueQuotaRemaining()
        {
            return _gameBootstrap != null
                && _gameBootstrap.CurrentState != null
                && _gameBootstrap.CurrentState.RescueCount < 3;
        }

        private IEnumerator ContinueCountdownRoutine()
        {
            _continueCountdownRemaining = Mathf.Max(1f, _continueCountdownRemaining <= 0f
                ? _continueCountdownSeconds
                : _continueCountdownRemaining);

            while (_continueCountdownRemaining > 0f && _isActive)
            {
                UpdateContinueCountdownText(_continueCountdownRemaining);
                yield return null;

                if (_waitingRewardedResult)
                    continue;

                _continueCountdownRemaining -= Time.unscaledDeltaTime;
            }

            _continueCountdownRoutine = null;

            if (_isActive && !_waitingRewardedResult)
                FinalGameOverRequested?.Invoke(_pendingFinalScore);
        }

        private void UpdateContinueCountdownText(float remainingSeconds)
        {
            if (_continueCountdownText == null)
                return;

            int seconds = Mathf.CeilToInt(Mathf.Max(0f, remainingSeconds));
            _continueCountdownText.text = FormatContinueCountdown(seconds);
        }

        private void SetContinueOfferVisible(bool visible)
        {
            if (_continueOfferPanel == null)
            {
                Debug.LogError("[ContinueOffer] continueOfferPanel NULL!");
                return;
            }

            _continueOfferPanel.SetActive(visible);

            if (!visible)
                return;

            if (_gridView != null)
                _gridView.EnsureBoardBackdropVisible();
        }

        private void SetContinueOfferAdUiVisible(bool visible)
        {
            if (_continueButton != null)
            {
                _continueButton.gameObject.SetActive(visible);
                _continueButton.interactable = visible;
            }

            if (_continueOfferPanel == null)
                return;

            Transform offerHintPill = FindDeep(_continueOfferPanel.transform, "OfferHintPill");
            if (offerHintPill != null)
                offerHintPill.gameObject.SetActive(visible);
        }

        private bool IsRewardedAdReady()
        {
            if (!RewardedAdBridge.HasProvider)
            {
                Debug.LogError("[ContinueOffer] CRITICAL: Rewarded ad bridge provider is missing!");
                return false;
            }

            bool ready = RewardedAdBridge.IsReady();
            if (_canLog())
                Debug.Log($"[ContinueOffer] IsRewardedAdReady() = {ready}");
            return ready;
        }

        private void ShowRewardedAd()
        {
            HookAdCallbacks();

            if (!RewardedAdBridge.HasProvider)
            {
                Debug.LogError("[ContinueOffer] CRITICAL: Cannot show rewarded ad - bridge provider missing!");
                return;
            }

            RewardedAdBridge.Show();
        }

        private IEnumerator RewardedLoadTimeoutRoutine()
        {
            yield return new WaitForSecondsRealtime(_rewardedLoadTimeoutSeconds);
            _rewardedLoadTimeoutRoutine = null;

            if (!_isActive || !_waitingRewardedResult || !_queuedRewardedAdShow)
                yield break;

            HandleRewardedAdFailedToLoad("timeout");
        }

        private void HandleRewardedAdLoaded()
        {
            if (!_isActive)
                return;

            if (_queuedRewardedAdShow)
            {
                StopTimeoutRoutine();

                if (_noMovesLabel != null)
                    _noMovesLabel.text = GetRewardedOpeningMessage();

                ShowRewardedAd();
                return;
            }

            if (_waitingRewardedResult)
                return;

            if (_noMovesLabel != null)
                _noMovesLabel.text = GetNoMovesMessage();

            if (_continueButton != null)
                _continueButton.interactable = true;
        }

        private void HandleRewardedAdFailedToLoad(string errorMessage)
        {
            if (!_isActive)
                return;

            EmitContinueTelemetry(string.Equals(errorMessage, "timeout", StringComparison.OrdinalIgnoreCase)
                ? "continue_load_timeout"
                : "continue_load_failed");

            _waitingRewardedResult = false;
            _queuedRewardedAdShow = false;
            _rewardEarned = false;
            StopTimeoutRoutine();

            if (_continueButton != null)
                _continueButton.interactable = true;

            if (_noMovesLabel != null)
                _noMovesLabel.text = GetRewardedLoadFailedMessage();

            if (_isActive && _continueCountdownRoutine == null)
                _continueCountdownRoutine = _coroutineHost.StartCoroutine(ContinueCountdownRoutine());
        }

        private void HandleRewardedUserEarned()
        {
            if (_waitingRewardedResult)
            {
                _queuedRewardedAdShow = false;
                _rewardEarned = true;
            }
        }

        private void HandleRewardedAdClosed()
        {
            if (!_waitingRewardedResult)
                return;

            _waitingRewardedResult = false;
            _queuedRewardedAdShow = false;
            StopTimeoutRoutine();

            if (_rewardEarned)
            {
                CompleteContinue();
                return;
            }

            EmitContinueTelemetry("continue_denied");
            FinalGameOverRequested?.Invoke(_pendingFinalScore);
        }

        private void CompleteContinue()
        {
            _isActive = false;
            _queuedRewardedAdShow = false;
            _continueCountdownRemaining = 0f;
            StopCountdownRoutine();
            StopTimeoutRoutine();
            SetContinueOfferVisible(false);

            bool continued = _gameBootstrap != null && _gameBootstrap.TryContinueAfterRewardedAd();
            if (!continued)
            {
                EmitContinueTelemetry("continue_restore_failed");
                FinalGameOverRequested?.Invoke(_pendingFinalScore);
                return;
            }

            ContinuesUsedThisRun++;
            EmitContinueTelemetry("continue_success");
            GameOverScenePayload.Clear();
            ContinueSucceeded?.Invoke();
            HideRequested?.Invoke();
        }

        private void StopCountdownRoutine()
        {
            if (_continueCountdownRoutine != null)
            {
                _coroutineHost.StopCoroutine(_continueCountdownRoutine);
                _continueCountdownRoutine = null;
            }
        }

        private void StopTimeoutRoutine()
        {
            if (_rewardedLoadTimeoutRoutine != null)
            {
                _coroutineHost.StopCoroutine(_rewardedLoadTimeoutRoutine);
                _rewardedLoadTimeoutRoutine = null;
            }
        }

        private void HookAdCallbacks()
        {
            if (_adCallbacksHooked)
                return;

            RewardedAdBridge.RewardedUserEarned -= HandleRewardedUserEarned;
            RewardedAdBridge.RewardedUserEarned += HandleRewardedUserEarned;
            RewardedAdBridge.RewardedAdLoaded -= HandleRewardedAdLoaded;
            RewardedAdBridge.RewardedAdLoaded += HandleRewardedAdLoaded;
            RewardedAdBridge.RewardedAdClosed -= HandleRewardedAdClosed;
            RewardedAdBridge.RewardedAdClosed += HandleRewardedAdClosed;
            RewardedAdBridge.RewardedAdFailedToLoad -= HandleRewardedAdFailedToLoad;
            RewardedAdBridge.RewardedAdFailedToLoad += HandleRewardedAdFailedToLoad;
            _adCallbacksHooked = true;
        }

        private void UnhookAdCallbacks()
        {
            if (!_adCallbacksHooked)
                return;

            RewardedAdBridge.RewardedUserEarned -= HandleRewardedUserEarned;
            RewardedAdBridge.RewardedAdLoaded -= HandleRewardedAdLoaded;
            RewardedAdBridge.RewardedAdClosed -= HandleRewardedAdClosed;
            RewardedAdBridge.RewardedAdFailedToLoad -= HandleRewardedAdFailedToLoad;
            _adCallbacksHooked = false;
        }

        private static void EmitContinueTelemetry(string eventName)
        {
            try
            {
                var telemetryType = Type.GetType("AdTelemetry");
                var dispatchMethod = telemetryType?.GetMethod(
                    "DispatchLifecycleEvent",
                    BindingFlags.Public | BindingFlags.Static);
                dispatchMethod?.Invoke(null, new object[] { "rewarded", "continue_rewarded", eventName, string.Empty, 0d });
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ContinueOffer] Continue telemetry dispatch failed: {ex.Message}");
            }
        }

        private string GetNoMovesMessage()
        {
            return TrEn(_noMovesMessage, string.IsNullOrWhiteSpace(_noMovesMessageEnglish) ? "No moves left!" : _noMovesMessageEnglish);
        }

        private string GetRewardedLoadingMessage()
        {
            return TrEn(_rewardedLoadingMessage, string.IsNullOrWhiteSpace(_rewardedLoadingMessageEnglish) ? "Loading ad..." : _rewardedLoadingMessageEnglish);
        }

        private string GetRewardedOpeningMessage()
        {
            return TrEn(_rewardedOpeningMessage, string.IsNullOrWhiteSpace(_rewardedOpeningMessageEnglish) ? "Opening ad..." : _rewardedOpeningMessageEnglish);
        }

        private string GetRewardedLoadFailedMessage()
        {
            return TrEn(_rewardedLoadFailedMessage, string.IsNullOrWhiteSpace(_rewardedLoadFailedMessageEnglish) ? "Ad is currently unavailable." : _rewardedLoadFailedMessageEnglish);
        }

        private static string FormatContinueCountdown(int seconds)
        {
            return TrEn($"Devam için: {seconds} sn", $"Continue in: {seconds}s");
        }

        private static bool IsEnglishSelected()
        {
            return LanguageManager.Instance != null
                && LanguageManager.Instance.CurrentLanguage == LanguageManager.Language.English;
        }

        private static readonly Dictionary<string, string> KoreanTranslations = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
        {
            { "YENİ REKOR!", "새로운 기록!" },
            { "NEW BEST!", "새로운 기록!" },
            { "Devam için:", "계속하기:" },
            { "Continue in:", "계속하기:" },
            { "No moves left!", "더 이상 움직일 수 없습니다!" },
            { "Hamlen Kalmadi!", "더 이상 움직일 수 없습니다!" },
            { "Loading ad...", "광고를 불러오는 중..." },
            { "Reklam yükleniyor...", "광고를 불러오는 중..." },
            { "Opening ad...", "광고를 여는 중..." },
            { "Reklam açılıyor...", "광고를 여는 중..." },
            { "Ad is currently unavailable.", "광고를 현재 사용할 수 없습니다." },
            { "Reklam şu anda kullanılamıyor.", "광고를 현재 사용할 수 없습니다." },
            { "Reklam izlemek için 5 saniyen var", "광고 시청까지 5초 남았습니다" },
            { "You have 5 seconds to watch the ad", "광고 시청까지 5초 남았습니다" },
            { "Reklam izleme hakkınız kalmadı", "더 이상 시청할 수 있는 광고가 없습니다" },
            { "You have no ad watches left", "더 이상 시청할 수 있는 광고가 없습니다" },
        };

        private static string TrEn(string turkish, string english)
        {
            if (LanguageManager.Instance == null)
                return english;

            if (LanguageManager.Instance.CurrentLanguage == LanguageManager.Language.Korean)
            {
                if (!string.IsNullOrEmpty(english))
                {
                    if (english.StartsWith("Continue in:", System.StringComparison.OrdinalIgnoreCase))
                    {
                        string secStr = english.Replace("Continue in:", "").Replace("s", "").Trim();
                        return $"계속하기: {secStr}초";
                    }
                    if (KoreanTranslations.TryGetValue(english, out string koTranslation))
                        return koTranslation;
                }
                if (!string.IsNullOrEmpty(turkish))
                {
                    if (turkish.StartsWith("Devam için:", System.StringComparison.OrdinalIgnoreCase))
                    {
                        string secStr = turkish.Replace("Devam için:", "").Replace("sn", "").Replace("s", "").Trim();
                        return $"계속하기: {secStr}초";
                    }
                    if (KoreanTranslations.TryGetValue(turkish, out string koTranslation))
                        return koTranslation;
                }
                return !string.IsNullOrEmpty(english) ? english : turkish;
            }

            return IsEnglishSelected() ? english : turkish;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name))
                return null;

            if (root.name == name)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform result = FindDeep(root.GetChild(i), name);
                if (result != null)
                    return result;
            }

            return null;
        }
    }
}
