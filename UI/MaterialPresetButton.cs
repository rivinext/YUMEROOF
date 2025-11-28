using UnityEngine;

public class MaterialPresetButton : MonoBehaviour
{
    [SerializeField] private MaterialHueController materialHueController;
    [SerializeField] private int presetIndex;
    [SerializeField] private bool allowLoad = true;
    [SerializeField] private bool allowSave;
    [Header("Optional preset data")]
    [SerializeField] private Color presetColor = Color.white;
    [SerializeField] private bool applyPresetColorToController;

    public void LoadPreset()
    {
        if (!allowLoad || materialHueController == null)
        {
            return;
        }

        materialHueController.LoadPreset(presetIndex);
    }

    public void SavePreset()
    {
        if (!allowSave || materialHueController == null)
        {
            return;
        }

        materialHueController.SavePreset(presetIndex);
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
        if (!applyPresetColorToController || materialHueController == null)
        {
            return;
        }

        MaterialHueController.ColorPreset preset = MaterialHueController.ColorPreset.FromColor(presetColor);
        materialHueController.SetBuiltInPreset(presetIndex, preset);
    }
}
