using UnityEngine;

// 配置システムのテスト用スクリプト
// FreePlacementSystemと同じGameObjectにアタッチして使用
public class PlacementTestController : MonoBehaviour
{
    private FreePlacementSystem placementSystem;

    [Header("Test Furniture Prefabs")]
    public GameObject tablePrefab;  // テーブルのプレハブ
    public GameObject chairPrefab;  // 椅子のプレハブ
    public GameObject plantPrefab;  // 植物のプレハブ

    void Start()
    {
        placementSystem = GetComponent<FreePlacementSystem>();

        if (placementSystem == null)
        {
            Debug.LogError("FreePlacementSystem not found!");
        }
    }

    void Update()
    {
        // テスト用のキー入力
        // 1キー: テーブルを配置
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            PlaceTestFurniture(tablePrefab, "Table01", true);
        }

        // 2キー: 椅子を配置
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            PlaceTestFurniture(chairPrefab, "Chair01", false);
        }

        // 3キー: 植物を配置
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            PlaceTestFurniture(plantPrefab, "Plant01", false);
        }
    }

    void PlaceTestFurniture(GameObject prefab, string nameID, bool canStackOn)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"Prefab for {nameID} is not assigned!");
            return;
        }

        // テスト用の家具データを作成
        FurnitureData testData = new FurnitureData
        {
            itemID = nameID,
            nameID = nameID,
            cozy = 10,
            nature = 5,
            category = "家具",
            placementRules = PlacementRule.Floor,
            canStackOn = canStackOn,  // テーブルは上に物を置ける
            sellPrice = 100,
            iconName = "icon_" + nameID.ToLower(),
            modelName = nameID,
            recipeID = "",
            rarity = Rarity.Common,
            interactionType = InteractionType.None,
            isMovable = true,
            descriptionID = "DESC_" + nameID,
            weatherAttribute = WeatherAttribute.None
        };

        placementSystem.StartPlacingNewFurniture(prefab, testData);
        Debug.Log($"Started placing {nameID}. Click to place, Q/E to rotate, ESC to cancel.");
    }
}
