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

    private Transform resolvedTarget;
    private Transform lastCachedTarget;
    private bool isDetached;

    private Collider[] cachedColliders = System.Array.Empty<Collider>();
    private Renderer[] cachedRenderers = System.Array.Empty<Renderer>();

    private void Awake()
    {
        InitializeTarget(target);
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            InitializeTarget(target);
        }
    }

    /// <summary>
    /// Allows reusing the same billboard by providing a new target to track.
    /// Passing <c>null</c> hides the billboard until another target is set.
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        SetTargetInternal(newTarget, overrideOffset: null);
    }

    /// <summary>
    /// Same as <see cref="SetTarget(Transform)"/> but also overrides the height offset for this billboard.
    /// </summary>
    public void SetTarget(Transform newTarget, float overrideOffset)
    {
        SetTargetInternal(newTarget, overrideOffset);
    }

    private void SetTargetInternal(Transform newTarget, float? overrideOffset)
    {
        if (overrideOffset.HasValue)
            heightOffset = overrideOffset.Value;

        bool detach = newTarget == null;
        bool detachStateChanged = isDetached != detach;

        isDetached = detach;
        target = newTarget;
        resolvedTarget = ResolveTarget(newTarget);

        RefreshCachedComponents(force: detach || detachStateChanged || !ReferenceEquals(resolvedTarget, lastCachedTarget));
    }

    private void InitializeTarget(Transform initialTarget)
    {
        isDetached = false;
        resolvedTarget = ResolveTarget(initialTarget);
        cachedColliders = System.Array.Empty<Collider>();
        cachedRenderers = System.Array.Empty<Renderer>();
        lastCachedTarget = null;

        RefreshCachedComponents(force: true);
    }

    private Transform ResolveTarget(Transform candidate)
    {
        return candidate != null ? candidate : transform;
    }

    private void RefreshCachedComponents(bool force = false)
    {
        if (isDetached || resolvedTarget == null)
        {
            cachedColliders = System.Array.Empty<Collider>();
            cachedRenderers = System.Array.Empty<Renderer>();
            lastCachedTarget = null;
            return;
        }

        if (!force && ReferenceEquals(resolvedTarget, lastCachedTarget))
            return;

        cachedColliders = resolvedTarget.GetComponentsInChildren<Collider>(true);
        cachedRenderers = resolvedTarget.GetComponentsInChildren<Renderer>(true);
        lastCachedTarget = resolvedTarget;
    }

    private void LateUpdate()
    {
        if (isDetached || resolvedTarget == null)
            return;

        UpdatePosition();
        FaceCamera();
    }

    private void UpdatePosition()
    {
        if (resolvedTarget == null)
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

        Vector3 basePosition = resolvedTarget.position;
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
