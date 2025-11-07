using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Draws a silhouette for the player when their collider is occluded by geometry.
/// Environment objects that share the player's root can now act as occluders;
/// place any equipment or attachments that should be ignored under <see cref="ignoreRoot"/>.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SimplePlayerOcclusionSilhouette : MonoBehaviour
{
    [SerializeField] private Camera overrideCamera;
    [SerializeField] private Material silhouetteMaterial;
    [SerializeField] private Renderer[] targetRenderers;
    [SerializeField] private LayerMask occluderMask = ~0;
    [SerializeField] private float checkInterval = 0.1f;
    [SerializeField, Tooltip("Transforms under this root are ignored when checking for occlusion. Leave empty to default to the player's root.")]
    private Transform ignoreRoot;

    private readonly List<Material[]> originalMaterials = new();
    private readonly List<Material[]> occludedMaterials = new();
    private readonly List<Material> createdSilhouetteInstances = new();

    private Collider playerCollider;
    private Collider[] playerColliders = System.Array.Empty<Collider>();
    private readonly HashSet<Collider> playerColliderSet = new();
    private bool colliderCacheDirty;
    private Camera targetCamera;
    private float nextCheckTime;
    private bool isOccluded;

    private void Awake()
    {
        RebuildPlayerColliderCache();
        targetCamera = overrideCamera != null ? overrideCamera : Camera.main;

        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            targetRenderers = GetComponentsInChildren<Renderer>();
        }

        originalMaterials.Clear();
        occludedMaterials.Clear();

        foreach (var renderer in targetRenderers)
        {
            if (renderer == null)
            {
                originalMaterials.Add(null);
                occludedMaterials.Add(null);
                continue;
            }

            var runtimeMaterials = renderer.materials;
            originalMaterials.Add(runtimeMaterials);

            if (silhouetteMaterial == null)
            {
                occludedMaterials.Add(null);
                continue;
            }

            var combined = new Material[runtimeMaterials.Length + 1];
            runtimeMaterials.CopyTo(combined, 0);

            var silhouetteInstance = new Material(silhouetteMaterial);
            combined[combined.Length - 1] = silhouetteInstance;
            createdSilhouetteInstances.Add(silhouetteInstance);

            occludedMaterials.Add(combined);
        }
    }

    private void OnEnable()
    {
        colliderCacheDirty = true;
    }

    private void OnTransformChildrenChanged()
    {
        colliderCacheDirty = true;
    }

    private void LateUpdate()
    {
        if (colliderCacheDirty)
        {
            RebuildPlayerColliderCache();
        }

        if (targetCamera == null)
        {
            targetCamera = overrideCamera != null ? overrideCamera : Camera.main;
        }

        if (targetCamera == null || playerCollider == null)
        {
            return;
        }

        if (checkInterval > 0f && Time.time < nextCheckTime)
        {
            return;
        }

        nextCheckTime = Time.time + Mathf.Max(0f, checkInterval);

        bool occluded = IsOccluded();
        if (occluded == isOccluded)
        {
            return;
        }

        isOccluded = occluded;
        ApplyMaterials(occluded);
    }

    private void RebuildPlayerColliderCache()
    {
        colliderCacheDirty = false;

        if (ignoreRoot == null)
        {
            ignoreRoot = transform.root != null ? transform.root : transform;
        }

        playerCollider = GetComponent<Collider>();

        playerColliderSet.RemoveWhere(collider => collider == null);
        playerColliderSet.Clear();

        var collectedColliders = new List<Collider>();

        void TryAddCollider(Collider collider)
        {
            if (collider == null)
            {
                return;
            }

            if (playerColliderSet.Add(collider))
            {
                collectedColliders.Add(collider);
            }
        }

        TryAddCollider(playerCollider);

        var hierarchyColliders = GetComponentsInChildren<Collider>(true);
        if (hierarchyColliders != null)
        {
            foreach (var collider in hierarchyColliders)
            {
                TryAddCollider(collider);
            }
        }

        if (ignoreRoot != null && ignoreRoot != transform)
        {
            var ignoredColliders = ignoreRoot.GetComponentsInChildren<Collider>(true);
            if (ignoredColliders != null)
            {
                foreach (var collider in ignoredColliders)
                {
                    TryAddCollider(collider);
                }
            }
        }

        playerColliders = collectedColliders.Count > 0
            ? collectedColliders.ToArray()
            : System.Array.Empty<Collider>();
    }

    private const float SampleSurfaceOffset = 0.01f;

    private static readonly Vector3[] OcclusionSampleOffsets =
    {
        Vector3.zero,
        Vector3.right,
        Vector3.left,
        Vector3.up,
        Vector3.down,
        Vector3.forward,
        Vector3.back,
        new Vector3(1f, 1f, 1f),
        new Vector3(1f, 1f, -1f),
        new Vector3(1f, -1f, 1f),
        new Vector3(-1f, 1f, 1f),
        new Vector3(1f, -1f, -1f),
        new Vector3(-1f, 1f, -1f),
        new Vector3(-1f, -1f, 1f),
        new Vector3(-1f, -1f, -1f)
    };

    private bool IsOccluded()
    {
        Bounds bounds = playerCollider.bounds;
        Vector3 extents = bounds.extents;
        Vector3 cameraPosition = targetCamera.transform.position;

        foreach (var offset in OcclusionSampleOffsets)
        {
            Vector3 samplePoint = bounds.center + new Vector3(extents.x * offset.x, extents.y * offset.y, extents.z * offset.z);

            Vector3 offsetDirection = samplePoint - bounds.center;
            if (offsetDirection.sqrMagnitude > Mathf.Epsilon)
            {
                samplePoint += offsetDirection.normalized * SampleSurfaceOffset;
            }
            else
            {
                Vector3 cameraToCenter = bounds.center - cameraPosition;
                if (cameraToCenter.sqrMagnitude > Mathf.Epsilon)
                {
                    samplePoint += cameraToCenter.normalized * SampleSurfaceOffset;
                }
            }

            Vector3 toSample = samplePoint - cameraPosition;
            float sqrDistance = toSample.sqrMagnitude;
            if (sqrDistance <= Mathf.Epsilon)
            {
                continue;
            }

            float distance = Mathf.Sqrt(sqrDistance);
            Vector3 direction = toSample / distance;

            var hits = Physics.RaycastAll(cameraPosition, direction, distance, occluderMask, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0)
            {
                continue;
            }

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                if (hit.distance >= distance)
                {
                    continue;
                }

                var hitCollider = hit.collider;
                if (hitCollider == null)
                {
                    continue;
                }

                if (playerColliderSet.Contains(hitCollider))
                {
                    continue;
                }

                Transform hitTransform = hit.transform;
                Transform ignoreTarget = ignoreRoot != null ? ignoreRoot : transform;
                if (ignoreTarget != null && (hitTransform == ignoreTarget || hitTransform.IsChildOf(ignoreTarget)))
                {
                    continue;
                }

                return true;
            }
        }

        return false;
    }

    private void ApplyMaterials(bool occluded)
    {
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            var renderer = targetRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            renderer.materials = occluded && occludedMaterials[i] != null
                ? occludedMaterials[i]
                : originalMaterials[i];
        }
    }

    private void OnDestroy()
    {
        foreach (var material in createdSilhouetteInstances)
        {
            if (material != null)
            {
                Destroy(material);
            }
        }
        createdSilhouetteInstances.Clear();
    }
}
