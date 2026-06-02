using System.Diagnostics;

namespace BlockPuzzle.Core.Common
{
    /// <summary>
    /// Centralized runtime logger to keep release builds quieter.
    /// </summary>
    public static class GameLogger
    {
        public static bool isDebugBuild
        {
            get
            {
#if DEBUG || DEVELOPMENT_BUILD || UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        private static bool IsVerboseEnabled => isDebugBuild;

        public static void Log(string message)
        {
            if (!IsVerboseEnabled)
                return;

            Trace.WriteLine(message);
        }

        public static void LogWarning(string message)
        {
            if (!IsVerboseEnabled)
                return;

            Trace.TraceWarning(message);
        }

        public static void LogError(string message)
        {
            // Keep errors visible in all builds.
            Trace.TraceError(message);
        }
    }
}
