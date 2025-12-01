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
    [SerializeField] private bool applyInitialPresetOnStart = false;
    [SerializeField] private int initialPresetIndex = 0;
    [SerializeField] private bool applyFirstDefaultSlot = false;

    [Header("Preset Slots")]
    [SerializeField] private List<MaterialHuePresetSlot> presetSlots = new()
    {
        new MaterialHuePresetSlot(),
        new MaterialHuePresetSlot(),
        new MaterialHuePresetSlot()
    };

    [Header("Selection")]
    [SerializeField] private int selectedSlotIndex = 0;

    public int SlotCount => presetSlots?.Count ?? 0;
    public IReadOnlyList<MaterialHuePresetSlot> PresetSlots => presetSlots;
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

            selectedSlotIndex = Mathf.Clamp(value, 0, SlotCount - 1);
        }
    }

    private void Awake()
    {
        SelectedSlotIndex = ResolveInitialSlotIndex();
    }

    private void Start()
    {
        if (applyInitialPresetOnStart)
        {
            LoadPreset(SelectedSlotIndex);
        }
    }

    // 指定スロットに、すべての MaterialHueController の色を保存
    public void SavePreset(int slotIndex)
    {
        if (!TryValidateSlot(slotIndex, out int clampedSlot))
        {
            return;
        }

        if (IsDefaultSlot(clampedSlot))
        {
            Debug.LogWarning($"Slot index {clampedSlot} is configured as default and cannot be saved.");
            return;
        }

        for (int i = 0; i < controllers.Count; i++)
        {
            MaterialHueController controller = controllers[i];
            if (controller == null) continue;

            string baseKey = $"{keyPrefix}_{clampedSlot}_{i}";
            PlayerPrefs.SetFloat(baseKey + "_h", controller.Hue);
            PlayerPrefs.SetFloat(baseKey + "_s", controller.Saturation);
            PlayerPrefs.SetFloat(baseKey + "_v", controller.Value);
        }

        PlayerPrefs.Save();
        Debug.Log($"Saved preset slot {clampedSlot}");
    }

    // 指定スロットから、すべての MaterialHueController の色を復元
    public void LoadPreset(int slotIndex)
    {
        if (!TryValidateSlot(slotIndex, out int clampedSlot))
        {
            return;
        }

        MaterialHuePresetSlot slot = presetSlots[clampedSlot];
        if (slot != null && slot.IsDefaultPreset)
        {
            LoadDefaultPreset(slot, clampedSlot);
            return;
        }

        LoadUserPreset(clampedSlot);
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

    private void LoadDefaultPreset(MaterialHuePresetSlot slot, int slotIndex)
    {
        IReadOnlyList<HSVColor> defaultColors = slot.DefaultColors;

        if (defaultColors == null || defaultColors.Count == 0)
        {
            Debug.LogWarning($"No default colors configured for slot index {slotIndex}.");
            return;
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
            controller.SetHSV(defaultColor.H, defaultColor.S, defaultColor.V);
        }

        Debug.Log($"Loaded default preset slot {slotIndex}");
    }

    private void LoadUserPreset(int slotIndex)
    {
        for (int i = 0; i < controllers.Count; i++)
        {
            MaterialHueController controller = controllers[i];
            if (controller == null) continue;

            string baseKey = $"{keyPrefix}_{slotIndex}_{i}";
            string hueKey = baseKey + "_h";

            // そのスロットにまだ保存されていない場合はスキップ
            if (!PlayerPrefs.HasKey(hueKey))
                continue;

            float h = PlayerPrefs.GetFloat(hueKey, controller.Hue);
            float s = PlayerPrefs.GetFloat(baseKey + "_s", controller.Saturation);
            float v = PlayerPrefs.GetFloat(baseKey + "_v", controller.Value);

            controller.SetHSV(h, s, v);
        }

        Debug.Log($"Loaded preset slot {slotIndex}");
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
