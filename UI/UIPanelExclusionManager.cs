using UnityEngine;

/// <summary>
/// Ensures that key UI panels remain mutually exclusive by closing the others
/// when one panel is opened.
/// </summary>
public class UIPanelExclusionManager : MonoBehaviour
{
    private static UIPanelExclusionManager instance;

    public static UIPanelExclusionManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<UIPanelExclusionManager>();
            }

            return instance;
        }
    }

    [SerializeField] private SettingsPanelAnimator settingsPanel;
    [SerializeField] private CameraControlPanelAnimator cameraControlPanel;
    [SerializeField] private MilestonePanel milestonePanel;
    [SerializeField] private InventoryUI inventoryPanel;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public void Register(SettingsPanelAnimator panel)
    {
        if (panel != null)
        {
            settingsPanel = panel;
        }
    }

    public void Register(CameraControlPanelAnimator panel)
    {
        if (panel != null)
        {
            cameraControlPanel = panel;
        }
    }

    public void Register(MilestonePanel panel)
    {
        if (panel != null)
        {
            milestonePanel = panel;
        }
    }

    public void Register(InventoryUI panel)
    {
        if (panel != null)
        {
            inventoryPanel = panel;
        }
    }

    public void NotifyOpened(SettingsPanelAnimator panel)
    {
        Register(panel);
        CloseOtherPanels(panel);
    }

    public void NotifyOpened(CameraControlPanelAnimator panel)
    {
        Register(panel);
        CloseOtherPanels(panel);
    }

    public void NotifyOpened(MilestonePanel panel)
    {
        Register(panel);
        CloseOtherPanels(panel);
    }

    public void NotifyOpened(InventoryUI panel)
    {
        Register(panel);
        CloseOtherPanels(panel);
    }

    private void CloseOtherPanels(object openedPanel)
    {
        if (settingsPanel != null && openedPanel != settingsPanel && settingsPanel.IsOpen)
        {
            settingsPanel.ClosePanel();
        }

        if (cameraControlPanel != null && openedPanel != cameraControlPanel && cameraControlPanel.IsOpen)
        {
            cameraControlPanel.ClosePanel();
        }

        if (milestonePanel != null && openedPanel != milestonePanel && milestonePanel.IsOpen)
        {
            milestonePanel.ClosePanel();
        }

        if (inventoryPanel != null && openedPanel != inventoryPanel && inventoryPanel.IsOpen)
        {
            inventoryPanel.CloseInventory();
        }
    }
}
