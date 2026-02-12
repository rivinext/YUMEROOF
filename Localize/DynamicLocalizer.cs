using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Components;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 汎用的な動的ローカライズコンポーネント
/// 複数のテキストを異なるテーブルからローカライズ可能
/// フォントの自動切り替え機能付き
/// </summary>
public class DynamicLocalizer : MonoBehaviour
{
    [System.Serializable]
    public class LocalizedTextField
    {
        [Header("設定")]
        public string fieldName = "Name";  // 識別用の名前（例：Name, Description）
        public TextMeshProUGUI textComponent;
        public Text legacyTextComponent;
        public InputField legacyInputField;
        public string localizationTableName = "ItemNames";  // 使用するテーブル名

        [Header("デバッグ")]
        [SerializeField] private string currentKey;  // 現在設定されているキー（デバッグ用）

        private LocalizeStringEvent localizeEvent;
        private bool debugMode;
        private bool isUpdateStringListenerRegistered;

        /// <summary>
        /// 初期化処理
        /// </summary>
        public void Initialize(bool debugModeEnabled)
        {
            if (legacyInputField != null && legacyTextComponent == null)
            {
                legacyTextComponent = legacyInputField.textComponent;
            }

            if (textComponent == null && legacyTextComponent == null) return;

            debugMode = debugModeEnabled;

            GameObject targetObject = textComponent != null
                ? textComponent.gameObject
                : legacyTextComponent.gameObject;

            // LocalizeStringEventコンポーネントを取得または追加
            localizeEvent = targetObject.GetComponent<LocalizeStringEvent>();
            if (localizeEvent == null)
            {
                localizeEvent = targetObject.AddComponent<LocalizeStringEvent>();
            }

            if (localizeEvent != null && localizeEvent.OnUpdateString != null && !isUpdateStringListenerRegistered)
            {
                localizeEvent.OnUpdateString.RemoveListener(ApplyLocalizedString);
                localizeEvent.OnUpdateString.AddListener(ApplyLocalizedString);
                isUpdateStringListenerRegistered = true;
            }
        }

        private void ApplyLocalizedString(string value)
        {
            if (textComponent != null)
            {
                textComponent.SetText(value);
            }

            if (legacyTextComponent != null)
            {
                legacyTextComponent.text = value;
            }
        }

        /// <summary>
        /// ローカライズキーを設定して更新（改良版）
        /// </summary>
        public IEnumerator SetLocalizedKeyAsync(string key)
        {
            if ((textComponent == null && legacyTextComponent == null) || string.IsNullOrEmpty(key))
            {
                if (textComponent != null)
                    textComponent.text = "";
                if (legacyTextComponent != null)
                    legacyTextComponent.text = "";
                yield break;
            }

            currentKey = key;  // デバッグ用に保存

            // LocalizationSettingsの初期化を待つ
            yield return LocalizationSettings.InitializationOperation;

            if (localizeEvent != null)
            {
                // 既存のStringReferenceをクリア
                localizeEvent.StringReference.Clear();

                // 新しいLocalizedStringを作成
                LocalizedString localizedString = new LocalizedString
                {
                    TableReference = localizationTableName,
                    TableEntryReference = key
                };

                // LocalizeStringEventに設定
                localizeEvent.StringReference = localizedString;

                // OnEnableイベントを強制的に呼び出して更新
                localizeEvent.enabled = false;
                localizeEvent.enabled = true;
            }
            else
            {
                // フォールバック：キーをそのまま表示
                if (debugMode)
                {
                    Debug.LogWarning($"LocalizeStringEvent not found for {fieldName}. Showing key: {key}");
                }
                ApplyLocalizedString(key);
            }
        }

        /// <summary>
        /// 同期版のキー設定（LocalizationSettings初期化済みの場合）
        /// </summary>
        public void SetLocalizedKeyImmediate(string key)
        {
            if ((textComponent == null && legacyTextComponent == null) || string.IsNullOrEmpty(key))
            {
                if (textComponent != null)
                    textComponent.text = "";
                if (legacyTextComponent != null)
                    legacyTextComponent.text = "";
                return;
            }

            currentKey = key;

            if (localizeEvent != null)
            {
                // 既存のStringReferenceをクリア
                localizeEvent.StringReference.Clear();

                // 新しいLocalizedStringを作成して設定
                localizeEvent.StringReference = new LocalizedString(localizationTableName, key);

                // 強制的に更新
                localizeEvent.enabled = false;
                localizeEvent.enabled = true;
            }
            else
            {
                if (debugMode)
                {
                    Debug.LogWarning($"LocalizeStringEvent not found for {fieldName}");
                }
                ApplyLocalizedString(key);
            }
        }

        /// <summary>
        /// フォントを設定
        /// </summary>
        public void SetFont(TMP_FontAsset font)
        {
            if (textComponent != null && font != null)
            {
                textComponent.font = font;
            }
        }

        public void SetLegacyFont(Font font)
        {
            if (font == null) return;

            if (legacyInputField != null)
            {
                if (legacyInputField.textComponent != null)
                {
                    legacyInputField.textComponent.font = font;
                }
            }

            if (legacyTextComponent != null)
            {
                legacyTextComponent.font = font;
            }
        }

        /// <summary>
        /// フィールドをクリア
        /// </summary>
        public void Clear()
        {
            if (textComponent != null)
                textComponent.text = "";
            if (legacyTextComponent != null)
                legacyTextComponent.text = "";
            currentKey = "";

            if (localizeEvent != null)
            {
                localizeEvent.StringReference.Clear();
            }
        }

        /// <summary>
        /// 現在のキーを取得
        /// </summary>
        public string GetCurrentKey()
        {
            return currentKey;
        }
    }

    [System.Serializable]
    public class FontSettings
    {
        public string localeCode = "en";  // 言語コード（en, ja, zh-CN, ko など）
        public TMP_FontAsset font;        // TextMeshPro用フォント
        public Font legacyFont;           // Legacy UI.Text / InputField用フォント
    }

    [Header("ローカライズ設定")]
    [SerializeField] private LocalizedTextField[] localizedFields = new LocalizedTextField[0];

    [Header("フォント設定")]
    [SerializeField] private bool enableAutoFontSwitch = true;  // フォント自動切り替えを有効化
    [SerializeField] private FontSettings[] fontSettings = new FontSettings[]
    {
        new FontSettings { localeCode = "en", font = null },      // 英語
        new FontSettings { localeCode = "ja", font = null },      // 日本語
        new FontSettings { localeCode = "zh-CN", font = null },   // 中国語（簡体字）
        new FontSettings { localeCode = "zh-TW", font = null },   // 中国語（繁体字）
        new FontSettings { localeCode = "ko", font = null },      // 韓国語
        new FontSettings { localeCode = "es", font = null },      // スペイン語
        new FontSettings { localeCode = "fr", font = null },      // フランス語
        new FontSettings { localeCode = "de", font = null },      // ドイツ語
        new FontSettings { localeCode = "it", font = null },      // イタリア語
        new FontSettings { localeCode = "pt", font = null },      // ポルトガル語
        new FontSettings { localeCode = "ru", font = null },      // ロシア語
    };
    [SerializeField] private TMP_FontAsset defaultFont;  // TMPのデフォルトフォント（該当なしの場合）
    [SerializeField] private Font defaultLegacyFont;   // Legacy UI.Textのデフォルトフォント（該当なしの場合）

    [Header("オプション")]
    [SerializeField] private bool waitForLocalizationInit = true;  // 初期化を待つかどうか
    [SerializeField] private bool debugMode = false;
    private const bool EnableDebugLogs = false;

    private bool isLocalizationReady = false;
    private Locale currentLocale;
    private bool IsDebugEnabled => debugMode && EnableDebugLogs;

    private void Awake()
    {
        ValidateUniqueFieldNames();
        InitializeAllFields();
        StartCoroutine(WaitForLocalizationReady());

        // 言語変更イベントに登録
        if (enableAutoFontSwitch)
        {
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        }
    }

    private void OnDestroy()
    {
        // イベントの登録解除
        if (enableAutoFontSwitch)
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        }
    }

    /// <summary>
    /// Localization初期化を待つ
    /// </summary>
    private IEnumerator WaitForLocalizationReady()
    {
        yield return LocalizationSettings.InitializationOperation;
        isLocalizationReady = true;

        // 初期化完了後、現在の言語を取得してフォントを設定
        currentLocale = LocalizationSettings.SelectedLocale;
        UpdateFontsForLocale(currentLocale);

        if (IsDebugEnabled)
        {
            Debug.Log($"[DynamicLocalizer] Localization ready. Current locale: {currentLocale?.name}");
        }
    }

    /// <summary>
    /// 言語変更時のコールバック
    /// </summary>
    private void OnLocaleChanged(Locale newLocale)
    {
        if (IsDebugEnabled)
        {
            Debug.Log($"[DynamicLocalizer] Locale changed from {currentLocale?.name} to {newLocale?.name}");
        }

        currentLocale = newLocale;
        UpdateFontsForLocale(newLocale);

        // テキストも更新（キーが設定されている場合）
        RefreshAll();
    }

    /// <summary>
    /// 指定された言語に応じてフォントを更新
    /// </summary>
    private void UpdateFontsForLocale(Locale locale)
    {
        if (!enableAutoFontSwitch || locale == null) return;

        // 言語コードに対応するフォントを探す
        TMP_FontAsset targetFont = null;
        Font targetLegacyFont = null;
        string localeCode = locale.Identifier.Code;

        foreach (var fontSetting in fontSettings)
        {
            if (fontSetting.localeCode == localeCode)
            {
                targetFont = fontSetting.font;
                targetLegacyFont = fontSetting.legacyFont;
                break;
            }
        }

        // 見つからない場合はデフォルトフォントを使用
        if (targetFont == null)
        {
            targetFont = defaultFont;
        }

        if (targetLegacyFont == null)
        {
            targetLegacyFont = defaultLegacyFont;
        }

        if (IsDebugEnabled && (targetFont == defaultFont || targetLegacyFont == defaultLegacyFont))
        {
            Debug.Log($"[DynamicLocalizer] Using default font for locale: {localeCode}");
        }

        // 全フィールドのフォントを更新
        foreach (var field in localizedFields)
        {
            field.SetFont(targetFont);
            field.SetLegacyFont(targetLegacyFont);
        }

        if (IsDebugEnabled)
        {
            string tmpFontName = targetFont != null ? targetFont.name : "None";
            string legacyFontName = targetLegacyFont != null ? targetLegacyFont.name : "None";
            Debug.Log($"[DynamicLocalizer] Updated fonts TMP={tmpFontName}, Legacy={legacyFontName}");
        }
    }

    /// <summary>
    /// 全フィールドを初期化
    /// </summary>
    private void InitializeAllFields()
    {
        foreach (var field in localizedFields)
        {
            field.Initialize(IsDebugEnabled);
        }

        if (IsDebugEnabled)
        {
            Debug.Log($"[DynamicLocalizer] Initialized {localizedFields.Length} fields");
        }
    }

    /// <summary>
    /// 名前でフィールドを検索してキーを設定（改良版）
    /// </summary>
    public void SetFieldByName(string fieldName, string key)
    {
        var field = GetFieldByName(fieldName);
        if (field != null)
        {
            if (waitForLocalizationInit && !isLocalizationReady)
            {
                // 非同期で設定
                StartCoroutine(SetFieldByNameAsync(fieldName, key));
            }
            else
            {
                // 即座に設定
                field.SetLocalizedKeyImmediate(key);
            }

            if (IsDebugEnabled)
                Debug.Log($"[DynamicLocalizer] Set {fieldName} to {key}");
        }
        else if (IsDebugEnabled)
        {
            Debug.LogWarning($"[DynamicLocalizer] Field '{fieldName}' not found");
        }
    }

    /// <summary>
    /// 非同期でフィールドを設定
    /// </summary>
    private IEnumerator SetFieldByNameAsync(string fieldName, string key)
    {
        yield return new WaitUntil(() => isLocalizationReady);

        var field = GetFieldByName(fieldName);
        if (field != null)
        {
            field.SetLocalizedKeyImmediate(key);
        }
    }

    /// <summary>
    /// インデックスでフィールドにキーを設定
    /// </summary>
    public void SetFieldByIndex(int index, string key)
    {
        if (index >= 0 && index < localizedFields.Length)
        {
            if (waitForLocalizationInit && !isLocalizationReady)
            {
                StartCoroutine(SetFieldByIndexAsync(index, key));
            }
            else
            {
                localizedFields[index].SetLocalizedKeyImmediate(key);
            }

            if (IsDebugEnabled)
                Debug.Log($"[DynamicLocalizer] Set field[{index}] to {key}");
        }
        else if (IsDebugEnabled)
        {
            Debug.LogWarning($"[DynamicLocalizer] Index {index} out of range");
        }
    }

    /// <summary>
    /// 非同期でインデックス指定のフィールドを設定
    /// </summary>
    private IEnumerator SetFieldByIndexAsync(int index, string key)
    {
        yield return new WaitUntil(() => isLocalizationReady);

        if (index >= 0 && index < localizedFields.Length)
        {
            localizedFields[index].SetLocalizedKeyImmediate(key);
        }
    }

    /// <summary>
    /// 複数のキーを一度に設定（辞書形式）
    /// </summary>
    public void SetMultipleFields(Dictionary<string, string> fieldKeyPairs)
    {
        foreach (var pair in fieldKeyPairs)
        {
            SetFieldByName(pair.Key, pair.Value);
        }
    }

    /// <summary>
    /// ローカライズフィールドの重複名を検知して警告を出す
    /// </summary>
    private void ValidateUniqueFieldNames()
    {
        var seen = new HashSet<string>();
        var duplicates = new HashSet<string>();

        foreach (var field in localizedFields)
        {
            if (field == null || string.IsNullOrEmpty(field.fieldName))
            {
                continue;
            }

            if (!seen.Add(field.fieldName))
            {
                duplicates.Add(field.fieldName);
            }
        }

        if (IsDebugEnabled && duplicates.Count > 0)
        {
            Debug.LogWarning($"[DynamicLocalizer] Duplicate field names detected: {string.Join(", ", duplicates)}. Please ensure each LocalizedTextField has a unique Field Name.");
        }
    }

    /// <summary>
    /// 名前でフィールドをクリア
    /// </summary>
    public void ClearField(string fieldName)
    {
        var field = GetFieldByName(fieldName);
        if (field != null)
        {
            field.Clear();

            if (IsDebugEnabled)
            {
                Debug.Log($"[DynamicLocalizer] Cleared field '{fieldName}'");
            }
        }
        else if (IsDebugEnabled)
        {
            Debug.LogWarning($"[DynamicLocalizer] Field '{fieldName}' not found when attempting to clear");
        }
    }

    /// <summary>
    /// ItemSOから情報を設定
    /// </summary>
    public void SetFromItemSO(ItemSO itemSO)
    {
        if (itemSO == null)
        {
            ClearAll();
            return;
        }

        // よく使うフィールド名で自動設定
        SetFieldByName("Name", itemSO.NameID);
        SetFieldByName("Description", itemSO.DescriptionID);
    }

    /// <summary>
    /// MaterialSOから情報を設定
    /// </summary>
    public void SetFromMaterialSO(MaterialSO materialSO)
    {
        if (materialSO == null)
        {
            ClearAll();
            return;
        }

        // MaterialSO用のフィールド設定
        SetFieldByName("Name", materialSO.MaterialNameID);
        SetFieldByName("Description", materialSO.DescriptionID);
    }

    /// <summary>
    /// 全フィールドをクリア
    /// </summary>
    public void ClearAll()
    {
        foreach (var field in localizedFields)
        {
            field.Clear();
        }
    }

    /// <summary>
    /// 全フィールドを強制更新
    /// </summary>
    public void RefreshAll()
    {
        foreach (var field in localizedFields)
        {
            string key = field.GetCurrentKey();
            if (!string.IsNullOrEmpty(key))
            {
                field.SetLocalizedKeyImmediate(key);
            }
        }
    }

    /// <summary>
    /// 名前でフィールドを取得
    /// </summary>
    private LocalizedTextField GetFieldByName(string fieldName)
    {
        foreach (var field in localizedFields)
        {
            if (field.fieldName == fieldName)
                return field;
        }
        return null;
    }

    /// <summary>
    /// テーブル名を動的に変更
    /// </summary>
    public void SetTableForField(string fieldName, string tableName)
    {
        var field = GetFieldByName(fieldName);
        if (field != null)
        {
            field.localizationTableName = tableName;
            field.Initialize(IsDebugEnabled);  // 再初期化
        }
    }

    /// <summary>
    /// 手動でフォントを設定（特定の言語コード用）
    /// </summary>
    public void SetFontForLocale(string localeCode, TMP_FontAsset font)
    {
        foreach (var fontSetting in fontSettings)
        {
            if (fontSetting.localeCode == localeCode)
            {
                fontSetting.font = font;

                // 現在の言語と一致する場合は即座に適用
                if (currentLocale != null && currentLocale.Identifier.Code == localeCode)
                {
                    UpdateFontsForLocale(currentLocale);
                }
                break;
            }
        }
    }

    /// <summary>
    /// OnEnable時に再更新（プール使用時対策）
    /// </summary>
    private void OnEnable()
    {
        if (isLocalizationReady)
        {
            // 少し遅延させて更新
            StartCoroutine(DelayedRefresh());
        }
    }

    private IEnumerator DelayedRefresh()
    {
        yield return null;  // 1フレーム待つ

        // フォントを現在の言語に合わせて更新
        if (currentLocale != null)
        {
            UpdateFontsForLocale(currentLocale);
        }

        // テキストを更新
        RefreshAll();
    }

#if UNITY_EDITOR
    /// <summary>
    /// エディタ用：フィールドを追加
    /// </summary>
    [ContextMenu("Add Field")]
    private void AddField()
    {
        var newList = new List<LocalizedTextField>(localizedFields);
        newList.Add(new LocalizedTextField());
        localizedFields = newList.ToArray();
    }

    /// <summary>
    /// エディタ用：設定を検証
    /// </summary>
    [ContextMenu("Validate Setup")]
    private void ValidateSetup()
    {
        int validCount = 0;
        foreach (var field in localizedFields)
        {
            if (field.textComponent != null)
            {
                validCount++;
                Debug.Log($"✓ Field '{field.fieldName}' - Table: {field.localizationTableName}");
            }
            else
            {
                Debug.LogWarning($"✗ Field '{field.fieldName}' - TextComponent is missing!");
            }
        }
        Debug.Log($"Validation complete: {validCount}/{localizedFields.Length} fields valid");

        // フォント設定も検証
        Debug.Log("--- Font Settings ---");
        foreach (var fontSetting in fontSettings)
        {
            if (fontSetting.font != null)
            {
                Debug.Log($"✓ {fontSetting.localeCode}: TMP={fontSetting.font.name}, Legacy={(fontSetting.legacyFont != null ? fontSetting.legacyFont.name : "None")}");
            }
            else
            {
                Debug.LogWarning($"✗ {fontSetting.localeCode}: No TMP font assigned");
            }
        }

        if (defaultFont != null)
        {
            Debug.Log($"Default TMP font: {defaultFont.name}");
        }
        else
        {
            Debug.LogWarning("No default TMP font assigned");
        }


        if (defaultLegacyFont != null)
        {
            Debug.Log($"Default Legacy font: {defaultLegacyFont.name}");
        }
        else
        {
            Debug.LogWarning("No default Legacy font assigned");
        }
    }

    /// <summary>
    /// エディタ用：強制更新
    /// </summary>
    [ContextMenu("Force Refresh All")]
    private void ForceRefresh()
    {
        if (currentLocale != null)
        {
            UpdateFontsForLocale(currentLocale);
        }
        RefreshAll();
    }
#endif
}
