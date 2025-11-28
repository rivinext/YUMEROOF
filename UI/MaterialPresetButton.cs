using UnityEngine;

public class MaterialPresetButton : MonoBehaviour
{
    [SerializeField] private MaterialHueController materialHueController;
    [SerializeField] private MaterialHueController[] extraControllers = System.Array.Empty<MaterialHueController>();
    [SerializeField] private MaterialHueControllerGroup controllerGroup;
    [SerializeField] private int presetIndex;
    [SerializeField] private bool allowLoad = true;
    [SerializeField] private bool allowSave;
    [Header("Optional preset data")]
    [SerializeField] private Color presetColor = Color.white;
    [SerializeField] private bool applyPresetColorToController;

    public void LoadPreset()
    {
        if (!allowLoad)
        {
            return;
        }

        if (TryLoadThroughGroup())
        {
            return;
        }

        foreach (MaterialHueController controller in EnumerateControllers())
        {
            controller.LoadPreset(presetIndex);
        }
    }

    public void SavePreset()
    {
        if (!allowSave)
        {
            return;
        }

        if (TrySaveThroughGroup())
        {
            return;
        }

        foreach (MaterialHueController controller in EnumerateControllers())
        {
            controller.SavePreset(presetIndex);
        }
    }

    private void Awake()
    {
        ApplyPresetData();
    }

    private void OnValidate()
    {
        ApplyPresetData();
    }

    private void ApplyPresetData()
    {
        if (!applyPresetColorToController)
        {
            return;
        }

        MaterialHueController.ColorPreset preset = MaterialHueController.ColorPreset.FromColor(presetColor);
        foreach (MaterialHueController controller in EnumerateControllers())
        {
            controller.SetBuiltInPreset(presetIndex, preset);
        }
    }

    private bool TryLoadThroughGroup()
    {
        if (controllerGroup == null)
        {
            return false;
        }

        controllerGroup.LoadPreset(presetIndex);
        return true;
    }

    private bool TrySaveThroughGroup()
    {
        if (controllerGroup == null)
        {
            return false;
        }

        controllerGroup.SavePreset(presetIndex);
        return true;
    }

    private System.Collections.Generic.IEnumerable<MaterialHueController> EnumerateControllers()
    {
        System.Collections.Generic.HashSet<MaterialHueController> uniqueControllers = new();

        if (materialHueController != null)
        {
            uniqueControllers.Add(materialHueController);
        }

        if (extraControllers != null)
        {
            foreach (MaterialHueController controller in extraControllers)
            {
                if (controller != null)
                {
                    uniqueControllers.Add(controller);
                }
            }
        }

        if (controllerGroup != null)
        {
            foreach (MaterialHueController controller in controllerGroup.Controllers)
            {
                if (controller != null)
                {
                    uniqueControllers.Add(controller);
                }
            }
        }

        foreach (MaterialHueController controller in uniqueControllers)
        {
            yield return controller;
        }
    }
}
