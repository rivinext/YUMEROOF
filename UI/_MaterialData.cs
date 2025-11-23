using UnityEngine;
using System;

// 素材データクラス
[Serializable]
public class MaterialData
{
    public string materialID;          // 素材ID（M001など）
    public string nameID;              // 名前ID
    public string materialName;        // 素材名
    public string category;            // カテゴリ（木材、石材、植物など）
    public int maxStack = 99;          // 最大スタック数
    public int sellPrice;              // 売却価格
    public string iconName;            // 2Dアイコン画像名（リソース名）
    public Rarity rarity;              // レアリティ
    public string descriptionID;       // 説明文ID
    public WeatherAttribute weatherAttribute; // 天候属性（風/雨）

    // ドロップ元情報（どの家具から入手できるか）
    public string[] sourceItems;       // ドロップ元の家具ID
    public float[] dropRates;          // ドロップ率
}

// インベントリアイテムのインターフェース
public interface IInventoryItem
{
    string GetID();
    string GetName();
    string GetIconName();
    Rarity GetRarity();
    int GetSellPrice();
    string GetCategory();
    bool IsStackable();
    int GetMaxStack();
}
