using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct HSVColor
{
    [Range(0f, 1f)]
    [SerializeField] private float h;

    [Range(0f, 1f)]
    [SerializeField] private float s;

    [Range(0f, 1f)]
    [SerializeField] private float v;

    public float H => Mathf.Repeat(h, 1f);
    public float S => Mathf.Clamp01(s);
    public float V => Mathf.Clamp01(v);

    public HSVColor(float hue, float saturation, float value)
    {
        h = Mathf.Repeat(hue, 1f);
        s = Mathf.Clamp01(saturation);
        v = Mathf.Clamp01(value);
    }
}

[System.Serializable]
public class MaterialHuePresetSlot
{
    [SerializeField] private string label = "Slot";
    [SerializeField] private bool isDefaultPreset = false;
    [SerializeField] private List<HSVColor> defaultColors = new();

    public string Label => string.IsNullOrWhiteSpace(label) ? "Slot" : label.Trim();
    public bool IsDefaultPreset => isDefaultPreset;
    public IReadOnlyList<HSVColor> DefaultColors => defaultColors;
}

public class MaterialHuePresetManager : MonoBehaviour
{
    [SerializeField] private List<MaterialHueController> controllers = new();
    [SerializeField] private string keyPrefix = "multi_mat_preset";

    [Header("Initial Load")]
    [SerializeField] private bool applyInitialPresetOnStart = true;
    [SerializeField] private int initialPresetIndex = 0;
    [SerializeField] private bool applyFirstDefaultSlot = false;

    [Header("Auto Save")]
    [SerializeField] private float autoSaveDebounceSeconds = 0.2f;

    [Header("Preset Slots")]
    [SerializeField] private List<MaterialHuePresetSlot> presetSlots = new()
    {
        new MaterialHuePresetSlot(),
        new MaterialHuePresetSlot(),
        new MaterialHuePresetSlot()
    };

    [Header("Selection")]
    [SerializeField] private int selectedSlotIndex = 0;

    [Header("Save Slot Behavior")]
    [SerializeField] private bool allowGlobalSaveWhenSlotEmpty = true;

    private bool hasAppliedSaveData = false;
    private bool hasSavedSelectedSlotThisSession = false;
    private Coroutine autoSaveCoroutine;
    private bool isWaitingForSlotKey = false;
    private bool pendingInitialLoad = true;
    private bool hasPendingSaveData = false;
    private MaterialHueManagerSaveData pendingSaveData;

    public event Action<string> OnSaveSlotWarning;

    public int SlotCount => presetSlots?.Count ?? 0;
    public IReadOnlyList<MaterialHuePresetSlot> PresetSlots => presetSlots;
    public string KeyPrefix => keyPrefix;
    private string SelectedSlotKey => GetNamespacedKey("selectedSlot");
    private string LegacySelectedSlotKey => $"{keyPrefix}_selectedSlot";
    public int SelectedSlotIndex
    {
        get
        {
            if (SlotCount <= 0)
            {
                return 0;
            }

            return Mathf.Clamp(selectedSlotIndex, 0, SlotCount - 1);
        }
        set
        {
            if (SlotCount <= 0)
            {
                selectedSlotIndex = 0;
                return;
            }

            int clampedIndex = Mathf.Clamp(value, 0, SlotCount - 1);
            if (selectedSlotIndex == clampedIndex)
            {
                return;
            }

            selectedSlotIndex = clampedIndex;
            hasSavedSelectedSlotThisSession = false;
            SaveSelectedSlotIndex();
        }
    }

    private void Awake()
    {
        if (SlotCount > 0)
        {
            selectedSlotIndex = Mathf.Clamp(initialPresetIndex, 0, SlotCount - 1);
        }

        pendingInitialLoad = true;
    }

    private void OnEnable()
    {
        SubscribeToControllers();
    }

    private void OnDisable()
    {
        UnsubscribeFromControllers();
        UnsubscribeFromSlotKeyChanged();

        if (autoSaveCoroutine != null)
        {
            StopCoroutine(autoSaveCoroutine);
            autoSaveCoroutine = null;
        }

        SaveSelectedSlotIndex();
        SaveSelectedSlotIfNeeded("OnDisable");
    }

    private void OnApplicationQuit()
    {
        SaveSelectedSlotIndex();
        SaveSelectedSlotIfNeeded("OnApplicationQuit");
    }

    private void Start()
    {
        EnsureInitialLoadWhenReady();
    }

    public void EnsureInitialLoadWhenReady()
    {
        if (hasAppliedSaveData || !pendingInitialLoad)
        {
            return;
        }

        pendingInitialLoad = true;

        if (!IsSaveSlotReady())
        {
            WaitForSaveSlotKey();
            return;
        }

        PerformInitialLoad();
    }

    public static void EnsureAllManagersInitialized()
    {
        foreach (MaterialHuePresetManager manager in Resources.FindObjectsOfTypeAll<MaterialHuePresetManager>())
        {
            if (manager == null)
            {
                continue;
            }

            manager.EnsureInitialLoadWhenReady();
        }
    }

    public static void ApplySaveDataToAllManagers(MaterialHueSaveData data)
    {
        List<MaterialHueManagerSaveData> managerSaveData = data?.managers;
        bool hasManagerList = managerSaveData != null && managerSaveData.Count > 0;
        MaterialHueManagerSaveData legacyData = null;

        if (!hasManagerList && data != null)
        {
            legacyData = new MaterialHueManagerSaveData
            {
                selectedSlotIndex = data.selectedSlotIndex,
                controllerColors = data.controllerColors
            };
        }

        foreach (MaterialHuePresetManager manager in Resources.FindObjectsOfTypeAll<MaterialHuePresetManager>())
        {
            if (manager == null)
            {
                continue;
            }

            MaterialHueManagerSaveData matchingData = null;

            if (hasManagerList)
            {
                foreach (MaterialHueManagerSaveData saveData in managerSaveData)
                {
                    if (saveData == null)
                    {
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(saveData.keyPrefix)
                        && string.Equals(saveData.keyPrefix, manager.KeyPrefix, StringComparison.Ordinal))
                    {
                        matchingData = saveData;
                        break;
                    }
                }

                matchingData ??= managerSaveData[0];
            }
            else
            {
                matchingData = legacyData;
            }

            manager.ApplyFromSaveData(matchingData);
        }
    }

    private void PerformInitialLoad()
    {
        if (hasAppliedSaveData || !pendingInitialLoad)
        {
            return;
        }

        if (!IsSaveSlotReady())
        {
            WaitForSaveSlotKey();
            return;
        }

        pendingInitialLoad = false;

        if (applyInitialPresetOnStart)
        {
            SelectedSlotIndex = ResolveInitialSlotIndex();
        }

        int slotIndex = SelectedSlotIndex;

        if (IsDefaultSlot(slotIndex))
        {
            Debug.Log($"Loading default preset for slot {slotIndex} on start.");
            LoadPreset(slotIndex);
            return;
        }

        if (HasSavedPreset(slotIndex))
        {
            Debug.Log($"Loading saved preset for slot {slotIndex} on start.");
            LoadPreset(slotIndex);
            return;
        }

        Debug.LogWarning($"No saved preset found for slot {slotIndex} on start. Applying default fallback.");
        ApplyDefaultPresetFallback();
    }

    private void SubscribeToControllers()
    {
        foreach (MaterialHueController controller in controllers)
        {
            if (controller == null)
            {
                continue;
            }

            controller.OnAppliedColorChanged -= HandleControllerAppliedColorChanged;
            controller.OnAppliedColorChanged += HandleControllerAppliedColorChanged;
        }
    }

    private void UnsubscribeFromControllers()
    {
        foreach (MaterialHueController controller in controllers)
        {
            if (controller == null)
            {
                continue;
            }

            controller.OnAppliedColorChanged -= HandleControllerAppliedColorChanged;
        }
    }

    public MaterialHueManagerSaveData GetSaveData()
    {
        var data = new MaterialHueManagerSaveData
        {
            keyPrefix = KeyPrefix,
            selectedSlotIndex = SelectedSlotIndex
        };

        foreach (var controller in controllers)
        {
            if (controller == null)
            {
                data.controllerColors.Add(new HSVColor());
                continue;
            }

            data.controllerColors.Add(new HSVColor(controller.AppliedHue, controller.AppliedSaturation, controller.AppliedValue));
        }

        return data;
    }

    // 指定スロットに、すべての MaterialHueController の色を保存
    public void SavePreset(int slotIndex)
    {
        if (!TryValidateSlot(slotIndex, out int clampedSlot))
        {
            return;
        }

        if (!IsSaveSlotReady())
        {
            string message = "Save slot is not selected. Please select a slot before saving.";
            Debug.LogWarning(message);
            OnSaveSlotWarning?.Invoke(message);
            return;
        }

        if (IsDefaultSlot(clampedSlot))
        {
            Debug.LogWarning($"Slot index {clampedSlot} is configured as default and cannot be saved.");
            return;
        }

        ClearLegacyPresetKeys(clampedSlot);

        for (int i = 0; i < controllers.Count; i++)
        {
            MaterialHueController controller = controllers[i];
            if (controller == null) continue;

            string baseKey = GetPresetBaseKey(clampedSlot, i);
            PlayerPrefs.SetFloat(baseKey + "_h", controller.AppliedHue);
            PlayerPrefs.SetFloat(baseKey + "_s", controller.AppliedSaturation);
            PlayerPrefs.SetFloat(baseKey + "_v", controller.AppliedValue);
        }

        PlayerPrefs.Save();
        hasSavedSelectedSlotThisSession = clampedSlot == SelectedSlotIndex;
        Debug.Log($"Saved preset slot {clampedSlot}");
    }

    // 指定スロットの色を読み取り、コントローラに一時反映
    public void PreviewPreset(int slotIndex)
    {
        ApplyPresetToControllers(slotIndex, "Previewed", applyToMaterial: false);
    }

    // 指定スロットから、すべての MaterialHueController の色を復元
    public void LoadPreset(int slotIndex)
    {
        ApplyPresetToControllers(slotIndex, "Loaded", applyToMaterial: true);
    }

    private bool ApplyPresetToControllers(int slotIndex, string actionLabel, bool applyToMaterial)
    {
        if (!TryValidateSlot(slotIndex, out int clampedSlot))
        {
            ResetPreviewToAppliedColors("Failed to apply preset (invalid slot)", slotIndex);
            return false;
        }

        MaterialHuePresetSlot slot = presetSlots[clampedSlot];
        if (slot != null && slot.IsDefaultPreset)
        {
            bool loadedDefault = LoadDefaultPreset(slot, clampedSlot, actionLabel, applyToMaterial);
            if (!loadedDefault)
            {
                ResetPreviewToAppliedColors($"Failed to apply default preset ({actionLabel.ToLowerInvariant()})", clampedSlot);
            }
            return loadedDefault;
        }

        return LoadUserPreset(clampedSlot, actionLabel, applyToMaterial);
    }

    public bool IsDefaultSlot(int slotIndex)
    {
        if (presetSlots == null || slotIndex < 0 || slotIndex >= SlotCount)
        {
            return false;
        }

        MaterialHuePresetSlot slot = presetSlots[slotIndex];
        return slot != null && slot.IsDefaultPreset;
    }

    public bool HasSavedPreset(int slotIndex)
    {
        if (!TryValidateSlot(slotIndex, out int clampedSlot))
        {
            return false;
        }

        for (int i = 0; i < controllers.Count; i++)
        {
            string baseKey = GetPresetBaseKey(clampedSlot, i) + "_h";
            if (PlayerPrefs.HasKey(baseKey))
            {
                return true;
            }
        }

        for (int i = 0; i < controllers.Count; i++)
        {
            string legacyBaseKey = $"{keyPrefix}_{clampedSlot}_{i}_h";
            if (PlayerPrefs.HasKey(legacyBaseKey))
            {
                return true;
            }
        }

        return false;
    }

    private void SaveSelectedSlotIndex()
    {
        if (SlotCount <= 0)
        {
            return;
        }

        PlayerPrefs.SetInt(SelectedSlotKey, SelectedSlotIndex);
        PlayerPrefs.DeleteKey(LegacySelectedSlotKey);
        PlayerPrefs.Save();
    }

    private void SaveSelectedSlotIfNeeded(string triggerLabel)
    {
        if (!IsSaveSlotReady())
        {
            return;
        }

        int slotIndex = SelectedSlotIndex;

        if (IsDefaultSlot(slotIndex))
        {
            Debug.Log($"Auto-save skipped on {triggerLabel}: selected slot {slotIndex} is default.");
            return;
        }

        if (hasSavedSelectedSlotThisSession)
        {
            Debug.Log($"Auto-save skipped on {triggerLabel}: slot {slotIndex} was already saved this session.");
            return;
        }

        Debug.Log($"Auto-saving preset slot {slotIndex} on {triggerLabel}.");
        SavePreset(slotIndex);
    }

    private bool LoadDefaultPreset(MaterialHuePresetSlot slot, int slotIndex, string actionLabel = "Loaded", bool applyToMaterial = true)
    {
        IReadOnlyList<HSVColor> defaultColors = slot.DefaultColors;

        if (defaultColors == null || defaultColors.Count == 0)
        {
            Debug.LogWarning($"No default colors configured for slot index {slotIndex}.");
            return false;
        }

        if (defaultColors.Count < controllers.Count)
        {
            Debug.LogWarning($"Default colors count ({defaultColors.Count}) is less than controller count ({controllers.Count}) for slot index {slotIndex}.");
        }

        for (int i = 0; i < controllers.Count && i < defaultColors.Count; i++)
        {
            MaterialHueController controller = controllers[i];
            if (controller == null)
            {
                continue;
            }

            HSVColor defaultColor = defaultColors[i];
            controller.SetHSV(defaultColor.H, defaultColor.S, defaultColor.V, applyToMaterial: applyToMaterial);
        }

        Debug.Log($"{actionLabel} default preset slot {slotIndex}");
        return true;
    }

    private bool LoadUserPreset(int slotIndex, string actionLabel = "Loaded", bool applyToMaterial = true)
    {
        if (TryLoadUserPresetFromPrefs(slotIndex, actionLabel, applyToMaterial))
        {
            return true;
        }

        Debug.LogWarning($"No saved preset found for slot {slotIndex}.");
        return false;
    }

    public void ApplyFromSaveData(MaterialHueManagerSaveData data)
    {
        if (hasAppliedSaveData && !pendingInitialLoad)
        {
            return;
        }

        if (!IsSaveSlotReady())
        {
            pendingSaveData = data;
            hasPendingSaveData = true;
            WaitForSaveSlotKey();
            return;
        }

        ApplyFromSaveDataInternal(data);
    }

    private void HandleControllerAppliedColorChanged(MaterialHueController controller)
    {
        int slotIndex = SelectedSlotIndex;

        if (IsDefaultSlot(slotIndex))
        {
            return;
        }

        if (autoSaveDebounceSeconds <= 0f)
        {
            SavePreset(slotIndex);
            return;
        }

        if (autoSaveCoroutine != null)
        {
            StopCoroutine(autoSaveCoroutine);
        }

        autoSaveCoroutine = StartCoroutine(DebouncedAutoSave());
    }

    private IEnumerator DebouncedAutoSave()
    {
        yield return new WaitForSeconds(autoSaveDebounceSeconds);

        int slotIndex = SelectedSlotIndex;
        if (!IsDefaultSlot(slotIndex))
        {
            SavePreset(slotIndex);
        }

        autoSaveCoroutine = null;
    }

    private bool TryValidateSlot(int slotIndex, out int validSlotIndex)
    {
        validSlotIndex = Mathf.Max(0, slotIndex);

        if (controllers == null || controllers.Count == 0)
        {
            Debug.LogWarning("No MaterialHueControllers are assigned.");
            return false;
        }

        if (SlotCount <= 0)
        {
            Debug.LogWarning("No preset slots are configured.");
            return false;
        }

        if (validSlotIndex >= SlotCount)
        {
            Debug.LogWarning($"Slot index {validSlotIndex} is out of range. Available slots: {SlotCount}");
            validSlotIndex = Mathf.Clamp(validSlotIndex, 0, SlotCount - 1);
        }

        return true;
    }

    private int ResolveInitialSlotIndex()
    {
        if (SlotCount <= 0)
        {
            return 0;
        }

        if (TryGetSavedSlotIndex(out int savedSlotIndex))
        {
            return savedSlotIndex;
        }

        if (applyFirstDefaultSlot)
        {
            int defaultSlotIndex = FindFirstDefaultSlotIndex();
            if (defaultSlotIndex >= 0)
            {
                return defaultSlotIndex;
            }

            Debug.LogWarning("No default preset slot found. Falling back to the configured initial preset index.");
        }

        if (initialPresetIndex < 0 || initialPresetIndex >= SlotCount)
        {
            int clampedIndex = Mathf.Clamp(initialPresetIndex, 0, SlotCount - 1);
            Debug.LogWarning($"Initial preset index {initialPresetIndex} is out of range. Falling back to {clampedIndex}.");
            return clampedIndex;
        }

        return initialPresetIndex;
    }

    private bool TryGetSavedSlotIndex(out int savedSlotIndex)
    {
        savedSlotIndex = 0;

        if (SlotCount <= 0)
        {
            return false;
        }

        if (!PlayerPrefs.HasKey(SelectedSlotKey))
        {
            if (PlayerPrefs.HasKey(LegacySelectedSlotKey))
            {
                savedSlotIndex = Mathf.Clamp(PlayerPrefs.GetInt(LegacySelectedSlotKey, 0), 0, SlotCount - 1);
                PlayerPrefs.SetInt(SelectedSlotKey, savedSlotIndex);
                PlayerPrefs.DeleteKey(LegacySelectedSlotKey);
                PlayerPrefs.Save();
                return true;
            }

            return false;
        }

        savedSlotIndex = Mathf.Clamp(PlayerPrefs.GetInt(SelectedSlotKey, 0), 0, SlotCount - 1);
        return true;
    }

    private string GetSaveSlotNamespace()
    {
        string slotKey = SaveGameManager.Instance?.CurrentSlotKey;
        return string.IsNullOrWhiteSpace(slotKey) ? "global" : slotKey;
    }

    private string GetNamespacedKey(string suffix)
    {
        return $"{keyPrefix}_{GetSaveSlotNamespace()}_{suffix}";
    }

    private string GetPresetBaseKey(int slotIndex, int controllerIndex)
    {
        return GetNamespacedKey($"{slotIndex}_{controllerIndex}");
    }

    private void ClearLegacyPresetKeys(int slotIndex)
    {
        for (int i = 0; i < controllers.Count; i++)
        {
            string legacyBaseKey = $"{keyPrefix}_{slotIndex}_{i}";
            PlayerPrefs.DeleteKey(legacyBaseKey + "_h");
            PlayerPrefs.DeleteKey(legacyBaseKey + "_s");
            PlayerPrefs.DeleteKey(legacyBaseKey + "_v");
        }
    }

    private bool LoadPresetFromPlayerPrefs(int slotIndex, string actionLabel, bool applyToMaterial, bool useLegacyKeys)
    {
        bool loadedAny = false;

        for (int i = 0; i < controllers.Count; i++)
        {
            MaterialHueController controller = controllers[i];
            if (controller == null)
            {
                continue;
            }

            string baseKey = useLegacyKeys ? $"{keyPrefix}_{slotIndex}_{i}" : GetPresetBaseKey(slotIndex, i);
            string hueKey = baseKey + "_h";

            if (!PlayerPrefs.HasKey(hueKey))
            {
                continue;
            }

            float h = PlayerPrefs.GetFloat(hueKey, controller.Hue);
            float s = PlayerPrefs.GetFloat(baseKey + "_s", controller.Saturation);
            float v = PlayerPrefs.GetFloat(baseKey + "_v", controller.Value);

            controller.SetHSV(h, s, v, applyToMaterial: applyToMaterial);
            loadedAny = true;
        }

        if (loadedAny)
        {
            string label = useLegacyKeys ? $"{actionLabel} (legacy PlayerPrefs)" : actionLabel;
            Debug.Log($"{label} preset slot {slotIndex}");

            if (useLegacyKeys && !IsDefaultSlot(slotIndex))
            {
                SavePreset(slotIndex);
            }
        }

        return loadedAny;
    }

    private bool TryLoadUserPresetFromPrefs(int slotIndex, string actionLabel, bool applyToMaterial)
    {
        if (LoadPresetFromPlayerPrefs(slotIndex, actionLabel, applyToMaterial, useLegacyKeys: false))
        {
            return true;
        }

        if (LoadPresetFromPlayerPrefs(slotIndex, actionLabel, applyToMaterial, useLegacyKeys: true))
        {
            return true;
        }

        ResetPreviewToAppliedColors($"Failed to apply preset ({actionLabel.ToLowerInvariant()})", slotIndex);
        return false;
    }

    private void ResetPreviewToAppliedColors(string reason, int slotIndex)
    {
        if (controllers == null || controllers.Count == 0)
        {
            return;
        }

        for (int i = 0; i < controllers.Count; i++)
        {
            MaterialHueController controller = controllers[i];
            if (controller == null)
            {
                continue;
            }

            controller.SetHSV(controller.AppliedHue, controller.AppliedSaturation, controller.AppliedValue, applyToMaterial: false);
        }

        Debug.LogWarning($"{reason}. Reset preview to applied colors for slot {slotIndex}.");
    }

    private void ApplyFromSaveDataInternal(MaterialHueManagerSaveData data)
    {
        if (!IsSaveSlotReady())
        {
            pendingSaveData = data;
            hasPendingSaveData = true;
            pendingInitialLoad = true;
            WaitForSaveSlotKey();
            return;
        }

        if (!TryValidateSlot(data?.selectedSlotIndex ?? SelectedSlotIndex, out int clampedSlot))
        {
            return;
        }

        pendingInitialLoad = false;
        hasAppliedSaveData = true;
        SelectedSlotIndex = clampedSlot;

        if (data == null || data.controllerColors == null || data.controllerColors.Count == 0)
        {
            if (!TryLoadUserPresetFromPrefs(clampedSlot, "Loaded from PlayerPrefs fallback", applyToMaterial: true))
            {
                ApplyDefaultPresetFallback();
            }
            return;
        }

        if (data.controllerColors.Count < controllers.Count)
        {
            Debug.LogWarning($"Save data color count ({data.controllerColors.Count}) is less than controller count ({controllers.Count}). Missing controllers will keep current values.");
        }

        int colorCount = Mathf.Min(controllers.Count, data.controllerColors.Count);
        for (int i = 0; i < colorCount; i++)
        {
            MaterialHueController controller = controllers[i];
            if (controller == null)
            {
                continue;
            }

            HSVColor savedColor = data.controllerColors[i];
            controller.SetHSV(savedColor.H, savedColor.S, savedColor.V);
        }

        if (!IsDefaultSlot(clampedSlot))
        {
            SavePreset(clampedSlot);
        }
    }

    private void WaitForSaveSlotKey()
    {
        if (isWaitingForSlotKey)
        {
            return;
        }

        var saveManager = SaveGameManager.Instance;
        if (saveManager == null)
        {
            return;
        }

        saveManager.OnSlotKeyChanged -= HandleSlotKeyChanged;
        saveManager.OnSlotKeyChanged += HandleSlotKeyChanged;
        isWaitingForSlotKey = true;
    }

    private void UnsubscribeFromSlotKeyChanged()
    {
        if (!isWaitingForSlotKey)
        {
            return;
        }

        var saveManager = SaveGameManager.Instance;
        if (saveManager != null)
        {
            saveManager.OnSlotKeyChanged -= HandleSlotKeyChanged;
        }

        isWaitingForSlotKey = false;
    }

    private void HandleSlotKeyChanged(string slotKey)
    {
        if (string.IsNullOrWhiteSpace(slotKey))
        {
            return;
        }

        UnsubscribeFromSlotKeyChanged();

        if (hasPendingSaveData)
        {
            var data = pendingSaveData;
            pendingSaveData = null;
            hasPendingSaveData = false;
            ApplyFromSaveDataInternal(data);
            return;
        }

        if (pendingInitialLoad && !hasAppliedSaveData)
        {
            PerformInitialLoad();
        }
    }

    private bool IsSaveSlotReady()
    {
        string slotKey = SaveGameManager.Instance?.CurrentSlotKey;
        return !string.IsNullOrWhiteSpace(slotKey) || allowGlobalSaveWhenSlotEmpty;
    }

    public void ApplyDefaultPresetFallback()
    {
        int targetSlot = FindFirstDefaultSlotIndex();
        if (targetSlot < 0)
        {
            targetSlot = ResolveInitialSlotIndex();
        }

        SelectedSlotIndex = targetSlot;
        LoadPreset(targetSlot);
    }

    private int FindFirstDefaultSlotIndex()
    {
        if (presetSlots == null)
        {
            return -1;
        }

        for (int i = 0; i < presetSlots.Count; i++)
        {
            MaterialHuePresetSlot slot = presetSlots[i];
            if (slot != null && slot.IsDefaultPreset)
            {
                return i;
            }
        }

        return -1;
    }
}
