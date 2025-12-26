using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MaterialHuePresetButtonBinder : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MaterialHuePresetManager presetManager;
    [SerializeField] private Toggle toggleTemplate;
    [SerializeField] private Transform toggleContainer;
    [SerializeField] private ToggleGroup toggleGroup;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;

    [Header("Options")]
    [SerializeField] private bool rebuildOnEnable = true;
    [SerializeField] private bool saveBeforeSwitchingSlots = false;

    private readonly List<Toggle> spawnedToggles = new();

    private void OnEnable()
    {
        if (rebuildOnEnable)
        {
            RebuildToggles();
        }

        BindActionButtons();
        UpdateSaveButtonState();
    }

    public void RebuildToggles()
    {
        ClearSpawnedToggles();

        if (presetManager == null)
        {
            Debug.LogWarning("MaterialHuePresetManager is not assigned.");
            return;
        }

        int slotCount = presetManager.SlotCount;
        if (slotCount <= 0)
        {
            Debug.LogWarning("No preset slots configured on the manager.");
            return;
        }

        for (int i = 0; i < slotCount; i++)
        {
            SpawnToggle(toggleTemplate, toggleContainer, i);
        }

        UpdateSaveButtonState();
    }

    private void SpawnToggle(Toggle template, Transform parent, int slotIndex)
    {
        if (template == null || parent == null)
        {
            return;
        }

        Toggle toggleInstance = Instantiate(template, parent);
        toggleInstance.gameObject.SetActive(true);

        if (toggleGroup != null)
        {
            toggleInstance.group = toggleGroup;
        }

        string label = GetSlotLabel(slotIndex);
        TextMeshProUGUI tmpLabel = toggleInstance.GetComponentInChildren<TextMeshProUGUI>();
        if (tmpLabel != null)
        {
            tmpLabel.text = label;
        }
        else
        {
            Text textComponent = toggleInstance.GetComponentInChildren<Text>();
            if (textComponent != null)
            {
                textComponent.text = label;
            }
        }
        bool shouldSelect = slotIndex == presetManager.SelectedSlotIndex;
        toggleInstance.SetIsOnWithoutNotify(shouldSelect);

        toggleInstance.onValueChanged.AddListener(isOn =>
        {
            if (!isOn || presetManager == null)
            {
                return;
            }

            int oldIndex = presetManager.SelectedSlotIndex;
            if (saveBeforeSwitchingSlots && !presetManager.IsDefaultSlot(oldIndex))
            {
                presetManager.SavePreset(oldIndex);
            }

            presetManager.SelectedSlotIndex = slotIndex;
            presetManager.PreviewPreset(slotIndex);
            UpdateSaveButtonState();
        });

        spawnedToggles.Add(toggleInstance);
    }

    private string GetSlotLabel(int slotIndex)
    {
        string defaultLabel = $"Slot {slotIndex + 1}";

        if (presetManager?.PresetSlots == null || slotIndex < 0 || slotIndex >= presetManager.PresetSlots.Count)
        {
            return defaultLabel;
        }

        MaterialHuePresetSlot slot = presetManager.PresetSlots[slotIndex];
        string slotLabel = slot?.Label?.Trim() ?? string.Empty;
        return string.IsNullOrWhiteSpace(slotLabel) ? defaultLabel : slotLabel;
    }

    private void BindActionButtons()
    {
        if (loadButton != null)
        {
            loadButton.onClick.RemoveAllListeners();
            loadButton.onClick.AddListener(() =>
            {
                if (presetManager == null)
                {
                    return;
                }

                presetManager.LoadPreset(presetManager.SelectedSlotIndex);
            });
        }

        if (saveButton != null)
        {
            saveButton.onClick.RemoveAllListeners();
            saveButton.onClick.AddListener(() =>
            {
                if (presetManager == null)
                {
                    return;
                }

                int targetSlot = presetManager.SelectedSlotIndex;
                if (presetManager.IsDefaultSlot(targetSlot))
                {
                    Debug.LogWarning($"Slot {targetSlot} is a default preset and cannot be saved.");
                    return;
                }

                presetManager.SavePreset(targetSlot);
                presetManager.PreviewPreset(targetSlot);
            });
        }
    }

    private void UpdateSaveButtonState()
    {
        if (saveButton == null || presetManager == null)
        {
            return;
        }

        saveButton.interactable = !presetManager.IsDefaultSlot(presetManager.SelectedSlotIndex);
    }

    private void ClearSpawnedToggles()
    {
        foreach (Toggle toggle in spawnedToggles)
        {
            if (toggle != null)
            {
                Destroy(toggle.gameObject);
            }
        }

        spawnedToggles.Clear();
    }
}
