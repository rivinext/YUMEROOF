using UnityEngine;
using Steamworks;

namespace Yume
{
    public class SteamManager : MonoBehaviour
    {
        public static bool Initialized { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<SteamManager>() != null)
            {
                return;
            }

            var gameObject = new GameObject(nameof(SteamManager));
            gameObject.AddComponent<SteamManager>();
            DontDestroyOnLoad(gameObject);
        }

        private void Awake()
        {
            if (Initialized)
            {
                return;
            }

            if (!SteamAPI.IsSteamRunning())
            {
                Debug.LogWarning("Steam is not running. Steam API initialization skipped.");
                Initialized = false;
                return;
            }

            Initialized = SteamAPI.Init();

            if (!Initialized)
            {
                Debug.LogError("Steam API initialization failed.");
            }
            else
            {
                Debug.Log("Steam API initialized successfully.");
            }
        }

        private void Update()
        {
            if (!Initialized)
            {
                return;
            }

            SteamAPI.RunCallbacks();
        }

        private void OnDestroy()
        {
            if (!Initialized)
            {
                return;
            }

            SteamAPI.Shutdown();
            Initialized = false;
        }
    }
}
