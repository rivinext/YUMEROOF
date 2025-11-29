using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MaterialHuePresetManager : MonoBehaviour
{
    private const string PrefsKeyPrefix = "MaterialHuePresetManager";
    private const string UserPresetScope = "user";

    [System.Serializable]
    public class ControllerColor
    {
        public string ControllerId;
        public int SlotNumber;
        public MaterialHueController.HsvColorData ColorData;
    }

    [System.Serializable]
    public class ControllerColorMapDto
    {
        public ControllerColor[] Entries = System.Array.Empty<ControllerColor>();

        public ControllerColorMapDto()
        {
        }

        public ControllerColorMapDto(IEnumerable<ControllerColor> entries)
        {
            Entries = SortBySlot(entries);
        }

        public ControllerColor[] GetEntriesSortedBySlot()
        {
            return SortBySlot(Entries);
        }

        public ControllerColor[] GetEntriesSortedById()
        {
            return SortById(Entries);
        }

        public ControllerColor[] GetEntriesForControllers(Dictionary<string, MaterialHueController> lookup)
        {
            return GetEntriesSortedBySlot()
                .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.ControllerId))
                .Where(entry => lookup.ContainsKey(entry.ControllerId))
                .ToArray();
        }

        private static ControllerColor[] SortBySlot(IEnumerable<ControllerColor> entries)
        {
            return entries
                .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.ControllerId))
                .OrderBy(entry => entry.SlotNumber)
                .ThenBy(entry => entry.ControllerId)
                .ToArray();
        }

        private static ControllerColor[] SortById(IEnumerable<ControllerColor> entries)
        {
            return entries
                .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.ControllerId))
                .OrderBy(entry => entry.ControllerId)
                .ThenBy(entry => entry.SlotNumber)
                .ToArray();
        }
    }

    [System.Serializable]
    public class MaterialHuePreset
    {
        public string PresetId;
        public ControllerColorMapDto ColorMap;
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
        SavePresetToPlayerPrefs(capturedPreset.ColorMap, UserPresetScope, presetIndex);
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
                SlotNumber = controller.SlotNumber,
                ColorData = controller.GetCurrentColorData(),
            });
        }

        ControllerColorMapDto colorMap = new ControllerColorMapDto(entries);
        return new MaterialHuePreset
        {
            PresetId = presetId,
            ColorMap = colorMap,
            ControllerColors = colorMap.GetEntriesSortedById(),
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
        ControllerColor[] controllerColors = GetValidControllerColors(preset);
        if (controllerColors == null)
        {
            return;
        }

        foreach (ControllerColor entry in controllerColors)
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

    private ControllerColor[] GetValidControllerColors(MaterialHuePreset preset)
    {
        if (preset == null)
        {
            return null;
        }

        ControllerColorMapDto map = preset.ColorMap;
        if (map == null && preset.ControllerColors != null)
        {
            map = new ControllerColorMapDto(preset.ControllerColors);
            preset.ColorMap = map;
        }

        return map?.GetEntriesForControllers(controllerLookup);
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
                SlotNumber = controller.SlotNumber,
                ColorData = new MaterialHueController.HsvColorData
                {
                    Hue = PlayerPrefs.GetFloat(GetControllerKey(UserPresetScope, presetIndex, controller.ControllerId, nameof(MaterialHueController.HsvColorData.Hue))),
                    Saturation = PlayerPrefs.GetFloat(GetControllerKey(UserPresetScope, presetIndex, controller.ControllerId, nameof(MaterialHueController.HsvColorData.Saturation))),
                    Value = PlayerPrefs.GetFloat(GetControllerKey(UserPresetScope, presetIndex, controller.ControllerId, nameof(MaterialHueController.HsvColorData.Value))),
                }
            });
        }

        ControllerColorMapDto colorMap = new ControllerColorMapDto(colors);
        return new MaterialHuePreset
        {
            PresetId = GetUserPresetId(presetIndex),
            ColorMap = colorMap,
            ControllerColors = colorMap.GetEntriesSortedById(),
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

    private void SavePresetToPlayerPrefs(ControllerColorMapDto preset, string presetScope, int presetIndex)
    {
        ControllerColor[] entries = preset?.GetEntriesSortedById();
        if (entries == null)
        {
            return;
        }

        foreach (ControllerColor entry in entries)
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
