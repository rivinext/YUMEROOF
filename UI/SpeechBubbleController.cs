using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;

/// <summary>
/// 拡張版吹き出しコントローラー
/// 小さいアイコンとテキストパネルの切り替えに対応
/// </summary>
public class SpeechBubbleController : MonoBehaviour
{
    [Header("UI要素")]
    [SerializeField] private GameObject bubbleIcon;      // 小さいアイコン (BubbleIcon)
    [SerializeField] private GameObject hintPanel;       // テキストパネル (HintPanel)
    [SerializeField] private TextMeshProUGUI messageText; // テキスト表示用

    [Header("表示設定")]
    [SerializeField] private Vector3 offset = new Vector3(0, 2f, 0);
    [SerializeField] private float fadeSpeed = 5f;
    [SerializeField] private bool smoothTransition = true;

    [Header("デバッグ")]
    [SerializeField] private bool showDebugInfo = false;
    [SerializeField] private bool isShowingText = false;
    [SerializeField] private string currentMessage = "";

    private Transform followTarget;
    private BuildingGhostInteractable currentGhost;
    private CanvasGroup iconCanvasGroup;
    private CanvasGroup panelCanvasGroup;
    private Coroutine transitionCoroutine;

    void Awake()
    {
        // UI要素の検証
        if (bubbleIcon == null)
        {
            GameObject icon = transform.Find("BubbleIcon")?.gameObject;
            if (icon != null) bubbleIcon = icon;
            else Debug.LogError("[SpeechBubbleControllerExtended] BubbleIcon not found!");
        }

        if (hintPanel == null)
        {
            GameObject panel = transform.Find("HintPanel")?.gameObject;
            if (panel != null) hintPanel = panel;
            else Debug.LogError("[SpeechBubbleControllerExtended] HintPanel not found!");
        }

        if (messageText == null && hintPanel != null)
        {
            messageText = hintPanel.GetComponentInChildren<TextMeshProUGUI>();
        }

        // CanvasGroupを取得または追加（フェード用）
        if (bubbleIcon != null)
        {
            iconCanvasGroup = bubbleIcon.GetComponent<CanvasGroup>();
            if (iconCanvasGroup == null)
                iconCanvasGroup = bubbleIcon.AddComponent<CanvasGroup>();
        }

        if (hintPanel != null)
        {
            panelCanvasGroup = hintPanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
                panelCanvasGroup = hintPanel.AddComponent<CanvasGroup>();
        }

        // Ensure this object is under a Canvas or has its own Canvas component
        if (GetComponentInParent<Canvas>() == null && GetComponent<Canvas>() == null)
        {
            gameObject.AddComponent<Canvas>();
        }

    }

    void Start()
    {
        // No longer detach from parent or move to active scene
        //if (transform.parent != null) transform.SetParent(null);
        //SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());

        // 初期状態ではアイコンのみを表示
        ShowIconOnly();
    }

    /// <summary>
    /// ビルおばけ用の表示処理
    /// PlayerRayInteractorのSetHighlightから呼ばれる
    /// </summary>
    public void Show(Transform target, string fallbackMessage = null)
    {
        followTarget = target;

        // 既存のゴーストからイベント購読を解除
        if (currentGhost != null)
        {
            currentGhost.HintTextLoaded -= ShowWithText;
        }

        // BuildingGhostInteractableコンポーネントを探す
        currentGhost = target.GetComponentInParent<BuildingGhostInteractable>();

        if (currentGhost != null)
        {
            currentGhost.HintTextLoaded += ShowWithText;
        }

        if (currentGhost != null && currentGhost.IsHintReady())
        {
            // ヒントテキストを取得して表示
            string hintText = currentGhost.GetCachedHintText();
            ShowWithText(hintText);
        }
        else if (!string.IsNullOrEmpty(fallbackMessage))
        {
            // フォールバックメッセージを使用
            ShowWithText(fallbackMessage);
        }
        else
        {
            // テキストなしでアイコンのみ
            ShowIconOnly();
        }

        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;

        gameObject.SetActive(true);
    }

    void OnLocaleChanged(Locale _)
    {
        currentGhost?.ReloadHint();
    }

    /// <summary>
    /// テキスト付きで表示
    /// </summary>
    void ShowWithText(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        currentMessage = text;
        isShowingText = true;

        if (messageText != null)
        {
            messageText.text = text;
        }

        // トランジション実行
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        if (smoothTransition)
            transitionCoroutine = StartCoroutine(TransitionToText());
        else
        {
            // 即座に切り替え
            if (bubbleIcon != null) bubbleIcon.SetActive(false);
            if (hintPanel != null) hintPanel.SetActive(true);
        }

        if (showDebugInfo)
        {
            Debug.Log($"[SpeechBubbleControllerExtended] Showing text: {text}");
        }
    }

    /// <summary>
    /// アイコンのみ表示
    /// </summary>
    void ShowIconOnly()
    {
        isShowingText = false;
        currentMessage = "";

        // トランジション実行
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        if (smoothTransition)
            transitionCoroutine = StartCoroutine(TransitionToIcon());
        else
        {
            // 即座に切り替え
            if (bubbleIcon != null) bubbleIcon.SetActive(true);
            if (hintPanel != null) hintPanel.SetActive(false);
        }

        if (showDebugInfo)
        {
            Debug.Log("[SpeechBubbleControllerExtended] Showing icon only");
        }
    }

    /// <summary>
    /// 非表示処理
    /// PlayerRayInteractorから呼ばれる
    /// </summary>
    public void Hide()
    {
        // レイが外れたらアイコンのみ表示に戻す
        ShowIconOnly();

        if (currentGhost != null)
        {
            currentGhost.HintTextLoaded -= ShowWithText;
            currentGhost = null;
        }

        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;

        // followTargetは維持（アイコンは表示し続ける）
        // 完全に非表示にする場合は以下のコメントを解除
        // followTarget = null;
        // gameObject.SetActive(false);
    }

    /// <summary>
    /// アイコンからテキストへのトランジション
    /// </summary>
    IEnumerator TransitionToText()
    {
        // アイコンをフェードアウト
        if (iconCanvasGroup != null && bubbleIcon != null)
        {
            bubbleIcon.SetActive(true);
            float alpha = 1f;
            while (alpha > 0)
            {
                alpha -= Time.deltaTime * fadeSpeed;
                iconCanvasGroup.alpha = Mathf.Clamp01(alpha);
                yield return null;
            }
            bubbleIcon.SetActive(false);
        }

        // テキストパネルをフェードイン
        if (panelCanvasGroup != null && hintPanel != null)
        {
            hintPanel.SetActive(true);
            panelCanvasGroup.alpha = 0f;
            float alpha = 0f;
            while (alpha < 1f)
            {
                alpha += Time.deltaTime * fadeSpeed;
                panelCanvasGroup.alpha = Mathf.Clamp01(alpha);
                yield return null;
            }
        }
    }

    /// <summary>
    /// テキストからアイコンへのトランジション
    /// </summary>
    IEnumerator TransitionToIcon()
    {
        // テキストパネルをフェードアウト
        if (panelCanvasGroup != null && hintPanel != null)
        {
            float alpha = panelCanvasGroup.alpha;
            while (alpha > 0)
            {
                alpha -= Time.deltaTime * fadeSpeed;
                panelCanvasGroup.alpha = Mathf.Clamp01(alpha);
                yield return null;
            }
            hintPanel.SetActive(false);
        }

        // アイコンをフェードイン
        if (iconCanvasGroup != null && bubbleIcon != null)
        {
            bubbleIcon.SetActive(true);
            iconCanvasGroup.alpha = 0f;
            float alpha = 0f;
            while (alpha < 1f)
            {
                alpha += Time.deltaTime * fadeSpeed;
                iconCanvasGroup.alpha = Mathf.Clamp01(alpha);
                yield return null;
            }
        }
    }

    /// <summary>
    /// 位置追従処理
    /// </summary>
    void LateUpdate()
    {
        if (followTarget != null)
        {
            transform.position = followTarget.position + offset;
        }
    }

    /// <summary>
    /// 手動でリフレッシュ（デバッグ用）
    /// </summary>
    [ContextMenu("Refresh Display")]
    public void RefreshDisplay()
    {
        if (isShowingText && !string.IsNullOrEmpty(currentMessage))
        {
            ShowWithText(currentMessage);
        }
        else
        {
            ShowIconOnly();
        }
    }

    void OnDestroy()
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        if (currentGhost != null)
        {
            currentGhost.HintTextLoaded -= ShowWithText;
        }

        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }
}
