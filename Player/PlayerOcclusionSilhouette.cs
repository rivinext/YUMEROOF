using System;
using System.Collections.Generic;
using UnityEngine;

namespace Player
{
    [DisallowMultipleComponent]
    public class PlayerOcclusionSilhouette : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Override material used for silhouettes. If unset, a material will be generated from the shader.")]
        private Material silhouetteMaterialOverride;

        [SerializeField]
        [Tooltip("Renderers that will receive the silhouette material. Leave empty to auto-collect child renderers.")]
        private Renderer[] silhouetteTargetRenderers;

        [SerializeField]
        [Tooltip("If true, child renderers are collected automatically; otherwise, use the provided renderer list.")]
        private bool autoCollectRenderers = true;

        [SerializeField]
        [Tooltip("Layers that will be treated as occluders when checking visibility.")]
        private LayerMask occluderMask = ~0;

        [SerializeField]
        [Tooltip("If true, screen-space depth is sampled before physics checks to determine occlusion.")]
        private bool useDepthOcclusion;

        private enum OcclusionCheckOrder
        {
            PhysicsOnly,
            DepthOnly,
            DepthThenPhysics,
            PhysicsThenDepth
        }

        [SerializeField]
        [Tooltip("Determines how depth and physics occlusion checks are combined.")]
        private OcclusionCheckOrder occlusionCheckOrder = OcclusionCheckOrder.DepthThenPhysics;

        public bool ForceSilhouette { get; set; }

        private Camera overrideCamera;
        private Material silhouetteMaterialTemplate;
        private Renderer[] targetRenderers;

        private readonly List<Material[]> originalMaterials = new List<Material[]>();
        private readonly List<Material[]> silhouetteMaterials = new List<Material[]>();
        private readonly List<MaterialPropertyBlock> propertyBlocks = new List<MaterialPropertyBlock>();
        private readonly List<Material[]> occludedMaterials = new List<Material[]>();
        private readonly List<Material> createdSilhouetteMaterials = new List<Material>();

        private int lastAutoCollectedRendererCount = -1;
        private bool rendererSetDirty;

        private Collider playerCollider;
        private Camera targetCamera;
        private int playerLayer = -1;
        private Transform playerRootOverride;
        private Transform[] ignoredOccluderRoots;
        private Transform currentPlayerRoot;
        private bool isOccluded;
        private float nextCheckTime;
        private Texture2D depthSampleTexture;
        private static readonly RaycastHit[] RaycastHits = new RaycastHit[4];
        private const float CheckInterval = 0.1f;

        private void Awake()
        {
            playerCollider = GetComponent<Collider>();
            silhouetteMaterialTemplate = CreateSilhouetteTemplate();
            targetCamera = overrideCamera != null ? overrideCamera : Camera.main;

            EnsureDepthTexture();

            UpdatePlayerRoot();

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

            bool hasManualTargets = silhouetteTargetRenderers != null && silhouetteTargetRenderers.Length > 0;
            bool shouldAutoCollect = autoCollectRenderers || !hasManualTargets && targetRenderers == null;

            Renderer[] manualRenderers = hasManualTargets ? silhouetteTargetRenderers : null;
            Renderer[] autoCollectedRenderers = shouldAutoCollect ? CollectChildRenderers() : null;

            if (autoCollectedRenderers != null)
            {
                lastAutoCollectedRendererCount = autoCollectedRenderers.Length;
            }

            targetRenderers = MergeTargetRenderers(manualRenderers, autoCollectedRenderers);

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

                Material[] silhouetteArray = null;
                Material template = silhouetteMaterialTemplate ?? CreateSilhouetteTemplate();
                if (template != null)
                {
                    silhouetteMaterialTemplate = template;
                    silhouetteArray = new Material[sharedMaterials.Length];
                    for (int m = 0; m < sharedMaterials.Length; m++)
                    {
                        silhouetteArray[m] = CreateSilhouetteMaterialInstance(sharedMaterials[m]);
                    }
                }
                silhouetteMaterials.Add(silhouetteArray);

                Material[] combinedMaterials = null;
                if (sharedMaterialsCopy != null && silhouetteArray != null)
                {
                    combinedMaterials = new Material[sharedMaterialsCopy.Length + silhouetteArray.Length];
                    Array.Copy(sharedMaterialsCopy, combinedMaterials, sharedMaterialsCopy.Length);
                    Array.Copy(silhouetteArray, 0, combinedMaterials, sharedMaterialsCopy.Length, silhouetteArray.Length);
                }
                else if (sharedMaterialsCopy != null)
                {
                    combinedMaterials = sharedMaterialsCopy;
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
            if (silhouetteMaterialTemplate == null)
            {
                silhouetteMaterialTemplate = CreateSilhouetteTemplate();
                if (silhouetteMaterialTemplate == null)
                {
                    return null;
                }
            }

            Material materialInstance = new Material(silhouetteMaterialTemplate);

            int baseRenderQueue = silhouetteMaterialTemplate.renderQueue;
            if (sourceMaterial != null && materialInstance.renderQueue <= sourceMaterial.renderQueue)
            {
                baseRenderQueue = sourceMaterial.renderQueue + 1;
            }

            materialInstance.renderQueue = baseRenderQueue;

            createdSilhouetteMaterials.Add(materialInstance);
            return materialInstance;
        }

        private Material CreateSilhouetteTemplate()
        {
            if (silhouetteMaterialOverride != null)
            {
                return silhouetteMaterialOverride;
            }

            Shader shader = Shader.Find("Custom/URP/OccludedSilhouette");
            if (shader == null)
            {
                Debug.LogWarning("Custom/URP/OccludedSilhouette shader not found; silhouettes will be disabled.", this);
                return null;
            }

            Material template = new Material(shader);
            return template;
        }

        private void LateUpdate()
        {
            if (autoCollectRenderers)
            {
                if (rendererSetDirty || DetectRendererSetChanges())
                {
                    rendererSetDirty = false;
                    RefreshTargetRenderers();
                }
            }

            if (targetCamera == null)
            {
                targetCamera = overrideCamera != null ? overrideCamera : Camera.main;
            }

            EnsureDepthTexture();

            if (targetCamera == null || playerCollider == null)
            {
                return;
            }

            UpdatePlayerRoot();

            if (ForceSilhouette)
            {
                if (!isOccluded)
                {
                    isOccluded = true;
                    ApplyMaterials(true);
                }

                return;
            }

            if (CheckInterval > 0f && Time.time < nextCheckTime)
            {
                return;
            }

            nextCheckTime = Time.time + Mathf.Max(0f, CheckInterval);

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
            Bounds bounds = GetReferenceBounds();

            switch (occlusionCheckOrder)
            {
                case OcclusionCheckOrder.PhysicsOnly:
                    return CheckPhysicsOcclusion(bounds);
                case OcclusionCheckOrder.DepthOnly:
                    return useDepthOcclusion && CheckDepthOcclusion(bounds);
                case OcclusionCheckOrder.DepthThenPhysics:
                    return (useDepthOcclusion && CheckDepthOcclusion(bounds)) || CheckPhysicsOcclusion(bounds);
                case OcclusionCheckOrder.PhysicsThenDepth:
                    return CheckPhysicsOcclusion(bounds) || (useDepthOcclusion && CheckDepthOcclusion(bounds));
                default:
                    return CheckPhysicsOcclusion(bounds);
            }
        }

        private Bounds GetReferenceBounds()
        {
            if (targetRenderers != null && targetRenderers.Length > 0)
            {
                bool initialized = false;
                Bounds combined = default;
                for (int i = 0; i < targetRenderers.Length; i++)
                {
                    Renderer renderer = targetRenderers[i];
                    if (renderer == null)
                    {
                        continue;
                    }

                    if (!initialized)
                    {
                        combined = renderer.bounds;
                        initialized = true;
                    }
                    else
                    {
                        combined.Encapsulate(renderer.bounds);
                    }
                }

                if (initialized)
                {
                    return combined;
                }
            }

            return playerCollider.bounds;
        }

        private Vector3[] BuildSamplePoints(Bounds bounds)
        {
            Vector3 upOffset = Vector3.up * bounds.extents.y;
            Vector3 forwardOffset = transform.forward * bounds.extents.z;
            Vector3 rightOffset = transform.right * bounds.extents.x;

            return new[]
            {
                bounds.center,
                bounds.center + upOffset,
                bounds.center - upOffset * 0.5f,
                bounds.center + forwardOffset * 0.5f,
                bounds.center - forwardOffset * 0.5f,
                bounds.center + rightOffset * 0.5f,
                bounds.center - rightOffset * 0.5f
            };
        }

        private bool CheckPhysicsOcclusion(Bounds bounds)
        {
            Vector3[] samplePoints = BuildSamplePoints(bounds);
            Vector3 cameraPosition = targetCamera.transform.position;
            Transform playerTransform = transform;
            Transform playerRoot = currentPlayerRoot != null ? currentPlayerRoot : ResolvePlayerRoot();
            LayerMask occluderMaskWithoutPlayer = occluderMask;
            int excludedLayer = playerLayer >= 0 ? playerLayer : playerTransform.gameObject.layer;
            if (excludedLayer >= 0 && excludedLayer < 32)
            {
                occluderMaskWithoutPlayer &= ~(1 << excludedLayer);
            }

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
                int hitCount = Physics.RaycastNonAlloc(point, direction, RaycastHits, distance, occluderMaskWithoutPlayer, QueryTriggerInteraction.Ignore);
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

                    Transform hitTransform = hitCollider.transform;

                    if (ShouldIgnoreHit(hitTransform))
                    {
                        continue;
                    }

                    if (hitTransform == playerTransform || hitTransform.IsChildOf(playerTransform) || hitTransform.root == playerRoot)
                    {
                        continue;
                    }

                    return true;
                }
            }

            return false;
        }

        private bool CheckDepthOcclusion(Bounds bounds)
        {
            RenderTexture depthTexture = Shader.GetGlobalTexture("_CameraDepthTexture") as RenderTexture;
            if (depthTexture == null)
            {
                return false;
            }

            if (depthSampleTexture == null)
            {
                depthSampleTexture = new Texture2D(1, 1, TextureFormat.RFloat, false, true);
            }

            Vector3[] samplePoints = BuildSamplePoints(bounds);

            for (int i = 0; i < samplePoints.Length; i++)
            {
                Vector3 point = samplePoints[i];
                Vector3 viewportPoint = targetCamera.WorldToViewportPoint(point);
                if (viewportPoint.z <= 0f)
                {
                    continue;
                }

                if (viewportPoint.x < 0f || viewportPoint.x > 1f || viewportPoint.y < 0f || viewportPoint.y > 1f)
                {
                    continue;
                }

                float linearDepthToPlayer = targetCamera.WorldToCameraMatrix.MultiplyPoint(point).z;
                if (linearDepthToPlayer <= 0f)
                {
                    continue;
                }

                int pixelX = Mathf.Clamp(Mathf.RoundToInt(viewportPoint.x * depthTexture.width), 0, depthTexture.width - 1);
                int pixelY = Mathf.Clamp(Mathf.RoundToInt(viewportPoint.y * depthTexture.height), 0, depthTexture.height - 1);

                RenderTexture active = RenderTexture.active;
                RenderTexture.active = depthTexture;
                depthSampleTexture.ReadPixels(new Rect(pixelX, pixelY, 1, 1), 0, 0);
                depthSampleTexture.Apply();
                RenderTexture.active = active;

                float rawDepth = depthSampleTexture.GetPixel(0, 0).r;
                float linearDepth = RawToLinearDepth(rawDepth);

                if (linearDepth > 0f && linearDepth < linearDepthToPlayer)
                {
                    return true;
                }
            }

            return false;
        }

        private float RawToLinearDepth(float rawDepth)
        {
            float near = targetCamera.nearClipPlane;
            float far = targetCamera.farClipPlane;
            return (2f * near) / (far + near - rawDepth * (far - near));
        }

        private void EnsureDepthTexture()
        {
            if (!useDepthOcclusion)
            {
                return;
            }

            if (targetCamera == null)
            {
                return;
            }

            targetCamera.depthTextureMode |= DepthTextureMode.Depth;
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

        private void OnEnable()
        {
            UpdatePlayerRoot();
        }

        private void OnTransformParentChanged()
        {
            UpdatePlayerRoot();
        }

        private void OnTransformChildrenChanged()
        {
            if (autoCollectRenderers)
            {
                rendererSetDirty = true;
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

            if (depthSampleTexture != null)
            {
                Destroy(depthSampleTexture);
                depthSampleTexture = null;
            }
        }

        private void UpdatePlayerRoot()
        {
            currentPlayerRoot = ResolvePlayerRoot();
        }

        private bool DetectRendererSetChanges()
        {
            int rendererCount = CountChildRenderers();
            if (rendererCount != lastAutoCollectedRendererCount)
            {
                lastAutoCollectedRendererCount = rendererCount;
                return true;
            }

            return false;
        }

        private Renderer[] CollectChildRenderers()
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

            return filteredRenderers.ToArray();
        }

        private Renderer[] MergeTargetRenderers(Renderer[] manualRenderers, Renderer[] autoCollectedRenderers)
        {
            bool hasManual = manualRenderers != null && manualRenderers.Length > 0;
            bool hasAuto = autoCollectedRenderers != null && autoCollectedRenderers.Length > 0;

            if (!hasManual && !hasAuto)
            {
                return null;
            }

            HashSet<Renderer> uniqueRenderers = new HashSet<Renderer>();
            List<Renderer> merged = new List<Renderer>();

            if (hasManual)
            {
                for (int i = 0; i < manualRenderers.Length; i++)
                {
                    Renderer renderer = manualRenderers[i];
                    if (renderer != null && uniqueRenderers.Add(renderer))
                    {
                        merged.Add(renderer);
                    }
                }
            }

            if (hasAuto)
            {
                for (int i = 0; i < autoCollectedRenderers.Length; i++)
                {
                    Renderer renderer = autoCollectedRenderers[i];
                    if (renderer != null && uniqueRenderers.Add(renderer))
                    {
                        merged.Add(renderer);
                    }
                }
            }

            return merged.ToArray();
        }

        private int CountChildRenderers()
        {
            Renderer[] childRenderers = GetComponentsInChildren<Renderer>(true);
            int count = 0;
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

                count++;
            }

            return count;
        }

        private Transform ResolvePlayerRoot()
        {
            if (playerRootOverride != null)
            {
                return playerRootOverride;
            }

            return transform.root;
        }

        private bool ShouldIgnoreHit(Transform hitTransform)
        {
            if (ignoredOccluderRoots == null)
            {
                return false;
            }

            for (int i = 0; i < ignoredOccluderRoots.Length; i++)
            {
                Transform ignoredRoot = ignoredOccluderRoots[i];
                if (ignoredRoot == null)
                {
                    continue;
                }

                if (hitTransform == ignoredRoot || hitTransform.IsChildOf(ignoredRoot))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
