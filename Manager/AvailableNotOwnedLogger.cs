using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Logs items that are unlockable but not currently owned.
/// </summary>
public class AvailableNotOwnedLogger : MonoBehaviour
{
    [SerializeField] private FurnitureTabLogger furnitureTabLogger;
    [SerializeField] private UnlockItemConsole unlockItemConsole;
    [SerializeField] private Button logButton;

    private void Awake()
    {
        if (logButton == null)
        {
            logButton = GetComponent<Button>();
        }

        if (furnitureTabLogger == null)
        {
            furnitureTabLogger = FindFirstObjectByType<FurnitureTabLogger>(FindObjectsInactive.Include);
            if (furnitureTabLogger == null)
            {
                Debug.LogWarning("FurnitureTabLogger dependency could not be found.");
            }
        }

        if (unlockItemConsole == null)
        {
            unlockItemConsole = FindFirstObjectByType<UnlockItemConsole>(FindObjectsInactive.Include);
            if (unlockItemConsole == null)
            {
                Debug.LogWarning("UnlockItemConsole dependency could not be found.");
            }
        }
    }

    private void OnEnable()
    {
        if (logButton != null)
        {
            logButton.onClick.AddListener(LogNewItems);
        }
    }

    private void OnDisable()
    {
        if (logButton != null)
        {
            logButton.onClick.RemoveListener(LogNewItems);
        }
    }

    /// <summary>
    /// Calculates the difference between unlockable and owned items and returns the result.
    /// </summary>
    public IEnumerable<string> GetNewItemIds()
    {
        if (furnitureTabLogger == null || unlockItemConsole == null)
        {
            Debug.LogWarning("Dependencies are not assigned.");
            return Enumerable.Empty<string>();
        }

        HashSet<string> owned = new HashSet<string>(furnitureTabLogger.GetCurrentItemIds());
        IEnumerable<string> available = unlockItemConsole.GetUnlockableItemIds();

        return available.Where(id => !owned.Contains(id));
    }

    /// <summary>
    /// Logs the difference between unlockable and owned items.
    /// </summary>
    public void LogNewItems()
    {
        bool found = false;
        foreach (string id in GetNewItemIds())
        {
            Debug.Log(id);
            found = true;
        }

        if (!found)
        {
            Debug.Log("No new items available.");
        }
    }
}
