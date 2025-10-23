using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SimplePlayerOcclusionSilhouette : MonoBehaviour
{
    [SerializeField] private Camera overrideCamera;
    [SerializeField] private Material silhouetteMaterial;
    [SerializeField] private Renderer[] targetRenderers;
    [SerializeField] private LayerMask occluderMask = ~0;
    [SerializeField] private float checkInterval = 0.1f;

    private readonly List<Material[]> originalMaterials = new();
    private readonly List<Material[]> occludedMaterials = new();
    private readonly List<Material> createdSilhouetteInstances = new();

    private Collider playerCollider;
    private Camera targetCamera;
    private float nextCheckTime;
    private bool isOccluded;

    private void Awake()
    {
        playerCollider = GetComponent<Collider>();
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

    private void LateUpdate()
    {
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

    private bool IsOccluded()
    {
        Vector3 center = playerCollider.bounds.center;
        Vector3 toCamera = targetCamera.transform.position - center;
        float distance = toCamera.magnitude;
        if (distance <= Mathf.Epsilon)
        {
            return false;
        }

        if (Physics.Raycast(center, toCamera / distance, out RaycastHit hit, distance, occluderMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider != playerCollider && !hit.transform.IsChildOf(transform))
            {
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
