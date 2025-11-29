using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MaterialHuePresetManager : MonoBehaviour
{
    private const string PresetPrefsKey = "material_hue_presets";

    [SerializeField] private MaterialHueController[] hueControllers;
    [SerializeField, Min(1)] private int presetSlotCount = 3;
    [SerializeField] private int currentSlotIndex;
    [SerializeField] private UnityEvent<int> onCurrentSlotChanged = new UnityEvent<int>();
    [SerializeField] private UnityEvent onPresetsChanged = new UnityEvent();

    [Serializable]
    private class PresetSlotData
    {
        public int slotId;
        public MaterialHueController.HsvColorData[] controllerData;
    }

    [Serializable]
    private class PresetCollection
    {
        public List<PresetSlotData> slots = new List<PresetSlotData>();
    }

    public int SlotCount => Mathf.Max(1, presetSlotCount);
    public int CurrentSlotIndex => Mathf.Clamp(currentSlotIndex, 0, SlotCount - 1);
    public UnityEvent<int> OnCurrentSlotChanged => onCurrentSlotChanged;
    public UnityEvent OnPresetsChanged => onPresetsChanged;

    private void OnValidate()
    {
        presetSlotCount = Mathf.Max(1, presetSlotCount);
        currentSlotIndex = Mathf.Clamp(currentSlotIndex, 0, presetSlotCount - 1);
    }

    public void SetCurrentSlot(int slot)
    {
        int clampedSlot = Mathf.Clamp(slot, 0, SlotCount - 1);
        currentSlotIndex = clampedSlot;
        onCurrentSlotChanged.Invoke(CurrentSlotIndex);
    }

    public void SavePreset(int slot)
    {
        if (!IsValidSlot(slot))
        {
            Debug.LogWarning($"Attempted to save invalid preset slot {slot}.");
            return;
        }

        PresetCollection collection = LoadPresetCollection();
        PresetSlotData slotData = collection.slots.Find(data => data.slotId == slot);
        MaterialHueController.HsvColorData[] controllerData = BuildControllerData();

        if (slotData == null)
        {
            slotData = new PresetSlotData
            {
                slotId = slot,
                controllerData = controllerData
            };
            collection.slots.Add(slotData);
        }
        else
        {
            slotData.controllerData = controllerData;
        }

        SavePresetCollection(collection);
        SetCurrentSlot(slot);
    }

    public void LoadPreset(int slot)
    {
        if (!IsValidSlot(slot))
        {
            Debug.LogWarning($"Attempted to load invalid preset slot {slot}.");
            return;
        }

        if (!TryGetPresetData(slot, out MaterialHueController.HsvColorData[] controllerData))
        {
            Debug.LogWarning($"No preset saved in slot {slot}.");
            return;
        }

        int controllerCount = Mathf.Min(hueControllers.Length, controllerData.Length);
        for (int i = 0; i < controllerCount; i++)
        {
            if (hueControllers[i] == null)
            {
                continue;
            }

            hueControllers[i].ApplyColorData(controllerData[i]);
        }

        SetCurrentSlot(slot);
    }

    public bool HasPreset(int slot)
    {
        if (!IsValidSlot(slot))
        {
            return false;
        }

        PresetCollection collection = LoadPresetCollection();
        return collection.slots.Exists(data => data.slotId == slot && data.controllerData != null);
    }

    public bool TryGetPresetData(int slot, out MaterialHueController.HsvColorData[] controllerData)
    {
        controllerData = null;

        if (!IsValidSlot(slot))
        {
            return false;
        }

        PresetCollection collection = LoadPresetCollection();
        PresetSlotData data = collection.slots.Find(slotData => slotData.slotId == slot);

        if (data == null || data.controllerData == null)
        {
            return false;
        }

        controllerData = data.controllerData;
        return true;
    }

    public bool TryGetPresetColor(int slot, out Color color)
    {
        color = Color.clear;

        if (!TryGetPresetData(slot, out MaterialHueController.HsvColorData[] controllerData) ||
            controllerData.Length == 0)
        {
            return false;
        }

        MaterialHueController.HsvColorData first = controllerData[0];
        color = Color.HSVToRGB(first.Hue, first.Saturation, first.Value);
        return true;
    }

    private MaterialHueController.HsvColorData[] BuildControllerData()
    {
        MaterialHueController.HsvColorData[] controllerData = new MaterialHueController.HsvColorData[hueControllers.Length];
        for (int i = 0; i < hueControllers.Length; i++)
        {
            controllerData[i] = hueControllers[i] != null
                ? hueControllers[i].GetColorData()
                : default;
        }

        return controllerData;
    }

    private bool IsValidSlot(int slot)
    {
        return slot >= 0 && slot < SlotCount;
    }

    private PresetCollection LoadPresetCollection()
    {
        string json = PlayerPrefs.GetString(PresetPrefsKey, string.Empty);
        if (string.IsNullOrEmpty(json))
        {
            return new PresetCollection();
        }

        try
        {
            PresetCollection collection = JsonUtility.FromJson<PresetCollection>(json);
            return collection ?? new PresetCollection();
        }
        catch (ArgumentException)
        {
            Debug.LogWarning("Failed to parse preset data. Resetting presets.");
            return new PresetCollection();
        }
    }

    private void SavePresetCollection(PresetCollection collection)
    {
        string json = JsonUtility.ToJson(collection);
        PlayerPrefs.SetString(PresetPrefsKey, json);
        PlayerPrefs.Save();
        onPresetsChanged.Invoke();
    }
}
