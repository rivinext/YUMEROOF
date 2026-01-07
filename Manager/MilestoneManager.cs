using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MilestoneManager : MonoBehaviour
{
    public static MilestoneManager Instance { get; private set; }

    public static void CreateIfNeeded(GameObject prefab = null)
    {
        if (Instance == null)
        {
            if (prefab != null)
            {
                UnityEngine.Object.Instantiate(prefab);
            }
            else
            {
                var loaded = Resources.Load<GameObject>("MilestoneManager");
                if (loaded != null)
                    UnityEngine.Object.Instantiate(loaded);
                else
                    new GameObject("MilestoneManager").AddComponent<MilestoneManager>();
            }
        }
    }

    public event Action<Milestone, int, int, int> OnMilestoneProgress;

    [SerializeField] private string milestoneCSVPath = "DataResources/Data/YUME_ROOF - Milestone";

    private readonly List<Milestone> milestones = new List<Milestone>();
    private int currentMilestoneIndex = 0;

    private int currentCozy;
    private int currentNature;
    private int currentItemCount;
    private readonly Dictionary<Rarity, int> rarityPlacementCounts = new Dictionary<Rarity, int>();
    private static readonly string[] trackedScenesForRarity =
    {
        "RoofTop",
        "FirstFloorShop",
        "SecondFloorRoom",
        "StairRoom"
    };

    private bool isRestoringState;

    private FurnitureSaveManager furnitureSaveManager;

    public string CurrentMilestoneID =>
        currentMilestoneIndex < milestones.Count
            ? milestones[currentMilestoneIndex].id
            : string.Empty;

    // Index of the current milestone. Milestones with an index lower than this value are considered cleared.
    public int CurrentMilestoneIndex => currentMilestoneIndex;

    // Total number of milestones loaded from the CSV
    public int MilestoneCount => milestones.Count;

    // Provides read-only access to a milestone without exposing the list for modification.
    public bool TryGetMilestone(int index, out Milestone milestone)
    {
        if (index < 0 || index >= milestones.Count)
        {
            milestone = null;
            return false;
        }

        milestone = milestones[index];
        return true;
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
        LoadMilestones();
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
        if (scene.name == "MainMenu")
        {
            if (Instance == this) Instance = null;
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (EnvironmentStatsManager.Instance != null)
        {
            EnvironmentStatsManager.Instance.OnStatsChanged += UpdateEnvironment;
            UpdateEnvironment(EnvironmentStatsManager.Instance.CozyTotal,
                              EnvironmentStatsManager.Instance.NatureTotal);
        }
        StartCoroutine(WaitForFurnitureSaveManager());
    }

    void OnDestroy()
    {
        if (EnvironmentStatsManager.Instance != null)
        {
            EnvironmentStatsManager.Instance.OnStatsChanged -= UpdateEnvironment;
        }
        if (furnitureSaveManager != null)
        {
            furnitureSaveManager.OnFurnitureChanged -= HandleInventoryChanged;
        }
    }

    void LoadMilestones()
    {
        if (string.IsNullOrWhiteSpace(milestoneCSVPath))
        {
            Debug.LogWarning("Milestone CSV path is not set.");
            return;
        }

        TextAsset csv = Resources.Load<TextAsset>(milestoneCSVPath);
        if (csv == null)
        {
            Debug.LogWarning($"Milestone CSV not found at {milestoneCSVPath}");
            return;
        }

        var lines = csv.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            var parts = line.Split(',');
            if (parts.Length < 8) continue;

            string rarityRaw = parts.Length > 4 ? parts[4].Trim('"') : string.Empty;
            string rarityNormalized = NormalizeRarityString(rarityRaw);
            if (!Enum.TryParse(rarityNormalized, ignoreCase: true, out Rarity parsedRarityRequirement))
            {
                parsedRarityRequirement = Rarity.Common;
            }

            int parsedRarityCountRequirement = 0;
            string rarityCountRaw = parts.Length > 5 ? parts[5].Trim('"') : string.Empty;
            if (!int.TryParse(rarityCountRaw, out parsedRarityCountRequirement))
            {
                parsedRarityCountRequirement = 0;
            }

            Milestone m = new Milestone
            {
                id = parts[0],
                cozyRequirement = int.Parse(parts[1]),
                natureRequirement = int.Parse(parts[2]),
                itemCountRequirement = int.Parse(parts[3]),
                rarityRequirement = parsedRarityRequirement,
                rarityCountRequirement = parsedRarityCountRequirement,
                reward = parts[7],
                rewardArea = parts.Length > 9 ? parts[9].Trim() : string.Empty,
            };
            if (parts.Length > 8)
            {
                int.TryParse(parts[8], out m.moneyReward);
            }
            milestones.Add(m);
        }
    }

    private string NormalizeRarityString(string rarity)
    {
        if (string.IsNullOrWhiteSpace(rarity))
        {
            return string.Empty;
        }

        rarity = rarity.Trim().Replace(" ", string.Empty);

        switch (rarity.ToLowerInvariant())
        {
            case "unccmmon":
            case "uncomon":
            case "uncommon":
                return "Uncommon";
            default:
                return rarity;
        }
    }

    public void UpdateEnvironment(int cozy, int nature)
    {
        currentCozy = cozy;
        currentNature = nature;
        CheckProgress();
    }

    IEnumerator WaitForFurnitureSaveManager()
    {
        while (FurnitureSaveManager.Instance == null)
        {
            yield return null;
        }
        furnitureSaveManager = FurnitureSaveManager.Instance;
        furnitureSaveManager.OnFurnitureChanged += HandleInventoryChanged;
        HandleInventoryChanged();
    }

    void HandleInventoryChanged()
    {
        UpdateRarityPlacementCounts();
        currentItemCount = FurnitureSaveManager.Instance?
            .GetPlacedFurnitureCount(trackedScenesForRarity) ?? 0;
        CheckProgress();
    }

    private void UpdateRarityPlacementCounts()
    {
        rarityPlacementCounts.Clear();

        var furnitureManager = FurnitureSaveManager.Instance;
        var dataManager = FurnitureDataManager.Instance;
        if (furnitureManager == null || dataManager == null)
        {
            return;
        }

        var trackedScenes = new HashSet<string>(trackedScenesForRarity);
        var allFurniture = furnitureManager.GetAllFurniture();
        foreach (var furniture in allFurniture)
        {
            if (!trackedScenes.Contains(furniture.sceneName))
            {
                continue;
            }

            var furnitureData = dataManager.GetFurnitureData(furniture.furnitureID);
            if (furnitureData == null)
            {
                continue;
            }

            var rarity = furnitureData.rarity;
            if (rarityPlacementCounts.ContainsKey(rarity))
            {
                rarityPlacementCounts[rarity]++;
            }
            else
            {
                rarityPlacementCounts[rarity] = 1;
            }
        }
    }

    public int GetPlacedCountForRarity(Rarity rarity)
    {
        return rarityPlacementCounts.TryGetValue(rarity, out var count) ? count : 0;
    }

    public bool IsRestoringState => isRestoringState;

    public void SetCurrentMilestoneIndex(int index, bool notify = true)
    {
        currentMilestoneIndex = Mathf.Clamp(index, 0, milestones.Count);
        if (!notify)
        {
            isRestoringState = true;
            return;
        }

        isRestoringState = false;
        RequestProgressUpdate();
    }

    public void RequestProgressUpdate()
    {
        if (currentMilestoneIndex >= milestones.Count)
        {
            isRestoringState = false;
            return;
        }

        var m = milestones[currentMilestoneIndex];
        OnMilestoneProgress?.Invoke(m, currentCozy, currentNature, currentItemCount);
        isRestoringState = false;
    }

#if UNITY_EDITOR
    public void AdvanceMilestoneDebug()
    {
        if (currentMilestoneIndex < milestones.Count)
        {
            currentMilestoneIndex++;
            if (currentMilestoneIndex < milestones.Count)
            {
                var m = milestones[currentMilestoneIndex];
                OnMilestoneProgress?.Invoke(m, currentCozy, currentNature, currentItemCount);
            }
        }
    }
#endif

    void CheckProgress()
    {
        if (currentMilestoneIndex >= milestones.Count) return;

        var m = milestones[currentMilestoneIndex];
        OnMilestoneProgress?.Invoke(m, currentCozy, currentNature, currentItemCount);

        if (currentCozy >= m.cozyRequirement &&
            currentNature >= m.natureRequirement &&
            currentItemCount >= m.itemCountRequirement)
        {
            if (!string.IsNullOrEmpty(m.reward) && !string.Equals(m.reward, "None", StringComparison.OrdinalIgnoreCase))
            {
                bool added = InventoryManager.Instance?.AddFurniture(m.reward) ?? false;
                if (added)
                {
                    Debug.Log($"Milestone reward added: {m.reward}");
                }
            }

            if (m.moneyReward > 0)
            {
                MoneyManager.Instance?.AddMoney(m.moneyReward);
                Debug.Log($"Milestone money reward added: {m.moneyReward}");
            }

            currentMilestoneIndex++;
            if (currentMilestoneIndex < milestones.Count)
            {
                m = milestones[currentMilestoneIndex];
                OnMilestoneProgress?.Invoke(m, currentCozy, currentNature, currentItemCount);
            }
        }
    }

    [Serializable]
    public class Milestone
    {
        public string id;
        public int cozyRequirement;
        public int natureRequirement;
        public int itemCountRequirement;
        public Rarity rarityRequirement;
        public int rarityCountRequirement;
        public string reward;
        public int moneyReward;
        public string rewardArea;
    }
}
