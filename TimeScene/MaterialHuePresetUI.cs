using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MaterialHuePresetUI : MonoBehaviour
{
    private enum PresetType
    {
        Default,
        User,
    }

    [SerializeField] private MaterialHuePresetManager presetManager;
    [SerializeField] private Button defaultPresetButton;
    [SerializeField] private Button userPresetButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private TMP_Dropdown presetIndexDropdown;
    [SerializeField] private int defaultPresetLimit = 1;
    [SerializeField] private int userPresetLimit = 1;

    private PresetType currentPresetType = PresetType.Default;
    private int currentPresetIndex;

    private void Awake()
    {
        if (defaultPresetButton != null)
        {
            defaultPresetButton.onClick.AddListener(() => SetPresetType(PresetType.Default));
        }

        if (userPresetButton != null)
        {
            userPresetButton.onClick.AddListener(() => SetPresetType(PresetType.User));
        }

        if (saveButton != null)
        {
            saveButton.onClick.AddListener(SaveSelectedPreset);
        }

        if (loadButton != null)
        {
            loadButton.onClick.AddListener(LoadSelectedPreset);
        }

        if (presetIndexDropdown != null)
        {
            presetIndexDropdown.onValueChanged.AddListener(OnPresetIndexChanged);
        }

        RefreshPresetIndexOptions();
        UpdateButtonVisibility();
    }

    private void SetPresetType(PresetType presetType)
    {
        currentPresetType = presetType;
        ClampPresetIndex();
        RefreshPresetIndexOptions();
        UpdateButtonVisibility();
    }

    private void SaveSelectedPreset()
    {
        if (presetManager == null || currentPresetType != PresetType.User)
        {
            return;
        }

        presetManager.SaveUserPreset(currentPresetIndex);
    }

    private void LoadSelectedPreset()
    {
        if (presetManager == null)
        {
            return;
        }

        if (currentPresetType == PresetType.Default)
        {
            presetManager.ApplyDefaultPreset(currentPresetIndex);
        }
        else
        {
            presetManager.ApplyUserPreset(currentPresetIndex);
        }
    }

    private void OnPresetIndexChanged(int newIndex)
    {
        int maxIndex = GetPresetLimit(currentPresetType) - 1;
        if (maxIndex < 0)
        {
            currentPresetIndex = 0;
            return;
        }

        currentPresetIndex = Mathf.Clamp(newIndex, 0, maxIndex);
        LoadSelectedPreset();
    }

    private void RefreshPresetIndexOptions()
    {
        if (presetIndexDropdown == null)
        {
            return;
        }

        int optionCount = GetPresetLimit(currentPresetType);
        presetIndexDropdown.ClearOptions();

        if (optionCount <= 0)
        {
            presetIndexDropdown.interactable = false;
            return;
        }

        presetIndexDropdown.interactable = true;
        for (int i = 0; i < optionCount; i++)
        {
            presetIndexDropdown.options.Add(new TMP_Dropdown.OptionData($"{i + 1}"));
        }

        presetIndexDropdown.value = Mathf.Clamp(currentPresetIndex, 0, optionCount - 1);
        presetIndexDropdown.RefreshShownValue();
    }

    private void ClampPresetIndex()
    {
        int maxIndex = GetPresetLimit(currentPresetType) - 1;
        if (maxIndex < 0)
        {
            currentPresetIndex = 0;
            return;
        }

        currentPresetIndex = Mathf.Clamp(currentPresetIndex, 0, maxIndex);
    }

    private int GetPresetLimit(PresetType presetType)
    {
        int limit = presetType == PresetType.Default ? defaultPresetLimit : userPresetLimit;
        return Mathf.Max(0, limit);
    }

    private void UpdateButtonVisibility()
    {
        bool isDefault = currentPresetType == PresetType.Default;

        if (saveButton != null)
        {
            saveButton.gameObject.SetActive(!isDefault);
        }

        if (loadButton != null)
        {
            loadButton.gameObject.SetActive(true);
        }
    }
}
