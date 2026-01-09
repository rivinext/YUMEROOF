using UnityEngine;
using System;

// インベントリ内のアイテムを表すクラス
[Serializable]
public class InventoryItem
{
    public enum ItemType
    {
        Furniture,
        Material
    }

    public ItemType itemType;          // アイテムタイプ
    public string itemID;               // アイテムID
    public int quantity;                // 所持数
    public int maxStack;                // 最大スタック数（素材のみ）
    public bool isFavorite;             // お気に入り
    public bool isUnlocked = true;     // レシピ解放済みか（家具のみ）
    public bool canCraft = false;      // クラフト可能か（材料が揃っているか）

    // コンストラクタ
    public InventoryItem(ItemType type, string id, int count = 1, int maxStack = 0)
    {
        itemType = type;
        itemID = id;
        quantity = count;
        this.maxStack = maxStack;
        isFavorite = false;
        isUnlocked = true;
        canCraft = false;
    }

    // 数量を追加
    public void AddQuantity(int amount)
    {
        quantity += amount;

        // 最大スタック数チェック
        if (itemType == ItemType.Material)
        {
            if (maxStack > 0)
            {
                quantity = Mathf.Min(quantity, maxStack);
            }
        }
    }

    // 数量を減らす
    public bool RemoveQuantity(int amount)
    {
        if (quantity >= amount)
        {
            quantity -= amount;
            return true;
        }
        return false;
    }

    // お気に入り切り替え
    public void ToggleFavorite()
    {
        isFavorite = !isFavorite;
    }
}
