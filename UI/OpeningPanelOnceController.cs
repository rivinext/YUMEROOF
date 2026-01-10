using UnityEngine;
using UnityEngine.UI;

public class OpeningPanelOnceController : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;

    void OnEnable()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }
    }

    void OnDisable()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
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

    public void ShowIfNewSave(bool createdNewSave)
    {
        SetPanelVisible(createdNewSave);
    }

    void SetPanelVisible(bool isVisible)
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(isVisible);
        }
    }
}
