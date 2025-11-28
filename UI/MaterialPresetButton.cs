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
        if (selectOnly)
        {
            SelectPresetOnly();
        }
        else
        {
            LoadPreset();
        }
    }

    public void SelectPresetOnly()
    {
        if (!TryGetController(out MaterialHueController controller))
        {
            return;
        }

        controller.SelectPreset(presetIndex);
    }

    public void LoadPreset()
    {
        if (!TryGetController(out MaterialHueController controller))
        {
            return;
        }

        controller.SelectPreset(presetIndex);
        controller.LoadPreset(presetIndex);
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

    private bool TryGetController(out MaterialHueController controller)
    {
        if (hueController == null)
        {
            Debug.LogWarning($"{nameof(MaterialPresetButton)} on {name} is missing a reference to {nameof(MaterialHueController)}.");
            controller = null;
            return false;
        }

        controller = hueController;
        return true;
    }
}
