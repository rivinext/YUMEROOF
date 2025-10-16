using UnityEngine;
using TMPro;

/// <summary>
/// Item card used in the shop purchase tab.
/// Shares most elements with <see cref="InventoryItemCard"/> and adds a purchase price text.
/// </summary>
public class InventoryItemCardPurchase : InventoryItemCard
{
    [Header("Purchase Elements")]
    public TMP_Text purchasePriceText;

    /// <summary>
    /// Assign item data and update purchase price display.
    /// </summary>
    /// <param name="item">Inventory item to represent.</param>
    public new void SetItem(InventoryItem item)
    {
        base.SetItem(item, false);
        UpdatePurchasePrice();
    }

    void UpdatePurchasePrice()
    {
        if (purchasePriceText == null || currentItem == null) return;

        var data = FurnitureDataManager.Instance?.GetFurnitureData(currentItem.itemID);
        if (data != null)
        {
            purchasePriceText.text = data.buyPrice.ToString();
        }
    }
}
