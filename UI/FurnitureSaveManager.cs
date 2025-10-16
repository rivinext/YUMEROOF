using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

// 家具配置データの永続化管理クラス（修正版）
public class FurnitureSaveManager : MonoBehaviour
{
    public static FurnitureSaveManager instance;
    private static bool isQuitting = false;

    public static FurnitureSaveManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<FurnitureSaveManager>();
                if (instance == null)
                {
                    if (isQuitting) return null;
                    GameObject go = new GameObject("FurnitureSaveManager");
                    instance = go.AddComponent<FurnitureSaveManager>();
                }
            }
            return instance;
        }
    }

    [Header("Settings")]
    public bool debugMode = true;  // デバッグモードをデフォルトONに

    // 保存用データクラス
    [System.Serializable]
    public class FurnitureSaveData
    {
        public string furnitureID;
        public string sceneName;
        public float posX, posY, posZ;
        public float rotX, rotY, rotZ, rotW;
        public string parentFurnitureID; // 親家具のユニークID（スタック配置用）
        public string uniqueID; // このオブジェクト固有のID

        public FurnitureSaveData(string id, string scene, Vector3 pos, Quaternion rot, string parentID = "", string uid = "")
        {
            furnitureID = id;
            sceneName = scene;
            posX = pos.x; posY = pos.y; posZ = pos.z;
            rotX = rot.x; rotY = rot.y; rotZ = rot.z; rotW = rot.w;
            parentFurnitureID = parentID;
            uniqueID = string.IsNullOrEmpty(uid) ? System.Guid.NewGuid().ToString() : uid;
        }

        public Vector3 GetPosition() => new Vector3(posX, posY, posZ);
        public Quaternion GetRotation() => new Quaternion(rotX, rotY, rotZ, rotW);
    }

    // 全シーンの家具データを保持
    [System.Serializable]
    public class AllFurnitureData
    {
        public List<FurnitureSaveData> furnitureList = new List<FurnitureSaveData>();
    }

    private AllFurnitureData allFurnitureData = new AllFurnitureData();
    private Dictionary<string, GameObject> loadedFurnitureObjects = new Dictionary<string, GameObject>();
    private bool isLoadingScene = false; // シーンロード中フラグ

    public event Action OnFurnitureChanged;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // データは外部から読み込まれる
            Debug.Log("[FurnitureSave] Manager initialized");
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        // シーン遷移時のイベント登録
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (debugMode)
            Debug.Log("[FurnitureSave] Scene loaded event registered");

        // 現在のシーンに合わせて家具を読み込む
        var currentScene = SceneManager.GetActiveScene().name;
        if (currentScene != "MainMenu")
        {
            StartCoroutine(LoadFurnitureDelayed(currentScene));
        }
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnApplicationQuit()
    {
        isQuitting = true;
    }

    void OnDestroy()
    {
        isQuitting = true;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu")
        {
            DestroyLoadedFurniture();
            instance = null;
            Destroy(gameObject);
            return;
        }

        if (debugMode)
            Debug.Log($"[FurnitureSave] Scene loaded: {scene.name}");

        // 少し遅延してから家具を復元（シーンの初期化を待つ）
        StartCoroutine(LoadFurnitureDelayed(scene.name));
    }

    private void DestroyLoadedFurniture()
    {
        foreach (var obj in loadedFurnitureObjects.Values)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        loadedFurnitureObjects.Clear();
    }

    System.Collections.IEnumerator LoadFurnitureDelayed(string sceneName)
    {
        // 1フレーム待機（シーンの初期化完了を待つ）
        yield return null;

        isLoadingScene = true;
        LoadFurnitureForScene(sceneName);
        isLoadingScene = false;
    }

    // 家具を保存（配置/移動時に呼ばれる）
    public void SaveFurniture(PlacedFurniture furniture, string sceneName)
    {
        if (furniture == null || isLoadingScene) return;

        SaveFurnitureRecursive(furniture, sceneName);
        OnFurnitureChanged?.Invoke();
    }

    void SaveFurnitureRecursive(PlacedFurniture furniture, string scene)
    {
        SaveSingleFurniture(furniture, scene);
        foreach (var child in furniture.childFurnitures)
        {
            if (child != null)
                SaveFurnitureRecursive(child, scene);
        }
    }

    void SaveSingleFurniture(PlacedFurniture furniture, string scene)
    {
        string uniqueID = GetOrCreateUniqueID(furniture);
        string parentID = "";

        // 親家具がある場合はそのIDを取得
        if (furniture.parentFurniture != null)
        {
            parentID = GetOrCreateUniqueID(furniture.parentFurniture);
        }

        // 既存データを検索して更新または新規追加
        var existingData = allFurnitureData.furnitureList.FirstOrDefault(f => f.uniqueID == uniqueID);

        if (existingData != null)
        {
            // 更新
            existingData.posX = furniture.transform.position.x;
            existingData.posY = furniture.transform.position.y;
            existingData.posZ = furniture.transform.position.z;
            existingData.rotX = furniture.transform.rotation.x;
            existingData.rotY = furniture.transform.rotation.y;
            existingData.rotZ = furniture.transform.rotation.z;
            existingData.rotW = furniture.transform.rotation.w;
            existingData.parentFurnitureID = parentID;
        }
        else
        {
            // 新規追加
            FurnitureSaveData newData = new FurnitureSaveData(
                furniture.furnitureData.nameID,
                scene,
                furniture.transform.position,
                furniture.transform.rotation,
                parentID,
                uniqueID
            );
            allFurnitureData.furnitureList.Add(newData);
        }

        if (debugMode)
            Debug.Log($"[FurnitureSave] Saved: {furniture.furnitureData.nameID} at {furniture.transform.position} in {scene}");
    }

    // 家具を削除（インベントリに戻す時）
    public void RemoveFurniture(PlacedFurniture furniture)
    {
        if (furniture == null) return;

        string uniqueID = GetOrCreateUniqueID(furniture);

        // このオブジェクトと子オブジェクトを削除
        allFurnitureData.furnitureList.RemoveAll(f => f.uniqueID == uniqueID || f.parentFurnitureID == uniqueID);

        OnFurnitureChanged?.Invoke();

        if (debugMode)
            Debug.Log($"[FurnitureSave] Removed: {furniture.furnitureData.nameID}");
    }

    // 家具を削除（ユニークID指定）
    public void RemoveFurnitureByID(string uniqueID)
    {
        if (string.IsNullOrEmpty(uniqueID)) return;

        allFurnitureData.furnitureList.RemoveAll(f => f.uniqueID == uniqueID || f.parentFurnitureID == uniqueID);

        OnFurnitureChanged?.Invoke();

        if (debugMode)
            Debug.Log($"[FurnitureSave] Removed UID: {uniqueID}");
    }

    // シーンの全家具をクリア
    public void ClearSceneFurniture(string sceneName)
    {
        allFurnitureData.furnitureList.RemoveAll(f => f.sceneName == sceneName);
        OnFurnitureChanged?.Invoke();

        if (debugMode)
            Debug.Log($"[FurnitureSave] Cleared all furniture in {sceneName}");
    }

    // 全シーンの家具データを取得
    public List<FurnitureSaveData> GetAllFurniture()
    {
        return new List<FurnitureSaveData>(allFurnitureData.furnitureList);
    }

    // 指定したシーンの家具データを取得
    public List<FurnitureSaveData> GetFurnitureByScene(string sceneName)
    {
        return allFurnitureData.furnitureList
            .Where(f => f.sceneName == sceneName)
            .ToList();
    }

    public int GetPlacedFurnitureCount(IEnumerable<string> scenes)
    {
        if (scenes == null) return 0;
        var sceneSet = new HashSet<string>(scenes);
        return allFurnitureData.furnitureList.Count(f => sceneSet.Contains(f.sceneName));
    }

    public string SaveFurnitureData()
    {
        return JsonUtility.ToJson(allFurnitureData);
    }

    public void LoadFromData(string json)
    {
        if (!string.IsNullOrEmpty(json))
        {
            allFurnitureData = JsonUtility.FromJson<AllFurnitureData>(json);
        }
        else
        {
            allFurnitureData = new AllFurnitureData();
        }
        OnFurnitureChanged?.Invoke();
    }

    // 特定のシーンの家具を読み込んで配置
    void LoadFurnitureForScene(string sceneName)
    {
        // シーンに配置システムがあるか確認
        FreePlacementSystem placementSystem = FindFirstObjectByType<FreePlacementSystem>();
        if (placementSystem == null)
        {
            if (debugMode)
                Debug.Log($"[FurnitureSave] No FreePlacementSystem in {sceneName}, skip loading furniture");
            return;
        }

        // 既存の読み込み済みオブジェクトを破棄してクリア
        DestroyLoadedFurniture();

        // このシーンの家具データを取得
        var sceneFurniture = allFurnitureData.furnitureList
            .Where(f => f.sceneName == sceneName)
            .ToList();

        if (debugMode)
            Debug.Log($"[FurnitureSave] Loading {sceneFurniture.Count} furniture items for {sceneName}");

        // 親子関係を考慮しながら家具を配置
        var pending = new List<FurnitureSaveData>(sceneFurniture);
        int iterations = 0;
        int maxIterations = pending.Count * 4;

        while (pending.Count > 0 && (maxIterations <= 0 || iterations < maxIterations))
        {
            int processedThisPass = 0;

            for (int i = pending.Count - 1; i >= 0; i--)
            {
                var data = pending[i];
                bool parentLoaded = string.IsNullOrEmpty(data.parentFurnitureID) ||
                    loadedFurnitureObjects.ContainsKey(data.parentFurnitureID);

                if (!parentLoaded)
                    continue;

                LoadSingleFurniture(data, placementSystem);
                pending.RemoveAt(i);
                processedThisPass++;
            }

            if (processedThisPass == 0)
                break;

            iterations++;
        }

        if (pending.Count > 0)
        {
            if (debugMode)
            {
                Debug.LogWarning($"[FurnitureSave] Unable to resolve parents for {pending.Count} furniture items in {sceneName}. Loading without parent constraints.");
            }

            foreach (var data in pending)
            {
                LoadSingleFurniture(data, placementSystem);
            }
        }

        if (sceneFurniture.Count > 0 && debugMode)
            Debug.Log($"[FurnitureSave] Successfully loaded {loadedFurnitureObjects.Count} furniture objects");
    }

    // 単一の家具を読み込んで配置
    void LoadSingleFurniture(FurnitureSaveData data, FreePlacementSystem placementSystem)
    {
        // FurnitureDataManagerから家具データとプレハブを取得
        FurnitureDataManager dataManager = FurnitureDataManager.Instance;
        if (dataManager == null)
        {
            Debug.LogError("[FurnitureSave] FurnitureDataManager.Instance is null!");
            return;
        }

        // プレハブを取得（FurnitureDataManagerのメソッドを使用）
        GameObject furniturePrefab = dataManager.GetFurniturePrefab(data.furnitureID);

        if (furniturePrefab == null)
        {
            Debug.LogWarning($"[FurnitureSave] Prefab not found for {data.furnitureID}");
            return;
        }

        // 家具を生成
        GameObject furnitureObj = Instantiate(furniturePrefab);
        furnitureObj.transform.position = data.GetPosition();
        furnitureObj.transform.rotation = data.GetRotation();

        // PlacedFurnitureコンポーネントを確認/追加
        PlacedFurniture placedFurniture = furnitureObj.GetComponent<PlacedFurniture>();
        if (placedFurniture == null)
        {
            placedFurniture = furnitureObj.AddComponent<PlacedFurniture>();
        }

        // FurnitureDataを設定
        placedFurniture.furnitureData = dataManager.GetFurnitureData(data.furnitureID);

        if (placedFurniture.furnitureData == null)
        {
            Debug.LogWarning($"[FurnitureSave] FurnitureData not found for {data.furnitureID}");
            Destroy(furnitureObj);
            return;
        }

        if (placedFurniture.furnitureData.interactionType == InteractionType.Sit &&
            furnitureObj.GetComponent<SitTrigger>() == null)
        {
            furnitureObj.AddComponent<SitTrigger>();
        }

        // ユニークIDを設定
        SetUniqueID(placedFurniture, data.uniqueID);

        // レイヤーを設定
        SetLayerRecursively(furnitureObj, LayerMask.NameToLayer("Furniture"));

        // コライダーの設定を確認
        Collider[] colliders = furnitureObj.GetComponentsInChildren<Collider>();
        if (colliders.Length == 0)
        {
            BoxCollider boxCollider = furnitureObj.AddComponent<BoxCollider>();
            if (debugMode)
                Debug.Log($"[FurnitureSave] Added BoxCollider to {data.furnitureID}");
        }

        // 読み込んだ家具にもコーナーマーカーを付与
        if (placementSystem != null)
        {
            placementSystem.CreateCornerMarkers(placedFurniture);
        }

        // 読み込み済みリストに追加
        loadedFurnitureObjects[data.uniqueID] = furnitureObj;

        // 親家具の設定（親が既に読み込まれている場合）
        if (!string.IsNullOrEmpty(data.parentFurnitureID) && loadedFurnitureObjects.ContainsKey(data.parentFurnitureID))
        {
            GameObject parentObj = loadedFurnitureObjects[data.parentFurnitureID];
            PlacedFurniture parentFurniture = parentObj.GetComponent<PlacedFurniture>();
            if (parentFurniture != null)
            {
                placedFurniture.SetParentFurniture(parentFurniture);
            }
        }

        if (debugMode)
            Debug.Log($"[FurnitureSave] Loaded {data.furnitureID} at {data.GetPosition()}");
    }

    // レイヤーを再帰的に設定
    void SetLayerRecursively(GameObject obj, int layer)
    {
        int anchorLayer = LayerMask.NameToLayer("Anchor");
        SetLayerRecursively(obj, layer, anchorLayer);
    }

    void SetLayerRecursively(GameObject obj, int targetLayer, int anchorLayer)
    {
        bool isAnchorNode = (anchorLayer >= 0 && (obj.layer == anchorLayer || obj.GetComponent<AnchorPoint>() != null));

        int appliedLayer = isAnchorNode ? anchorLayer : targetLayer;
        obj.layer = appliedLayer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, appliedLayer, anchorLayer);
        }
    }

    // ユニークIDの取得または作成
    string GetOrCreateUniqueID(PlacedFurniture furniture)
    {
        // GameObjectの名前にIDが含まれているか確認
        string objName = furniture.gameObject.name;
        if (objName.Contains("_UID_"))
        {
            int index = objName.IndexOf("_UID_");
            return objName.Substring(index + 5);
        }

        // なければ新規作成
        string newID = System.Guid.NewGuid().ToString();
        furniture.gameObject.name = $"{furniture.furnitureData.nameID}_UID_{newID}";
        return newID;
    }

    // ユニークIDを設定
    void SetUniqueID(PlacedFurniture furniture, string uniqueID)
    {
        furniture.gameObject.name = $"{furniture.furnitureData.nameID}_UID_{uniqueID}";
    }

    // デバッグ用：全データをクリア
    [ContextMenu("Clear All Save Data")]
    public void ClearAllSaveData()
    {
        // シーン上の配置済み家具を全て破棄
        DestroyLoadedFurniture();

        allFurnitureData = new AllFurnitureData();
        OnFurnitureChanged?.Invoke();
        Debug.Log("[FurnitureSave] All save data cleared");
    }

    // デバッグ用：現在のデータを表示
    [ContextMenu("Show Current Save Data")]
    public void ShowCurrentSaveData()
    {
        Debug.Log($"[FurnitureSave] Total furniture items: {allFurnitureData.furnitureList.Count}");

        var grouped = allFurnitureData.furnitureList.GroupBy(f => f.sceneName);
        foreach (var group in grouped)
        {
            Debug.Log($"  Scene '{group.Key}': {group.Count()} items");
            foreach (var item in group)
            {
                Debug.Log($"    - {item.furnitureID} at ({item.posX:F2}, {item.posY:F2}, {item.posZ:F2})");
            }
        }
    }

    // デバッグ用：現在のシーンの家具を手動で保存
    [ContextMenu("Save Current Scene Furniture")]
    public void SaveCurrentSceneFurniture()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        PlacedFurniture[] allFurniture = FindObjectsOfType<PlacedFurniture>();

        Debug.Log($"[FurnitureSave] Found {allFurniture.Length} furniture in {currentScene}");

        foreach (var furniture in allFurniture)
        {
            SaveFurniture(furniture, currentScene);
        }

        Debug.Log("[FurnitureSave] Manual save completed");
    }

    // デバッグ用：現在のシーンの家具を手動で読み込み
    [ContextMenu("Load Current Scene Furniture")]
    public void LoadCurrentSceneFurniture()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        LoadFurnitureForScene(currentScene);
    }
}
