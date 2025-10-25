using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using TMPro;

public class LanguageDropdownController : MonoBehaviour
{
    [SerializeField] TMP_Dropdown languageDropdown;
    [SerializeField] TMP_FontAsset defaultFont;
    [SerializeField] List<LocaleFontMapping> localeFonts = new();

    TMP_FontAsset defaultItemFont;

    [System.Serializable]
    public struct LocaleFontMapping
    {
        public string localeCode;
        public TMP_FontAsset font;
    }

    // 固定文字列の辞書。ScriptableObject やローカライズシステムから取得しないことに注意。
    // 新しい言語を追加する際はこの辞書の内容を直接編集してください。
    static readonly Dictionary<string, string> LocaleDisplayNames = new Dictionary<string, string>
    {
        {"en", "English"},
        {"ja", "日本語"},
        {"zh-Hans", "简体字"},
        {"zh-Hant", "繁體字"},
        {"fr", "Français"},
        {"de", "Deutsch"},
        {"es", "Español"},
        {"ko", "한국어"},
    };

    void Awake()
    {
        if (languageDropdown != null)
        {
            if (defaultFont == null && languageDropdown.captionText != null)
            {
                defaultFont = languageDropdown.captionText.font;
            }

            if (languageDropdown.itemText != null)
            {
                defaultItemFont = languageDropdown.itemText.font;
            }

            if (defaultFont == null)
            {
                defaultFont = defaultItemFont;
            }
        }

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
        int selectedIndex = 0;
        for (int i = 0; i < locales.Count; i++)
        {
            var locale = locales[i];
            languageDropdown.options.Add(new TMP_Dropdown.OptionData(GetLocaleDisplayName(locale)));
            if (LocalizationSettings.SelectedLocale == locale)
            {
                selectedIndex = i;
            }
        }

        languageDropdown.value = selectedIndex;
        languageDropdown.RefreshShownValue();
        languageDropdown.onValueChanged.AddListener(OnLanguageChanged);

        ApplyLocaleFont(LocalizationSettings.SelectedLocale?.Identifier.Code);
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

            ApplyLocaleFont(selected.Identifier.Code);
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

    TMP_FontAsset FindFont(string localeCode)
    {
        if (string.IsNullOrEmpty(localeCode))
        {
            return null;
        }

        foreach (var mapping in localeFonts)
        {
            if (!string.IsNullOrEmpty(mapping.localeCode) &&
                string.Equals(mapping.localeCode, localeCode, StringComparison.OrdinalIgnoreCase))
            {
                return mapping.font;
            }
        }

        return null;
    }

    void ApplyLocaleFont(string localeCode)
    {
        var localeFont = FindFont(localeCode);
        var fallbackFont = defaultFont != null ? defaultFont : defaultItemFont;

        if (languageDropdown != null)
        {
            if (languageDropdown.captionText != null)
            {
                languageDropdown.captionText.font = localeFont != null ? localeFont : fallbackFont;
            }

            if (languageDropdown.itemText != null)
            {
                languageDropdown.itemText.font = localeFont != null ? localeFont : fallbackFont;
            }
        }
    }
}
