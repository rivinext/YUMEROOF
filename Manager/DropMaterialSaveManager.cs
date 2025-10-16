using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persists dropped material objects across scene loads. Stores drop
/// information per scene and respawns drops when scenes are loaded.
/// </summary>
public class DropMaterialSaveManager : MonoBehaviour
{
    public static DropMaterialSaveManager Instance { get; private set; }

    [System.Serializable]
    public class DropMaterialSaveData
    {
        public string materialID;
        public Vector3 position;
        public string anchorID;
    }

    [System.Serializable]
    private class SceneDropCollection
    {
        public string sceneName;
        public List<DropMaterialSaveData> drops = new List<DropMaterialSaveData>();
    }

    [System.Serializable]
    private class SaveWrapper
    {
        public List<SceneDropCollection> scenes = new List<SceneDropCollection>();
    }

    private Dictionary<string, List<DropMaterialSaveData>> dropsByScene = new();
    private GameObject dropPrefab;
    private const string PREF_KEY = "DropMaterialSave";
    [SerializeField] private string dropPrefabPath = "Materials/DropMaterial";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += HandleSceneLoaded;
        dropPrefab = Resources.Load<GameObject>(dropPrefabPath);
        if (dropPrefab != null)
        {
            Debug.Log($"[DropMaterialSaveManager] Loaded drop prefab from Resources/{dropPrefabPath}.");
        }
        else
        {
            Debug.LogWarning($"[DropMaterialSaveManager] Failed to load drop prefab at Resources/{dropPrefabPath}.");
        }
        LoadFromPrefs();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            Instance = null;
        }
    }

    void OnApplicationQuit()
    {
        SaveToPrefs();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu")
        {
            if (Instance == this) Instance = null;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            Destroy(gameObject);
            return;
        }
        SpawnDropsForScene(scene.name);
    }

    public bool IsDropRegistered(string sceneName, string materialID, Vector3 position, string anchorID = null)
    {
        return dropsByScene.TryGetValue(sceneName, out var list) &&
               list.Exists(d => d.materialID == materialID &&
                                (!string.IsNullOrEmpty(anchorID)
                                    ? d.anchorID == anchorID
                                    : (d.position - position).sqrMagnitude < 0.01f));
    }

    public void RegisterDrop(string sceneName, string materialID, Vector3 position, string anchorID = null)
    {
        if (!dropsByScene.TryGetValue(sceneName, out var list))
        {
            list = new List<DropMaterialSaveData>();
            dropsByScene[sceneName] = list;
            Debug.Log($"[DropMaterialSaveManager] Created drop list for scene {sceneName}.");
        }

        if (list.Exists(d => d.materialID == materialID &&
                              (!string.IsNullOrEmpty(anchorID)
                                  ? d.anchorID == anchorID
                                  : (d.position - position).sqrMagnitude < 0.01f)))
        {
            Debug.Log($"[DropMaterialSaveManager] Drop already registered for scene {sceneName} (material {materialID}). Skipping.");
            return;
        }

        list.Add(new DropMaterialSaveData { materialID = materialID, position = position, anchorID = anchorID });
        Debug.Log($"[DropMaterialSaveManager] Registered drop {materialID} at {position} (anchor: {anchorID}) for scene {sceneName}.");
    }

    public void RemoveDrop(string sceneName, string materialID, Vector3 position, string anchorID = null)
    {
        if (!dropsByScene.TryGetValue(sceneName, out var list)) return;
        int removed = list.RemoveAll(d => d.materialID == materialID &&
                              (!string.IsNullOrEmpty(anchorID)
                                  ? d.anchorID == anchorID
                                  : (d.position - position).sqrMagnitude < 0.01f));
        if (removed > 0)
        {
            Debug.Log($"[DropMaterialSaveManager] Removed {removed} drop(s) of {materialID} from scene {sceneName}.");
        }
    }

    /// <summary>
    /// Clears all registered drops and destroys any existing drop objects in
    /// the currently loaded scenes. Used when progressing to a new day so
    /// uncollected materials do not persist across days.
    /// </summary>
    public void ClearAllDrops()
    {
        // Destroy any drop objects present in the active scene(s)
        foreach (var drop in FindObjectsOfType<DropMaterial>())
        {
            Destroy(drop.gameObject);
        }

        // Remove all saved drop information
        int totalDrops = 0;
        foreach (var kvp in dropsByScene)
        {
            totalDrops += kvp.Value.Count;
        }
        Debug.Log($"[DropMaterialSaveManager] Clearing all drops. Removing {totalDrops} recorded entries across {dropsByScene.Count} scene(s).");
        dropsByScene.Clear();

        // Persist the cleared state so nothing respawns until new drops are generated
        SaveToPrefs();
    }

    private void SpawnDropsForScene(string sceneName)
    {
        if (dropPrefab == null)
        {
            Debug.LogWarning("[DropMaterialSaveManager] Cannot spawn drops because drop prefab is missing.");
            return;
        }
        if (!dropsByScene.TryGetValue(sceneName, out var list))
        {
            Debug.Log($"[DropMaterialSaveManager] No drops registered for scene {sceneName}. Nothing to spawn.");
            return;
        }

        foreach (var data in list)
        {
            GameObject obj = Instantiate(dropPrefab, data.position, Quaternion.identity);
            var comp = obj.GetComponent<DropMaterial>();
            if (comp != null)
            {
                comp.MaterialID = data.materialID;
                comp.AnchorID = data.anchorID;
            }
            Debug.Log($"[DropMaterialSaveManager] Spawned drop {data.materialID} at {data.position} in scene {sceneName}.");
        }
    }

    public void SaveToPrefs()
    {
        SaveWrapper wrapper = new SaveWrapper();
        foreach (var kvp in dropsByScene)
        {
            wrapper.scenes.Add(new SceneDropCollection
            {
                sceneName = kvp.Key,
                drops = new List<DropMaterialSaveData>(kvp.Value)
            });
        }
        string json = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString(PREF_KEY, json);
        PlayerPrefs.Save();
        Debug.Log($"[DropMaterialSaveManager] Saved {wrapper.scenes.Count} scene(s) of drop data to PlayerPrefs.");
    }

    private void LoadFromPrefs()
    {
        string json = PlayerPrefs.GetString(PREF_KEY, "");
        if (string.IsNullOrEmpty(json))
        {
            Debug.Log("[DropMaterialSaveManager] No saved drop data found in PlayerPrefs.");
            return;
        }
        SaveWrapper wrapper = JsonUtility.FromJson<SaveWrapper>(json);
        dropsByScene.Clear();
        foreach (var sceneData in wrapper.scenes)
        {
            dropsByScene[sceneData.sceneName] = sceneData.drops;
        }
        Debug.Log($"[DropMaterialSaveManager] Loaded drop data for {wrapper.scenes.Count} scene(s) from PlayerPrefs.");
    }
}
