using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

public class FurnitureDataManager : MonoBehaviour
{
    private static FurnitureDataManager instance;
    public static FurnitureDataManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<FurnitureDataManager>();
            }
            return instance;
        }
    }

    [Header("ScriptableObject Database")]
    [SerializeField] private FurnitureDataSO[] furnitureDatabase;  // ScriptableObjectの配列
    [SerializeField] private MaterialDataSO[] materialDatabase;    // MaterialのScriptableObject配列

    // データ辞書（高速アクセス用）
    private Dictionary<string, FurnitureDataSO> furnitureDict = new Dictionary<string, FurnitureDataSO>();
    private Dictionary<string, MaterialDataSO> materialDict = new Dictionary<string, MaterialDataSO>();

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAllData();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu" && scene == SceneManager.GetActiveScene())
        {
            instance = null;
            Destroy(gameObject);
        }
    }

    void LoadAllData()
    {
        // ScriptableObjectデータベースの自動ロード（Editor限定）
        #if UNITY_EDITOR
        if (furnitureDatabase == null || furnitureDatabase.Length == 0)
        {
            LoadFurnitureSODatabase();
        }
        if (materialDatabase == null || materialDatabase.Length == 0)
        {
            LoadMaterialSODatabase();
        }
        #endif

        // 辞書に変換（高速アクセス用）
        BuildDictionaries();
    }

    #if UNITY_EDITOR
    void LoadFurnitureSODatabase()
    {
        // ResourcesフォルダからすべてのFurnitureDataSOを自動ロード
        furnitureDatabase = Resources.LoadAll<FurnitureDataSO>("Data/ScriptableObjects/Furniture");

        // またはAssetDatabaseを使用（エディタ限定）
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:FurnitureDataSO");
        List<FurnitureDataSO> dataList = new List<FurnitureDataSO>();

        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            FurnitureDataSO data = UnityEditor.AssetDatabase.LoadAssetAtPath<FurnitureDataSO>(path);
            if (data != null)
            {
                dataList.Add(data);
            }
        }

        furnitureDatabase = dataList.ToArray();
        Debug.Log($"Loaded {furnitureDatabase.Length} furniture ScriptableObjects");
    }

    void LoadMaterialSODatabase()
    {
        // MaterialDataSOも同様に
        materialDatabase = Resources.LoadAll<MaterialDataSO>("Data/ScriptableObjects/Materials");

        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:MaterialDataSO");
        List<MaterialDataSO> dataList = new List<MaterialDataSO>();

        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            MaterialDataSO data = UnityEditor.AssetDatabase.LoadAssetAtPath<MaterialDataSO>(path);
            if (data != null)
            {
                dataList.Add(data);
            }
        }

        materialDatabase = dataList.ToArray();
        Debug.Log($"Loaded {materialDatabase.Length} material ScriptableObjects");
    }
    #endif

    void BuildDictionaries()
    {
        // Furniture辞書構築
        furnitureDict.Clear();
        if (furnitureDatabase != null)
        {
            foreach (var data in furnitureDatabase)
            {
                if (data != null)
                {
                    // nameIDとitemIDの両方で検索可能にする
                    if (!string.IsNullOrEmpty(data.nameID))
                        furnitureDict[data.nameID] = data;
                    if (!string.IsNullOrEmpty(data.itemID) && data.itemID != data.nameID)
                        furnitureDict[data.itemID] = data;
                }
            }
        }

        // Material辞書構築
        materialDict.Clear();
        if (materialDatabase != null)
        {
            foreach (var data in materialDatabase)
            {
                if (data != null && !string.IsNullOrEmpty(data.materialID))
                {
                    materialDict[data.materialID] = data;
                }
            }
        }

        Debug.Log($"Dictionary built: {furnitureDict.Count} furniture, {materialDict.Count} materials");
    }

    // 旧FurnitureData形式への変換（互換性維持）
    public FurnitureData GetFurnitureData(string idOrName)
    {
        var so = GetFurnitureDataSO(idOrName);
        if (so == null) return null;

        return new FurnitureData
        {
            itemID = so.itemID,
            nameID = so.nameID,
            cozy = so.cozy,
            nature = so.nature,
            category = so.category,
            placementRules = so.placementRules,
            canStackOn = so.canStackOn,
            sellPrice = so.sellPrice,
            buyPrice = so.buyPrice,
            iconName = so.iconName,
            modelName = so.modelName,
            recipeID = so.recipeID,
            rarity = so.rarity,
            interactionType = so.interactionType,
            isMovable = so.isMovable,
            descriptionID = so.descriptionID,
            weatherAttribute = so.weatherAttribute,

            // レシピ情報を追加
            recipeMaterialIDs = so.recipeMaterialIDs,
            recipeMaterialQuantities = so.recipeMaterialQuantities,

            // ドロップ情報を追加
            dropMaterialIDs = so.dropMaterialIDs,
            dropRates = so.dropRates
        };
    }

    // ScriptableObjectを直接取得
    public FurnitureDataSO GetFurnitureDataSO(string idOrName)
    {
        if (string.IsNullOrEmpty(idOrName)) return null;

        if (furnitureDict.ContainsKey(idOrName))
            return furnitureDict[idOrName];

        // 辞書にない場合は線形検索（フォールバック）
        if (furnitureDatabase != null)
        {
            foreach (var data in furnitureDatabase)
            {
                if (data != null && (data.nameID == idOrName || data.itemID == idOrName))
                    return data;
            }
        }

        Debug.LogWarning($"FurnitureDataSO not found: {idOrName}");
        return null;
    }

    // Prefab取得
    public GameObject GetFurniturePrefab(string idOrName)
    {
        var so = GetFurnitureDataSO(idOrName);
        return so != null ? so.prefab : null;
    }

    // アイコン取得
    public Sprite GetFurnitureIcon(string idOrName)
    {
        var so = GetFurnitureDataSO(idOrName);
        return so != null ? so.icon : null;
    }

    // Material関連
    public MaterialData GetMaterialData(string materialID)
    {
        var so = GetMaterialDataSO(materialID);
        if (so == null) return null;

        return new MaterialData
        {
            materialID = so.materialID,
            materialName = so.materialName,
            category = so.category,
            maxStack = so.maxStack,
            sellPrice = so.sellPrice,
            iconName = so.iconName,
            rarity = so.rarity,
            descriptionID = so.descriptionID,
            weatherAttribute = so.weatherAttribute,
            sourceItems = so.sourceItems,
            dropRates = so.dropRates
        };
    }

    public MaterialDataSO GetMaterialDataSO(string materialID)
    {
        if (string.IsNullOrEmpty(materialID)) return null;

        if (materialDict.ContainsKey(materialID))
            return materialDict[materialID];

        Debug.LogWarning($"MaterialDataSO not found: {materialID}");
        return null;
    }

    // デバッグ用
    [ContextMenu("Log Database Status")]
    void LogDatabaseStatus()
    {
        Debug.Log("=== Database Status ===");
        Debug.Log($"Furniture ScriptableObjects: {furnitureDatabase?.Length ?? 0}");
        Debug.Log($"Material ScriptableObjects: {materialDatabase?.Length ?? 0}");
        Debug.Log($"Furniture Dictionary: {furnitureDict.Count}");
        Debug.Log($"Material Dictionary: {materialDict.Count}");

        if (furnitureDict.Count > 0)
        {
            Debug.Log("Available Furniture IDs:");
            foreach (var key in furnitureDict.Keys)
            {
                Debug.Log($"  - {key}");
            }
        }
    }

    [ContextMenu("Force Reload Database")]
    void ForceReloadDatabase()
    {
        LoadAllData();
        Debug.Log("Database reloaded!");
    }
}
