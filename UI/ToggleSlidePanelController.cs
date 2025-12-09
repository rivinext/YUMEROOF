using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ToggleSlidePanelController : MonoBehaviour
{
    [SerializeField] private Toggle toggle;
    [SerializeField] private UISlidePanel slidePanel;
    [SerializeField] private Object exclusionTarget;

    private void Start()
    {
        if (toggle == null || slidePanel == null)
        {
            return;
        }

        ApplyToggleState(toggle.isOn);
        toggle.onValueChanged.AddListener(HandleToggleValueChanged);
    }

    private void OnDestroy()
    {
        if (toggle != null)
        {
            toggle.onValueChanged.RemoveListener(HandleToggleValueChanged);
        }
    }

    private void HandleToggleValueChanged(bool isOn)
    {
        ApplyToggleState(isOn);
    }

    private void ApplyToggleState(bool isOn)
    {
        if (slidePanel == null)
        {
            return;
        }

        if (isOn)
        {
            slidePanel.SlideIn();
            NotifyExclusionManager();
        }
        else
        {
            slidePanel.SlideOut();
        }
    }

    private void NotifyExclusionManager()
    {
        if (exclusionTarget == null)
        {
            return;
        }

        var manager = UIPanelExclusionManager.Instance;
        if (manager == null)
        {
            return;
        }

        switch (exclusionTarget)
        {
            case SettingsPanelAnimator settings:
                manager.NotifyOpened(settings);
                break;
            case CameraControlPanel camera:
                manager.NotifyOpened(camera);
                break;
            case MilestonePanel milestone:
                manager.NotifyOpened(milestone);
                break;
            case InventoryUI inventory:
                manager.NotifyOpened(inventory);
                break;
            case WardrobeUIController wardrobe:
                manager.NotifyOpened(wardrobe);
                break;
            case ColorPanelController colorPanel:
                manager.NotifyOpened(colorPanel);
                break;
        }
    }
}
