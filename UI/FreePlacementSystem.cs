using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class FreePlacementSystem : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;
    public LayerMask floorLayer;
    public LayerMask wallLayer;
    public LayerMask furnitureLayer;
    public LayerMask anchorLayer;
    public GhostPreviewManager ghostManager;

    [Header("Highlight Settings")]
    [SerializeField] private string outlineLayerName = "Outline";

    [Header("Placement Settings")]
    public GameObject cornerMarkerPrefab;
    public GameObject removeButtonPrefab;
    public float wallSnapDistance = 0.1f;
    [Tooltip("アンカー同士の吸着を許可する距離")]
    [SerializeField] private float anchorSnapDistance = 0.3f;
    [Header("Rotation Settings")]
    [SerializeField] private float rotationStepDegrees = 5f;
    [SerializeField] private float fastRotationStepDegrees = 90f;
    [SerializeField] private float rotationHoldInterval = 0.1f;

    public float AnchorSnapDistance => anchorSnapDistance;

    private AnchorPoint snappedAnchor;
    private PlacedFurniture snappedParentFurniture;

    private float rotationHoldTimerQ;
    private float rotationHoldTimerE;

    private int outlineLayerMask;

    [Header("Player Control")]
    public PlacementPlayerControl playerControl;
    public ParticleSystem placementEffect;

    // 現在の状態
    private PlacedFurniture selectedFurniture;
    private bool isMovingFurniture = false;
    private bool isPlacingNewFurniture = false;
    private GameObject previewObject;
    private FurnitureData currentFurnitureData;
    private Vector3 moveOffset;
    private bool useDragPlane;
    private Plane dragPlane;

    // 元の位置を記憶（既存配置物の移動用）
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private PlacedFurniture originalParentFurniture;

    public System.Action OnPlacementCompleted;
    public System.Action OnPlacementCancelled;

    // FreePlacementSystem.cs に追加
    public bool IsPlacing()
    {
        return isPlacingNewFurniture || isMovingFurniture;
    }

    void Awake()
    {
        RefreshOutlineLayerMask();

        EnsurePlayerControl(true, "during Awake");

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        RefreshOutlineLayerMask();
    }
#endif

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsurePlayerControl(true, $"after scene load: {scene.name}");
    }

    private bool EnsurePlayerControl(bool logWarning = false, string context = null)
    {
        if (playerControl == null)
        {
            playerControl = FindFirstObjectByType<PlacementPlayerControl>();
            if (playerControl == null && logWarning)
            {
                string message = "FreePlacementSystem: PlacementPlayerControl not found";
                if (!string.IsNullOrEmpty(context))
                {
                    message += $" ({context})";
                }

                Debug.LogWarning(message);
            }
        }

        return playerControl != null;
    }

    private void RefreshOutlineLayerMask()
    {
        int layer = LayerMask.NameToLayer(outlineLayerName);
        outlineLayerMask = layer >= 0 ? 1 << layer : 0;
    }

    private int GetFurnitureRaycastMask()
    {
        return furnitureLayer.value | outlineLayerMask;
    }

    void Update()
    {
        HandleInput();

        if (isMovingFurniture || isPlacingNewFurniture)
        {
            UpdateFurniturePosition();
        }
    }

    void HandleInput()
    {
        // ESCキーでキャンセル（動作を分ける）
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPlacingNewFurniture)
            {
                // 新規配置の場合：インベントリを開く
                CancelNewPlacementAndOpenInventory();
            }
            else if (isMovingFurniture)
            {
                // 既存移動の場合：元の位置に戻す
                CancelMovementAndRestore();
            }
            return;
        }

        // Deleteキーで削除（移動中のみ）
        if (Input.GetKeyDown(KeyCode.Delete) && isMovingFurniture && selectedFurniture != null)
        {
            DeleteSelectedFurniture();
            return;
        }

        // 左クリック
        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverUI())
            {
                return;
            }

            if (isMovingFurniture || isPlacingNewFurniture)
            {
                PlaceFurniture();
            }
            else
            {
                SelectFurniture();
            }
        }

        // 回転（Q/E）
        if ((isMovingFurniture || isPlacingNewFurniture) && previewObject != null)
        {
            HandleRotationInput();
        }
    }

    private void HandleRotationInput()
    {
        bool isFastRotate = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        float stepAngle = isFastRotate ? fastRotationStepDegrees : rotationStepDegrees;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            RotateFurniture(-stepAngle);
            rotationHoldTimerQ = rotationHoldInterval;
        }
        else if (Input.GetKey(KeyCode.Q))
        {
            rotationHoldTimerQ -= Time.deltaTime;
            if (rotationHoldTimerQ <= 0f)
            {
                RotateFurniture(-stepAngle);
                rotationHoldTimerQ += rotationHoldInterval;
            }
        }
        else
        {
            rotationHoldTimerQ = 0f;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            RotateFurniture(stepAngle);
            rotationHoldTimerE = rotationHoldInterval;
        }
        else if (Input.GetKey(KeyCode.E))
        {
            rotationHoldTimerE -= Time.deltaTime;
            if (rotationHoldTimerE <= 0f)
            {
                RotateFurniture(stepAngle);
                rotationHoldTimerE += rotationHoldInterval;
            }
        }
        else
        {
            rotationHoldTimerE = 0f;
        }
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        if (EventSystem.current.IsPointerOverGameObject())
        {
            return true;
        }

        for (int i = 0; i < Input.touchCount; i++)
        {
            if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId))
            {
                return true;
            }
        }

        return false;
    }

    // 新規配置をキャンセルしてインベントリを開く
    void CancelNewPlacementAndOpenInventory()
    {
        if (previewObject != null)
        {
            Destroy(previewObject);
        }

        isPlacingNewFurniture = false;
        previewObject = null;
        currentFurnitureData = null;

        if (EnsurePlayerControl())
        {
            playerControl.EnableControl();
        }

        // インベントリを開く
        var inventoryUI = FindFirstObjectByType<InventoryUI>();
        if (inventoryUI != null)
        {
            inventoryUI.OpenInventory();
            inventoryUI.SwitchTab(false);
        }

        OnPlacementCancelled?.Invoke();
        snappedAnchor = null;
        snappedParentFurniture = null;
    }

    // 既存配置物の移動をキャンセルして元の位置に戻す
    void CancelMovementAndRestore()
    {
        if (previewObject != null && isMovingFurniture)
        {
            AnchorPoint parentAnchor = previewObject.transform.parent?.GetComponent<AnchorPoint>();
            if (parentAnchor != null)
            {
                parentAnchor.SetOccupied(false);
            }

            PlacedFurniture pf = previewObject.GetComponent<PlacedFurniture>();
            if (pf != null)
            {
                pf.SetParentFurniture(null);
            }

            // 元の位置に戻す
            previewObject.transform.position = originalPosition;
            previewObject.transform.rotation = originalRotation;
            if (pf != null && originalParentFurniture != null)
                pf.SetParentFurniture(originalParentFurniture);
            originalParentFurniture = null;
        }

        if (selectedFurniture != null)
        {
            selectedFurniture.SetSelected(false);
            selectedFurniture = null;
        }

        isMovingFurniture = false;
        previewObject = null;
        currentFurnitureData = null;
        useDragPlane = false;

        if (EnsurePlayerControl())
        {
            playerControl.EnableControl();
        }

        ghostManager?.DestroyGhost();
        snappedAnchor = null;
        snappedParentFurniture = null;
    }

    // 選択した家具を削除（インベントリに戻す）
    void DeleteSelectedFurniture()
    {
        if (selectedFurniture == null || !isMovingFurniture) return;

        PlacedFurniture furnitureToRemove = selectedFurniture;

        AnchorPoint parentAnchor = furnitureToRemove.transform.parent?.GetComponent<AnchorPoint>();
        if (parentAnchor != null)
        {
            parentAnchor.SetOccupied(false);
        }

        furnitureToRemove.SetParentFurniture(null);

        var furnitures = furnitureToRemove.GetComponentsInChildren<PlacedFurniture>();
        foreach (var f in furnitures)
        {
            if (f.furnitureData != null)
            {
                EnvironmentStatsManager.Instance?.RemoveValues(
                    f.furnitureData.cozy,
                    f.furnitureData.nature);
            }
        }

        string furnitureID = furnitureToRemove.furnitureData.itemID;

        foreach (var f in furnitures)
        {
            if (f != null && f.furnitureData != null)
            {
                InventoryManager.Instance?.AddFurniture(f.furnitureData.itemID, 1);
            }
        }

        if (previewObject != null)
        {
            Destroy(previewObject);
            previewObject = null;
        }

        isMovingFurniture = false;
        selectedFurniture = null;
        currentFurnitureData = null;

        if (EnsurePlayerControl())
        {
            playerControl.EnableControl();
        }

        Debug.Log($"Furniture {furnitureID} returned to inventory");

        var inventoryUI = FindFirstObjectByType<InventoryUI>();
        if (inventoryUI != null && inventoryUI.IsOpen)
        {
            //inventoryUI.OpenInventory();
            //inventoryUI.SwitchTab(false);
            inventoryUI.RefreshInventoryDisplay();
        }

        if (FurnitureSaveManager.Instance != null)
        {
            FurnitureSaveManager.Instance.RemoveFurniture(furnitureToRemove);
        }

        ghostManager?.DestroyGhost();
        snappedAnchor = null;
    }

    void SelectFurniture()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 600f, GetFurnitureRaycastMask(), QueryTriggerInteraction.Ignore))
        {
            PlacedFurniture furniture = hit.collider.GetComponentInParent<PlacedFurniture>();

            if (furniture != null && furniture.furnitureData.isMovable)
            {
                // 前の選択を解除
                if (selectedFurniture != null)
                {
                    selectedFurniture.SetSelected(false);
                }

                // 新しい家具を選択
                selectedFurniture = furniture;
                selectedFurniture.SetSelected(true);

                // 同じレイを床にも飛ばし、床のヒット位置を取得
                RaycastHit floorHit;
                Vector3 floorPoint = hit.point;
                if (Physics.Raycast(ray, out floorHit, 100f, floorLayer, QueryTriggerInteraction.Ignore))
                {
                    floorPoint = floorHit.point;
                }

                // 移動モード開始（配置ルールに応じた参照位置を渡す）
                Vector3 referencePoint = floorPoint;
                if (furniture.furnitureData != null &&
                    furniture.furnitureData.placementRules == PlacementRule.Wall)
                {
                    referencePoint = hit.point;
                }

                StartMovingFurniture(furniture, referencePoint);
            }
        }
        else
        {
            if (selectedFurniture != null)
            {
                selectedFurniture.SetSelected(false);
                selectedFurniture = null;
            }
        }
    }

    void StartMovingFurniture(PlacedFurniture furniture, Vector3 referencePoint)
    {
        AnchorPoint parentAnchor = furniture.transform.parent?.GetComponent<AnchorPoint>();
        if (parentAnchor != null)
        {
            parentAnchor.SetOccupied(false);
        }
        originalParentFurniture = furniture.parentFurniture;
        furniture.SetParentFurniture(null);
        snappedAnchor = null;

        isMovingFurniture = true;

        if (EnsurePlayerControl())
        {
            playerControl.DisableControl();
        }

        previewObject = furniture.gameObject;
        currentFurnitureData = furniture.furnitureData;

        // 元の位置を保存（Escキーで戻すため）
        originalPosition = furniture.transform.position;
        originalRotation = furniture.transform.rotation;

        // ゴーストを生成
        ghostManager?.CreateGhost(previewObject, originalPosition, originalRotation);

        useDragPlane = false;

        // 参照位置とオブジェクト位置の差をオフセットとして保存
        moveOffset = furniture.transform.position - referencePoint;

        if (furniture.furnitureData == null ||
            furniture.furnitureData.placementRules != PlacementRule.Wall)
        {
            // 高さ方向の参照に床を使うと操作量が増幅されるため、
            // オブジェクトの高さに合わせたドラッグ用平面を生成する
            dragPlane = new Plane(Vector3.up, furniture.transform.position);
            useDragPlane = true;

            Vector3 planeReference = dragPlane.ClosestPointOnPlane(referencePoint);
            moveOffset = furniture.transform.position - planeReference;
        }
    }

    public void StartPlacingNewFurniture(GameObject furniturePrefab, FurnitureData data)
    {
        CancelCurrentAction();

        isPlacingNewFurniture = true;
        currentFurnitureData = data;

        previewObject = Instantiate(furniturePrefab);
        previewObject.name = "Preview_" + data.nameID;

        PlacedFurniture placedComp = previewObject.AddComponent<PlacedFurniture>();
        placedComp.furnitureData = data;

        CreateCornerMarkers(placedComp);
        placedComp.SetSelected(true);

        if (EnsurePlayerControl())
        {
            playerControl.DisableControl();
        }
    }

    void UpdateFurniturePosition()
    {
        if (previewObject == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // まずアンカーへのレイキャストを行う
        bool snappedByAnchorRaycast = false;
        float sphereRadius = Mathf.Max(anchorSnapDistance, 0.01f);
        RaycastHit[] anchorHits = Physics.SphereCastAll(ray, sphereRadius, 600f, anchorLayer);
        if (anchorHits.Length > 0)
        {
            System.Array.Sort(anchorHits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var anchorHit in anchorHits)
            {
                AnchorPoint anchor = anchorHit.collider.GetComponent<AnchorPoint>();
                if (anchor == null) continue;
                if (previewObject != null && anchor.transform.IsChildOf(previewObject.transform))
                    continue;
                if (anchor.IsOccupied) continue;

                snappedAnchor = anchor;
                var pf = previewObject.GetComponent<PlacedFurniture>();
                Vector3 offset = pf != null ? pf.GetBottomOffset() : Vector3.zero;
                Vector3 targetPosition = snappedAnchor.transform.position + offset;

                previewObject.transform.position = targetPosition;

                PlacedFurniture placedComp = previewObject.GetComponent<PlacedFurniture>();
                if (placedComp != null)
                {
                    bool canPlace = !placedComp.IsOverlapping();
                    placedComp.SetPlacementValid(canPlace);
                }

                snappedByAnchorRaycast = true;
                break;
            }
        }

        if (snappedByAnchorRaycast)
        {
            return;
        }

        snappedAnchor = null;

        LayerMask targetLayer = floorLayer;

        if (currentFurnitureData.placementRules == PlacementRule.Wall)
        {
            targetLayer = wallLayer;
        }
        else if (currentFurnitureData.placementRules == PlacementRule.Both)
        {
            targetLayer = floorLayer | wallLayer;
        }

        Vector3 targetPosition;
        bool hasTargetPosition = false;
        bool hasSurfaceHit = false;
        RaycastHit surfaceHit = default;

        if (isMovingFurniture && useDragPlane)
        {
            float enter;
            if (dragPlane.Raycast(ray, out enter))
            {
                Vector3 planePoint = ray.GetPoint(enter);
                targetPosition = planePoint + moveOffset;
                hasTargetPosition = true;
            }
        }

        if (!hasTargetPosition && Physics.Raycast(ray, out hit, 300f, targetLayer, QueryTriggerInteraction.Ignore))
        {
            targetPosition = hit.point;
            hasTargetPosition = true;
            hasSurfaceHit = true;
            surfaceHit = hit;

            if (isMovingFurniture && (!useDragPlane || currentFurnitureData.placementRules == PlacementRule.Wall))
            {
                targetPosition += moveOffset;
            }
        }

        if (!hasTargetPosition)
        {
            return;
        }

        if (currentFurnitureData.placementRules == PlacementRule.Wall &&
            hasSurfaceHit &&
            ((1 << surfaceHit.collider.gameObject.layer) & wallLayer) != 0)
        {
            Vector3 projected = Vector3.ProjectOnPlane(surfaceHit.normal, Vector3.up);
            if (projected.sqrMagnitude < Mathf.Epsilon)
            {
                projected = Vector3.forward;
            }

            Quaternion targetRotation = Quaternion.LookRotation(projected, Vector3.up);
            previewObject.transform.rotation = targetRotation;

            targetPosition = CalculateWallSnapPosition(previewObject, targetPosition, surfaceHit.point, surfaceHit.normal);
        }

        CheckStackPlacement(ref targetPosition);
        TrySnapToAnchor(ref targetPosition);

        if (isMovingFurniture &&
            useDragPlane &&
            snappedParentFurniture == null &&
            currentFurnitureData.placementRules != PlacementRule.Wall)
        {
            Ray downRay = new Ray(targetPosition + Vector3.up * 5f, Vector3.down);
            if (Physics.Raycast(downRay, out var downHit, 15f, floorLayer, QueryTriggerInteraction.Ignore))
            {
                targetPosition.y = downHit.point.y;
            }
        }

        previewObject.transform.position = targetPosition;

        PlacedFurniture placedComp = previewObject.GetComponent<PlacedFurniture>();
        if (placedComp != null)
        {
            bool canPlace = !placedComp.IsOverlapping();
            placedComp.SetPlacementValid(canPlace);
        }
    }

    // 残りのメソッドは同じ...
    Vector3 CalculateWallSnapPosition(GameObject preview, Vector3 targetPosition, Vector3 hitPoint, Vector3 wallNormal)
    {
        if (preview == null)
        {
            return targetPosition;
        }

        if (wallNormal.sqrMagnitude < Mathf.Epsilon)
        {
            return targetPosition;
        }

        Transform previewTransform = preview.transform;
        Vector3 normalizedNormal = wallNormal.normalized;
        Renderer[] renderers = preview.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return targetPosition + normalizedNormal * wallSnapDistance;
        }

        PlacedFurniture previewPlacedFurniture = preview.GetComponent<PlacedFurniture>();
        if (previewPlacedFurniture != null)
        {
            List<Renderer> filteredRenderers = new List<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                PlacedFurniture rendererOwner = renderer.GetComponentInParent<PlacedFurniture>();
                if (rendererOwner == previewPlacedFurniture)
                {
                    filteredRenderers.Add(renderer);
                }
            }

            if (filteredRenderers.Count == 0)
            {
                return targetPosition + normalizedNormal * wallSnapDistance;
            }

            renderers = filteredRenderers.ToArray();
        }

        Vector3 minLocalPoint = Vector3.zero;
        float minLocalZ = 0f;
        bool foundPoint = false;

        foreach (Renderer renderer in renderers)
        {
            Bounds localBounds = renderer.localBounds;
            Vector3 center = localBounds.center;
            Vector3 extents = localBounds.extents;

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 cornerLocal = new Vector3(
                            center.x + extents.x * x,
                            center.y + extents.y * y,
                            center.z + extents.z * z);

                        Vector3 worldPoint = renderer.transform.TransformPoint(cornerLocal);
                        Vector3 previewLocal = previewTransform.InverseTransformPoint(worldPoint);

                        if (!foundPoint || previewLocal.z < minLocalZ)
                        {
                            minLocalPoint = previewLocal;
                            minLocalZ = previewLocal.z;
                            foundPoint = true;
                        }
                    }
                }
            }
        }

        if (!foundPoint)
        {
            return targetPosition + normalizedNormal * wallSnapDistance;
        }

        Vector3 offset = previewTransform.TransformPoint(minLocalPoint) - previewTransform.position;
        Vector3 planePoint = hitPoint + normalizedNormal * wallSnapDistance;
        Vector3 worldPointAtTarget = targetPosition + offset;
        float distanceToPlane = Vector3.Dot(normalizedNormal, worldPointAtTarget - planePoint);

        return targetPosition - normalizedNormal * distanceToPlane;
    }

    void CheckStackPlacement(ref Vector3 position)
    {
        snappedParentFurniture = null;

        Ray ray = new Ray(position + Vector3.up * 5f, Vector3.down);
        RaycastHit[] hits = Physics.RaycastAll(ray, 10f, GetFurnitureRaycastMask(), QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            // 自身(previewObject)へのヒットは無視する
            if (previewObject != null &&
                (hit.collider.transform == previewObject.transform ||
                 hit.collider.transform.IsChildOf(previewObject.transform)))
            {
                continue;
            }

            PlacedFurniture targetFurniture = hit.collider.GetComponentInParent<PlacedFurniture>();
            Debug.Log($"CheckStackPlacement: Raycast hit {hit.collider.name}");

            if (targetFurniture != null &&
                targetFurniture.furnitureData.canStackOn &&
                !targetFurniture.isOnSurface)
            {
                position.y = targetFurniture.GetSurfaceHeight();
                snappedParentFurniture = targetFurniture;
                Debug.Log($"CheckStackPlacement: Snapped to {targetFurniture.name}");
            }
            else
            {
                Debug.Log("CheckStackPlacement: Hit furniture is not stackable");
            }

            return;
        }

        Debug.Log("CheckStackPlacement: No furniture hit");
    }

    void TrySnapToAnchor(ref Vector3 target)
    {
        AnchorPoint nearest = null;
        float nearestDistance = float.MaxValue;

        AnchorPoint[] anchors = FindObjectsOfType<AnchorPoint>();
        foreach (var anchor in anchors)
        {
            if (anchor.IsOccupied) continue;
            if (previewObject != null && anchor.transform.IsChildOf(previewObject.transform)) continue;

            float distance = Vector3.Distance(target, anchor.transform.position);
            float snapRadius = Mathf.Min(anchor.SnapRadius, anchorSnapDistance);

            if (distance <= snapRadius && distance < nearestDistance)
            {
                nearest = anchor;
                nearestDistance = distance;
            }
        }

        if (nearest != null && previewObject != null)
        {
            var pf = previewObject.GetComponent<PlacedFurniture>();
            Vector3 offset = pf != null ? pf.GetBottomOffset() : Vector3.zero;
            target = nearest.transform.position + offset;
            snappedAnchor = nearest;
            Debug.Log($"TrySnapToAnchor: Snapped to {nearest.name} (distance {nearestDistance:F2})");
        }
        else
        {
            Debug.Log("TrySnapToAnchor: No anchor within range");
            snappedAnchor = null;
        }
    }

    void PlaceFurniture()
    {
        if (previewObject == null) return;

        bool anchorUsed = false;
        if (snappedAnchor != null && !snappedAnchor.IsOccupied)
        {
            previewObject.transform.SetParent(snappedAnchor.transform);
            previewObject.transform.localPosition = previewObject.GetComponent<PlacedFurniture>().GetBottomOffset();
            anchorUsed = true;
            Debug.Log($"PlaceFurniture: Placed on anchor {snappedAnchor.name}");
        }

        PlacedFurniture placedComp = previewObject.GetComponent<PlacedFurniture>();
        if (placedComp != null && !placedComp.IsOverlapping())
        {
            if (isPlacingNewFurniture)
            {
                placedComp.SetSelected(false);
            }
            else if (isMovingFurniture)
            {
                selectedFurniture.SetSelected(false);
                selectedFurniture = null;
            }

            if (anchorUsed)
            {
                var parentPF = snappedAnchor.GetComponentInParent<PlacedFurniture>();
                if (parentPF != null) placedComp.SetParentFurniture(parentPF);
                previewObject.transform.SetParent(snappedAnchor.transform);
                snappedParentFurniture = null;
            }
            else if (snappedParentFurniture != null)
            {
                placedComp.SetParentFurniture(snappedParentFurniture);
                Debug.Log($"PlaceFurniture: Set parent to {snappedParentFurniture.name}");
                snappedParentFurniture = null;
            }

            if (isPlacingNewFurniture && placedComp.furnitureData.isMovable)
            {
                var storeButton = placedComp.gameObject.AddComponent<StoreToInventoryButton>();
            }

            if (isPlacingNewFurniture && placedComp.furnitureData != null)
            {
                EnvironmentStatsManager.Instance?.AddValues(
                    placedComp.furnitureData.cozy,
                    placedComp.furnitureData.nature);
            }

            AnimatePlacementScale(placedComp);
            PlayPlacementEffect(placedComp.transform.position);

            if (anchorUsed)
            {
                snappedAnchor.SetOccupied(true);
            }
            snappedAnchor = null;

            OnPlacementCompleted?.Invoke();

            isMovingFurniture = false;
            isPlacingNewFurniture = false;
            previewObject = null;
            currentFurnitureData = null;
            useDragPlane = false;

            if (EnsurePlayerControl())
            {
                playerControl.EnableControl();
            }
            originalParentFurniture = null;
        }
        // 家具配置データを保存
        if (FurnitureSaveManager.Instance != null)
        {
            string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            FurnitureSaveManager.Instance.SaveFurniture(placedComp, currentSceneName);
        }

        ghostManager?.DestroyGhost();
        snappedParentFurniture = null;
    }

    private void AnimatePlacementScale(PlacedFurniture placed)
    {
        if (placed == null)
        {
            return;
        }

        Transform targetTransform = placed.transform;
        if (targetTransform == null)
        {
            return;
        }

        PlacementScaleAnimator animator = targetTransform.GetComponent<PlacementScaleAnimator>();
        if (animator == null)
        {
            animator = targetTransform.gameObject.AddComponent<PlacementScaleAnimator>();
        }

        animator.Play();
    }

    void HandleStackPlacement(PlacedFurniture furniture)
    {
        Ray ray = new Ray(furniture.transform.position + Vector3.up * 5f, Vector3.down);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 10f, GetFurnitureRaycastMask(), QueryTriggerInteraction.Ignore))
        {
            PlacedFurniture parentFurniture = hit.collider.GetComponentInParent<PlacedFurniture>();

            if (parentFurniture != null &&
                parentFurniture != furniture &&
                parentFurniture.furnitureData.canStackOn &&
                !parentFurniture.isOnSurface)
            {
                furniture.SetParentFurniture(parentFurniture);
            }
        }
    }

    private void PlayPlacementEffect(Vector3 position)
    {
        if (placementEffect == null) return;
        placementEffect.transform.position = position;
        placementEffect.Play();
    }

    void RotateFurniture(float angle)
    {
        if (previewObject != null && currentFurnitureData.placementRules != PlacementRule.Wall)
        {
            previewObject.transform.Rotate(0, angle, 0);
        }
    }

    void CancelCurrentAction()
    {
        if (selectedFurniture != null)
        {
            selectedFurniture.SetSelected(false);
            selectedFurniture = null;
        }

        if (isPlacingNewFurniture && previewObject != null)
        {
            Destroy(previewObject);
        }

        isMovingFurniture = false;
        isPlacingNewFurniture = false;
        previewObject = null;
        currentFurnitureData = null;
        useDragPlane = false;
        originalParentFurniture = null;

        if (EnsurePlayerControl())
        {
            playerControl.EnableControl();
        }

        ghostManager?.DestroyGhost();
        snappedAnchor = null;
        snappedParentFurniture = null;
    }

    void OnDisable()
    {
        if (isMovingFurniture && originalParentFurniture != null && previewObject != null)
        {
            PlacedFurniture pf = previewObject.GetComponent<PlacedFurniture>();
            if (pf != null)
                pf.SetParentFurniture(originalParentFurniture);
            originalParentFurniture = null;
        }
    }

    public void CreateCornerMarkers(PlacedFurniture furniture)
    {
        if (cornerMarkerPrefab == null) return;

        if (furniture.cornerMarkers != null && furniture.cornerMarkers.Length > 0)
        {
            foreach (var existing in furniture.cornerMarkers)
            {
                if (existing != null)
                    return; // すでにマーカーが存在する場合は生成しない
            }
        }

        Renderer[] renderers = furniture.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        furniture.cornerMarkers = new GameObject[4];

        PlacementRule placementRule = furniture.furnitureData != null
            ? furniture.furnitureData.placementRules
            : PlacementRule.Floor;

        if (placementRule == PlacementRule.Wall)
        {
            Vector3 localMin = Vector3.zero;
            Vector3 localMax = Vector3.zero;
            bool hasLocalBounds = false;

            foreach (Renderer renderer in renderers)
            {
                Bounds localBounds = renderer.localBounds;
                Vector3 center = localBounds.center;
                Vector3 extents = localBounds.extents;

                for (int xSign = -1; xSign <= 1; xSign += 2)
                {
                    for (int ySign = -1; ySign <= 1; ySign += 2)
                    {
                        for (int zSign = -1; zSign <= 1; zSign += 2)
                        {
                            Vector3 rendererLocalCorner = center + Vector3.Scale(extents, new Vector3(xSign, ySign, zSign));
                            Vector3 worldCorner = renderer.transform.TransformPoint(rendererLocalCorner);
                            Vector3 furnitureLocalCorner = furniture.transform.InverseTransformPoint(worldCorner);

                            if (!hasLocalBounds)
                            {
                                localMin = furnitureLocalCorner;
                                localMax = furnitureLocalCorner;
                                hasLocalBounds = true;
                            }
                            else
                            {
                                localMin = Vector3.Min(localMin, furnitureLocalCorner);
                                localMax = Vector3.Max(localMax, furnitureLocalCorner);
                            }
                        }
                    }
                }
            }

            if (!hasLocalBounds)
                return;

            float minLocalZ = localMin.z;

            Vector3[] localCorners = new Vector3[]
            {
                new Vector3(localMin.x, localMin.y, minLocalZ),
                new Vector3(localMin.x, localMax.y, minLocalZ),
                new Vector3(localMax.x, localMax.y, minLocalZ),
                new Vector3(localMax.x, localMin.y, minLocalZ)
            };

            float[] cornerRotations = { 90f, 0f, 270f, 180f };
            Quaternion baseRotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);

            for (int i = 0; i < localCorners.Length; i++)
            {
                Vector3 worldCorner = furniture.transform.TransformPoint(localCorners[i]);
                GameObject marker = Instantiate(cornerMarkerPrefab, worldCorner, Quaternion.identity);
                marker.transform.SetParent(furniture.transform);
                marker.transform.localPosition = localCorners[i];
                marker.transform.localRotation = baseRotation * Quaternion.AngleAxis(cornerRotations[i], Vector3.forward);
                furniture.cornerMarkers[i] = marker;
                marker.SetActive(false);
            }
        }
        else
        {
            float markerHeight = bounds.min.y + 0.01f;

            Vector3[] corners = new Vector3[]
            {
                new Vector3(bounds.min.x, markerHeight, bounds.min.z),
                new Vector3(bounds.max.x, markerHeight, bounds.min.z),
                new Vector3(bounds.max.x, markerHeight, bounds.max.z),
                new Vector3(bounds.min.x, markerHeight, bounds.max.z)
            };

            float[] yRotations = { 270f, 180f, 90f, 0f };

            for (int i = 0; i < 4; i++)
            {
                Quaternion rotation = Quaternion.Euler(90f, yRotations[i], 0f);
                GameObject marker = Instantiate(cornerMarkerPrefab, corners[i], rotation);
                marker.transform.SetParent(furniture.transform);
                furniture.cornerMarkers[i] = marker;
                marker.SetActive(false);
            }
        }
    }
}
