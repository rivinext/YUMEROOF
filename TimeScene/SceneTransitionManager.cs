using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class SceneTransitionManager : MonoBehaviour
{
    private static SceneTransitionManager instance;
    public static SceneTransitionManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<SceneTransitionManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("SceneTransitionManager");
                    instance = go.AddComponent<SceneTransitionManager>();
                }
            }
            return instance;
        }
    }

    [Header("Scene Settings")]
    [SerializeField] private string baseSceneName = "TimeScene";

    [Header("Fade Settings")]
    public float fadeDuration = 1f;
    public Color fadeColor = Color.black;

    [Header("Audio Settings")]
    public AudioClip doorOpenSound;
    public AudioClip transitionSound;

    [Header("Player Settings")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private string playerPrefabResourcePath = string.Empty;

    private Canvas fadeCanvas;
    private Image fadeImage;
    private AudioSource audioSource;
    private bool isTransitioning = false;
    private string lastSceneName = "";

    // 遷移情報を保持
    public string LastSceneName => lastSceneName;
    public bool IsTransitioning => isTransitioning;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            CreateFadeCanvas();
            SetupAudioSource();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void CreateFadeCanvas()
    {
        // フェード用キャンバス作成
        GameObject canvasGO = new GameObject("FadeCanvas");
        canvasGO.transform.SetParent(transform);

        fadeCanvas = canvasGO.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 9999; // 最前面

        canvasGO.AddComponent<CanvasScaler>();
        // canvasGO.AddComponent<GraphicRaycaster>();

        // フェード用画像
        GameObject imageGO = new GameObject("FadeImage");
        imageGO.transform.SetParent(canvasGO.transform, false);

        fadeImage = imageGO.AddComponent<Image>();
        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0);

        // ★ ここを追加：クリックを常に拾わない
        fadeImage.raycastTarget = false;

        // 画面全体を覆う
        RectTransform rect = imageGO.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
    }

    void SetupAudioSource()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    // シーン遷移（ドア用）
    public void TransitionToScene(string sceneName, string spawnPointName, bool playDoorSound = true)
    {
        if (!isTransitioning)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            StartCoroutine(TransitionCoroutine(sceneName, spawnPointName, playDoorSound));
        }
    }

    // シーン遷移（エリア用）
    public void TransitionToSceneInstant(string sceneName, string spawnPointName)
    {
        if (!isTransitioning)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            StartCoroutine(TransitionCoroutine(sceneName, spawnPointName, false));
        }
    }

    IEnumerator TransitionCoroutine(string sceneName, string spawnPointName, bool playDoorSound)
    {
        isTransitioning = true;

        // 現在のシーン名を保存
        lastSceneName = SceneManager.GetActiveScene().name;

        // プレイヤー入力を無効化
        DisablePlayerControl(true);

        // ドア音再生
        if (playDoorSound && doorOpenSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(doorOpenSound);
            yield return new WaitForSeconds(0.3f); // ドア音を少し聞かせる
        }

        // 遷移音再生
        if (transitionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(transitionSound);
        }

        // フェードアウト
        yield return StartCoroutine(Fade(1f));

        // ベースシーン読み込み確認
        Scene baseScene = SceneManager.GetSceneByName(baseSceneName);
        if (!string.IsNullOrEmpty(baseSceneName) && (!baseScene.IsValid() || !baseScene.isLoaded))
        {
            AsyncOperation baseLoad = SceneManager.LoadSceneAsync(baseSceneName, LoadSceneMode.Additive);
            while (baseLoad != null && !baseLoad.isDone)
            {
                yield return null;
            }
            baseScene = SceneManager.GetSceneByName(baseSceneName);
        }

        // シーン読み込み
        var slotKey = SaveGameManager.Instance?.CurrentSlotKey;
        GameSessionInitializer.CreateIfNeeded(slotKey);
        Scene newScene = SceneManager.GetSceneByName(sceneName);
        if (!newScene.IsValid() || !newScene.isLoaded)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
            newScene = SceneManager.GetSceneByName(sceneName);
        }

        if (newScene.IsValid())
        {
            SceneManager.SetActiveScene(newScene);
        }

        // スポーン位置に移動
        MovePlayerToSpawnPoint(spawnPointName);

        if (!string.IsNullOrEmpty(lastSceneName) && lastSceneName != baseSceneName && lastSceneName != sceneName)
        {
            AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(lastSceneName);
            while (unloadOp != null && !unloadOp.isDone)
            {
                yield return null;
            }
        }

        // 少し待機
        yield return new WaitForSeconds(0.1f);

        // フェードイン
        yield return StartCoroutine(Fade(0f));

        // プレイヤー入力を有効化
        DisablePlayerControl(false);

        isTransitioning = false;
    }

    // Allow external scripts to trigger a fade without a scene transition
    public IEnumerator FadeRoutine(float targetAlpha)
    {
        yield return Fade(targetAlpha);
    }

    IEnumerator Fade(float targetAlpha)
    {
        if (fadeImage == null) yield break;

        float startAlpha = fadeImage.color.a;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / fadeDuration);
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
            yield return null;
        }

        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, targetAlpha);
    }

    void MovePlayerToSpawnPoint(string spawnPointName)
    {
        GameObject player = EnsurePlayerExists();
        if (player == null)
            return;

        // スポーンポイントを探す
        PlayerSpawnPoint[] spawnPoints = FindObjectsOfType<PlayerSpawnPoint>();
        foreach (var point in spawnPoints)
        {
            if (point.spawnPointName == spawnPointName)
            {
                player.transform.position = point.transform.position;
                player.transform.rotation = point.transform.rotation;

                // カメラの位置もリセット（必要に応じて）
                var cameraController = FindObjectOfType<OrthographicCameraController>();
                if (cameraController != null)
                {
                    cameraController.ResetCamera();
                }

                Debug.Log($"Player moved to spawn point: {spawnPointName}");
                return;
            }
        }

        Debug.LogWarning($"Spawn point '{spawnPointName}' not found!");
    }

    GameObject EnsurePlayerExists()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            return player;
        }

        GameObject prefabToSpawn = playerPrefab;
        if (prefabToSpawn == null && !string.IsNullOrEmpty(playerPrefabResourcePath))
        {
            prefabToSpawn = Resources.Load<GameObject>(playerPrefabResourcePath);
        }

        if (prefabToSpawn != null)
        {
            player = Instantiate(prefabToSpawn);
            Scene baseScene = SceneManager.GetSceneByName(baseSceneName);
            if (!string.IsNullOrEmpty(baseSceneName) && baseScene.IsValid() && baseScene.isLoaded)
            {
                SceneManager.MoveGameObjectToScene(player, baseScene);
            }
            return player;
        }

        Debug.LogWarning("Player not found and no prefab assigned for fallback instantiation.");
        return null;
    }

    void DisablePlayerControl(bool disable)
    {
        // プレイヤーコントローラーの制御
        PlayerController.SetGlobalInputEnabled(!disable);

        // カメラコントローラーの制御
        var cameraController = FindObjectOfType<OrthographicCameraController>();
        if (cameraController != null)
        {
            cameraController.SetCameraControlEnabled(!disable);
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu")
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var cameraController = Camera.main?.GetComponent<OrthographicCameraController>();
                if (cameraController != null)
                {
                    cameraController.cameraTarget = null;
                }
                Destroy(player);
            }
        }
        else
        {
            // If we're not in the MainMenu and no GameClock exists, create one.
            if (FindObjectOfType<GameClock>() == null)
            {
                new GameObject("GameClock").AddComponent<GameClock>();
            }
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
