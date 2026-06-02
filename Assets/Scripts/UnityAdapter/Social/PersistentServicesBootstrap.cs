using BlockPuzzle.Core.Social;
using BlockPuzzle.UnityAdapter.Auth;
using UnityEngine;

namespace BlockPuzzle.UnityAdapter.Social
{
    /// <summary>
    /// Guarantees the social/auth services exist even when starting play from a non-entry scene.
    /// Existing scene instances are preserved; missing services are created under a single
    /// persistent root so all scenes can rely on the same global managers.
    /// </summary>
    public static class PersistentServicesBootstrap
    {
        private const string RootName = "[PersistentServices]";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureServices()
        {
            var root = FindExistingRoot();
            if (root == null)
            {
                root = new GameObject(RootName);
                Object.DontDestroyOnLoad(root);
            }
            else
            {
                Object.DontDestroyOnLoad(root);
            }

            EnsureService<FirebaseManager>(root.name);
            EnsureService<GooglePlayGamesManager>(root.name);
            EnsureService<AuthManager>(root.name);
            EnsureService<LeaderboardManager>(root.name);
        }

        private static GameObject FindExistingRoot()
        {
            var roots = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < roots.Length; i++)
            {
                var transform = roots[i];
                if (transform != null && transform.parent == null && transform.name == RootName)
                    return transform.gameObject;
            }

            return null;
        }

        private static void EnsureService<T>(string rootName) where T : Component
        {
            if (Object.FindFirstObjectByType<T>(FindObjectsInactive.Include) != null)
                return;

            var serviceObject = new GameObject($"{rootName}/{typeof(T).Name}");
            serviceObject.AddComponent<T>();
        }
    }
}
