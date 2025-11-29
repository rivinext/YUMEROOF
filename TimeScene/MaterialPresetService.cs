using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public class MaterialColor
{
    public string MaterialId;
    public float Hue;
    public float Saturation;
    public float Value;

    public MaterialColor()
    {
    }

    public MaterialColor(string materialId, float hue, float saturation, float value)
    {
        MaterialId = materialId;
        Hue = hue;
        Saturation = saturation;
        Value = value;
    }
}

[System.Serializable]
public class MaterialColorSet
{
    [SerializeField] private List<MaterialColor> colors = new();

    public IReadOnlyList<MaterialColor> Colors => colors ?? new List<MaterialColor>();

    public MaterialColorSet()
    {
    }

    public MaterialColorSet(IEnumerable<MaterialColor> colorValues)
    {
        colors = colorValues?.Where(c => c != null).ToList() ?? new List<MaterialColor>();
    }

    public bool TryGetColor(string materialId, out MaterialColor color)
    {
        color = Colors.FirstOrDefault(c => string.Equals(NormalizeId(c.MaterialId), NormalizeId(materialId)));
        return color != null;
    }

    public static MaterialColorSet FromColor(Color color, string materialId)
    {
        Color.RGBToHSV(color, out float hue, out float saturation, out float value);
        return new MaterialColorSet(new[] { new MaterialColor(materialId, hue, saturation, value) });
    }

    private static string NormalizeId(string materialId)
    {
        return materialId ?? string.Empty;
    }
}

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
    [SerializeField] private MaterialColorSet[] defaultPresetColors;
    [FormerlySerializedAs("defaultPresetColors")]
    [SerializeField] private Color[] legacyDefaultPresetColors;
    [SerializeField] private string userPresetKeyPrefix = DefaultUserPresetKeyPrefix;

    public PresetSlotInfo GetSlotInfo()
    {
        return new PresetSlotInfo(defaultPresetCount, userPresetCount);
    }

    public bool LoadPreset(PresetCategory category, int slotIndex, MaterialColorSet fallbackSet, out MaterialColorSet presetSet)
    {
        PresetSlotInfo slotInfo = GetSlotInfo();
        presetSet = new MaterialColorSet();

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

            return TryLoadDefaultPreset(slotIndex, fallbackSet, slotInfo.DefaultSlotCount, out presetSet);
        }

        if (slotInfo.UserSlotCount <= 0)
        {
            return false;
        }

        return TryLoadUserPreset(slotIndex, fallbackSet, slotInfo.UserSlotCount, out presetSet);
    }

    public bool SavePreset(PresetCategory category, int slotIndex, MaterialColorSet presetSet)
    {
        PresetSlotInfo slotInfo = GetSlotInfo();
        if (category != PresetCategory.User || slotInfo.UserSlotCount <= 0)
        {
            Debug.LogWarning("Attempted to save a preset in an unavailable category.");
            return false;
        }

        int clampedSlot = Mathf.Clamp(slotIndex, 0, slotInfo.UserSlotCount - 1);
        IReadOnlyList<MaterialColor> colors = presetSet?.Colors ?? new List<MaterialColor>();

        foreach (MaterialColor color in colors)
        {
            if (color == null)
            {
                continue;
            }

            string materialId = NormalizeMaterialId(color.MaterialId);
            PlayerPrefs.SetFloat(GetUserHueKey(clampedSlot, materialId), color.Hue);
            PlayerPrefs.SetFloat(GetUserSaturationKey(clampedSlot, materialId), color.Saturation);
            PlayerPrefs.SetFloat(GetUserValueKey(clampedSlot, materialId), color.Value);
        }

        PlayerPrefs.Save();
        return true;
    }

    private bool TryLoadDefaultPreset(int slotIndex, MaterialColorSet fallbackSet, int availableSlots, out MaterialColorSet presetSet)
    {
        presetSet = GetDefaultPresetSet(slotIndex, availableSlots, fallbackSet);
        return true;
    }

    private bool TryLoadUserPreset(int slotIndex, MaterialColorSet fallbackSet, int availableSlots, out MaterialColorSet presetSet)
    {
        int clampedSlot = Mathf.Clamp(slotIndex, 0, availableSlots - 1);
        MaterialColorSet defaultSet = GetDefaultPresetSet(clampedSlot, availableSlots, fallbackSet);
        IReadOnlyList<MaterialColor> targetColors = fallbackSet?.Colors?.Count > 0
            ? fallbackSet.Colors
            : defaultSet.Colors;

        if (targetColors == null || targetColors.Count == 0)
        {
            presetSet = defaultSet;
            return true;
        }

        List<MaterialColor> loadedColors = new List<MaterialColor>();

        for (int i = 0; i < targetColors.Count; i++)
        {
            MaterialColor target = targetColors[i];
            if (target == null)
            {
                continue;
            }

            string materialId = NormalizeMaterialId(target.MaterialId, i);
            string hueKey = GetUserHueKey(clampedSlot, materialId);

            if (!PlayerPrefs.HasKey(hueKey))
            {
                if (defaultSet.TryGetColor(materialId, out MaterialColor defaultColor))
                {
                    loadedColors.Add(new MaterialColor(materialId, defaultColor.Hue, defaultColor.Saturation, defaultColor.Value));
                    continue;
                }

                loadedColors.Add(new MaterialColor(materialId, target.Hue, target.Saturation, target.Value));
                continue;
            }

            float hue = PlayerPrefs.GetFloat(hueKey);
            float saturation = PlayerPrefs.GetFloat(GetUserSaturationKey(clampedSlot, materialId));
            float value = PlayerPrefs.GetFloat(GetUserValueKey(clampedSlot, materialId));
            loadedColors.Add(new MaterialColor(materialId, hue, saturation, value));
        }

        presetSet = new MaterialColorSet(loadedColors);
        return true;
    }

    private MaterialColorSet GetDefaultPresetSet(int slotIndex, int availableSlots, MaterialColorSet fallbackSet)
    {
        if (defaultPresetColors != null && defaultPresetColors.Length > 0)
        {
            int clampedSlot = Mathf.Clamp(slotIndex, 0, Mathf.Min(defaultPresetColors.Length, availableSlots) - 1);
            return defaultPresetColors[clampedSlot] ?? new MaterialColorSet();
        }

        if (legacyDefaultPresetColors != null && legacyDefaultPresetColors.Length > 0)
        {
            int clampedSlot = Mathf.Clamp(slotIndex, 0, Mathf.Min(legacyDefaultPresetColors.Length, availableSlots) - 1);
            Color legacyColor = legacyDefaultPresetColors[clampedSlot];
            string materialId = TryGetFallbackMaterialId(fallbackSet) ?? $"material_{clampedSlot}";
            return MaterialColorSet.FromColor(legacyColor, materialId);
        }

        return fallbackSet ?? new MaterialColorSet();
    }

    private string GetUserHueKey(int slotIndex, string materialId)
    {
        return $"{userPresetKeyPrefix}_{slotIndex}_{materialId}_hue";
    }

    private string GetUserSaturationKey(int slotIndex, string materialId)
    {
        return $"{userPresetKeyPrefix}_{slotIndex}_{materialId}_saturation";
    }

    private string GetUserValueKey(int slotIndex, string materialId)
    {
        return $"{userPresetKeyPrefix}_{slotIndex}_{materialId}_value";
    }

    private string NormalizeMaterialId(string materialId, int index = 0)
    {
        string sanitized = string.IsNullOrEmpty(materialId) ? $"material_{index}" : materialId.Replace(" ", "_");
        return sanitized;
    }

    private string TryGetFallbackMaterialId(MaterialColorSet fallbackSet)
    {
        if (fallbackSet?.Colors == null || fallbackSet.Colors.Count <= 0)
        {
            return null;
        }

        MaterialColor first = fallbackSet.Colors.FirstOrDefault(c => c != null);
        return first != null ? NormalizeMaterialId(first.MaterialId) : null;
    }
}
