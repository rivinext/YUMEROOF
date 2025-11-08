using UnityEngine;

// MaterialDataのScriptableObject版
[CreateAssetMenu(fileName = "MaterialData", menuName = "YumeRoof/MaterialData")]
public class MaterialDataSO : ScriptableObject
{
    [Header("基本情報")]
    public string materialID;              // 素材ID
    public string nameID;                  // 名前ID
    public string materialName;            // 素材名
    public string category;                // カテゴリ

    [Header("スタックと価格")]
    public int maxStack = 99;              // 最大スタック数
    public int sellPrice;                  // 売却価格

    [Header("リソース名")]
    public string iconName;                // アイコン名

    [Header("表示設定")]
    public Rarity rarity;                  // レアリティ
    public string descriptionID;           // 説明文ID
    public WeatherAttribute weatherAttribute; // 天候属性

    [Header("ドロップ情報")]
    public string[] sourceItems;           // ドロップ元の家具ID
    public float[] dropRates;              // ドロップ率

    [Header("アセット参照（手動設定）")]
    public Sprite icon;                    // 2Dアイコン
}
