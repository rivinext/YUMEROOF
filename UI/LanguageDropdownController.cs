using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using TMPro;

public class LanguageDropdownController : MonoBehaviour
{
    [SerializeField] TMP_Dropdown languageDropdown;

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
            languageDropdown.options.Add(new TMP_Dropdown.OptionData(locale.LocaleName));
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
}
