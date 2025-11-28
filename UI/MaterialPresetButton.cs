using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class MaterialPresetButton : MonoBehaviour
{
    [SerializeField] private MaterialHueController hueController;
    [SerializeField] private int presetIndex = -1;
    [SerializeField] private bool selectOnly;

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

    public void HandleClick()
    {
        if (hueController == null)
        {
            Debug.LogWarning($"{nameof(MaterialPresetButton)} on {name} is missing a reference to {nameof(MaterialHueController)}.");
            return;
        }

        hueController.SelectPreset(presetIndex);

        if (selectOnly)
        {
            return;
        }

        hueController.LoadPreset(presetIndex);
    }

    public void SetSelectOnly(bool value)
    {
        selectOnly = value;
    }

    public void SetPresetIndex(int index)
    {
        presetIndex = index;
    }

    public void SetController(MaterialHueController controller)
    {
        hueController = controller;
    }
}
