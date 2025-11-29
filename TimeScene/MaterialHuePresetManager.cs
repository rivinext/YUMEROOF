using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MaterialHuePresetSlot
{
    [SerializeField] private string label = "Slot";

    public string Label => string.IsNullOrWhiteSpace(label) ? "Slot" : label.Trim();
}

public class MaterialHuePresetManager : MonoBehaviour
{
    [SerializeField] private List<MaterialHueController> controllers = new();
    [SerializeField] private string keyPrefix = "multi_mat_preset";

    [Header("Preset Slots")]
    [SerializeField] private List<MaterialHuePresetSlot> presetSlots = new()
    {
        new MaterialHuePresetSlot(),
        new MaterialHuePresetSlot(),
        new MaterialHuePresetSlot()
    };

    public int SlotCount => presetSlots?.Count ?? 0;
    public IReadOnlyList<MaterialHuePresetSlot> PresetSlots => presetSlots;

    // 指定スロットに、すべての MaterialHueController の色を保存
    public void SavePreset(int slotIndex)
    {
        if (!TryValidateSlot(slotIndex, out int clampedSlot))
        {
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

        for (int i = 0; i < controllers.Count; i++)
        {
            MaterialHueController controller = controllers[i];
            if (controller == null) continue;

            string baseKey = $"{keyPrefix}_{clampedSlot}_{i}";
            string hueKey = baseKey + "_h";

            // そのスロットにまだ保存されていない場合はスキップ
            if (!PlayerPrefs.HasKey(hueKey))
                continue;

            float h = PlayerPrefs.GetFloat(hueKey, controller.Hue);
            float s = PlayerPrefs.GetFloat(baseKey + "_s", controller.Saturation);
            float v = PlayerPrefs.GetFloat(baseKey + "_v", controller.Value);

            controller.SetHSV(h, s, v);
        }

        Debug.Log($"Loaded preset slot {clampedSlot}");
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
}
