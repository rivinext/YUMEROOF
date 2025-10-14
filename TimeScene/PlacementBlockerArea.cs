using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Collider))]
public class PlacementBlockerArea : MonoBehaviour
{
    public enum BlockerMode
    {
        PlacementOnly,
        PlacementAndPlayer
    }

    [SerializeField]
    private BlockerMode mode = BlockerMode.PlacementOnly;

    [SerializeField]
    private Color fillColor = new Color(1f, 0f, 0f, 0.15f);

    [SerializeField]
    private Color outlineColor = new Color(1f, 0f, 0f, 0.5f);

    [SerializeField]
    private Color placementAndPlayerFillColor = new Color(1f, 0.4f, 0f, 0.15f);

    [SerializeField]
    private Color placementAndPlayerOutlineColor = new Color(1f, 0.4f, 0f, 0.5f);

    private Collider cachedCollider;

    private void Awake()
    {
        ApplyLayer();
        ConfigureCollider();
    }

    private void Reset()
    {
        ApplyLayer();
        ConfigureCollider();
    }

    private void OnValidate()
    {
        ApplyLayer();
        ConfigureCollider();
    }

    private void ApplyLayer()
    {
        int blockerLayer = LayerMask.NameToLayer("PlacementBlocker");
        if (blockerLayer != -1 && gameObject.layer != blockerLayer)
        {
            gameObject.layer = blockerLayer;
        }
    }

    private void ConfigureCollider()
    {
        if (cachedCollider == null)
        {
            cachedCollider = GetComponent<Collider>();
        }

        if (cachedCollider != null)
        {
            cachedCollider.isTrigger = mode == BlockerMode.PlacementOnly;
        }
    }

    private void OnDrawGizmos()
    {
        Collider collider = cachedCollider != null ? cachedCollider : GetComponent<Collider>();
        cachedCollider = collider;
        if (collider is BoxCollider boxCollider)
        {
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Color previousColor = Gizmos.color;

            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Vector3 scaledSize = Vector3.Scale(boxCollider.size, transform.lossyScale);

            Color modeFillColor = mode == BlockerMode.PlacementOnly ? fillColor : placementAndPlayerFillColor;
            Color modeOutlineColor = mode == BlockerMode.PlacementOnly ? outlineColor : placementAndPlayerOutlineColor;

            Gizmos.color = modeFillColor;
            Gizmos.DrawCube(boxCollider.center, scaledSize);

            Gizmos.color = modeOutlineColor;
            Gizmos.DrawWireCube(boxCollider.center, scaledSize);

            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;

#if UNITY_EDITOR
            Vector3 labelPosition = boxCollider.bounds.center + Vector3.up * 0.1f;
            Handles.Label(labelPosition, mode == BlockerMode.PlacementOnly ? "Placement Only" : "Placement + Player");
#endif
        }
    }
}
