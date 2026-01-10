using UnityEngine;

public class OpeningPanelOnceController : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;

    void Start()
    {
        bool shouldShow = GameSessionInitializer.LastLoadCreatedNewSave;
        SetPanelVisible(shouldShow);
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
}
