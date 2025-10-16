using UnityEngine;

public class PlayerSpawnPoint : MonoBehaviour
{
    [Header("Spawn Point Settings")]
    public string spawnPointName = "FromStairRoom"; // 例: FromRoofTop, FromSecondFloor等

    [Header("Visual Settings")]
    public Color gizmoColor = Color.cyan;
    public float gizmoSize = 1f;

    void Start()
    {
        // 実行時は非表示
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.enabled = false;
    }

    void OnDrawGizmos()
    {
        if (Application.isPlaying) return;
        // エディタでスポーン位置を可視化
        Gizmos.color = gizmoColor;

        // 位置を示す球
        Gizmos.DrawSphere(transform.position, gizmoSize * 0.3f);

        // 向きを示す矢印
        Gizmos.DrawRay(transform.position, transform.forward * gizmoSize);

        // 名前を表示
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, spawnPointName);
        #endif
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, gizmoSize * 0.5f);
    }
}
