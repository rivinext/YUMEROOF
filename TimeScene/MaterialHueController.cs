using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MaterialHueController : MonoBehaviour
{
    [SerializeField] private HueSyncCoordinator hueCoordinator;
    [SerializeField] private MaterialPresetService presetService;
    [SerializeField] private MaterialPresetUIController presetUIController;
    [SerializeField] private int initialPresetIndex;

    [SerializeField] private List<MaterialHueBinding> materialBindings = new();

    private void Awake()
    {
        if (hueCoordinator == null)
        {
            hueCoordinator = GetComponent<HueSyncCoordinator>();
        }

        if (presetService == null)
        {
            presetService = GetComponent<MaterialPresetService>();
        }

        if (presetUIController == null)
        {
            presetUIController = GetComponent<MaterialPresetUIController>();
        }
    }

    private void Start()
    {
        if (materialBindings.Count > 0)
        {
            SetupMaterialBindings();
            return;
        }

        hueCoordinator?.InitializeSelectors();
        presetUIController?.Initialize(presetService, hueCoordinator != null ? new List<HueSyncCoordinator> { hueCoordinator } : new List<HueSyncCoordinator>(), initialPresetIndex);
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        hueCoordinator?.ApplyColor();
    }

    private void SetupMaterialBindings()
    {
        Dictionary<MaterialPresetUIController, PresetUIBindingGroup> presetUIGroups = new();

        foreach (MaterialHueBinding binding in materialBindings.Where(b => b != null))
        {
            HueSyncCoordinator coordinator = CreateCoordinator(binding);
            if (coordinator == null)
            {
                continue;
            }

            binding.ApplySessionKey(coordinator);
            coordinator.ConfigureSelectors(binding.EnumerateHueSelectors(), binding.EnumerateSaturationValuePalettes());
            coordinator.ConfigureTargets(binding.TargetMaterial, binding.TargetMaterials, binding.PreviewImage, binding.PreviewRawImage, binding.PreviewGraphics);
            coordinator.InitializeSelectors();

            MaterialPresetService bindingPresetService = binding.PresetService != null ? binding.PresetService : presetService;
            MaterialPresetUIController bindingPresetUI = binding.PresetUIController != null ? binding.PresetUIController : presetUIController;

            if (bindingPresetUI == null)
            {
                continue;
            }

            if (!presetUIGroups.TryGetValue(bindingPresetUI, out PresetUIBindingGroup group))
            {
                group = new PresetUIBindingGroup();
                presetUIGroups[bindingPresetUI] = group;
            }

            group.Service ??= bindingPresetService;
            group.Coordinators.Add(coordinator);

            if (!group.InitialPresetIndex.HasValue)
            {
                group.InitialPresetIndex = binding.InitialPresetIndex;
            }
        }

        foreach (KeyValuePair<MaterialPresetUIController, PresetUIBindingGroup> entry in presetUIGroups)
        {
            PresetUIBindingGroup group = entry.Value;
            int presetIndex = group.InitialPresetIndex ?? initialPresetIndex;
            entry.Key.Initialize(group.Service, group.Coordinators, presetIndex);
        }
    }

    private HueSyncCoordinator CreateCoordinator(MaterialHueBinding binding)
    {
        HueSyncCoordinator coordinator = null;

        if (binding.CoordinatorPrefab != null)
        {
            coordinator = Instantiate(binding.CoordinatorPrefab, transform);
        }
        else if (binding.CoordinatorInstance != null)
        {
            coordinator = binding.CoordinatorInstance;
        }
        else
        {
            GameObject coordinatorObject = new GameObject(string.IsNullOrEmpty(binding.SessionKey) ? "HueSyncCoordinator" : $"HueSyncCoordinator_{binding.SessionKey}");
            coordinatorObject.transform.SetParent(transform, false);
            coordinator = coordinatorObject.AddComponent<HueSyncCoordinator>();
        }

        return coordinator;
    }

    private class PresetUIBindingGroup
    {
        public MaterialPresetService Service;
        public readonly List<HueSyncCoordinator> Coordinators = new();
        public int? InitialPresetIndex;
    }
}

[System.Serializable]
public class MaterialHueBinding
{
    [SerializeField] private string sessionKey = string.Empty;
    [SerializeField] private HueSyncCoordinator coordinatorPrefab;
    [SerializeField] private HueSyncCoordinator coordinatorInstance;
    [SerializeField] private HueRingSelector hueRingSelector;
    [SerializeField] private List<HueRingSelector> hueRingSelectors = new();
    [SerializeField] private SaturationValuePalette saturationValuePalette;
    [SerializeField] private List<SaturationValuePalette> saturationValuePalettes = new();
    [SerializeField] private Material targetMaterial;
    [SerializeField] private List<Material> targetMaterials = new();
    [SerializeField] private UnityEngine.UI.Image previewImage;
    [SerializeField] private UnityEngine.UI.RawImage previewRawImage;
    [SerializeField] private List<UnityEngine.UI.Graphic> previewGraphics = new();
    [SerializeField] private MaterialPresetService presetService;
    [SerializeField] private MaterialPresetUIController presetUIController;
    [SerializeField] private int initialPresetIndex;

    public string SessionKey => sessionKey;
    public HueSyncCoordinator CoordinatorPrefab => coordinatorPrefab;
    public HueSyncCoordinator CoordinatorInstance => coordinatorInstance;
    public MaterialPresetService PresetService => presetService;
    public MaterialPresetUIController PresetUIController => presetUIController;
    public int InitialPresetIndex => initialPresetIndex;
    public Material TargetMaterial => targetMaterial;
    public List<Material> TargetMaterials => targetMaterials;
    public UnityEngine.UI.Image PreviewImage => previewImage;
    public UnityEngine.UI.RawImage PreviewRawImage => previewRawImage;
    public List<UnityEngine.UI.Graphic> PreviewGraphics => previewGraphics;

    public IEnumerable<HueRingSelector> EnumerateHueSelectors()
    {
        foreach (HueRingSelector selector in hueRingSelectors)
        {
            if (selector != null)
            {
                ApplySessionKey(selector);
                yield return selector;
            }
        }

        if (hueRingSelector != null && !hueRingSelectors.Contains(hueRingSelector))
        {
            ApplySessionKey(hueRingSelector);
            yield return hueRingSelector;
        }
    }

    public IEnumerable<SaturationValuePalette> EnumerateSaturationValuePalettes()
    {
        foreach (SaturationValuePalette palette in saturationValuePalettes)
        {
            if (palette != null)
            {
                ApplySessionKey(palette);
                yield return palette;
            }
        }

        if (saturationValuePalette != null && !saturationValuePalettes.Contains(saturationValuePalette))
        {
            ApplySessionKey(saturationValuePalette);
            yield return saturationValuePalette;
        }
    }

    public void ApplySessionKey(HueSyncCoordinator coordinator)
    {
        coordinator.SetSessionKey(sessionKey);
    }

    private void ApplySessionKey(HueRingSelector selector)
    {
        selector.SetSessionKey(sessionKey);
    }

    private void ApplySessionKey(SaturationValuePalette palette)
    {
        palette.SetSessionKey(sessionKey);
    }
}
