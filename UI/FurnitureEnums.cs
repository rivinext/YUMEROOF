using UnityEngine;

// 配置ルール
public enum PlacementRule
{
    Floor,      // 床のみ
    Wall,       // 壁のみ
    Ceiling,    // 天井のみ
    Both        // 床と壁の両方
}

// 天候属性
public enum WeatherAttribute
{
    None,       // 属性なし
    Wind,       // 風
    Rain        // 雨
}

// インタラクションタイプ
public enum InteractionType
{
    None,           // なし
    Workbench,      // ワークベンチ（クラフト）
    Bed,            // ベッド（睡眠）
    Collectable,    // 素材回収可能
    Sit             // 椅子に座る
}

// レアリティ
public enum Rarity
{
    Common,
    Uncommon,
    Rare
}
