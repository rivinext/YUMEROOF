using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// インタラクト用のUIを対象オブジェクトのバウンディングボックスに合わせて配置し、
/// 常にカメラの方向を向かせるためのコンポーネント。
/// </summary>
public class InteractionPromptBillboard : MonoBehaviour
{
    [Tooltip("位置決めの基準となる対象。未指定の場合は親Transformを利用します。")]
    [SerializeField] private Transform target;

    [Tooltip("バウンディングボックスの下端からY方向に加算するオフセット。")]
    [SerializeField] private float offsetY = 0.1f;

    private readonly List<Renderer> targetRenderers = new();
    private readonly List<Collider> targetColliders = new();

    /// <summary>
    /// バウンディングボックス算出に使用するターゲットを取得・設定します。
    /// </summary>
    public Transform Target
    {
        get => target;
        set
        {
            if (target == value)
                return;

            target = value;
            CollectTargetComponents();
        }
    }

    private void Awake()
    {
        if (target == null)
        {
            target = transform.parent;
        }

        CollectTargetComponents();
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        if (!TryGetTargetBounds(out Bounds bounds))
        {
            bounds = new Bounds(target.position, Vector3.zero);
        }

        Vector3 newPosition = new Vector3(bounds.center.x, bounds.min.y + offsetY, bounds.center.z);
        transform.position = newPosition;

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Transform cameraTransform = mainCamera.transform;
            transform.forward = cameraTransform.forward;
        }
    }

    private void CollectTargetComponents()
    {
        targetRenderers.Clear();
        targetColliders.Clear();

        if (target == null)
            return;

        target.GetComponentsInChildren(true, targetRenderers);
        target.GetComponentsInChildren(true, targetColliders);
    }

    private bool TryGetTargetBounds(out Bounds bounds)
    {
        bool hasBounds = false;
        bounds = default;

        for (int i = targetRenderers.Count - 1; i >= 0; i--)
        {
            Renderer renderer = targetRenderers[i];
            if (renderer == null || !renderer.enabled)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        for (int i = targetColliders.Count - 1; i >= 0; i--)
        {
            Collider collider = targetColliders[i];
            if (collider == null || !collider.enabled)
                continue;

            Bounds colliderBounds = collider.bounds;
            if (!hasBounds)
            {
                bounds = colliderBounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(colliderBounds);
            }
        }

        return hasBounds;
    }
}
