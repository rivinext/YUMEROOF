using UnityEngine;

public enum PresetCategory
{
    Default,
    User
}

public struct PresetSlotInfo
{
    public int DefaultSlotCount { get; }
    public int UserSlotCount { get; }
    public int TotalSlotCount => DefaultSlotCount + UserSlotCount;
    public bool HasDefaultSlots => DefaultSlotCount > 0;
    public bool HasUserSlots => UserSlotCount > 0;

    public PresetSlotInfo(int defaultSlotCount, int userSlotCount)
    {
        DefaultSlotCount = Mathf.Max(defaultSlotCount, 0);
        UserSlotCount = Mathf.Max(userSlotCount, 0);
    }
}

public class MaterialPresetService : MonoBehaviour
{
    private const string DefaultUserPresetKeyPrefix = "material_user_preset";

    [SerializeField] private int defaultPresetCount = 3;
    [SerializeField] private int userPresetCount = 5;
    [SerializeField] private Color[] defaultPresetColors;
    [SerializeField] private string userPresetKeyPrefix = DefaultUserPresetKeyPrefix;

    public PresetSlotInfo GetSlotInfo()
    {
        return new PresetSlotInfo(defaultPresetCount, userPresetCount);
    }

    public bool LoadPreset(PresetCategory category, int slotIndex, Color fallbackColor, out float hue, out float saturation, out float value)
    {
        PresetSlotInfo slotInfo = GetSlotInfo();
        hue = 0f;
        saturation = 0f;
        value = 0f;

        if (slotInfo.TotalSlotCount <= 0)
        {
            return false;
        }

        if (category == PresetCategory.Default)
        {
            if (slotInfo.DefaultSlotCount <= 0)
            {
                return false;
            }

            return TryLoadDefaultPreset(slotIndex, fallbackColor, slotInfo.DefaultSlotCount, out hue, out saturation, out value);
        }

        if (slotInfo.UserSlotCount <= 0)
        {
            return false;
        }

        return TryLoadUserPreset(slotIndex, fallbackColor, slotInfo.UserSlotCount, out hue, out saturation, out value);
    }

    public bool SavePreset(PresetCategory category, int slotIndex, float hue, float saturation, float value)
    {
        PresetSlotInfo slotInfo = GetSlotInfo();
        if (category != PresetCategory.User || slotInfo.UserSlotCount <= 0)
        {
            Debug.LogWarning("Attempted to save a preset in an unavailable category.");
            return false;
        }

        int clampedSlot = Mathf.Clamp(slotIndex, 0, slotInfo.UserSlotCount - 1);
        PlayerPrefs.SetFloat(GetUserHueKey(clampedSlot), hue);
        PlayerPrefs.SetFloat(GetUserSaturationKey(clampedSlot), saturation);
        PlayerPrefs.SetFloat(GetUserValueKey(clampedSlot), value);
        PlayerPrefs.Save();
        return true;
    }

    private bool TryLoadDefaultPreset(int slotIndex, Color fallbackColor, int availableSlots, out float hue, out float saturation, out float value)
    {
        Color color = GetDefaultPresetColor(slotIndex, availableSlots, fallbackColor);
        Color.RGBToHSV(color, out hue, out saturation, out value);
        return true;
    }

    private bool TryLoadUserPreset(int slotIndex, Color fallbackColor, int availableSlots, out float hue, out float saturation, out float value)
    {
        int clampedSlot = Mathf.Clamp(slotIndex, 0, availableSlots - 1);
        string hueKey = GetUserHueKey(clampedSlot);

        if (!PlayerPrefs.HasKey(hueKey))
        {
            Color color = GetDefaultPresetColor(clampedSlot, availableSlots, fallbackColor);
            Color.RGBToHSV(color, out hue, out saturation, out value);
            return true;
        }

        hue = PlayerPrefs.GetFloat(hueKey);
        saturation = PlayerPrefs.GetFloat(GetUserSaturationKey(clampedSlot));
        value = PlayerPrefs.GetFloat(GetUserValueKey(clampedSlot));
        return true;
    }

    private Color GetDefaultPresetColor(int slotIndex, int availableSlots, Color fallbackColor)
    {
        if (defaultPresetColors == null || defaultPresetColors.Length == 0)
        {
            return fallbackColor;
        }

        int clampedSlot = Mathf.Clamp(slotIndex, 0, Mathf.Min(defaultPresetColors.Length, availableSlots) - 1);
        return defaultPresetColors[clampedSlot];
    }

    private string GetUserHueKey(int slotIndex)
    {
        return $"{userPresetKeyPrefix}_{slotIndex}_hue";
    }

    private string GetUserSaturationKey(int slotIndex)
    {
        return $"{userPresetKeyPrefix}_{slotIndex}_saturation";
    }

    private string GetUserValueKey(int slotIndex)
    {
        return $"{userPresetKeyPrefix}_{slotIndex}_value";
    }
}
