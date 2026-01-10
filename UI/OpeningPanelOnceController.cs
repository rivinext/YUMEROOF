using UnityEngine;
using UnityEngine.UI;

public class OpeningPanelOnceController : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;
    [SerializeField] private UISlidePanel targetSlidePanel;
    private bool hasShown;

    void Start()
    {
        SetPanelVisible(false);
    }

    void OnEnable()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }

        if (targetSlidePanel != null)
        {
            targetSlidePanel.OnSlideOutStarting += HandleSlideOutStarting;
        }
    }

    void OnDisable()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
        }

        if (targetSlidePanel != null)
        {
            targetSlidePanel.OnSlideOutStarting -= HandleSlideOutStarting;
        }
    }

    public void ClosePanel()
    {
        var saveGameManager = SaveGameManager.Instance;
        saveGameManager.HasSeenOpeningPanel = true;
        var slotKey = saveGameManager.CurrentSlotKey;
        if (!string.IsNullOrEmpty(slotKey))
        {
            saveGameManager.Save(slotKey);
        }

        SetPanelVisible(false);
    }

    void SetPanelVisible(bool isVisible)
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(isVisible);
            return;
        }

        gameObject.SetActive(isVisible);
    }

    void HandleSlideOutStarting()
    {
        if (hasShown)
            return;

        if (!GameSessionInitializer.LastLoadCreatedNewSave)
            return;

        hasShown = true;
        SetPanelVisible(true);
    }
}
