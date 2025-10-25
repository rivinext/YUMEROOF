using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using TMPro;

public class LanguageDropdownController : MonoBehaviour
{
    [SerializeField] TMP_Dropdown languageDropdown;

    [System.Serializable]
    struct LocaleFontSetting
    {
        public string localeCode;
        public TMP_FontAsset font;
    }

    [Header("フォント設定（ローカライズの影響を受けず固定表示する）")]
    [SerializeField] TMP_FontAsset defaultFont;
    [SerializeField] List<LocaleFontSetting> localeFonts = new List<LocaleFontSetting>();

    // 固定文字列の辞書。ScriptableObject やローカライズシステムから取得しないことに注意。
    // 新しい言語を追加する際はこの辞書の内容を直接編集してください。
    static readonly Dictionary<string, string> LocaleDisplayNames = new Dictionary<string, string>
    {
        {"", "None"},
        {"en", "English"},
        {"ja", "日本語"},
        {"zh-Hans", "简体字"},
        {"zh-CN", "简体字"},
        {"zh-Hant", "繁體字"},
        {"zh-TW", "繁體字"},
        {"fr", "Français"},
        {"de", "Deutsch"},
        {"it", "Italiano"},
        {"pt", "Português"},
        {"es", "Español"},
    };

    static readonly FieldInfo DropdownField = typeof(TMP_Dropdown).GetField("m_Dropdown", BindingFlags.Instance | BindingFlags.NonPublic);

    readonly Dictionary<string, TMP_FontAsset> localeFontLookup = new Dictionary<string, TMP_FontAsset>();
    readonly List<TMP_FontAsset> optionFonts = new List<TMP_FontAsset>();
    GameObject currentDropdownList;

    void Awake()
    {
        InitializeLocaleFontLookup();

        string savedCode = PlayerPrefs.GetString("language", "");
        if (!string.IsNullOrEmpty(savedCode))
        {
            var locales = LocalizationSettings.AvailableLocales.Locales;
            foreach (var locale in locales)
            {
                if (locale.Identifier.Code == savedCode)
                {
                    LocalizationSettings.SelectedLocale = locale;
                    break;
                }
            }
        }
    }

    void Start()
    {
        StartCoroutine(SetupDropdown());
    }

    IEnumerator SetupDropdown()
    {
        yield return LocalizationSettings.InitializationOperation;

        var locales = LocalizationSettings.AvailableLocales.Locales;

        languageDropdown.options.Clear();
        optionFonts.Clear();
        int selectedIndex = 0;
        for (int i = 0; i < locales.Count; i++)
        {
            var locale = locales[i];
            languageDropdown.options.Add(new TMP_Dropdown.OptionData(GetLocaleDisplayName(locale)));
            optionFonts.Add(GetFontForLocale(locale.Identifier.Code));
            if (LocalizationSettings.SelectedLocale == locale)
            {
                selectedIndex = i;
            }
        }

        languageDropdown.value = selectedIndex;
        languageDropdown.RefreshShownValue();
        languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        ApplyFontToCaption(selectedIndex);
    }

    public void OnLanguageChanged(int index)
    {
        var locales = LocalizationSettings.AvailableLocales.Locales;
        if (index >= 0 && index < locales.Count)
        {
            var selected = locales[index];
            LocalizationSettings.SelectedLocale = selected;
            PlayerPrefs.SetString("language", selected.Identifier.Code);
            PlayerPrefs.Save();
            ApplyFontToCaption(index);
        }
    }

    string GetLocaleDisplayName(Locale locale)
    {
        if (LocaleDisplayNames.TryGetValue(locale.Identifier.Code, out var displayName))
        {
            return displayName;
        }

        return locale.Identifier.CultureInfo?.NativeName ?? locale.LocaleName;
    }

    void InitializeLocaleFontLookup()
    {
        localeFontLookup.Clear();
        foreach (var setting in localeFonts)
        {
            if (string.IsNullOrEmpty(setting.localeCode) || setting.font == null)
            {
                continue;
            }

            localeFontLookup[setting.localeCode] = setting.font;
        }
    }

    TMP_FontAsset GetFontForLocale(string localeCode)
    {
        if (!string.IsNullOrEmpty(localeCode) && localeFontLookup.TryGetValue(localeCode, out var font))
        {
            return font;
        }

        return defaultFont;
    }

    void ApplyFontToCaption(int index)
    {
        if (languageDropdown.captionText == null || index < 0 || index >= optionFonts.Count)
        {
            return;
        }

        var font = optionFonts[index] ?? defaultFont;
        if (font != null)
        {
            languageDropdown.captionText.font = font;
        }
    }

    void LateUpdate()
    {
        UpdateDropdownListFonts();
    }

    void UpdateDropdownListFonts()
    {
        if (languageDropdown == null)
        {
            return;
        }

        var dropdownList = DropdownField?.GetValue(languageDropdown) as GameObject;
        if (dropdownList == null)
        {
            currentDropdownList = null;
            return;
        }

        if (dropdownList == currentDropdownList)
        {
            return;
        }

        ApplyFontToDropdownItems(dropdownList);
        currentDropdownList = dropdownList;
    }

    void ApplyFontToDropdownItems(GameObject dropdownList)
    {
        if (dropdownList == null)
        {
            return;
        }

        var items = dropdownList.GetComponentsInChildren<TMP_Dropdown.DropdownItem>(true);
        for (int i = 0; i < items.Length && i < optionFonts.Count; i++)
        {
            var text = items[i]?.text;
            if (text == null)
            {
                continue;
            }

            var font = optionFonts[i] ?? defaultFont;
            if (font != null)
            {
                text.font = font;
            }
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        EnsureLocaleFontEntries();
        InitializeLocaleFontLookup();
    }

    void EnsureLocaleFontEntries()
    {
        if (localeFonts == null)
        {
            localeFonts = new List<LocaleFontSetting>();
        }

        foreach (var kvp in LocaleDisplayNames)
        {
            var code = kvp.Key;
            if (string.IsNullOrEmpty(code))
            {
                continue;
            }

            bool exists = false;
            for (int i = 0; i < localeFonts.Count; i++)
            {
                if (localeFonts[i].localeCode == code)
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                localeFonts.Add(new LocaleFontSetting { localeCode = code, font = null });
            }
        }
    }
#endif
}
