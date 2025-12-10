// ========== _FurnitureDataSO.cs の修正版 ==========
using UnityEngine;

// FurnitureDataのScriptableObject版
[CreateAssetMenu(fileName = "FurnitureData", menuName = "YumeRoof/FurnitureData")]
public class FurnitureDataSO : ScriptableObject
{
    [Header("基本情報")]
    public string itemID;                  // アイテムID（番号）
    public string nameID;                  // 名前ID
    public int cozy;                       // Cozy値
    public int nature;                     // Nature値
    public string category;                // カテゴリ

    [Header("配置設定")]
    public PlacementRule placementRules;   // 配置ルール
    public bool canStackOn;                // 上に物を置けるか
    public bool isMovable;                 // 移動可能か

    [Header("価格とレアリティ")]
    public int sellPrice;                  // 売却価格
    public int buyPrice;                   // 購入価格
    public Rarity rarity;                  // レアリティ

    [Header("リソース名")]
    public string iconName;                // 2Dアイコン名
    public string modelName;               // 3Dモデル名

    [Header("ゲームプレイ")]
    public string recipeID;                // レシピID（互換性のため残す）
    public string unlockCondition;         // アンロック条件
    public InteractionType interactionType; // インタラクションタイプ
    public string descriptionID;           // 説明ID
    public WeatherAttribute weatherAttribute; // 天候属性

    [Header("レシピ情報")]
    public string[] recipeMaterialIDs = new string[3];     // レシピに必要な素材ID
    public int[] recipeMaterialQuantities = new int[3];    // レシピに必要な素材数量

    [Header("ドロップ情報")]
    public string[] dropMaterialIDs = new string[3];       // ドロップする素材ID
    public float[] dropRates = new float[3];               // 各素材のドロップ率

    [Header("アセット参照（手動設定）")]
    public GameObject prefab;              // 3Dモデルのプレハブ
    public Sprite icon;                    // 2Dアイコン

    // ヘルパープロパティ
    public bool HasRecipe => recipeMaterialIDs != null && recipeMaterialIDs.Length > 0 && !string.IsNullOrEmpty(recipeMaterialIDs[0]);
    public bool CanDropMaterial => dropMaterialIDs != null && dropMaterialIDs.Length > 0 && !string.IsNullOrEmpty(dropMaterialIDs[0]);
}
