using UnityEngine;

/// <summary>
/// 家具などの設置オブジェクトにスナップ位置を作るためのコンポーネント。
/// 被設置オブジェクトの子に <see cref="AnchorPoint"/> を配置してください。
/// </summary>
public class AnchorPoint : MonoBehaviour
{
    [Tooltip("このアンカーに吸着させる半径")]
    [SerializeField] private float snapRadius = 0.2f;

    private SphereCollider sphereCollider;

    public float SnapRadius => snapRadius;

    public bool IsOccupied { get; private set; }

    void Awake()
    {
        ApplyAnchorLayer();
        SetupCollider();
    }

    void OnValidate()
    {
        ApplyAnchorLayer();
        SetupCollider();
    }

    void SetupCollider()
    {
        if (sphereCollider == null)
        {
            sphereCollider = GetComponent<SphereCollider>();
            if (sphereCollider == null)
            {
                sphereCollider = gameObject.AddComponent<SphereCollider>();
            }
        }

        sphereCollider.isTrigger = true;
        sphereCollider.radius = snapRadius;
    }

    public void SetOccupied(bool value)
    {
        IsOccupied = value;
    }

    void ApplyAnchorLayer()
    {
        int anchorLayer = LayerMask.NameToLayer("Anchor");

        if (anchorLayer < 0)
        {
            Debug.LogWarning("[AnchorPoint] 'Anchor' layer is not defined. Assigning to Default layer to keep anchors functional.");
            anchorLayer = LayerMask.NameToLayer("Default");

            if (anchorLayer < 0)
            {
                anchorLayer = 0; // Default layer should exist, but fall back to 0 just in case
            }
        }

        gameObject.layer = anchorLayer;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = IsOccupied ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, snapRadius);
    }
}
