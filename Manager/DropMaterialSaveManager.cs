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

    public static DropMaterialSaveManager CreateIfNeeded()
    {
        if (Instance != null)
        {
            return Instance;
        }

        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            return null;
        }

        var existing = FindFirstObjectByType<DropMaterialSaveManager>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        var go = new GameObject("DropMaterialSaveManager");
        return go.AddComponent<DropMaterialSaveManager>();
    }

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
        LoadFromPrefs();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            // Persist drop information before tearing down the singleton so the
            // next session (or scene reload) can restore any remaining drops.
            SaveToPrefs();

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
            if (Instance == this)
            {
                // Persist any registered drops so that returning to the game
                // scene can restore them, then tear down the singleton.
                SaveToPrefs();
                SceneManager.sceneLoaded -= HandleSceneLoaded;
                Instance = null;
            }
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
        }

        if (list.Exists(d => d.materialID == materialID &&
                              (!string.IsNullOrEmpty(anchorID)
                                  ? d.anchorID == anchorID
                                  : (d.position - position).sqrMagnitude < 0.01f)))
        {
            return;
        }

        list.Add(new DropMaterialSaveData { materialID = materialID, position = position, anchorID = anchorID });
    }

    public void RemoveDrop(string sceneName, string materialID, Vector3 position, string anchorID = null)
    {
        if (!dropsByScene.TryGetValue(sceneName, out var list)) return;
        list.RemoveAll(d => d.materialID == materialID &&
                              (!string.IsNullOrEmpty(anchorID)
                                  ? d.anchorID == anchorID
                                  : (d.position - position).sqrMagnitude < 0.01f));
    }

    /// <summary>
    /// Clears all registered drops and destroys any existing drop objects in
    /// the currently loaded scenes. Used when progressing to a new day so
    /// uncollected materials do not persist across days.
    /// </summary>
    public void ClearAllDrops()
    {
        // Destroy any drop objects present in the active scene(s)
        foreach (var drop in FindObjectsByType<DropMaterial>(FindObjectsSortMode.None))
        {
            Destroy(drop.gameObject);
        }

        // Remove all saved drop information
        dropsByScene.Clear();

        // Persist the cleared state so nothing respawns until new drops are generated
        SaveToPrefs();
    }

    private void SpawnDropsForScene(string sceneName)
    {
        if (dropPrefab == null) return;
        if (!dropsByScene.TryGetValue(sceneName, out var list)) return;

        foreach (var data in list)
        {
            GameObject obj = Instantiate(dropPrefab, data.position, Quaternion.identity);
            var comp = obj.GetComponent<DropMaterial>();
            if (comp != null)
            {
                comp.MaterialID = data.materialID;
                comp.AnchorID = data.anchorID;
            }
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
    }

    private void LoadFromPrefs()
    {
        string json = PlayerPrefs.GetString(PREF_KEY, "");
        if (string.IsNullOrEmpty(json)) return;
        SaveWrapper wrapper = JsonUtility.FromJson<SaveWrapper>(json);
        dropsByScene.Clear();
        foreach (var sceneData in wrapper.scenes)
        {
            dropsByScene[sceneData.sceneName] = sceneData.drops;
        }
    }
}
