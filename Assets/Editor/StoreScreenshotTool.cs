#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BlokDunyasiTools
{
    /// <summary>
    /// One-click Play Store screenshot generator.
    /// Menu: BlokDunyasi/Store/Generate Play Store Screenshots
    /// </summary>
    [InitializeOnLoad]
    public static class StoreScreenshotTool
    {
        private const double WarmupSeconds = 1.2d;
        private const double ResizeSettleSeconds = 0.35d;
        private const double CaptureTimeoutSeconds = 12d;
        private const long MaxGooglePlayBytes = 8L * 1024L * 1024L;
        private const string RootOutputFolderName = "StoreScreenshots";

        private enum RunState
        {
            Idle,
            WaitingForPlayMode,
            Warmup,
            WaitingAfterResize,
            WaitingForCaptureFile,
            FinishingPlayMode
        }

        private readonly struct ScreenshotTarget
        {
            public readonly string Category;
            public readonly string Name;
            public readonly int Width;
            public readonly int Height;

            public ScreenshotTarget(string category, string name, int width, int height)
            {
                Category = category;
                Name = name;
                Width = width;
                Height = height;
            }

            public override string ToString()
            {
                return $"{Category}/{Name} ({Width}x{Height})";
            }
        }

        // Safe-by-default set for Play Store constraints:
        // - phone: at least 2
        // - 7-inch tablet: at least 1
        // - 10-inch tablet: at least 1 (min edge >= 1080)
        private static readonly ScreenshotTarget[] Targets =
        {
            new ScreenshotTarget("phone", "phone_01", 1080, 1920),
            new ScreenshotTarget("phone", "phone_02", 1170, 2080),
            new ScreenshotTarget("tablet7", "tablet7_01", 1260, 2240),
            new ScreenshotTarget("tablet10", "tablet10_01", 1440, 2560),
        };

        private static Queue<ScreenshotTarget> _queue;
        private static ScreenshotTarget _currentTarget;
        private static string _currentCaptureAbsolutePath;
        private static string _batchOutputAbsolutePath;
        private static string _lastBatchOutputAbsolutePath;
        private static bool _isRunning;
        private static bool _ownsPlayMode;
        private static RunState _state;
        private static double _stateStartedAt;
        private static long _lastObservedFileSize;
        private static int _stableFileChecks;
        private static int _captureCount;

        static StoreScreenshotTool()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem("BlokDunyasi/Store/Generate Play Store Screenshots")]
        public static void GeneratePlayStoreScreenshots()
        {
            if (_isRunning)
            {
                Debug.LogWarning("[StoreScreenshotTool] Capture is already running.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            _queue = new Queue<ScreenshotTarget>(Targets);
            _batchOutputAbsolutePath = CreateBatchOutputFolder();
            _captureCount = 0;
            _isRunning = true;
            _ownsPlayMode = !EditorApplication.isPlaying;
            _state = _ownsPlayMode ? RunState.WaitingForPlayMode : RunState.Warmup;
            _stateStartedAt = EditorApplication.timeSinceStartup;
            _lastObservedFileSize = -1L;
            _stableFileChecks = 0;

            EditorApplication.update += Tick;

            Debug.Log($"[StoreScreenshotTool] Output folder: {ToProjectRelative(_batchOutputAbsolutePath)}");

            if (_ownsPlayMode)
            {
                EditorApplication.isPlaying = true;
            }
            else
            {
                Debug.Log("[StoreScreenshotTool] Play Mode already active, starting capture sequence.");
            }
        }

        [MenuItem("BlokDunyasi/Store/Generate Play Store Screenshots", true)]
        private static bool ValidateGeneratePlayStoreScreenshots()
        {
            return !_isRunning;
        }

        [MenuItem("BlokDunyasi/Store/Open Last Screenshot Folder", false, 301)]
        public static void OpenLastScreenshotFolder()
        {
            if (string.IsNullOrEmpty(_lastBatchOutputAbsolutePath) || !Directory.Exists(_lastBatchOutputAbsolutePath))
            {
                Debug.LogWarning("[StoreScreenshotTool] No screenshot folder found yet.");
                return;
            }

            EditorUtility.RevealInFinder(_lastBatchOutputAbsolutePath);
        }

        [MenuItem("BlokDunyasi/Store/Open Last Screenshot Folder", true)]
        private static bool ValidateOpenLastScreenshotFolder()
        {
            return !string.IsNullOrEmpty(_lastBatchOutputAbsolutePath) && Directory.Exists(_lastBatchOutputAbsolutePath);
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange stateChange)
        {
            if (!_isRunning)
                return;

            if (stateChange == PlayModeStateChange.EnteredPlayMode && _state == RunState.WaitingForPlayMode)
            {
                _state = RunState.Warmup;
                _stateStartedAt = EditorApplication.timeSinceStartup;
                Debug.Log("[StoreScreenshotTool] Entered Play Mode. Warmup started.");
                return;
            }

            if (stateChange == PlayModeStateChange.EnteredEditMode && _state == RunState.FinishingPlayMode)
            {
                CompleteRun(success: true);
                return;
            }

            // If play mode exits unexpectedly while capturing, abort safely.
            if (stateChange == PlayModeStateChange.EnteredEditMode &&
                _state != RunState.FinishingPlayMode &&
                _state != RunState.Idle)
            {
                AbortRun("Play Mode exited before screenshot generation completed.");
            }
        }

        private static void Tick()
        {
            if (!_isRunning)
                return;

            switch (_state)
            {
                case RunState.WaitingForPlayMode:
                    return;

                case RunState.Warmup:
                    if (HasElapsed(WarmupSeconds))
                        BeginNextCapture();
                    return;

                case RunState.WaitingAfterResize:
                    if (HasElapsed(ResizeSettleSeconds))
                        TriggerCapture();
                    return;

                case RunState.WaitingForCaptureFile:
                    PollCaptureFile();
                    return;

                case RunState.FinishingPlayMode:
                    return;
            }
        }

        private static void BeginNextCapture()
        {
            if (_queue == null || _queue.Count == 0)
            {
                FinishSequence();
                return;
            }

            _currentTarget = _queue.Dequeue();
            string folder = Path.Combine(_batchOutputAbsolutePath, _currentTarget.Category);
            Directory.CreateDirectory(folder);

            _currentCaptureAbsolutePath = Path.Combine(
                folder,
                $"{_currentTarget.Name}_{_currentTarget.Width}x{_currentTarget.Height}.png");

            if (File.Exists(_currentCaptureAbsolutePath))
                File.Delete(_currentCaptureAbsolutePath);

            if (!GameViewReflection.TrySetGameViewSize(_currentTarget.Width, _currentTarget.Height, _currentTarget.Name))
            {
                Debug.LogWarning($"[StoreScreenshotTool] Could not set Game View to {_currentTarget.Width}x{_currentTarget.Height}. Capturing with current size.");
            }

            _state = RunState.WaitingAfterResize;
            _stateStartedAt = EditorApplication.timeSinceStartup;
            _lastObservedFileSize = -1L;
            _stableFileChecks = 0;

            Debug.Log($"[StoreScreenshotTool] Capturing {_currentTarget} ...");
        }

        private static void TriggerCapture()
        {
            ScreenCapture.CaptureScreenshot(_currentCaptureAbsolutePath);
            _state = RunState.WaitingForCaptureFile;
            _stateStartedAt = EditorApplication.timeSinceStartup;
        }

        private static void PollCaptureFile()
        {
            if (!File.Exists(_currentCaptureAbsolutePath))
            {
                if (HasElapsed(CaptureTimeoutSeconds))
                {
                    Debug.LogError($"[StoreScreenshotTool] Capture timeout: {_currentTarget}");
                    BeginNextCapture();
                }
                return;
            }

            long currentSize = new FileInfo(_currentCaptureAbsolutePath).Length;
            if (currentSize <= 0)
            {
                if (HasElapsed(CaptureTimeoutSeconds))
                {
                    Debug.LogError($"[StoreScreenshotTool] Capture file is empty: {_currentCaptureAbsolutePath}");
                    BeginNextCapture();
                }
                return;
            }

            if (currentSize == _lastObservedFileSize)
            {
                _stableFileChecks++;
            }
            else
            {
                _stableFileChecks = 0;
                _lastObservedFileSize = currentSize;
            }

            if (_stableFileChecks < 2)
                return;

            ValidateCaptureFile(_currentCaptureAbsolutePath, _currentTarget);
            _captureCount++;
            BeginNextCapture();
        }

        private static void ValidateCaptureFile(string absolutePath, ScreenshotTarget target)
        {
            long fileSize = new FileInfo(absolutePath).Length;
            float sizeMb = fileSize / (1024f * 1024f);

            byte[] bytes = File.ReadAllBytes(absolutePath);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            bool loaded = tex.LoadImage(bytes, markNonReadable: true);
            int actualW = tex.width;
            int actualH = tex.height;
            UnityEngine.Object.DestroyImmediate(tex);

            if (!loaded)
            {
                Debug.LogError($"[StoreScreenshotTool] Could not decode screenshot file: {absolutePath}");
                return;
            }

            if (actualW != target.Width || actualH != target.Height)
            {
                Debug.LogWarning(
                    $"[StoreScreenshotTool] Resolution mismatch for {target.Name}. Expected {target.Width}x{target.Height}, got {actualW}x{actualH}.");
            }

            if (fileSize > MaxGooglePlayBytes)
            {
                Debug.LogWarning(
                    $"[StoreScreenshotTool] {target.Name} is {sizeMb:F2} MB (>{MaxGooglePlayBytes / (1024f * 1024f):F0} MB Play Store limit).");
            }

            Debug.Log($"[StoreScreenshotTool] Saved {target.Name}: {actualW}x{actualH}, {sizeMb:F2} MB");
        }

        private static void FinishSequence()
        {
            AssetDatabase.Refresh();
            _lastBatchOutputAbsolutePath = _batchOutputAbsolutePath;

            if (_ownsPlayMode && EditorApplication.isPlaying)
            {
                _state = RunState.FinishingPlayMode;
                EditorApplication.isPlaying = false;
                return;
            }

            CompleteRun(success: true);
        }

        private static void CompleteRun(bool success)
        {
            string relative = ToProjectRelative(_batchOutputAbsolutePath);
            CleanupRunState();

            if (success)
            {
                Debug.Log($"[StoreScreenshotTool] Completed. Captures: {_captureCount}. Folder: {relative}");
                if (!string.IsNullOrEmpty(_lastBatchOutputAbsolutePath) && Directory.Exists(_lastBatchOutputAbsolutePath))
                    EditorUtility.RevealInFinder(_lastBatchOutputAbsolutePath);
            }
        }

        private static void AbortRun(string reason)
        {
            Debug.LogError($"[StoreScreenshotTool] Aborted: {reason}");
            CleanupRunState();
        }

        private static void CleanupRunState()
        {
            EditorApplication.update -= Tick;

            _isRunning = false;
            _ownsPlayMode = false;
            _state = RunState.Idle;
            _queue = null;
            _currentCaptureAbsolutePath = null;
            _batchOutputAbsolutePath = null;
            _stateStartedAt = 0d;
            _lastObservedFileSize = -1L;
            _stableFileChecks = 0;
            _captureCount = 0;
        }

        private static bool HasElapsed(double seconds)
        {
            return EditorApplication.timeSinceStartup - _stateStartedAt >= seconds;
        }

        private static string CreateBatchOutputFolder()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string root = Path.Combine(projectRoot, RootOutputFolderName);
            Directory.CreateDirectory(root);

            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string batch = Path.Combine(root, $"batch_{stamp}");
            Directory.CreateDirectory(batch);
            return batch;
        }

        private static string ToProjectRelative(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath))
                return string.Empty;

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string normalizedProjectRoot = projectRoot.Replace('\\', '/');
            string normalizedAbsolute = Path.GetFullPath(absolutePath).Replace('\\', '/');

            if (!normalizedAbsolute.StartsWith(normalizedProjectRoot, StringComparison.OrdinalIgnoreCase))
                return normalizedAbsolute;

            return normalizedAbsolute.Substring(normalizedProjectRoot.Length + 1);
        }

        private static class GameViewReflection
        {
            private static readonly Assembly EditorAssembly = typeof(Editor).Assembly;
            private static readonly Type GameViewType = EditorAssembly.GetType("UnityEditor.GameView");
            private static readonly Type GameViewSizesType = EditorAssembly.GetType("UnityEditor.GameViewSizes");
            private static readonly Type GameViewSizeType = EditorAssembly.GetType("UnityEditor.GameViewSize");
            private static readonly Type GameViewSizeTypeEnum = EditorAssembly.GetType("UnityEditor.GameViewSizeType");

            public static bool TrySetGameViewSize(int width, int height, string label)
            {
                try
                {
                    if (GameViewType == null || GameViewSizesType == null || GameViewSizeType == null || GameViewSizeTypeEnum == null)
                        return false;

                    object group = GetCurrentGroup();
                    if (group == null)
                        return false;

                    int sizeIndex = FindSizeIndex(group, width, height);
                    if (sizeIndex < 0)
                        sizeIndex = AddCustomSize(group, width, height, label);

                    var gameView = GetMainGameView();
                    if (gameView == null)
                        return false;

                    var selectedSizeIndexProp = GameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (selectedSizeIndexProp == null)
                        return false;

                    selectedSizeIndexProp.SetValue(gameView, sizeIndex, null);
                    gameView.Repaint();
                    gameView.Focus();
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[StoreScreenshotTool] GameView resize failed: {ex.Message}");
                    return false;
                }
            }

            private static EditorWindow GetMainGameView()
            {
                var method = GameViewType.GetMethod("GetMainGameView", BindingFlags.NonPublic | BindingFlags.Static);
                if (method == null)
                    return null;

                return method.Invoke(null, null) as EditorWindow;
            }

            private static object GetCurrentGroup()
            {
                var instanceProp = GameViewSizesType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
                object instance = instanceProp?.GetValue(null, null);
                if (instance == null)
                    return null;

                var getCurrentGroupTypeMethod = GameViewSizesType.GetMethod("GetCurrentGroupType", BindingFlags.NonPublic | BindingFlags.Static);
                object currentGroupType = getCurrentGroupTypeMethod?.Invoke(null, null);
                if (currentGroupType == null)
                    return null;

                var getGroupMethod = GameViewSizesType.GetMethod("GetGroup");
                return getGroupMethod?.Invoke(instance, new[] { currentGroupType });
            }

            private static int FindSizeIndex(object group, int width, int height)
            {
                var groupType = group.GetType();
                var getTotalCount = groupType.GetMethod("GetTotalCount");
                var getGameViewSize = groupType.GetMethod("GetGameViewSize");

                if (getTotalCount == null || getGameViewSize == null)
                    return -1;

                int count = (int)getTotalCount.Invoke(group, null);
                for (int i = 0; i < count; i++)
                {
                    object size = getGameViewSize.Invoke(group, new object[] { i });
                    if (size == null)
                        continue;

                    var sizeObjType = size.GetType();
                    var widthProp = sizeObjType.GetProperty("width");
                    var heightProp = sizeObjType.GetProperty("height");
                    if (widthProp == null || heightProp == null)
                        continue;

                    int w = (int)widthProp.GetValue(size, null);
                    int h = (int)heightProp.GetValue(size, null);

                    if (w == width && h == height)
                        return i;
                }

                return -1;
            }

            private static int AddCustomSize(object group, int width, int height, string label)
            {
                object fixedResolutionType = Enum.ToObject(GameViewSizeTypeEnum, 1); // FixedResolution
                object newSize = Activator.CreateInstance(GameViewSizeType, fixedResolutionType, width, height, label);

                var groupType = group.GetType();
                var addCustomSize = groupType.GetMethod("AddCustomSize");
                var getTotalCount = groupType.GetMethod("GetTotalCount");

                addCustomSize?.Invoke(group, new[] { newSize });
                return (int)getTotalCount.Invoke(group, null) - 1;
            }
        }
    }
}
#endif
