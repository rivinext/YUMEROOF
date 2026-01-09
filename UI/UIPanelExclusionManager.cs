using UnityEngine;
using UnityEngine.SceneManagement;

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
    [SerializeField] private CameraControlPanel cameraControlPanel;
    [SerializeField] private MilestonePanel milestonePanel;
    [SerializeField] private InventoryUI inventoryPanel;
    [SerializeField] private WardrobeUIController wardrobePanel;
    [SerializeField] private ColorPanelController colorPanel;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        RegisterScenePanels();
        CloseAllRegisteredPanels();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RegisterScenePanels();
        CloseAllRegisteredPanels();
    }

    public void Register(SettingsPanelAnimator panel)
    {
        if (panel != null)
        {
            settingsPanel = panel;
        }
    }

    public void Register(CameraControlPanel panel)
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

    public void Register(WardrobeUIController panel)
    {
        if (panel != null)
        {
            wardrobePanel = panel;
        }
    }

    public void Register(ColorPanelController panel)
    {
        if (panel != null)
        {
            colorPanel = panel;
        }
    }

    private void RegisterScenePanels()
    {
        settingsPanel = FindAnyObjectByType<SettingsPanelAnimator>(FindObjectsInactive.Include);
        cameraControlPanel = FindAnyObjectByType<CameraControlPanel>(FindObjectsInactive.Include);
        milestonePanel = FindAnyObjectByType<MilestonePanel>(FindObjectsInactive.Include);
        inventoryPanel = FindAnyObjectByType<InventoryUI>(FindObjectsInactive.Include);
        wardrobePanel = FindAnyObjectByType<WardrobeUIController>(FindObjectsInactive.Include);
        colorPanel = FindAnyObjectByType<ColorPanelController>(FindObjectsInactive.Include);
    }

    public void NotifyOpened(SettingsPanelAnimator panel)
    {
        Register(panel);
        CloseOtherPanels(panel);
    }

    public void NotifyOpened(CameraControlPanel panel)
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

    public void NotifyOpened(WardrobeUIController panel)
    {
        Register(panel);
        CloseOtherPanels(panel);
    }

    public void NotifyOpened(ColorPanelController panel)
    {
        Register(panel);
        CloseOtherPanels(panel);
    }

    private void CloseOtherPanels(UnityEngine.Object openedPanel)
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

        if (wardrobePanel != null && openedPanel != wardrobePanel && wardrobePanel.IsShown)
        {
            wardrobePanel.HidePanel();
        }

        if (colorPanel != null && openedPanel != colorPanel && colorPanel.IsOpen)
        {
            colorPanel.ClosePanel();
        }
    }

    private void CloseAllRegisteredPanels()
    {
        if (settingsPanel != null)
        {
            settingsPanel.ClosePanel();
        }

        if (cameraControlPanel != null)
        {
            cameraControlPanel.ClosePanel();
        }

        if (milestonePanel != null)
        {
            milestonePanel.ClosePanel();
        }

        if (inventoryPanel != null)
        {
            inventoryPanel.CloseInventory();
        }

        if (wardrobePanel != null)
        {
            wardrobePanel.HidePanel();
        }

        if (colorPanel != null)
        {
            colorPanel.ClosePanel();
        }
    }
}
