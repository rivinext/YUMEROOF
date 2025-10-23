using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Player
{
    [DisallowMultipleComponent]
    public class PlayerOcclusionSilhouette : MonoBehaviour
    {
        public Camera overrideCamera;

        public Material silhouetteMaterial;

        public Renderer[] targetRenderers;

        public LayerMask occluderMask = ~0;

        public float checkInterval = 0.1f;
        public bool forceSilhouette = false;

        [Header("Occlusion Sampling")]
        [Tooltip("シルエット判定に使用する垂直方向のサンプル数です。1 以上の値を設定してください。")]
        public int verticalSampleCount = 3;

        [Tooltip("シルエット判定に使用する左右方向のサンプル数です。1 以上の値を設定してください。")]
        public int horizontalSampleCount = 3;

        [Tooltip("シルエット判定に使用する前後方向のサンプル数です。1 以上の値を設定してください。")]
        public int depthSampleCount = 3;

        [Tooltip("シルエットマテリアルのレンダーキューを調整したい場合は 0 より大きい値を設定します。")]
        public int silhouetteRenderQueueOverride = 0;

        [Tooltip("Override が 0 の場合にベースマテリアルから加算するオフセット値です。")]
        public int silhouetteRenderQueueOffset = 0;

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
            RefreshTargetCamera();

            if (targetRenderers == null || targetRenderers.Length == 0)
            {
                targetRenderers = GetComponentsInChildren<Renderer>();
            }

            originalMaterials.Clear();
            silhouetteMaterials.Clear();
            propertyBlocks.Clear();
            occludedMaterials.Clear();
            createdSilhouetteMaterials.Clear();

            if (targetRenderers == null)
            {
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

                Material[] runtimeMaterials = renderer.materials;
                Material[] runtimeMaterialsCopy = new Material[runtimeMaterials.Length];
                for (int m = 0; m < runtimeMaterials.Length; m++)
                {
                    runtimeMaterialsCopy[m] = runtimeMaterials[m];
                }
                originalMaterials.Add(runtimeMaterialsCopy);

                Material[] silhouetteArray = new Material[runtimeMaterials.Length];
                for (int m = 0; m < runtimeMaterials.Length; m++)
                {
                    silhouetteArray[m] = CreateSilhouetteMaterialInstance(runtimeMaterials[m]);
                }
                silhouetteMaterials.Add(silhouetteArray);

                Material[] combinedMaterials = null;
                if (runtimeMaterialsCopy != null && silhouetteArray != null)
                {
                    combinedMaterials = new Material[runtimeMaterialsCopy.Length + silhouetteArray.Length];
                    runtimeMaterialsCopy.CopyTo(combinedMaterials, 0);
                    silhouetteArray.CopyTo(combinedMaterials, runtimeMaterialsCopy.Length);
                }
                occludedMaterials.Add(combinedMaterials);

                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                propertyBlocks.Add(block);
            }
        }

        private Material CreateSilhouetteMaterialInstance(Material sourceMaterial)
        {
            if (silhouetteMaterial == null)
            {
                return null;
            }

            Material materialInstance = new Material(silhouetteMaterial);

            if (silhouetteRenderQueueOverride > 0)
            {
                materialInstance.renderQueue = silhouetteRenderQueueOverride;
            }
            else
            {
                int baseRenderQueue = silhouetteMaterial.renderQueue;
                if (sourceMaterial != null && sourceMaterial.renderQueue > baseRenderQueue)
                {
                    baseRenderQueue = sourceMaterial.renderQueue;
                }

                int renderQueueOffset = silhouetteRenderQueueOffset;
                if (renderQueueOffset == 0 && materialInstance.renderQueue <= baseRenderQueue)
                {
                    renderQueueOffset = 1;
                }

                materialInstance.renderQueue = baseRenderQueue + renderQueueOffset;
            }

            createdSilhouetteMaterials.Add(materialInstance);
            return materialInstance;
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            RefreshTargetCamera();
        }

        private void LateUpdate()
        {
            if (targetCamera == null)
            {
                RefreshTargetCamera();
            }

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

            int verticalSteps = Mathf.Max(1, verticalSampleCount);
            int horizontalSteps = Mathf.Max(1, horizontalSampleCount);
            int depthSteps = Mathf.Max(1, depthSampleCount);

            List<Vector3> samplePoints = new List<Vector3>(verticalSteps * horizontalSteps * depthSteps + 8)
            {
                bounds.center,
                bounds.center + upOffset,
                bounds.center - upOffset,
                bounds.center + forwardOffset,
                bounds.center - forwardOffset,
                bounds.center + rightOffset,
                bounds.center - rightOffset
            };

            for (int y = 0; y < verticalSteps; y++)
            {
                float verticalT = verticalSteps == 1 ? 0f : Mathf.Lerp(-1f, 1f, y / (float)(verticalSteps - 1));
                Vector3 verticalOffset = upOffset * verticalT;

                for (int x = 0; x < horizontalSteps; x++)
                {
                    float horizontalT = horizontalSteps == 1 ? 0f : Mathf.Lerp(-1f, 1f, x / (float)(horizontalSteps - 1));
                    Vector3 horizontalOffset = rightOffset * horizontalT;

                    for (int z = 0; z < depthSteps; z++)
                    {
                        float depthT = depthSteps == 1 ? 0f : Mathf.Lerp(-1f, 1f, z / (float)(depthSteps - 1));
                        Vector3 depthOffset = forwardOffset * depthT;

                        Vector3 samplePoint = bounds.center + verticalOffset + horizontalOffset + depthOffset;
                        samplePoints.Add(samplePoint);
                    }
                }
            }

            Vector3 cameraPosition = targetCamera.transform.position;
            Transform playerTransform = transform;

            for (int i = 0; i < samplePoints.Count; i++)
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
            for (int i = 0; i < targetRenderers.Length; i++)
            {
                Renderer renderer = targetRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                if (occluded)
                {
                    Material[] combinedMaterials = occludedMaterials[i];
                    if (combinedMaterials == null)
                    {
                        continue;
                    }

                    renderer.materials = combinedMaterials;
                }
                else
                {
                    // 遮蔽されていない場合は通常マテリアルのみ
                    renderer.materials = originalMaterials[i];
                }

                MaterialPropertyBlock block = propertyBlocks[i];
                if (block != null)
                {
                    renderer.SetPropertyBlock(block);
                }
            }
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;

            if (isOccluded)
            {
                ApplyMaterials(false);
                isOccluded = false;
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;

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

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RefreshTargetCamera();
        }

        private void RefreshTargetCamera()
        {
            Camera cameraToUse = overrideCamera != null ? overrideCamera : Camera.main;

            if (cameraToUse == null && Camera.allCamerasCount > 0)
            {
                Camera[] cameras = Camera.allCameras;
                if (cameras != null && cameras.Length > 0)
                {
                    cameraToUse = cameras[0];
                }
            }

            if (cameraToUse != null)
            {
                overrideCamera = overrideCamera == null ? cameraToUse : overrideCamera;
            }

            if (targetCamera != cameraToUse)
            {
                targetCamera = cameraToUse;
                nextCheckTime = 0f;
            }
        }
    }
}
