// ========== _FurnitureData.cs の修正版 ==========
using UnityEngine;
using System;

// 家具データクラス
[Serializable]
public class FurnitureData
{
    public string itemID;                  // アイテムID
    public string nameID;                  // 名前ID
    public int cozy;                       // Cozy値
    public int nature;                     // Nature値
    public string category;                // カテゴリ
    public PlacementRule placementRules;   // 配置ルール
    public bool canStackOn;                // 上に物を置けるか（SurfaceType）
    public int sellPrice;                  // 売却価格
    public int buyPrice;                   // 購入価格
    public string iconName;                // アイコン名
    public string modelName;               // モデル名
    public string recipeID;                // レシピID（削除予定）
    public Rarity rarity;                  // レアリティ
    public InteractionType interactionType; // インタラクションタイプ
    public bool isMovable;                 // 移動可能か
    public string descriptionID;           // 説明ID
    public WeatherAttribute weatherAttribute; // 天候属性（風/雨）

    // レシピ情報（新規追加）
    public string[] recipeMaterialIDs;     // レシピに必要な素材ID（最大3つ）
    public int[] recipeMaterialQuantities; // レシピに必要な素材数量（最大3つ）

    // ドロップ設定（修正）
    public string[] dropMaterialIDs;       // ドロップする素材ID（最大3つ）
    public float[] dropRates;              // 各素材のドロップ率（独立確率）

    // ヘルパープロパティ
    public bool HasRecipe => recipeMaterialIDs != null && recipeMaterialIDs.Length > 0 && !string.IsNullOrEmpty(recipeMaterialIDs[0]);
    public bool CanDropMaterial => dropMaterialIDs != null && dropMaterialIDs.Length > 0 && !string.IsNullOrEmpty(dropMaterialIDs[0]);

    // 互換性のため（削除予定）
    public bool canDropMaterial => CanDropMaterial;
}
