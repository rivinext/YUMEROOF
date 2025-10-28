using UnityEngine;

[CreateAssetMenu(fileName = "Material_", menuName = "Game/Material")]
public class MaterialSO : ScriptableObject
{
    [Header("Basic Info")]
    public string MaterialID;
    public string MaterialNameID;  // ローカライズキー（MaterialNamesテーブル用）
    public string DescriptionID;   // 説明のローカライズキー（DescNamesテーブル用）

    [Header("Properties")]
    public string Category;
    public int MaxStack = 99;
    public int SellPrice;
    public string Rarity;

    [Header("Visual")]
    public Sprite IconSprite;  // IconName から変更

    [Header("Attributes")]
    public float WeatherAttribute;

    [Header("Source Info")]
    public string[] SourceItems;  // どのアイテムからドロップするか
    public float[] DropRates;     // ドロップ率
}
