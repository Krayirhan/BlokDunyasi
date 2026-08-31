using UnityEngine;
using BlockPuzzle.Core.Common;

/// <summary>
/// Oyun başlangıcında AppInitializer'ı başlatır.
/// RuntimeInitializeOnLoadAttribute ile çalışır.
/// </summary>
/// <summary>
/// App-level runtime bootstrap.
/// This class is not the gameplay scene composition root.
/// It auto-initializes before scene load to ensure AppInitializer startup.
/// The active gameplay composition root is `BlockPuzzle.UnityAdapter.Boot.GameBootstrap`.
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    private static GameBootstrap instance;

    public static GameBootstrap Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject obj = new GameObject("GameBootstrap");
                instance = obj.AddComponent<GameBootstrap>();
                DontDestroyOnLoad(obj);
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // AppInitializer singleton'unu olustur ve Awake icerisindeki kurulum akisini tetikle
        _ = AppInitializer.Instance;
        GameLogger.Log("[GameBootstrap] Oyun baslatildi, sistem yoneticileri hazirlandi.");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        // AppInitializer'ı Start'a bırakma. RuntimeInitialize callback'i ile
        // reklam başlangıcını sahne yaşam döngüsünden bağımsız garanti et.
        _ = Instance;
        _ = AppInitializer.Instance;
        UnityEngine.Debug.Log("[GameBootstrap] AppInitializer otomatik olarak baslatildi.");
    }
}
