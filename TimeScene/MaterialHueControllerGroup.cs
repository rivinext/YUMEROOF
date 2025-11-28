using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MaterialHueControllerGroup : MonoBehaviour
{
    [SerializeField] private List<MaterialHueController> materialHueControllers = new();
    [SerializeField] private bool findControllersInChildren = true;

    private void Awake()
    {
        if (findControllersInChildren && materialHueControllers.Count == 0)
        {
            materialHueControllers = GetComponentsInChildren<MaterialHueController>(includeInactive: true).ToList();
        }
    }

    public void Register(MaterialHueController controller)
    {
        if (controller == null || materialHueControllers.Contains(controller))
        {
            return;
        }

        materialHueControllers.Add(controller);
    }

    public void LoadPreset(int presetIndex)
    {
        foreach (MaterialHueController controller in EnumerateControllers())
        {
            controller.LoadPreset(presetIndex);
        }
    }

    public void SavePreset(int presetIndex)
    {
        foreach (MaterialHueController controller in EnumerateControllers())
        {
            controller.SavePreset(presetIndex);
        }
    }

    public IReadOnlyList<MaterialHueController.ColorPreset> GetCurrentPresets()
    {
        return EnumerateControllers().Select(controller => controller.GetCurrentPreset()).ToList();
    }

    public IEnumerable<MaterialHueController> Controllers => EnumerateControllers();

    private IEnumerable<MaterialHueController> EnumerateControllers()
    {
        return materialHueControllers.Where(controller => controller != null);
    }
}
