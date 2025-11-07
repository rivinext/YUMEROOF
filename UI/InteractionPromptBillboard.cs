using UnityEngine;

/// <summary>
/// 指定したターゲットのバウンディングボックスを参照し、
/// 底面中心から任意のオフセット位置にインタラクション用パネルを配置するビルボード。
/// </summary>
public class InteractionPromptBillboard : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("バウンディングボックスを算出する対象の Transform。未指定の場合は自身を使用します。")]
    [SerializeField] private Transform target;

    [Tooltip("底面中心から上方向へ加算するオフセット。")]
    [SerializeField] private float heightOffset = 1f;

    private Collider[] cachedColliders;
    private Renderer[] cachedRenderers;

    private void Awake()
    {
        if (target == null)
            target = transform;

        CacheComponents();
    }

    private void OnValidate()
    {
        if (target == null)
            target = transform;

        if (!Application.isPlaying)
            CacheComponents();
    }

    private void CacheComponents()
    {
        if (target == null)
        {
            cachedColliders = System.Array.Empty<Collider>();
            cachedRenderers = System.Array.Empty<Renderer>();
            return;
        }

        cachedColliders = target.GetComponentsInChildren<Collider>(true);
        cachedRenderers = target.GetComponentsInChildren<Renderer>(true);
    }

    private void LateUpdate()
    {
        UpdatePosition();
        FaceCamera();
    }

    private void UpdatePosition()
    {
        if (target == null)
            return;

        bool hasBounds = false;
        Bounds bounds = default;

        if (cachedColliders != null)
        {
            foreach (var col in cachedColliders)
            {
                if (col == null || !col.enabled)
                    continue;

                if (!hasBounds)
                {
                    bounds = col.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(col.bounds);
                }
            }
        }

        if (cachedRenderers != null)
        {
            foreach (var renderer in cachedRenderers)
            {
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
        }

        Vector3 basePosition = target.position;
        if (hasBounds)
        {
            basePosition = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }

        transform.position = basePosition + Vector3.up * heightOffset;
    }

    private void FaceCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return;

        transform.forward = mainCamera.transform.forward;
    }
}
