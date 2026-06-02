using BlockPuzzle.Core.Board;
using BlockPuzzle.Core.Common;
using BlockPuzzle.UnityAdapter.Boot;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Debug = BlockPuzzle.Core.Common.GameLogger;

namespace BlockPuzzle.UnityAdapter.Audio
{
    [ExecuteAlways]
    [DefaultExecutionOrder(-250)]
    public sealed class AudioManager : MonoBehaviour
    {
        private const string MusicEnabledKey = SettingsKeys.MusicOn;
        private const string MusicVolumeKey = SettingsKeys.MusicVolume;
        private const string SfxEnabledKey = SettingsKeys.SfxOn;
        private const string SfxVolumeKey = SettingsKeys.SfxVolume;
        private const string DefaultBankResourcePath = "Audio/DefaultAudioBank";

        public static AudioManager Instance { get; private set; }

        [Header("Bank")]
        [SerializeField] private AudioBank bank;

        [Header("Lifecycle")]
        [SerializeField] private bool dontDestroyAcrossScenes = true;
        [SerializeField] private bool autoHookSceneButtons = true;
        [SerializeField] private bool autoPlaySceneMusic = true;

        [Header("Scene Music Routing")]
        [SerializeField] private string[] mainMenuSceneNames = { SceneCatalog.MainMenu, SceneCatalog.Settings };
        [SerializeField] private string[] gameplaySceneNames = { SceneCatalog.Game };
        [SerializeField] private string[] gameOverSceneNames = { SceneCatalog.GameOver };

        [Header("Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Mix")]
        [SerializeField] [Range(0f, 1f)] private float musicVolumeMultiplier = 1f;
        [SerializeField] [Range(0f, 1f)] private float sfxVolumeMultiplier = 1f;

        [Header("Runtime Overrides")]
        [SerializeField] private bool disableMusicPlayback = true;
        [SerializeField] private bool disableSfxPlayback = true;

        [Header("Debug")]
        [SerializeField] private bool verboseLogs = false;

        private bool _subscribed;
        private bool _musicEnabled = true;
        private bool _sfxEnabled = true;
        private float _musicVolume = 1f;
        private float _sfxVolume = 1f;

        private bool CanLog => verboseLogs && Debug.isDebugBuild;

        private void Awake()
        {
            EnsureConfigurationReferences();

            if (!Application.isPlaying)
                return;

            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (dontDestroyAcrossScenes)
                DontDestroyOnLoad(gameObject);

            SubscribeRuntimeHooks();
            RefreshFromPrefs();
            HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private void OnEnable()
        {
            EnsureConfigurationReferences();

            if (!Application.isPlaying)
                return;

            if (Instance == null)
                Instance = this;

            if (Instance == this)
                SubscribeRuntimeHooks();
        }

        private void OnDisable()
        {
            if (!Application.isPlaying || Instance != this)
                return;

            UnsubscribeRuntimeHooks();
        }

        private void OnDestroy()
        {
            if (!Application.isPlaying || Instance != this)
                return;

            UnsubscribeRuntimeHooks();
            Instance = null;
        }

        private void OnValidate()
        {
            musicVolumeMultiplier = Mathf.Clamp01(musicVolumeMultiplier);
            sfxVolumeMultiplier = Mathf.Clamp01(sfxVolumeMultiplier);
            EnsureConfigurationReferences();
        }

        private void EnsureConfigurationReferences()
        {
            if (bank == null)
                bank = Resources.Load<AudioBank>(DefaultBankResourcePath);

            musicSource = EnsureChildSource(musicSource, "MusicSource", shouldLoop: true);
            sfxSource = EnsureChildSource(sfxSource, "SfxSource", shouldLoop: false);
        }

        private AudioSource EnsureChildSource(AudioSource current, string childName, bool shouldLoop)
        {
            if (current == null)
            {
                Transform child = transform.Find(childName);
                GameObject childObject;

                if (child != null)
                {
                    childObject = child.gameObject;
                }
                else
                {
                    childObject = new GameObject(childName);
                    childObject.transform.SetParent(transform, false);
                }

                current = childObject.GetComponent<AudioSource>();
                if (current == null)
                    current = childObject.AddComponent<AudioSource>();
            }

            current.playOnAwake = false;
            current.loop = shouldLoop;
            current.spatialBlend = 0f;
            current.volume = 1f;
            current.mute = false;
            current.priority = shouldLoop ? 64 : 128;
            return current;
        }

        private void SubscribeRuntimeHooks()
        {
            if (_subscribed)
                return;

            SceneManager.sceneLoaded += HandleSceneLoaded;
            GameBootstrap.OnBoardChanged += HandleBoardChanged;
            GameBootstrap.OnScoreBreakdown += HandleScoreBreakdown;
            GameBootstrap.OnGameOver += HandleGameOver;
            _subscribed = true;
        }

        private void UnsubscribeRuntimeHooks()
        {
            if (!_subscribed)
                return;

            SceneManager.sceneLoaded -= HandleSceneLoaded;
            GameBootstrap.OnBoardChanged -= HandleBoardChanged;
            GameBootstrap.OnScoreBreakdown -= HandleScoreBreakdown;
            GameBootstrap.OnGameOver -= HandleGameOver;
            _subscribed = false;
        }

        public void RefreshFromPrefs()
        {
            _musicEnabled = PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1;
            _musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, 1f));
            _sfxEnabled = PlayerPrefs.GetInt(SfxEnabledKey, 1) == 1;
            _sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, 1f));

            if (musicSource != null)
                musicSource.volume = _musicEnabled ? _musicVolume * musicVolumeMultiplier : 0f;

            if (!_musicEnabled && musicSource != null && musicSource.isPlaying)
                musicSource.Stop();
        }

        public void PlayUiClick()
        {
            PlayCue(bank != null ? bank.uiClick : null, 1f);
        }

        public void PlayBlockPlace()
        {
            PlayCue(bank != null ? bank.blockPlace : null, 1f);
        }

        public void PlayInvalidDrop()
        {
            PlayCue(bank != null ? bank.invalidDrop : null, 1f);
        }

        public void PlayLineClear(int linesCleared)
        {
            float intensity = Mathf.Lerp(1f, 1.2f, Mathf.Clamp01((Mathf.Max(1, linesCleared) - 1) / 3f));
            PlayCue(bank != null ? bank.lineClear : null, intensity);
        }

        public void PlayCombo(int comboStreak)
        {
            float intensity = Mathf.Lerp(1f, 1.25f, Mathf.Clamp01((Mathf.Max(2, comboStreak) - 2) / 6f));
            PlayCue(bank != null ? bank.combo : null, intensity);
        }

        public void PlayGameOver()
        {
            PlayCue(bank != null ? bank.gameOver : null, 1f);
        }

        private void PlayCue(AudioCue cue, float intensity)
        {
            // Kullanıcı isteği üzerine tüm ses efektleri şimdilik kapatıldı.
            if (disableSfxPlayback)
                return;

            if (!_sfxEnabled || cue == null || sfxSource == null)
                return;

            if (!cue.TryPick(out var clip, out var volume, out var pitch))
                return;

            sfxSource.pitch = pitch;
            sfxSource.PlayOneShot(clip, volume * _sfxVolume * sfxVolumeMultiplier * Mathf.Max(0f, intensity));
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureConfigurationReferences();
            RefreshFromPrefs();

            if (autoHookSceneButtons)
                HookSceneButtons(scene);

            if (autoPlaySceneMusic)
                PlayMusicForScene(scene.name);
        }

        private void HookSceneButtons(Scene scene)
        {
            if (!scene.IsValid())
                return;

            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                var buttons = roots[i].GetComponentsInChildren<Button>(true);
                for (int j = 0; j < buttons.Length; j++)
                {
                    var button = buttons[j];
                    if (button == null)
                        continue;

                    button.onClick.RemoveListener(HandleAnyButtonClicked);
                    button.onClick.AddListener(HandleAnyButtonClicked);
                }
            }
        }

        private void HandleAnyButtonClicked()
        {
            PlayUiClick();
        }

        private void PlayMusicForScene(string sceneName)
        {
            // Kullanıcı isteği üzerine oyun müziği tamamen kapatıldı.
            if (disableMusicPlayback)
            {
                if (musicSource != null && musicSource.isPlaying)
                    musicSource.Stop();
                return;
            }

            if (musicSource == null)
                return;

            AudioClip targetClip = ResolveMusicClip(sceneName);
            if (!_musicEnabled || targetClip == null)
            {
                if (musicSource.isPlaying)
                    musicSource.Stop();
                musicSource.clip = null;
                return;
            }

            musicSource.volume = _musicVolume * musicVolumeMultiplier;
            if (musicSource.clip == targetClip && musicSource.isPlaying)
                return;

            musicSource.clip = targetClip;
            musicSource.Play();

            if (CanLog)
                Debug.Log($"[AudioManager] Playing music for scene '{sceneName}': {targetClip.name}");
        }

        private AudioClip ResolveMusicClip(string sceneName)
        {
            if (bank == null || string.IsNullOrWhiteSpace(sceneName))
                return null;

            if (ContainsScene(mainMenuSceneNames, sceneName))
                return bank.mainMenuMusic;

            if (ContainsScene(gameOverSceneNames, sceneName))
                return bank.gameOverMusic;

            if (ContainsScene(gameplaySceneNames, sceneName))
                return bank.gameplayMusic;

            return null;
        }

        private static bool ContainsScene(string[] sceneNames, string sceneName)
        {
            if (sceneNames == null)
                return false;

            for (int i = 0; i < sceneNames.Length; i++)
            {
                if (string.Equals(sceneNames[i], sceneName, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private void HandleBoardChanged(BoardState boardState, Int2[] clearedPositions, int linesCleared)
        {
            if (linesCleared > 0)
                PlayLineClear(linesCleared);
            else
                PlayBlockPlace();
        }

        private void HandleScoreBreakdown(ScoreBreakdownInfo breakdown)
        {
            if (breakdown.LinesCleared > 0 && breakdown.ComboStreak >= 2)
                PlayCombo(breakdown.ComboStreak);
        }

        private void HandleGameOver(int finalScore)
        {
            PlayGameOver();
        }
    }
}
