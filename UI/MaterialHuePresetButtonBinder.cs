using System.Collections.Generic;
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

    [Header("Labels")]
    [SerializeField] private string toggleLabelFormat = "Preset {0}";

    [Header("Options")]
    [SerializeField] private bool rebuildOnEnable = true;

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

        string label = string.Format(toggleLabelFormat, slotIndex + 1);
        Text textComponent = toggleInstance.GetComponentInChildren<Text>();
        if (textComponent != null)
        {
            textComponent.text = label;
        }
        bool shouldSelect = slotIndex == presetManager.SelectedSlotIndex || (slotIndex == 0 && spawnedToggles.Count == 0);
        toggleInstance.isOn = shouldSelect;
        if (shouldSelect)
        {
            presetManager.SelectedSlotIndex = slotIndex;
        }

        toggleInstance.onValueChanged.AddListener(isOn =>
        {
            if (!isOn || presetManager == null)
            {
                return;
            }

            presetManager.SelectedSlotIndex = slotIndex;
            UpdateSaveButtonState();
        });

        spawnedToggles.Add(toggleInstance);
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
