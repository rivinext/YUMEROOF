using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Applies a silhouette material to the player renderers when they are occluded from the main camera.
/// </summary>
public class PlayerOcclusionSilhouette : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Renderer[] targetRenderers = System.Array.Empty<Renderer>();
    [SerializeField] private Collider[] selfColliders = System.Array.Empty<Collider>();
    [SerializeField] private Material silhouetteMaterial;
    [SerializeField] private LayerMask occluderLayers = ~0;

    private readonly HashSet<Collider> selfColliderSet = new();
    private Material[][] originalSharedMaterials = System.Array.Empty<Material[]>();
    private bool[] rendererOccludedStates = System.Array.Empty<bool>();

    void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        CollectSelfColliders();
        CacheOriginalMaterials();
    }

    void Start()
    {
        // Ensure colliders are collected even if added after Awake.
        CollectSelfColliders();
    }

    void LateUpdate()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                RestoreAllMaterials();
                return;
            }
        }

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            var targetRenderer = targetRenderers[i];
            if (targetRenderer == null)
            {
                continue;
            }

            bool occluded = IsRendererOccluded(targetRenderer);
            if (occluded)
            {
                ApplySilhouette(targetRenderer, i);
            }
            else
            {
                RestoreRendererMaterials(targetRenderer, i);
            }

            rendererOccludedStates[i] = occluded;
        }
    }

    private void CollectSelfColliders()
    {
        selfColliders = GetComponentsInChildren<Collider>(true);
        selfColliderSet.Clear();
        if (selfColliders == null)
        {
            return;
        }

        for (int i = 0; i < selfColliders.Length; i++)
        {
            if (selfColliders[i] != null)
            {
                selfColliderSet.Add(selfColliders[i]);
            }
        }
    }

    private void CacheOriginalMaterials()
    {
        if (targetRenderers == null)
        {
            targetRenderers = System.Array.Empty<Renderer>();
        }

        originalSharedMaterials = new Material[targetRenderers.Length][];
        rendererOccludedStates = new bool[targetRenderers.Length];

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            var targetRenderer = targetRenderers[i];
            originalSharedMaterials[i] = targetRenderer != null
                ? targetRenderer.sharedMaterials
                : System.Array.Empty<Material>();
        }
    }

    private bool IsRendererOccluded(Renderer targetRenderer)
    {
        Vector3 targetPosition = targetRenderer.bounds.center;
        Vector3 cameraPosition = mainCamera.transform.position;
        Vector3 direction = targetPosition - cameraPosition;
        float distance = direction.magnitude;

        if (distance <= Mathf.Epsilon)
        {
            return false;
        }

        direction /= distance;

        RaycastHit[] hits = Physics.RaycastAll(cameraPosition, direction, distance, occluderLayers, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
        {
            return false;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            var hitCollider = hits[i].collider;
            if (hitCollider == null)
            {
                continue;
            }

            if (selfColliderSet.Contains(hitCollider))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private void ApplySilhouette(Renderer targetRenderer, int index)
    {
        if (silhouetteMaterial == null)
        {
            return;
        }

        if (originalSharedMaterials.Length > index && rendererOccludedStates.Length > index && rendererOccludedStates[index])
        {
            // Already applied this frame or previous frame.
            return;
        }

        Material[] originalMaterials = GetOriginalMaterials(index);
        if (originalMaterials.Length == 0)
        {
            targetRenderer.sharedMaterial = silhouetteMaterial;
            return;
        }

        Material[] silhouetteArray = new Material[originalMaterials.Length];
        for (int i = 0; i < silhouetteArray.Length; i++)
        {
            silhouetteArray[i] = silhouetteMaterial;
        }

        targetRenderer.sharedMaterials = silhouetteArray;
    }

    private void RestoreRendererMaterials(Renderer targetRenderer, int index)
    {
        if (rendererOccludedStates.Length > index && !rendererOccludedStates[index])
        {
            return;
        }

        Material[] originalMaterials = GetOriginalMaterials(index);
        if (originalMaterials.Length == 0)
        {
            return;
        }

        targetRenderer.sharedMaterials = originalMaterials;
    }

    private void RestoreAllMaterials()
    {
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] == null)
            {
                continue;
            }

            RestoreRendererMaterials(targetRenderers[i], i);
            rendererOccludedStates[i] = false;
        }
    }

    private Material[] GetOriginalMaterials(int index)
    {
        if (index < 0 || index >= originalSharedMaterials.Length)
        {
            return System.Array.Empty<Material>();
        }

        return originalSharedMaterials[index] ?? System.Array.Empty<Material>();
    }
}
