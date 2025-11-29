using System.Collections.Generic;
using UnityEngine;

public class MaterialHuePresetManager : MonoBehaviour
{
    private const string PrefsKeyPrefix = "MaterialHuePresetManager";
    private const string UserPresetScope = "user";

    [System.Serializable]
    public class ControllerColor
    {
        public string ControllerId;
        public MaterialHueController.HsvColorData ColorData;
    }

    [System.Serializable]
    public class MaterialHuePreset
    {
        public string PresetId;
        public ControllerColor[] ControllerColors;
    }

    [SerializeField] private MaterialHueController[] materialHueControllers;
    [SerializeField] private MaterialHuePreset[] defaultPresets;
    [SerializeField] private MaterialHuePreset[] userPresets;

    private readonly Dictionary<string, MaterialHueController> controllerLookup = new();

    private void Awake()
    {
        InitializeControllers();
        BuildControllerLookup();
    }

    public MaterialHueController[] GetControllers()
    {
        return materialHueControllers;
    }

    public void ApplyDefaultPreset(int presetIndex)
    {
        MaterialHuePreset preset = GetPreset(defaultPresets, presetIndex);
        if (preset != null)
        {
            ApplyPreset(preset, false);
        }
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
        if (preset != null)
        {
            ApplyPreset(preset, false);
        }
    }

    public MaterialHuePreset CreatePresetSnapshot(string presetId)
    {
        List<ControllerColor> entries = new();
        foreach (MaterialHueController controller in materialHueControllers)
        {
            if (controller == null)
            {
                continue;
            }

            entries.Add(new ControllerColor
            {
                ControllerId = controller.ControllerId,
                ColorData = controller.GetCurrentColorData(),
            });
        }

        return new MaterialHuePreset
        {
            PresetId = presetId,
            ControllerColors = entries.ToArray(),
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

    public void ApplyPreset(MaterialHuePreset preset, bool saveToControllers)
    {
        if (preset?.ControllerColors == null)
        {
            return;
        }

        foreach (ControllerColor entry in preset.ControllerColors)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.ControllerId))
            {
                continue;
            }

            if (!controllerLookup.TryGetValue(entry.ControllerId, out MaterialHueController controller) || controller == null)
            {
                continue;
            }

            controller.ApplyColorData(entry.ColorData, saveToControllers);
        }
    }

    private void InitializeControllers()
    {
        if (materialHueControllers == null || materialHueControllers.Length == 0)
        {
            materialHueControllers = FindObjectsOfType<MaterialHueController>(true);
        }
    }

    private void BuildControllerLookup()
    {
        controllerLookup.Clear();
        foreach (MaterialHueController controller in materialHueControllers)
        {
            if (controller == null || string.IsNullOrWhiteSpace(controller.ControllerId))
            {
                continue;
            }

            controllerLookup[controller.ControllerId] = controller;
        }
    }

    private MaterialHuePreset LoadUserPreset(int presetIndex)
    {
        if (!HasSavedUserPreset(presetIndex))
        {
            return null;
        }

        List<ControllerColor> colors = new();
        foreach (MaterialHueController controller in materialHueControllers)
        {
            if (controller == null)
            {
                continue;
            }

            colors.Add(new ControllerColor
            {
                ControllerId = controller.ControllerId,
                ColorData = new MaterialHueController.HsvColorData
                {
                    Hue = PlayerPrefs.GetFloat(GetControllerKey(UserPresetScope, presetIndex, controller.ControllerId, nameof(MaterialHueController.HsvColorData.Hue))),
                    Saturation = PlayerPrefs.GetFloat(GetControllerKey(UserPresetScope, presetIndex, controller.ControllerId, nameof(MaterialHueController.HsvColorData.Saturation))),
                    Value = PlayerPrefs.GetFloat(GetControllerKey(UserPresetScope, presetIndex, controller.ControllerId, nameof(MaterialHueController.HsvColorData.Value))),
                }
            });
        }

        return new MaterialHuePreset
        {
            PresetId = GetUserPresetId(presetIndex),
            ControllerColors = colors.ToArray(),
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
        if (preset?.ControllerColors == null)
        {
            return;
        }

        foreach (ControllerColor entry in preset.ControllerColors)
        {
            string hueKey = GetControllerKey(presetScope, presetIndex, entry.ControllerId, nameof(MaterialHueController.HsvColorData.Hue));
            string saturationKey = GetControllerKey(presetScope, presetIndex, entry.ControllerId, nameof(MaterialHueController.HsvColorData.Saturation));
            string valueKey = GetControllerKey(presetScope, presetIndex, entry.ControllerId, nameof(MaterialHueController.HsvColorData.Value));

            PlayerPrefs.SetFloat(hueKey, entry.ColorData.Hue);
            PlayerPrefs.SetFloat(saturationKey, entry.ColorData.Saturation);
            PlayerPrefs.SetFloat(valueKey, entry.ColorData.Value);
        }

        PlayerPrefs.SetInt(GetSavedFlagKey(presetScope, presetIndex), 1);
        PlayerPrefs.Save();
    }

    private bool HasSavedUserPreset(int presetIndex)
    {
        return PlayerPrefs.GetInt(GetSavedFlagKey(UserPresetScope, presetIndex), 0) == 1;
    }

    private string GetSavedFlagKey(string presetScope, int presetIndex)
    {
        return $"{PrefsKeyPrefix}/{presetScope}/{presetIndex}/saved";
    }

    private string GetControllerKey(string presetScope, int presetIndex, string controllerId, string propertyName)
    {
        return $"{PrefsKeyPrefix}/{presetScope}/{presetIndex}/{controllerId}/{propertyName}";
    }

    private string GetUserPresetId(int presetIndex)
    {
        return $"{UserPresetScope}_{presetIndex}";
    }
}
