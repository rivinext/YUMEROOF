using UnityEngine;

public class MaterialPresetSelectButton : MonoBehaviour
{
    [SerializeField] private MaterialHueController materialHueController;
    [SerializeField] private int presetIndex;

    public void SelectPreset()
    {
        if (materialHueController == null)
        {
            return;
        }

        materialHueController.SelectPreset(presetIndex);
    }
}
