using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MaterialHuePresetManager : MonoBehaviour
{
    private const string PrefsKeyPrefix = "MaterialHuePresetManager";
    private const string UserPresetScope = "user";

    [System.Serializable]
    public class MaterialHuePreset
    {
        public string PresetId;
        public Color[] Colors = System.Array.Empty<Color>();
    }

    [SerializeField] private MaterialHueController[] materialHueControllers;
    [SerializeField] private MaterialHuePreset[] defaultPresets;
    [SerializeField] private MaterialHuePreset[] userPresets;

    private void Awake()
    {
        InitializeControllers();
    }

    public MaterialHueController[] GetControllers()
    {
        return materialHueControllers;
    }

    public void ApplyDefaultPreset(int presetIndex)
    {
        MaterialHuePreset preset = GetPreset(defaultPresets, presetIndex);
        NotifyControllers(preset, false);
    }

    public void SaveUserPreset(int presetIndex)
    {
        if (!IsValidPresetIndex(userPresets, presetIndex))
        {
            return;
        }

        MaterialHuePreset capturedPreset = CreatePresetSnapshot(GetUserPresetId(presetIndex));
        userPresets[presetIndex] = capturedPreset;
        SavePresetToPlayerPrefs(capturedPreset, UserPresetScope, presetIndex);
    }

    public void ApplyUserPreset(int presetIndex)
    {
        if (!IsValidPresetIndex(userPresets, presetIndex))
        {
            return;
        }

        MaterialHuePreset preset = LoadUserPreset(presetIndex) ?? userPresets[presetIndex];
        NotifyControllers(preset, true);
    }

    public MaterialHuePreset CreatePresetSnapshot(string presetId)
    {
        List<Color> colors = CaptureColorsBySlot();
        return new MaterialHuePreset
        {
            PresetId = presetId,
            Colors = colors.ToArray(),
        };
    }

    public MaterialHueController.HsvColorData[] GetCurrentColors()
    {
        List<MaterialHueController.HsvColorData> colors = new();
        foreach (MaterialHueController controller in materialHueControllers)
        {
            if (controller != null)
            {
                colors.Add(controller.GetCurrentColorData());
            }
        }

        return colors.ToArray();
    }

    private void NotifyControllers(MaterialHuePreset preset, bool saveToControllers)
    {
        Color[] colors = preset?.Colors;
        if (colors == null)
        {
            return;
        }

        foreach (MaterialHueController controller in materialHueControllers)
        {
            controller?.ApplyPresetColors(colors, saveToControllers);
        }
    }

    private void InitializeControllers()
    {
        if (materialHueControllers == null || materialHueControllers.Length == 0)
        {
            materialHueControllers = FindObjectsOfType<MaterialHueController>(true);
        }
    }

    private List<Color> CaptureColorsBySlot()
    {
        List<Color> colors = new();

        MaterialHueController[] orderedControllers = materialHueControllers
            .Where(controller => controller != null)
            .OrderBy(controller => controller.SlotNumber)
            .ToArray();

        int requiredSlotCount = orderedControllers
            .Select(controller => Mathf.Max(controller.SlotNumber + 1, controller.DefaultSlotCount + controller.FixedSlotCount))
            .DefaultIfEmpty(0)
            .Max();

        for (int i = 0; i < requiredSlotCount; i++)
        {
            colors.Add(Color.black);
        }

        foreach (MaterialHueController controller in orderedControllers)
        {
            int slotIndex = Mathf.Clamp(controller.SlotNumber, 0, requiredSlotCount - 1);
            colors[slotIndex] = controller.GetCurrentColor();
        }

        return colors;
    }

    private MaterialHuePreset LoadUserPreset(int presetIndex)
    {
        if (!HasSavedUserPreset(presetIndex))
        {
            return null;
        }

        int colorCount = PlayerPrefs.GetInt(GetCountKey(UserPresetScope, presetIndex), -1);
        if (colorCount <= 0)
        {
            return null;
        }

        Color[] colors = new Color[colorCount];
        for (int i = 0; i < colorCount; i++)
        {
            float r = PlayerPrefs.GetFloat(GetColorKey(UserPresetScope, presetIndex, i, "r"), 0f);
            float g = PlayerPrefs.GetFloat(GetColorKey(UserPresetScope, presetIndex, i, "g"), 0f);
            float b = PlayerPrefs.GetFloat(GetColorKey(UserPresetScope, presetIndex, i, "b"), 0f);
            colors[i] = new Color(r, g, b, 1f);
        }

        return new MaterialHuePreset
        {
            PresetId = GetUserPresetId(presetIndex),
            Colors = colors,
        };
    }

    private MaterialHuePreset GetPreset(MaterialHuePreset[] presets, int index)
    {
        if (!IsValidPresetIndex(presets, index))
        {
            return null;
        }

        return presets[index];
    }

    private static bool IsValidPresetIndex(MaterialHuePreset[] presets, int index)
    {
        return presets != null && index >= 0 && index < presets.Length;
    }

    private void SavePresetToPlayerPrefs(MaterialHuePreset preset, string presetScope, int presetIndex)
    {
        Color[] colors = preset?.Colors;
        if (colors == null)
        {
            return;
        }

        PlayerPrefs.SetInt(GetCountKey(presetScope, presetIndex), colors.Length);

        for (int i = 0; i < colors.Length; i++)
        {
            PlayerPrefs.SetFloat(GetColorKey(presetScope, presetIndex, i, "r"), colors[i].r);
            PlayerPrefs.SetFloat(GetColorKey(presetScope, presetIndex, i, "g"), colors[i].g);
            PlayerPrefs.SetFloat(GetColorKey(presetScope, presetIndex, i, "b"), colors[i].b);
        }

        PlayerPrefs.SetInt(GetSavedFlagKey(presetScope, presetIndex), 1);
        PlayerPrefs.Save();
    }

    private bool HasSavedUserPreset(int presetIndex)
    {
        return PlayerPrefs.GetInt(GetSavedFlagKey(UserPresetScope, presetIndex), 0) == 1;
    }

    private string GetCountKey(string presetScope, int presetIndex)
    {
        return $"{PrefsKeyPrefix}/{presetScope}/{presetIndex}/count";
    }

    private string GetSavedFlagKey(string presetScope, int presetIndex)
    {
        return $"{PrefsKeyPrefix}/{presetScope}/{presetIndex}/saved";
    }

    private string GetColorKey(string presetScope, int presetIndex, int colorIndex, string propertyName)
    {
        return $"{PrefsKeyPrefix}/{presetScope}/{presetIndex}/{colorIndex}/{propertyName}";
    }

    private string GetUserPresetId(int presetIndex)
    {
        return $"{UserPresetScope}_{presetIndex}";
    }
}
