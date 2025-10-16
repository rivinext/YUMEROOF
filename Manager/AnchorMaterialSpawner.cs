using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Spawns a single material drop from a list of candidates at this anchor's
/// position. Registers the drop with <see cref="DropMaterialSaveManager"/> so
/// it persists across scene loads and listens for <see cref="GameClock.OnSleepAdvancedDay"/>
/// to respawn a new material after the player sleeps.
/// </summary>
public class AnchorMaterialSpawner : MonoBehaviour
{
    [System.Serializable]
    public struct SpawnMaterialInfo
    {
        public string materialID;
        public GameObject prefab;
        [Range(0,100)] public float minDropRate;
        [Range(0,100)] public float maxDropRate;
    }

    [SerializeField] private SpawnMaterialInfo[] materialCandidates;
    [SerializeField] private string anchorID;

    [Header("Gizmo Settings")]
    [SerializeField] private Color gizmoColor = Color.green;
    [SerializeField] private float gizmoRadius = 0.25f;

    private GameObject currentDrop;
    private GameClock clock;

    void Start()
    {
        Spawn();
    }

    void OnEnable()
    {
        clock = FindObjectOfType<GameClock>();
        if (clock != null)
        {
            clock.OnSleepAdvancedDay += HandleSleepAdvancedDay;
        }
    }

    void OnDisable()
    {
        if (clock != null)
        {
            clock.OnSleepAdvancedDay -= HandleSleepAdvancedDay;
            clock = null;
        }
    }

    private void HandleSleepAdvancedDay(int day)
    {
        ClearExisting();
        Spawn();
    }

    /// <summary>
    /// Spawns a random material from the candidate list at the anchor's
    /// position and registers it with the save manager.
    /// </summary>
    public void Spawn()
    {
        if (materialCandidates == null || materialCandidates.Length == 0)
        {
            return;
        }

        SpawnMaterialInfo info = materialCandidates[Random.Range(0, materialCandidates.Length)];

        float dropRate = Random.Range(info.minDropRate, info.maxDropRate);
        if (Random.value * 100f > dropRate)
        {
            return;
        }

        currentDrop = Instantiate(info.prefab, transform.position, transform.rotation);

        var drop = currentDrop.GetComponent<DropMaterial>();
        if (drop != null)
        {
            drop.MaterialID = info.materialID;
            drop.AnchorID = anchorID;
        }

        DropMaterialSaveManager.Instance?.RegisterDrop(
            SceneManager.GetActiveScene().name,
            info.materialID,
            currentDrop.transform.position,
            anchorID);
    }

    /// <summary>
    /// Removes any existing spawned material and unregisters it from the save
    /// manager.
    /// </summary>
    public void ClearExisting()
    {
        if (currentDrop == null) return;

        var drop = currentDrop.GetComponent<DropMaterial>();
        if (drop != null)
        {
            DropMaterialSaveManager.Instance?.RemoveDrop(
                SceneManager.GetActiveScene().name,
                drop.MaterialID,
                currentDrop.transform.position,
                anchorID);
        }

        Destroy(currentDrop);
        currentDrop = null;
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying) return;
        // Draw a solid gizmo to clearly visualize the spawn area in the scene
        var color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.35f);
        Gizmos.color = color;
        Gizmos.DrawSphere(transform.position, gizmoRadius);
        #if UNITY_EDITOR
        if (!string.IsNullOrEmpty(anchorID))
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * gizmoRadius, anchorID);
        }
        #endif
    }
}
