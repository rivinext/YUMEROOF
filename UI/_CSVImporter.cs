using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// CSV to ScriptableObject 変換用のEditorウィンドウ
public class CSVImporter : EditorWindow
{
    private TextAsset furnitureCSV;
    private TextAsset materialCSV;
    private string outputPath = "Assets/Data/ScriptableObjects/";

    [MenuItem("Tools/Yume Roof/CSV Importer")]
    public static void ShowWindow()
    {
        GetWindow<CSVImporter>("CSV Importer");
    }

    void OnGUI()
    {
        GUILayout.Label("CSV to ScriptableObject Converter", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        // CSV ファイル選択
        furnitureCSV = EditorGUILayout.ObjectField("Furniture CSV", furnitureCSV, typeof(TextAsset), false) as TextAsset;
        materialCSV = EditorGUILayout.ObjectField("Material CSV", materialCSV, typeof(TextAsset), false) as TextAsset;

        EditorGUILayout.Space();

        // 出力パス
        EditorGUILayout.LabelField("Output Path:");
        outputPath = EditorGUILayout.TextField(outputPath);

        EditorGUILayout.Space();

        // インポートボタン
        if (GUILayout.Button("Import Furniture Data", GUILayout.Height(30)))
        {
            ImportFurnitureData();
        }

        if (GUILayout.Button("Import Material Data", GUILayout.Height(30)))
        {
            ImportMaterialData();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Import All", GUILayout.Height(40)))
        {
            ImportFurnitureData();
            ImportMaterialData();
        }
    }

    void ImportFurnitureData()
    {
        if (furnitureCSV == null)
        {
            Debug.LogError("Furniture CSV file not assigned!");
            return;
        }

        // 出力フォルダ作成
        string furniturePath = outputPath + "Furniture/";
        if (!Directory.Exists(furniturePath))
        {
            Directory.CreateDirectory(furniturePath);
        }

        string[] lines = furnitureCSV.text.Split('\n');
        int importCount = 0;

        // ヘッダー行をスキップ
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] values = ParseCSVLine(lines[i]);
            // Ensure the CSV line has enough columns including BuyPrice
            if (values.Length < 26) continue; // 列数を更新

            // ScriptableObject作成
            FurnitureDataSO data = ScriptableObject.CreateInstance<FurnitureDataSO>();

            // 基本データ設定
            data.itemID = values[0];  // ItemID
            data.nameID = values[1];  // NameID
            data.cozy = ParseInt(values[2]);
            data.nature = ParseInt(values[3]);
            data.category = values[4];
            data.placementRules = ParsePlacementRule(values[5]);

            float surfaceType = ParseFloat(values[6]);
            data.canStackOn = (surfaceType > 0);

            data.sellPrice = ParseInt(values[7]);
            // BuyPrice is located after an empty column in the CSV, so use index 25
            data.buyPrice = (values.Length > 25) ? ParseInt(values[25]) : data.sellPrice;
            data.iconName = values[8];  // 2DImage
            data.modelName = values[9];  // 3DModel
            data.recipeID = values[10]; // Recipe ID（互換性のため残す）
            data.unlockCondition = values[11];
            data.rarity = ParseRarity(values[12]);
            data.interactionType = ParseInteractionType(values[13]);
            data.isMovable = ParseBool(values[14]);
            data.descriptionID = values[15];

            // ドロップ情報の処理
            data.dropMaterialIDs = new string[3];
            data.dropMaterialIDs[0] = values[16]; // DropMaterialID1
            data.dropMaterialIDs[1] = values[17]; // DropMaterialID2
            data.dropMaterialIDs[2] = values[18]; // DropMaterialID3

            // DropRateの処理（カンマ区切り）
            if (!string.IsNullOrEmpty(values[19]))
            {
                string[] dropRateStrs = values[19].Split(',');
                data.dropRates = new float[3];
                for (int j = 0; j < Mathf.Min(3, dropRateStrs.Length); j++)
                {
                    data.dropRates[j] = ParseFloat(dropRateStrs[j].Trim());
                }
            }
            else
            {
                data.dropRates = new float[3];
            }

            // レシピ情報の処理
            data.recipeMaterialIDs = new string[3];
            data.recipeMaterialIDs[0] = values[20]; // RecipeMaterialID1
            data.recipeMaterialIDs[1] = values[21]; // RecipeMaterialID2
            data.recipeMaterialIDs[2] = values[22]; // RecipeMaterialID3

            // Quantityの処理（カンマ区切り）
            if (!string.IsNullOrEmpty(values[23]))
            {
                string[] quantityStrs = values[23].Split(',');
                data.recipeMaterialQuantities = new int[3];
                for (int j = 0; j < Mathf.Min(3, quantityStrs.Length); j++)
                {
                    data.recipeMaterialQuantities[j] = ParseInt(quantityStrs[j].Trim());
                }
            }
            else
            {
                data.recipeMaterialQuantities = new int[3];
            }

            // アセットとして保存
            string assetPath = furniturePath + data.nameID + ".asset";
            AssetDatabase.CreateAsset(data, assetPath);
            importCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Successfully imported {importCount} furniture items!");
    }

    void ImportMaterialData()
    {
        if (materialCSV == null)
        {
            Debug.LogError("Material CSV file not assigned!");
            return;
        }

        // 出力フォルダ作成
        string materialPath = outputPath + "Materials/";
        if (!Directory.Exists(materialPath))
        {
            Directory.CreateDirectory(materialPath);
        }

        string[] lines = materialCSV.text.Split('\n');
        int importCount = 0;

        // ヘッダー行をスキップ
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] values = ParseCSVLine(lines[i]);
            if (values.Length < 9) continue;

            // ScriptableObject作成
            MaterialDataSO data = ScriptableObject.CreateInstance<MaterialDataSO>();

            // データ設定
            data.materialID = values[0];
            data.nameID = values[1];
            data.materialName = values[2];
            data.category = values[3];
            data.maxStack = ParseInt(values[4]);
            data.sellPrice = ParseInt(values[5]);
            data.iconName = values[6];
            data.rarity = ParseRarity(values[7]);
            data.descriptionID = values[8];

            // WeatherAttributeがある場合の処理
            if (values.Length > 9)
            {
                data.weatherAttribute = ParseWeatherAttribute(values[9]);
            }

            // sourceItemsの処理
            if (values.Length > 10 && !string.IsNullOrEmpty(values[10]))
            {
                data.sourceItems = values[10].Split(',');
            }
            else
            {
                data.sourceItems = new string[0];
            }

            // dropRatesの処理
            if (values.Length > 11 && !string.IsNullOrEmpty(values[11]))
            {
                string[] dropRateStrs = values[11].Split(',');
                data.dropRates = new float[dropRateStrs.Length];
                for (int j = 0; j < dropRateStrs.Length; j++)
                {
                    data.dropRates[j] = ParseFloat(dropRateStrs[j].Trim());
                }
            }
            else
            {
                data.dropRates = new float[0];
            }

            // アセットとして保存
            string assetPath = materialPath + data.materialID + ".asset";
            AssetDatabase.CreateAsset(data, assetPath);
            importCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Successfully imported {importCount} material items!");
    }

    // CSVパース関数（カンマ区切り、引用符対応）
    string[] ParseCSVLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        string currentField = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(currentField.Trim());
                currentField = "";
            }
            else
            {
                currentField += c;
            }
        }

        result.Add(currentField.Trim());
        return result.ToArray();
    }

    // 各種パース関数
    int ParseInt(string value)
    {
        int result;
        if (int.TryParse(value, out result))
            return result;
        return 0;
    }

    float ParseFloat(string value)
    {
        float result;
        if (float.TryParse(value, out result))
            return result;
        return 0f;
    }

    bool ParseBool(string value)
    {
        return value.ToLower() == "true" || value == "1";
    }

    PlacementRule ParsePlacementRule(string value)
    {
        switch (value.ToLower())
        {
            case "floor":
                return PlacementRule.Floor;
            case "wall":
                return PlacementRule.Wall;
            case "ceiling":
                return PlacementRule.Ceiling;
            case "both":
                return PlacementRule.Both;
            default:
                return PlacementRule.Floor;
        }
    }

    Rarity ParseRarity(string value)
    {
        switch (value.ToLower())
        {
            case "common":
                return Rarity.Common;
            case "uncommon":
                return Rarity.Uncommon;
            case "rare":
                return Rarity.Rare;
            default:
                return Rarity.Common;
        }
    }

    InteractionType ParseInteractionType(string value)
    {
        switch (value.ToLower())
        {
            case "workbench":
            case "craft":
                return InteractionType.Workbench;
            case "bed":
            case "sleep":
                return InteractionType.Bed;
            case "collectable":
            case "collect":
                return InteractionType.Collectable;
            case "sit":
                return InteractionType.Sit;
            default:
                return InteractionType.None;
        }
    }

    WeatherAttribute ParseWeatherAttribute(string value)
    {
        switch (value.ToLower())
        {
            case "wind":
                return WeatherAttribute.Wind;
            case "rain":
                return WeatherAttribute.Rain;
            default:
                return WeatherAttribute.None;
        }
    }
}
