using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Logs the furniture items currently displayed in the Furniture tab.
/// </summary>
[RequireComponent(typeof(Button))]
public class FurnitureTabLogger : MonoBehaviour
{
    [SerializeField] private GameObject furnitureContent; // Container holding InventoryItemCard components
    [SerializeField] private Button logButton;             // Button to trigger logging

    void Awake()
    {
        if (logButton == null)
        {
            logButton = GetComponent<Button>();
        }

        EnsureFurnitureContentAssigned();
    }

    void OnEnable()
    {
        EnsureFurnitureContentAssigned();
    }

    void Start()
    {
        logButton.onClick.AddListener(DumpItems);
    }

    /// <summary>
    /// Ensures the furniture content reference is assigned, even after scene changes.
    /// </summary>
    private void EnsureFurnitureContentAssigned()
    {
        if (furnitureContent != null)
        {
            return;
        }

        var inventoryUI = FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Include);
        if (inventoryUI != null &&
            inventoryUI.furnitureTabController != null &&
            inventoryUI.furnitureTabController.FurnitureContent != null)
        {
            furnitureContent = inventoryUI.furnitureTabController.FurnitureContent;
            return;
        }

        Debug.LogWarning("FurnitureTabLogger could not locate the furniture content container.");
    }

    /// <summary>
    /// Dumps all InventoryItemCard items within the furniture content to the console.
    /// </summary>
    public void DumpItems()
    {
        EnsureFurnitureContentAssigned();

        if (furnitureContent == null)
        {
            Debug.LogWarning("Furniture content is not assigned.");
            return;
        }

        var cards = furnitureContent.GetComponentsInChildren<InventoryItemCard>(true);
        foreach (var card in cards)
        {
            var item = card.currentItem;
            if (item != null)
            {
                Debug.Log($"{item.itemID} x{item.quantity}");
            }
        }
    }

    /// <summary>
    /// Returns the IDs of all items currently displayed in the furniture tab.
    /// </summary>
    public IEnumerable<string> GetCurrentItemIds()
    {
        if (furnitureContent == null)
        {
            yield break;
        }

        var cards = furnitureContent.GetComponentsInChildren<InventoryItemCard>(true);
        foreach (var card in cards)
        {
            var item = card.currentItem;
            if (item != null)
            {
                yield return item.itemID;
            }
        }
    }
}
