using UnityEngine;
using TMPro;

/// <summary>
/// Item card used in the shop sell tab.
/// Shares elements with <see cref="InventoryItemCard"/> and adds a sell price text.
/// </summary>
public class InventoryItemCardSell : InventoryItemCard
{
    [Header("Sell Elements")]
    public TMP_Text sellPriceText;

    /// <summary>
    /// Assign item data and update sell price display.
    /// </summary>
    /// <param name="item">Inventory item to represent.</param>
    /// <param name="isMaterial">Whether the item is a material.</param>
    public new void SetItem(InventoryItem item, bool isMaterial)
    {
        base.SetItem(item, isMaterial);

        var furnitureData = FurnitureDataManager.Instance?.GetFurnitureData(item.itemID);
        if (furnitureData != null && furnitureData.interactionType == InteractionType.Bed && backgroundImage != null)
        {
            backgroundImage.sprite = uncraftableBackground;
        }

        UpdateSellPrice();
    }

    void UpdateSellPrice()
    {
        if (sellPriceText == null || currentItem == null) return;

        int price = 0;

        switch (currentItem.itemType)
        {
            case InventoryItem.ItemType.Furniture:
                var furnitureSO = FurnitureDataManager.Instance?.GetFurnitureDataSO(currentItem.itemID);
                if (furnitureSO != null)
                    price = furnitureSO.sellPrice;
                break;
            case InventoryItem.ItemType.Material:
                var materialSO = FurnitureDataManager.Instance?.GetMaterialDataSO(currentItem.itemID);
                if (materialSO != null)
                    price = materialSO.sellPrice;
                break;
        }

        sellPriceText.text = price.ToString();
    }
}
