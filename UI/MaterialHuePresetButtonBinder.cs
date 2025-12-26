using System.Collections;
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
    [SerializeField] private TMP_Text saveWarningText;
    [SerializeField, Min(0f)] private float saveWarningDurationSeconds = 2f;

    [Header("Options")]
    [SerializeField] private bool rebuildOnEnable = true;

    private readonly List<Toggle> spawnedToggles = new();
    private Coroutine warningCoroutine;

    private void OnEnable()
    {
        if (rebuildOnEnable)
        {
            RebuildToggles();
        }

        BindActionButtons();
        BindWarningEvents();
        UpdateSaveButtonState();
        ClearSaveWarning();
    }

    private void OnDisable()
    {
        UnbindWarningEvents();
        ClearSaveWarning();
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
            if (!presetManager.IsDefaultSlot(oldIndex))
            {
                // Save before switching so edits persist even without pressing an explicit save button.
                // If rapid slot switching causes too many saves, consider debouncing here or surfacing
                // a "saving" indicator to inform the user.
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

        bool hasSlotKey = !string.IsNullOrWhiteSpace(SaveGameManager.Instance?.CurrentSlotKey);
        saveButton.interactable = hasSlotKey && !presetManager.IsDefaultSlot(presetManager.SelectedSlotIndex);
        if (hasSlotKey)
        {
            ClearSaveWarning();
        }
    }

    private void BindWarningEvents()
    {
        if (presetManager != null)
        {
            presetManager.OnSaveSlotWarning -= HandleSaveSlotWarning;
            presetManager.OnSaveSlotWarning += HandleSaveSlotWarning;
        }

        if (SaveGameManager.Instance != null)
        {
            SaveGameManager.Instance.OnSlotKeyChanged -= HandleSlotKeyChanged;
            SaveGameManager.Instance.OnSlotKeyChanged += HandleSlotKeyChanged;
        }
    }

    private void UnbindWarningEvents()
    {
        if (presetManager != null)
        {
            presetManager.OnSaveSlotWarning -= HandleSaveSlotWarning;
        }

        if (SaveGameManager.Instance != null)
        {
            SaveGameManager.Instance.OnSlotKeyChanged -= HandleSlotKeyChanged;
        }
    }

    private void HandleSaveSlotWarning(string message)
    {
        if (saveWarningText == null)
        {
            return;
        }

        saveWarningText.text = message;
        saveWarningText.gameObject.SetActive(true);

        if (warningCoroutine != null)
        {
            StopCoroutine(warningCoroutine);
        }

        warningCoroutine = StartCoroutine(HideSaveWarningAfterDelay());
    }

    private IEnumerator HideSaveWarningAfterDelay()
    {
        if (saveWarningDurationSeconds > 0f)
        {
            yield return new WaitForSeconds(saveWarningDurationSeconds);
        }

        ClearSaveWarning();
    }

    private void ClearSaveWarning()
    {
        if (warningCoroutine != null)
        {
            StopCoroutine(warningCoroutine);
            warningCoroutine = null;
        }

        if (saveWarningText != null)
        {
            saveWarningText.gameObject.SetActive(false);
        }
    }

    private void HandleSlotKeyChanged(string slotKey)
    {
        UpdateSaveButtonState();
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
