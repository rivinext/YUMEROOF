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
        gameObject.layer = LayerMask.NameToLayer("Anchor");
        SetupCollider();
    }

    void OnValidate()
    {
        gameObject.layer = LayerMask.NameToLayer("Anchor");
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

    private void OnDrawGizmos()
    {
        Gizmos.color = IsOccupied ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, snapRadius);
    }
}
