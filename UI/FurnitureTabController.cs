using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FurnitureTabController : MonoBehaviour
{
    [Header("Furniture Tab Elements")]
    [SerializeField] private GameObject furnitureContent;
    [SerializeField] private GameObject furnitureCardPrefab;
    [SerializeField] private ScrollRect furnitureScrollRect;
    [SerializeField] private bool debugMode = false;

    private InventoryCardManager cardManager;

    public GameObject FurnitureContent => furnitureContent;

    private void Awake()
    {
        EnsureCardManager();
        CacheScrollRect();
        ResetScrollPosition();
    }

    public void EnsureCardManager()
    {
        if (cardManager == null)
        {
            cardManager = GetComponent<InventoryCardManager>();
            if (cardManager == null)
            {
                cardManager = gameObject.AddComponent<InventoryCardManager>();
            }
        }

        cardManager.DebugMode = debugMode;
        cardManager.furnitureCardPrefab = furnitureCardPrefab;
        cardManager.Initialize(furnitureContent);
    }

    public void CacheScrollRect()
    {
        if (furnitureScrollRect == null)
        {
            furnitureScrollRect = FindScrollRect(furnitureContent);
        }
    }

    private ScrollRect FindScrollRect(GameObject target)
    {
        if (target == null)
            return null;

        return target.GetComponentInParent<ScrollRect>() ?? target.GetComponentInChildren<ScrollRect>();
    }

    public void ResetScrollPosition()
    {
        CacheScrollRect();

        if (furnitureScrollRect != null)
        {
            furnitureScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    public void RefreshFurnitureCards(List<InventoryItem> items)
    {
        EnsureCardManager();
        cardManager.RefreshFurnitureCards(items);
    }

    public void DeselectAll()
    {
        cardManager?.DeselectAll();
    }

    public void UpdateAllCraftableStatus()
    {
        cardManager?.UpdateAllCraftableStatus();
    }

    public InventoryItem GetSelectedItem()
    {
        return cardManager?.GetSelectedItem();
    }

    public void Cleanup()
    {
        cardManager?.Cleanup();
    }
}
