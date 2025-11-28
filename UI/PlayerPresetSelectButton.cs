using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class PlayerPresetSelectButton : MonoBehaviour
{
    [SerializeField] private MaterialHueController hueController;
    [SerializeField] private int presetIndex = -1;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (button != null)
        {
            button.onClick.AddListener(HandleClick);
        }
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }
    }

    private void HandleClick()
    {
        if (hueController == null)
        {
            Debug.LogWarning($"{nameof(PlayerPresetSelectButton)} on {name} is missing a reference to {nameof(MaterialHueController)}.");
            return;
        }

        hueController.SelectPreset(presetIndex);
    }

    public void SetHueController(MaterialHueController controller)
    {
        hueController = controller;
    }

    public void SetPresetIndex(int index)
    {
        presetIndex = index;
    }
}
