using UnityEngine;

public class ExclusivePanelCoordinator : MonoBehaviour
{
    public enum PanelType
    {
        Settings,
        CameraControl,
        Milestone,
        Inventory
    }

    public static ExclusivePanelCoordinator Instance { get; private set; }

    [SerializeField] private SettingsPanelAnimator settingsPanel;
    [SerializeField] private CameraControlPanelAnimator cameraControlPanel;
    [SerializeField] private MilestonePanel milestonePanel;
    [SerializeField] private InventoryUI inventoryPanel;

    private bool suppressNotifications;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureCoordinatorExists()
    {
        if (Instance != null)
        {
            return;
        }

        var coordinatorObject = new GameObject(nameof(ExclusivePanelCoordinator));
        DontDestroyOnLoad(coordinatorObject);
        coordinatorObject.AddComponent<ExclusivePanelCoordinator>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void NotifyPanelOpened(PanelType panelType)
    {
        if (suppressNotifications)
        {
            return;
        }

        EnsurePanelReferences();

        suppressNotifications = true;
        try
        {
            if (panelType != PanelType.Settings)
            {
                CloseSettingsPanel();
            }

            if (panelType != PanelType.CameraControl)
            {
                CloseCameraControlPanel();
            }

            if (panelType != PanelType.Milestone)
            {
                CloseMilestonePanel();
            }

            if (panelType != PanelType.Inventory)
            {
                CloseInventoryPanel();
            }
        }
        finally
        {
            suppressNotifications = false;
        }
    }

    public void NotifyPanelClosed(PanelType panelType)
    {
        if (suppressNotifications)
        {
            return;
        }

        EnsurePanelReferences();
    }

    private void CloseSettingsPanel()
    {
        if (settingsPanel != null && settingsPanel.IsOpen)
        {
            settingsPanel.ClosePanel();
        }
    }

    private void CloseCameraControlPanel()
    {
        if (cameraControlPanel != null && cameraControlPanel.IsOpen)
        {
            cameraControlPanel.ClosePanel();
        }
    }

    private void CloseMilestonePanel()
    {
        if (milestonePanel != null && milestonePanel.IsOpen)
        {
            milestonePanel.ClosePanel();
        }
    }

    private void CloseInventoryPanel()
    {
        if (inventoryPanel != null && inventoryPanel.IsInventoryOpen)
        {
            inventoryPanel.CloseInventory();
        }
    }

    private void EnsurePanelReferences()
    {
        if (settingsPanel == null)
        {
            settingsPanel = FindObjectOfType<SettingsPanelAnimator>(true);
        }

        if (cameraControlPanel == null)
        {
            cameraControlPanel = FindObjectOfType<CameraControlPanelAnimator>(true);
        }

        if (milestonePanel == null)
        {
            milestonePanel = FindObjectOfType<MilestonePanel>(true);
        }

        if (inventoryPanel == null)
        {
            inventoryPanel = FindObjectOfType<InventoryUI>(true);
        }
    }
}
