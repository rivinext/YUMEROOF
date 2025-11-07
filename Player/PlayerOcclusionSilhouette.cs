using System;
using System.Collections.Generic;
using UnityEngine;

namespace Player
{
    [DisallowMultipleComponent]
    public class PlayerOcclusionSilhouette : MonoBehaviour
    {
        public Camera overrideCamera;

        public Material silhouetteMaterial;

        public Renderer[] targetRenderers;

        [SerializeField]
        [Tooltip("When enabled, RefreshTargetRenderers automatically collects all child renderers, overwriting manual assignments.")]
        private bool autoCollectRenderers = true;

        public LayerMask occluderMask = ~0;

        public float checkInterval = 0.1f;
        public bool forceSilhouette = false;

        private readonly List<Material[]> originalMaterials = new List<Material[]>();
        private readonly List<Material[]> silhouetteMaterials = new List<Material[]>();
        private readonly List<MaterialPropertyBlock> propertyBlocks = new List<MaterialPropertyBlock>();
        private readonly List<Material[]> occludedMaterials = new List<Material[]>();
        private readonly List<Material> createdSilhouetteMaterials = new List<Material>();

        private Collider playerCollider;
        private Camera targetCamera;
        private bool isOccluded;
        private float nextCheckTime;
        private static readonly RaycastHit[] RaycastHits = new RaycastHit[4];

        private void Awake()
        {
            playerCollider = GetComponent<Collider>();
            targetCamera = overrideCamera != null ? overrideCamera : Camera.main;

            RefreshTargetRenderers();
        }

        [ContextMenu("Refresh Target Renderers")]
        public void RefreshTargetRenderers()
        {
            bool wasOccluded = isOccluded;

            bool canRestoreOriginals =
                wasOccluded &&
                targetRenderers != null &&
                originalMaterials.Count == targetRenderers.Length &&
                occludedMaterials.Count == targetRenderers.Length &&
                propertyBlocks.Count == targetRenderers.Length;

            if (canRestoreOriginals)
            {
                ApplyMaterials(false);
            }

            for (int i = 0; i < createdSilhouetteMaterials.Count; i++)
            {
                Material material = createdSilhouetteMaterials[i];
                if (material != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(material);
                    }
                    else
                    {
                        DestroyImmediate(material);
                    }
                }
            }

            createdSilhouetteMaterials.Clear();
            originalMaterials.Clear();
            silhouetteMaterials.Clear();
            propertyBlocks.Clear();
            occludedMaterials.Clear();

            bool shouldAutoCollect = autoCollectRenderers || targetRenderers == null || targetRenderers.Length == 0;

            if (shouldAutoCollect)
            {
                Renderer[] childRenderers = GetComponentsInChildren<Renderer>(true);
                List<Renderer> filteredRenderers = new List<Renderer>(childRenderers.Length);
                for (int i = 0; i < childRenderers.Length; i++)
                {
                    Renderer renderer = childRenderers[i];
                    if (renderer == null)
                    {
                        continue;
                    }

                    if (renderer is ParticleSystemRenderer)
                    {
                        continue;
                    }

                    filteredRenderers.Add(renderer);
                }

                targetRenderers = filteredRenderers.ToArray();
            }

            if (targetRenderers == null)
            {
                isOccluded = wasOccluded;
                return;
            }

            for (int i = 0; i < targetRenderers.Length; i++)
            {
                Renderer renderer = targetRenderers[i];
                if (renderer == null)
                {
                    originalMaterials.Add(null);
                    silhouetteMaterials.Add(null);
                    occludedMaterials.Add(null);
                    propertyBlocks.Add(null);
                    continue;
                }

                Material[] sharedMaterials = renderer.sharedMaterials;
                if (sharedMaterials == null)
                {
                    originalMaterials.Add(null);
                    silhouetteMaterials.Add(null);
                    occludedMaterials.Add(null);
                    propertyBlocks.Add(null);
                    continue;
                }

                Material[] sharedMaterialsCopy = new Material[sharedMaterials.Length];
                Array.Copy(sharedMaterials, sharedMaterialsCopy, sharedMaterials.Length);
                originalMaterials.Add(sharedMaterialsCopy);

                Material[] silhouetteArray = new Material[sharedMaterials.Length];
                for (int m = 0; m < sharedMaterials.Length; m++)
                {
                    silhouetteArray[m] = CreateSilhouetteMaterialInstance(sharedMaterials[m]);
                }
                silhouetteMaterials.Add(silhouetteArray);

                Material[] combinedMaterials = null;
                if (sharedMaterialsCopy != null && silhouetteArray != null)
                {
                    combinedMaterials = new Material[sharedMaterialsCopy.Length + silhouetteArray.Length];
                    Array.Copy(sharedMaterialsCopy, combinedMaterials, sharedMaterialsCopy.Length);
                    Array.Copy(silhouetteArray, 0, combinedMaterials, sharedMaterialsCopy.Length, silhouetteArray.Length);
                }
                occludedMaterials.Add(combinedMaterials);

                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                propertyBlocks.Add(block);
            }

            isOccluded = wasOccluded;
            ApplyMaterials(wasOccluded);
        }

        private Material CreateSilhouetteMaterialInstance(Material sourceMaterial)
        {
            if (silhouetteMaterial == null)
            {
                return null;
            }

            Material materialInstance = new Material(silhouetteMaterial);

            int baseRenderQueue = silhouetteMaterial.renderQueue;
            if (sourceMaterial != null && materialInstance.renderQueue <= sourceMaterial.renderQueue)
            {
                baseRenderQueue = sourceMaterial.renderQueue + 1;
            }

            materialInstance.renderQueue = baseRenderQueue;

            createdSilhouetteMaterials.Add(materialInstance);
            return materialInstance;
        }

        private void LateUpdate()
        {
            if (targetCamera == null || playerCollider == null)
            {
                return;
            }

            if (forceSilhouette)
            {
                if (!isOccluded)
                {
                    isOccluded = true;
                    ApplyMaterials(true);
                }

                return;
            }

            if (checkInterval > 0f && Time.time < nextCheckTime)
            {
                return;
            }

            nextCheckTime = Time.time + Mathf.Max(0f, checkInterval);

            bool occluded = CheckOcclusion();
            if (occluded == isOccluded)
            {
                return;
            }

            isOccluded = occluded;
            ApplyMaterials(occluded);
        }

        private bool CheckOcclusion()
        {
            Bounds bounds = playerCollider.bounds;
            Vector3 upOffset = Vector3.up * bounds.extents.y;
            Vector3 forwardOffset = transform.forward * bounds.extents.z;
            Vector3 rightOffset = transform.right * bounds.extents.x;

            Vector3[] samplePoints =
            {
                bounds.center,
                bounds.center + upOffset,
                bounds.center - upOffset * 0.5f,
                bounds.center + forwardOffset * 0.5f,
                bounds.center - forwardOffset * 0.5f,
                bounds.center + rightOffset * 0.5f,
                bounds.center - rightOffset * 0.5f
            };

            Vector3 cameraPosition = targetCamera.transform.position;
            Transform playerTransform = transform;

            for (int i = 0; i < samplePoints.Length; i++)
            {
                Vector3 point = samplePoints[i];
                Vector3 toCamera = cameraPosition - point;
                float distance = toCamera.magnitude;
                if (distance <= Mathf.Epsilon)
                {
                    continue;
                }

                Vector3 direction = toCamera / distance;
                int hitCount = Physics.RaycastNonAlloc(point, direction, RaycastHits, distance, occluderMask, QueryTriggerInteraction.Ignore);
                for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
                {
                    Collider hitCollider = RaycastHits[hitIndex].collider;
                    if (hitCollider == null)
                    {
                        continue;
                    }

                    if (hitCollider == playerCollider)
                    {
                        continue;
                    }

                    if (hitCollider.transform == playerTransform || hitCollider.transform.IsChildOf(playerTransform))
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
            if (targetRenderers == null)
            {
                return;
            }

            int rendererCount = targetRenderers.Length;
            int originalCount = originalMaterials.Count;
            int occludedCount = occludedMaterials.Count;
            int propertyBlockCount = propertyBlocks.Count;
            for (int i = 0; i < rendererCount; i++)
            {
                Renderer renderer = targetRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                if (occluded)
                {
                    Material[] combinedMaterials = i < occludedCount ? occludedMaterials[i] : null;
                    if (combinedMaterials == null)
                    {
                        continue;
                    }

                    renderer.sharedMaterials = combinedMaterials;
                }
                else
                {
                    // 遮蔽されていない場合は通常マテリアルのみ
                    Material[] originals = i < originalCount ? originalMaterials[i] : null;
                    if (originals == null)
                    {
                        continue;
                    }

                    renderer.sharedMaterials = originals;
                }

                MaterialPropertyBlock block = i < propertyBlockCount ? propertyBlocks[i] : null;
                if (block != null)
                {
                    renderer.SetPropertyBlock(block);
                }
            }
        }

        private void OnDisable()
        {
            if (isOccluded)
            {
                ApplyMaterials(false);
                isOccluded = false;
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < createdSilhouetteMaterials.Count; i++)
            {
                Material material = createdSilhouetteMaterials[i];
                if (material != null)
                {
                    Destroy(material);
                }
            }
            createdSilhouetteMaterials.Clear();
        }
    }
}
