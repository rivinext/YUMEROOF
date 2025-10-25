using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using TMPro;

public class LanguageDropdownController : MonoBehaviour
{
    [SerializeField] TMP_Dropdown languageDropdown;

    [System.Serializable]
    struct LocaleSprite
    {
        [Tooltip("ロケール識別子コード (例: en, ja, zh-Hans)")]
        public string localeCode;
        [Tooltip("ドロップダウンの項目およびキャプションに表示するスプライト")]
        public Sprite sprite;
    }

    [SerializeField]
    [Tooltip("ロケールコードとドロップダウンに表示するスプライトの対応表")]
    List<LocaleSprite> localeSprites = new List<LocaleSprite>();

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
            var sprite = GetLocaleSprite(locale);
            var option = new TMP_Dropdown.OptionData(GetLocaleDisplayName(locale), sprite);
            languageDropdown.options.Add(option);
            if (LocalizationSettings.SelectedLocale == locale)
            {
                selectedIndex = i;
            }
        }

        languageDropdown.value = selectedIndex;
        languageDropdown.RefreshShownValue();
        languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
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

    Sprite GetLocaleSprite(Locale locale)
    {
        if (locale == null)
        {
            return null;
        }

        if (localeSprites == null)
        {
            return null;
        }

        var code = locale.Identifier.Code;
        for (int i = 0; i < localeSprites.Count; i++)
        {
            var entry = localeSprites[i];
            if (!string.IsNullOrEmpty(entry.localeCode) && entry.localeCode == code)
            {
                return entry.sprite;
            }
        }

        return null;
    }
}
