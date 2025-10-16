using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;

/// <summary>
/// ビルおばけのインタラクション処理
/// PlayerRayInteractorからのレイを受けてヒントを表示
/// </summary>
public class BuildingGhostInteractable : MonoBehaviour, IInteractable
{
    [Header("ヒント設定")]
    [SerializeField] private TriggerType hintTriggerType = TriggerType.StatusCheck;
    [SerializeField] private string defaultTextID = "hint_default_greeting";

    [Header("デバッグ")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private string currentHintID = "";
    [SerializeField] private string currentTextID = "";
    [SerializeField] private string localizedText = "";

    // キャッシュされたヒント情報
    private HintSystem.HintData cachedHint;
    private string cachedLocalizedText;
    private bool hintInitialized = false;

    // ローカライゼーション
    private string localizationTableName = "Hints";

    public event Action<string> HintTextLoaded;

    void Start()
    {
        // if (transform.parent != null) transform.SetParent(null);
        // SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());

        // HintSystemの存在確認
        if (HintSystem.Instance == null)
        {
            Debug.LogError($"[BuildingGhostInteractable] HintSystem.Instance is null!");
        }

        // 起動時にヒントを取得
        InitializeHint();
    }

    /// <summary>
    /// ヒントを初期化（シーン開始時に1回だけ実行）
    /// </summary>
    void InitializeHint()
    {
        if (hintInitialized) return;

        if (HintSystem.Instance == null)
        {
            Debug.LogWarning("[BuildingGhostInteractable] HintSystem not available yet");
            return;
        }

        // 条件に基づいてヒントを取得
        cachedHint = HintSystem.Instance.RequestHint(hintTriggerType);

        if (cachedHint != null)
        {
            currentHintID = cachedHint.id;
            currentTextID = cachedHint.textID;

            // ローカライズテキストを取得
            StartCoroutine(LoadLocalizedText(cachedHint.textID));
        }
        else
        {
            // デフォルトヒントを使用
            currentTextID = defaultTextID;
            StartCoroutine(LoadLocalizedText(defaultTextID));

            if (debugMode)
            {
                Debug.Log($"[BuildingGhostInteractable] No hint found, using default: {defaultTextID}");
            }
        }

        hintInitialized = true;
    }

    /// <summary>
    /// ローカライズされたテキストを読み込み
    /// </summary>
    IEnumerator LoadLocalizedText(string textID)
    {
        // LocalizationSettingsの初期化を待つ
        yield return LocalizationSettings.InitializationOperation;

        // ローカライズされたテキストを取得
        var localizedString = new LocalizedString(localizationTableName, textID);
        var loadHandle = localizedString.GetLocalizedStringAsync();

        yield return loadHandle;

        if (loadHandle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
        {
            cachedLocalizedText = loadHandle.Result;
            localizedText = cachedLocalizedText;
            HintTextLoaded?.Invoke(cachedLocalizedText);

            if (debugMode)
            {
                Debug.Log($"[BuildingGhostInteractable] Loaded text: {cachedLocalizedText}");
            }
        }
        else
        {
            // フォールバック：TextIDをそのまま使用
            cachedLocalizedText = textID;
            localizedText = textID;

            Debug.LogWarning($"[BuildingGhostInteractable] Failed to load localized text for: {textID}");
        }
    }

    /// <summary>
    /// PlayerRayInteractorから呼ばれるインタラクション
    /// Eキー押下時の処理（今回は使用しない）
    /// </summary>
    public void Interact()
    {
        // Eキーでのインタラクションは不要
        if (debugMode)
        {
            Debug.Log("[BuildingGhostInteractable] Interact called (no action)");
        }
    }

    /// <summary>
    /// キャッシュされたローカライズテキストを取得
    /// SpeechBubbleControllerExtendedから呼ばれる
    /// </summary>
    public string GetCachedHintText()
    {
        // まだ初期化されていない場合は初期化を試みる
        if (!hintInitialized)
        {
            InitializeHint();
        }

        // キャッシュされたテキストを返す
        return string.IsNullOrEmpty(cachedLocalizedText) ? currentTextID : cachedLocalizedText;
    }

    /// <summary>
    /// 現在のTextIDを取得（デバッグ用）
    /// </summary>
    public string GetCurrentTextID()
    {
        return currentTextID;
    }

    /// <summary>
    /// ヒントが初期化済みかどうか
    /// </summary>
    public bool IsHintReady()
    {
        return hintInitialized && !string.IsNullOrEmpty(cachedLocalizedText);
    }

    public void ReloadHint()
    {
        if (!string.IsNullOrEmpty(currentTextID))
        {
            StartCoroutine(LoadLocalizedText(currentTextID));
        }
    }

    /// <summary>
    /// 手動でヒントをリフレッシュ（デバッグ用）
    /// シーン中は通常使用しない
    /// </summary>
    [ContextMenu("Force Refresh Hint")]
    public void ForceRefreshHint()
    {
        hintInitialized = false;
        cachedHint = null;
        cachedLocalizedText = "";
        InitializeHint();
    }

    void OnDrawGizmosSelected()
    {
        // インタラクション可能範囲を表示
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
    }
}
