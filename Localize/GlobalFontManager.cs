using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using TMPro;

/// <summary>
/// ゲーム全体のフォントを管理するマネージャー
/// シーン内の全てのTextMeshProのフォントを言語に応じて切り替える
/// </summary>
public class GlobalFontManager : MonoBehaviour
{
    private static GlobalFontManager instance;
    public static GlobalFontManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<GlobalFontManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("GlobalFontManager");
                    instance = go.AddComponent<GlobalFontManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    [System.Serializable]
    public class FontMapping
    {
        public string localeCode;
        public TMP_FontAsset font;
    }

    [Header("フォント設定")]
    [SerializeField] private List<FontMapping> fontMappings = new List<FontMapping>
    {
        new FontMapping { localeCode = "en", font = null },      // 英語 - Poppins
        new FontMapping { localeCode = "ja", font = null },      // 日本語 - NotoSansJP
        new FontMapping { localeCode = "zh-CN", font = null },   // 中国語（簡体字）
        new FontMapping { localeCode = "zh-TW", font = null },   // 中国語（繁体字）
        new FontMapping { localeCode = "ko", font = null },      // 韓国語
        new FontMapping { localeCode = "es", font = null },      // スペイン語
        new FontMapping { localeCode = "fr", font = null },      // フランス語
        new FontMapping { localeCode = "de", font = null },      // ドイツ語
        new FontMapping { localeCode = "it", font = null },      // イタリア語
        new FontMapping { localeCode = "pt", font = null },      // ポルトガル語
        new FontMapping { localeCode = "ru", font = null },      // ロシア語
        new FontMapping { localeCode = "ar", font = null },      // アラビア語
    };

    [SerializeField] private TMP_FontAsset defaultFont;

    [Header("オプション")]
    [SerializeField] private bool updateAllTextsOnLocaleChange = true;  // 言語変更時に全テキスト更新
    [SerializeField] private bool excludeDynamicLocalizer = false;  // DynamicLocalizerがあるテキストは除外
    [SerializeField] private bool debugMode = false;

    private Locale currentLocale;
    private Dictionary<string, TMP_FontAsset> fontDict = new Dictionary<string, TMP_FontAsset>();

    private void Awake()
    {
        // シングルトン処理
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        // 辞書を初期化
        InitializeFontDictionary();

        // 言語変更イベントに登録
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;

        // 初期化
        StartCoroutine(Initialize());
    }

    private void OnDestroy()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    /// <summary>
    /// フォント辞書を初期化
    /// </summary>
    private void InitializeFontDictionary()
    {
        fontDict.Clear();
        foreach (var mapping in fontMappings)
        {
            if (mapping.font != null && !string.IsNullOrEmpty(mapping.localeCode))
            {
                fontDict[mapping.localeCode] = mapping.font;
            }
        }
    }

    /// <summary>
    /// 初期化処理
    /// </summary>
    private System.Collections.IEnumerator Initialize()
    {
        yield return LocalizationSettings.InitializationOperation;

        currentLocale = LocalizationSettings.SelectedLocale;
        if (updateAllTextsOnLocaleChange)
        {
            UpdateAllTextFonts();
        }

        if (debugMode)
        {
            Debug.Log($"[GlobalFontManager] Initialized with locale: {currentLocale?.name}");
        }
    }

    /// <summary>
    /// 言語変更時の処理
    /// </summary>
    private void OnLocaleChanged(Locale newLocale)
    {
        if (debugMode)
        {
            Debug.Log($"[GlobalFontManager] Locale changed: {currentLocale?.name} → {newLocale?.name}");
        }

        currentLocale = newLocale;

        if (updateAllTextsOnLocaleChange)
        {
            UpdateAllTextFonts();
        }
    }

    /// <summary>
    /// 現在の言語に適したフォントを取得
    /// </summary>
    public TMP_FontAsset GetFontForCurrentLocale()
    {
        if (currentLocale == null)
        {
            return defaultFont;
        }

        string localeCode = currentLocale.Identifier.Code;
        if (fontDict.TryGetValue(localeCode, out TMP_FontAsset font))
        {
            return font;
        }

        return defaultFont;
    }

    /// <summary>
    /// 指定言語用のフォントを取得
    /// </summary>
    public TMP_FontAsset GetFontForLocale(string localeCode)
    {
        if (fontDict.TryGetValue(localeCode, out TMP_FontAsset font))
        {
            return font;
        }

        return defaultFont;
    }

    /// <summary>
    /// シーン内の全TextMeshProのフォントを更新
    /// </summary>
    public void UpdateAllTextFonts()
    {
        TMP_FontAsset targetFont = GetFontForCurrentLocale();
        if (targetFont == null)
        {
            if (debugMode)
            {
                Debug.LogWarning("[GlobalFontManager] No font available for current locale");
            }
            return;
        }

        // シーン内の全TextMeshProを取得
        TextMeshProUGUI[] allTexts = FindObjectsByType<TextMeshProUGUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        int updatedCount = 0;

        foreach (var text in allTexts)
        {
            // DynamicLocalizerがある場合はスキップ（オプション）
            if (excludeDynamicLocalizer)
            {
                if (text.GetComponent<DynamicLocalizer>() != null ||
                    text.GetComponentInParent<DynamicLocalizer>() != null)
                {
                    continue;
                }
            }

            // フォントを更新
            text.font = targetFont;
            updatedCount++;
        }

        if (debugMode)
        {
            Debug.Log($"[GlobalFontManager] Updated {updatedCount} text components with font: {targetFont.name}");
        }
    }

    /// <summary>
    /// 特定のTextMeshProのフォントを現在の言語に合わせて更新
    /// </summary>
    public void UpdateTextFont(TextMeshProUGUI textComponent)
    {
        if (textComponent == null) return;

        TMP_FontAsset targetFont = GetFontForCurrentLocale();
        if (targetFont != null)
        {
            textComponent.font = targetFont;
        }
    }

    /// <summary>
    /// 新しいシーンがロードされた時の処理
    /// </summary>
    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (updateAllTextsOnLocaleChange)
        {
            // 少し遅延させてから更新（シーンのオブジェクトが完全に初期化されるのを待つ）
            StartCoroutine(DelayedSceneUpdate());
        }
    }

    private System.Collections.IEnumerator DelayedSceneUpdate()
    {
        yield return new WaitForSeconds(0.1f);
        UpdateAllTextFonts();
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

#if UNITY_EDITOR
    [ContextMenu("Force Update All Fonts")]
    private void ForceUpdateAllFonts()
    {
        InitializeFontDictionary();
        UpdateAllTextFonts();
    }

    [ContextMenu("Log Font Mappings")]
    private void LogFontMappings()
    {
        Debug.Log("=== Font Mappings ===");
        foreach (var mapping in fontMappings)
        {
            string fontName = mapping.font != null ? mapping.font.name : "NOT SET";
            Debug.Log($"{mapping.localeCode}: {fontName}");
        }

        string defaultFontName = defaultFont != null ? defaultFont.name : "NOT SET";
        Debug.Log($"Default: {defaultFontName}");
    }
#endif
}
