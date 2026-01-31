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
                instance = FindFirstObjectByType<SceneTransitionManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("SceneTransitionManager");
                    instance = go.AddComponent<SceneTransitionManager>();
                }
            }
            return instance;
        }
    }

    [Header("Fade Settings")]
    public float fadeDuration = 1f;
    public Color fadeColor = Color.black;

    private Canvas fadeCanvas;
    private Image fadeImage;
    private bool isTransitioning = false;
    private string lastSceneName = "";
    private string lastSuccessfulSpawnPointName = string.Empty;

    // 遷移情報を保持
    public string LastSceneName => lastSceneName;
    public string LastSpawnPointName => lastSuccessfulSpawnPointName;
    public bool IsTransitioning => isTransitioning;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            CreateFadeCanvas();
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

    // シーン遷移（ドア用）
    public void TransitionToScene(
        string sceneName,
        string spawnPointName)
    {
        if (!isTransitioning)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            StartCoroutine(TransitionCoroutine(sceneName, spawnPointName));
        }
    }

    // シーン遷移（エリア用）
    public void TransitionToSceneInstant(
        string sceneName,
        string spawnPointName)
    {
        if (!isTransitioning)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            StartCoroutine(TransitionCoroutine(sceneName, spawnPointName));
        }
    }

    IEnumerator TransitionCoroutine(
        string sceneName,
        string spawnPointName)
    {
        isTransitioning = true;

        // 現在のシーン名を保存
        lastSceneName = SceneManager.GetActiveScene().name;

        // プレイヤー入力を無効化
        DisablePlayerControl(true);

        // フェードアウト
        yield return StartCoroutine(Fade(1f));

        // シーン読み込み
        var slotKey = SaveGameManager.Instance?.CurrentSlotKey;
        GameSessionInitializer.CreateIfNeeded(slotKey);
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // スポーン位置に移動
        MovePlayerToSpawnPoint(spawnPointName);

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

    public bool MovePlayerToSpawnPoint(string spawnPointName)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("Player not found!");
            return false;
        }

        PlayerSpawnPoint[] spawnPoints = FindObjectsByType<PlayerSpawnPoint>(FindObjectsSortMode.None);
        PlayerSpawnPoint target = FindSpawnPoint(spawnPoints, spawnPointName);
        if (target == null)
        {
            Debug.LogWarning($"Spawn point '{spawnPointName}' not found!");
            return false;
        }

        return ApplySpawnPoint(player, target, false, true, true);
    }

    public bool ForceReturnPlayerToSpawn(string overrideSpawnPointName = null, float idleCrossFade = 0.1f)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("Player not found!");
            return false;
        }

        ClearPlayerInteractionState(player);

        PlayerSpawnPoint[] spawnPoints = FindObjectsByType<PlayerSpawnPoint>(FindObjectsSortMode.None);
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("No spawn points available in the current scene.");
            return false;
        }

        PlayerSpawnPoint target = null;
        if (!string.IsNullOrEmpty(overrideSpawnPointName))
        {
            target = FindSpawnPoint(spawnPoints, overrideSpawnPointName);
        }

        if (target == null && !string.IsNullOrEmpty(lastSuccessfulSpawnPointName))
        {
            target = FindSpawnPoint(spawnPoints, lastSuccessfulSpawnPointName);
        }

        if (target == null)
        {
            target = spawnPoints[0];
        }

        if (!ApplySpawnPoint(player, target, true, true, true))
        {
            Debug.LogWarning("Failed to move player to an emergency return spawn point.");
            return false;
        }

        var animator = player.GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetFloat("moveSpeed", 0f);
        }

        var emoteController = player.GetComponent<PlayerEmoteController>();
        if (emoteController != null)
        {
            emoteController.ForceReturnToIdle(Mathf.Max(0f, idleCrossFade));
        }

        return true;
    }

    private void ClearPlayerInteractionState(GameObject player)
    {
        if (player == null)
        {
            return;
        }

        var sitStateController = player.GetComponent<PlayerSitStateController>();
        if (sitStateController != null)
        {
            sitStateController.ForceStandUpImmediate();
        }

        var bedTriggers = FindObjectsByType<BedTrigger>(FindObjectsSortMode.None);
        if (bedTriggers != null)
        {
            foreach (var bedTrigger in bedTriggers)
            {
                bedTrigger?.ForceExitBedImmediate();
            }
        }
    }

    private PlayerSpawnPoint FindSpawnPoint(PlayerSpawnPoint[] spawnPoints, string spawnPointName)
    {
        if (spawnPoints == null || string.IsNullOrEmpty(spawnPointName))
        {
            return null;
        }

        foreach (var point in spawnPoints)
        {
            if (point != null && point.spawnPointName == spawnPointName)
            {
                return point;
            }
        }

        return null;
    }

    private bool ApplySpawnPoint(GameObject player, PlayerSpawnPoint spawnPoint, bool resetVelocity, bool resetCamera, bool updateLastSpawn)
    {
        if (player == null || spawnPoint == null)
        {
            return false;
        }

        player.transform.SetPositionAndRotation(spawnPoint.transform.position, spawnPoint.transform.rotation);

        if (resetVelocity && player.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (resetCamera)
        {
            var cameraController = FindFirstObjectByType<OrthographicCameraController>();
            if (cameraController != null)
            {
                cameraController.ResetCamera();
            }
        }

        if (updateLastSpawn)
        {
            lastSuccessfulSpawnPointName = spawnPoint.spawnPointName;
        }

        Debug.Log($"Player moved to spawn point: {spawnPoint.spawnPointName}");
        return true;
    }

    void DisablePlayerControl(bool disable)
    {
        // プレイヤーコントローラーの制御
        PlayerController.SetGlobalInputEnabled(!disable);

        // カメラコントローラーの制御
        var cameraController = FindFirstObjectByType<OrthographicCameraController>();
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
            if (FindFirstObjectByType<GameClock>() == null)
            {
                new GameObject("GameClock").AddComponent<GameClock>();
            }
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

}
