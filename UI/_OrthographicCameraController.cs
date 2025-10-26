using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections;

public class OrthographicCameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    public Transform cameraTarget;         // カメラの注視点（屋上の中心）
    public float rotationSpeed = 1f;       // 回転速度
    public float panSpeed = 3f;            // パン速度

    [Header("Default Camera Position")]
    public float defaultDistance = 15f;    // デフォルトのカメラ距離
    public float defaultAngleX = 22f;      // デフォルトのX軸角度（見下ろし角度）
    public float defaultAngleY = 45f;      // デフォルトのY軸角度（水平回転）
    public float defaultFieldOfView = 60f;      // デフォルトの視野角

    [Header("Distance Settings")]
    public float distanceAdjustSpeed = 5f; // 距離調整速度

    [Header("Field of View Settings")]
    public float fieldOfViewAdjustSpeed = 5f; // 視野角調整速度
    [Tooltip("視野角を補間する際のスムーズタイム。値が小さいほど素早く目標値に追従します。")]
    public float fieldOfViewSmoothTime = 0.05f;

    [Header("Angle X Limits")]
    public float minAngleX = 10f;          // X軸最小角
    public float maxAngleX = 66f;          // X軸最大角

    [Header("Pan Limits")]
    public float panLimitX = 5;            // X軸の移動制限
    public float panLimitZ = 5f;           // Z軸の移動制限

    [Header("UI Interaction")]
    public bool blockCameraWhenUIActive = true; // UI操作時にカメラ操作をブロック
    public bool blockCameraWhenPlacing = true;  // 配置中にカメラ操作を制限
    [Tooltip("Editor で UI 検出ログを確認したいときのみ手動で有効化してください。ビルド時は自動で無効化されます。")]
    public bool debugUIDetection = false;       // UIデバッグモード

    private Camera orthographicCamera;
    private Vector3 lastMousePosition;
    private float currentRotationX;
    private float currentRotationY;
    private float currentDistance;
    private Vector3 targetOffset;
    public float followSmoothTime = 0.2f;
    public float defaultFollowSmoothTime = 0.2f;
    private Vector3 followVelocity;

    private float targetFieldOfView;
    private float fieldOfViewVelocity;
    private bool isFieldOfViewSmoothing;

    // フォーカス関連
    public Transform focusTarget;          // フォーカス対象
    public bool isFocusing = false;        // フォーカス状態
    private float focusFieldOfView;        // フォーカス時の視野角
    private float focusDuration;           // フォーカスにかける時間
    private float focusAngleX;             // フォーカス時のX軸回転角
    private float focusAngleY;             // フォーカス時のY軸回転角
    private Coroutine focusRoutine;        // フォーカス用コルーチン

    // UI検出用
    private bool isInventoryOpen = false;
    private InventoryUI inventoryUI;

    // 配置システム検出用
    private FreePlacementSystem placementSystem;
    private bool isPlacingFurniture = false;

    // 操作継続フラグ（新規追加）
    private bool isRotating = false;           // 右クリック回転中
    private bool isPanning = false;            // 中クリックパン中
    private bool startedOutsideUI = false;     // UI外で操作を開始したか

    // カメラの現在の回転を公開（PlayerControllerで使用）
    public float CurrentCameraRotationY => currentRotationY;
    public float CurrentDistance => currentDistance;
    public float CurrentFieldOfView
    {
        get
        {
            if (orthographicCamera != null)
            {
                return orthographicCamera.fieldOfView;
            }

            var cameraComponent = GetComponent<Camera>();
            return cameraComponent != null ? cameraComponent.fieldOfView : defaultFieldOfView;
        }
    }

    private float minFieldOfView = 0.1f;
    private float maxFieldOfView = 179f;
    private float minDistance = 0f;
    private float maxDistance = float.MaxValue;

    public event Action<float> FieldOfViewChanged;
    public event Action<float> DistanceChanged;

    void Awake()
    {
#if !UNITY_EDITOR
        debugUIDetection = false;
#endif
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        EnsureCameraTarget();
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        // 操作フラグをリセット
        isRotating = false;
        isPanning = false;
        startedOutsideUI = false;
    }

    void Start()
    {
        orthographicCamera = GetComponent<Camera>();

        if (orthographicCamera == null)
        {
            Debug.LogError("Camera component not found!");
            return;
        }

        // Perspectiveカメラに設定
        orthographicCamera.orthographic = false;
        defaultFieldOfView = Mathf.Max(defaultFieldOfView, 0.1f);
        SetFieldOfView(defaultFieldOfView);
        targetFieldOfView = orthographicCamera.fieldOfView;
        fieldOfViewVelocity = 0f;
        isFieldOfViewSmoothing = false;

        fieldOfViewAdjustSpeed = Mathf.Max(fieldOfViewAdjustSpeed, 0f);

        defaultDistance = Mathf.Max(defaultDistance, 0f);
        SetDistance(defaultDistance);

        EnsureCameraTarget();

        // デフォルト位置にカメラを配置
        currentRotationY = defaultAngleY;
        currentRotationX = Mathf.Clamp(defaultAngleX, minAngleX, maxAngleX);
        targetOffset = Vector3.zero;
        followSmoothTime = defaultFollowSmoothTime;
        UpdateCameraPosition();

        // InventoryUIの参照を取得
        FindInventoryUI();

        EnsureCameraTarget();
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureCameraTarget();
        UpdateCameraPosition();
    }

    void FindInventoryUI()
    {
        inventoryUI = FindFirstObjectByType<InventoryUI>();
        if (inventoryUI != null)
        {
            Debug.Log("[CameraController] Found InventoryUI");
        }

        // FreePlacementSystemを探す
        placementSystem = FindFirstObjectByType<FreePlacementSystem>();
        if (placementSystem != null)
        {
            Debug.Log("[CameraController] Found FreePlacementSystem");
        }
    }

    void Update()
    {
        // インベントリと配置システムの状態をチェック
        UpdateInventoryStatus();

        if (isFocusing)
        {
            return;
        }

        // カメラ操作をブロックすべきか判定
        bool shouldBlockCamera = false;

        // 操作を継続中の場合はブロックしない（重要）
        if (isRotating || isPanning)
        {
            // 既に操作中の場合は、UI上でも継続を許可
            shouldBlockCamera = false;

            if (debugUIDetection)
            {
                Debug.Log($"[CameraController] Continuing operation - Rotating: {isRotating}, Panning: {isPanning}");
            }
        }
        else
        {
            // 新規操作の場合はUI判定を行う
            if (UIInteractionManager.Instance != null)
            {
                shouldBlockCamera = UIInteractionManager.Instance.ShouldBlockCameraControl();

                if (debugUIDetection && shouldBlockCamera)
                {
                    Debug.Log("[CameraController] Blocking new camera operation via UIInteractionManager");
                }
            }
            else
            {
                // UIInteractionManagerがない場合は従来の方法
                bool isMouseOverUI = IsPointerOverUIElement();
                shouldBlockCamera = blockCameraWhenUIActive && (isInventoryOpen && isMouseOverUI);

                if (debugUIDetection && shouldBlockCamera)
                {
                    Debug.Log($"[CameraController] Blocking new camera operation - Inventory: {isInventoryOpen}, Mouse over UI: {isMouseOverUI}");
                }
            }
        }

        // 配置中は回転のみ許可（ズームとパンは制限）
        if (isPlacingFurniture && blockCameraWhenPlacing)
        {
            if (!shouldBlockCamera)
            {
                HandleRotation();  // 回転は許可
                // HandleZoom();   // ズームは制限
                // HandlePan();    // パンは制限
            }
        }
        else if (!shouldBlockCamera)
        {
            // 通常のカメラ操作
            HandleZoom();
            HandlePan();
            HandleRotation();
        }
        else
        {
            // ブロック中でも継続中の操作は処理
            if (isRotating)
            {
                HandleRotation();
            }
            if (isPanning)
            {
                HandlePan();
            }
        }

        UpdateFieldOfViewSmoothing();

    }

    void LateUpdate()
    {
        UpdateCameraPosition();
    }

    void UpdateInventoryStatus()
    {
        // インベントリの状態をチェック
        if (inventoryUI != null)
        {
            isInventoryOpen = inventoryUI.inventoryPanel != null && inventoryUI.inventoryPanel.activeSelf;
        }

        // 配置システムの状態をチェック
        if (placementSystem != null)
        {
            // FreePlacementSystemのisPlacingNewFurnitureやisMovingFurnitureをチェック
            // これらの変数がpublicでない場合は、FreePlacementSystemに getter を追加する必要がある
            isPlacingFurniture = false; // 仮の値（FreePlacementSystemの実装による）
        }
    }

    // UI要素上にマウスがあるかチェック（改善版）
    bool IsPointerOverUIElement()
    {
        // EventSystemがない場合はfalse
        if (EventSystem.current == null)
            return false;

        // EventSystemのIsPointerOverGameObjectを使用
        if (EventSystem.current.IsPointerOverGameObject())
        {
            // さらに詳細なチェック：インベントリパネル内かどうか
            if (isInventoryOpen && IsPointerOverInventoryPanel())
            {
                return true;
            }

            // その他のUI要素上の場合もtrueを返す
            return true;
        }

        return false;
    }

    // インベントリパネル上にマウスがあるか特定チェック（改良版）
    bool IsPointerOverInventoryPanel()
    {
        GameObject inventoryPanel = null;
        GameObject backgroundImage = null;

        if (inventoryUI != null && inventoryUI.inventoryPanel != null)
        {
            inventoryPanel = inventoryUI.inventoryPanel;
            Transform bgTransform = inventoryPanel.transform.Find("Background");
            if (bgTransform != null)
                backgroundImage = bgTransform.gameObject;
        }

        if (inventoryPanel == null)
            return false;

        // RaycastでUI要素を検出
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            // Background画像上にマウスがある場合のみtrueを返す
            if (backgroundImage != null && result.gameObject == backgroundImage)
            {
                if (debugUIDetection)
                {
                    Debug.Log($"[CameraController] Mouse over Background image");
                }
                return true;
            }

            // またはBackground画像の子要素上にある場合
            if (backgroundImage != null && result.gameObject.transform.IsChildOf(backgroundImage.transform))
            {
                if (debugUIDetection)
                {
                    Debug.Log($"[CameraController] Mouse over Background child: {result.gameObject.name}");
                }
                return true;
            }
        }

        return false;
    }

    // ズーム処理（マウスホイール）- 改良版
    void HandleZoom()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (scrollInput != 0)
        {
            // スクロール時はその瞬間のUI判定を使用（継続性なし）
            bool isOverUI = CheckIfMouseOverUI();

            if (!isOverUI) // UI外でのみズーム可能
            {
                bool isCtrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

                if (isCtrlHeld)
                {
                    float newFieldOfView = orthographicCamera.fieldOfView - scrollInput * fieldOfViewAdjustSpeed;
                    BeginFieldOfViewSmoothing(newFieldOfView);
                }
                else
                {
                    float newDistance = currentDistance - scrollInput * distanceAdjustSpeed;
                    SetDistance(newDistance);
                }
            }
        }
    }

    // パン処理（中ボタンドラッグ）- 改良版
    void HandlePan()
    {
        if (Input.GetMouseButtonDown(2)) // 中ボタン押下
        {
            // 操作開始時にUI上かチェック
            bool isOverUI = CheckIfMouseOverUI();

            if (!isOverUI || isPanning) // UI外で開始、または既に操作中
            {
                lastMousePosition = Input.mousePosition;
                isPanning = true;
                startedOutsideUI = !isOverUI;

                #if UNITY_EDITOR
                if (debugUIDetection)
                {
                    Debug.Log($"[CameraController] Started panning - Outside UI: {startedOutsideUI}");
                }
                #endif
            }
        }

        if (Input.GetMouseButton(2) && isPanning) // 中ボタンホールド中かつパン中
        {
            Vector3 mouseDelta = Input.mousePosition - lastMousePosition;

            // カメラの向きに基づいて移動方向を計算
            Vector3 moveDirection = new Vector3(-mouseDelta.x, 0, -mouseDelta.y);
            moveDirection = Quaternion.Euler(0, currentRotationY, 0) * moveDirection;

            // パン速度を適用（現在の視野角に応じてスケール）
            float distanceRatio = defaultDistance > 0 ? currentDistance / defaultDistance : 1f;
            float panScale = distanceRatio * 0.01f;
            targetOffset += moveDirection * panSpeed * panScale;

            // 制限を適用
            targetOffset.x = Mathf.Clamp(targetOffset.x, -panLimitX, panLimitX);
            targetOffset.z = Mathf.Clamp(targetOffset.z, -panLimitZ, panLimitZ);

            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(2)) // 中ボタンリリース
        {
            if (isPanning)
            {
                isPanning = false;
                startedOutsideUI = false;

                #if UNITY_EDITOR
                if (debugUIDetection)
                {
                    Debug.Log("[CameraController] Stopped panning");
                }
                #endif
            }
        }
    }

    // 回転処理（右ボタンドラッグ）- 改良版
    void HandleRotation()
    {
        if (Input.GetMouseButtonDown(1)) // 右ボタン押下
        {
            // 操作開始時にUI上かチェック
            bool isOverUI = CheckIfMouseOverUI();

            if (!isOverUI || isRotating) // UI外で開始、または既に操作中
            {
                lastMousePosition = Input.mousePosition;
                isRotating = true;
                startedOutsideUI = !isOverUI;

                #if UNITY_EDITOR
                if (debugUIDetection)
                {
                    // Debug.Log($"[CameraController] Started rotating - Outside UI: {startedOutsideUI}");
                }
                #endif
            }
        }

        if (Input.GetMouseButton(1) && isRotating) // 右ボタンホールド中かつ回転中
        {
            Vector3 mouseDelta = Input.mousePosition - lastMousePosition;

            // Y軸回転
            currentRotationY += mouseDelta.x * rotationSpeed * 0.1f;

            // X軸回転（上下ドラッグで変更、範囲内にクランプ）
            currentRotationX -= mouseDelta.y * rotationSpeed * 0.1f;
            currentRotationX = Mathf.Clamp(currentRotationX, minAngleX, maxAngleX);

            lastMousePosition = Input.mousePosition;

            #if UNITY_EDITOR
            if (debugUIDetection && Time.frameCount % 30 == 0) // 30フレームごとにログ
            {
                // Debug.Log($"[CameraController] Rotating - X: {currentRotationX:F1}, Y: {currentRotationY:F1}");
            }
            #endif
        }

        if (Input.GetMouseButtonUp(1)) // 右ボタンリリース
        {
            if (isRotating)
            {
                isRotating = false;
                startedOutsideUI = false;

                #if UNITY_EDITOR
                if (debugUIDetection)
                {
                    // Debug.Log("[CameraController] Stopped rotating");
                }
                #endif
            }
        }
    }

    // カメラ位置の更新
    void UpdateCameraPosition()
    {
        if (cameraTarget == null)
        {
            EnsureCameraTarget();
            if (cameraTarget == null)
            {
                return;
            }
        }

        Vector3 baseTarget = cameraTarget.position;
        if (isFocusing && focusTarget != null)
        {
            baseTarget = focusTarget.position;
        }

        // ターゲット位置（オフセット込み）
        Vector3 targetPosition = baseTarget + targetOffset;

        // 回転を考慮したカメラ位置
        Quaternion rotation = Quaternion.Euler(currentRotationX, currentRotationY, 0);
        Vector3 offset = rotation * new Vector3(0, 0, -currentDistance);

        // カメラ位置と向きを設定
        Vector3 desiredPos = targetPosition + offset;

        if (isRotating || isPanning)
        {
            transform.position = desiredPos;
        }
        else
        {
            if (followSmoothTime != defaultFollowSmoothTime)
            {
                followSmoothTime = defaultFollowSmoothTime;
            }
            transform.position = Vector3.SmoothDamp(
                transform.position, desiredPos, ref followVelocity, followSmoothTime);
        }

        transform.rotation = rotation;
    }

    // マウスが現在UI上にあるかチェック（ヘルパーメソッド）
    bool CheckIfMouseOverUI()
    {
        // UIInteractionManagerがある場合
        if (UIInteractionManager.Instance != null)
        {
            return UIInteractionManager.Instance.ShouldBlockCameraControl();
        }

        // 従来の方法
        return IsPointerOverUIElement();
    }

    // 指定したターゲットにフォーカス
    public void FocusOnTarget(Transform target, float fieldOfView, float duration, float? blurIntensity = null)
    {
        if (target == null || orthographicCamera == null)
            return;

        if (focusRoutine != null)
        {
            StopCoroutine(focusRoutine);
        }

        focusTarget = target;
        focusFieldOfView = Mathf.Max(fieldOfView, 0.1f);
        focusDuration = duration;
        focusAngleX = currentRotationX;
        focusAngleY = currentRotationY;

        Vector3 startOffset = targetOffset;
        float startFieldOfView = orthographicCamera.fieldOfView;

        Vector3 relativePos = target.position - cameraTarget.position;
        Vector3 endOffset = new Vector3(relativePos.x, 0, relativePos.z);
        endOffset.x = Mathf.Clamp(endOffset.x, -panLimitX, panLimitX);
        endOffset.z = Mathf.Clamp(endOffset.z, -panLimitZ, panLimitZ);

        isFieldOfViewSmoothing = false;
        fieldOfViewVelocity = 0f;
        targetFieldOfView = Mathf.Clamp(focusFieldOfView, minFieldOfView, maxFieldOfView);

        focusRoutine = StartCoroutine(FocusCoroutine(startOffset, endOffset, startFieldOfView, focusFieldOfView, duration));
    }

    IEnumerator FocusCoroutine(Vector3 startOffset, Vector3 endOffset, float startFieldOfView, float endFieldOfView, float duration)
    {
        float elapsed = 0f;
        isFocusing = true;
        isRotating = false;
        isPanning = false;
        startedOutsideUI = false;

        targetFieldOfView = Mathf.Clamp(endFieldOfView, minFieldOfView, maxFieldOfView);
        fieldOfViewVelocity = 0f;
        isFieldOfViewSmoothing = false;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            targetOffset = Vector3.Lerp(startOffset, endOffset, t);
            orthographicCamera.fieldOfView = Mathf.Lerp(startFieldOfView, endFieldOfView, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        targetOffset = endOffset;
        orthographicCamera.fieldOfView = endFieldOfView;
        FieldOfViewChanged?.Invoke(orthographicCamera.fieldOfView);
        isFocusing = true;
    }

    // フォーカスを解除してデフォルトに戻す
    public void ResetFocus(float duration)
    {
        if (focusRoutine != null)
        {
            StopCoroutine(focusRoutine);
        }
        focusRoutine = StartCoroutine(ResetFocusCoroutine(duration));
    }

    IEnumerator ResetFocusCoroutine(float duration)
    {
        float elapsed = 0f;
        Vector3 startOffset = targetOffset;
        float startFieldOfView = orthographicCamera.fieldOfView;
        float startAngleX = currentRotationX;
        float startAngleY = currentRotationY;

        isFocusing = true;
        isRotating = false;
        isPanning = false;
        startedOutsideUI = false;

        targetFieldOfView = Mathf.Clamp(defaultFieldOfView, minFieldOfView, maxFieldOfView);
        fieldOfViewVelocity = 0f;
        isFieldOfViewSmoothing = false;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            targetOffset = Vector3.Lerp(startOffset, Vector3.zero, t);
            orthographicCamera.fieldOfView = Mathf.Lerp(startFieldOfView, defaultFieldOfView, t);
            currentRotationX = Mathf.Lerp(startAngleX, defaultAngleX, t);
            currentRotationY = Mathf.Lerp(startAngleY, defaultAngleY, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        targetOffset = Vector3.zero;
        SetFieldOfView(defaultFieldOfView);
        currentRotationX = defaultAngleX;
        currentRotationY = defaultAngleY;
        focusTarget = null;
        isFocusing = false;
    }

    // カメラをデフォルト位置にリセット
    public void SetDistance(float distance, bool updateDefault = false)
    {
        distance = Mathf.Max(distance, 0f);
        float clampedDistance = Mathf.Clamp(distance, minDistance, maxDistance);
        currentDistance = clampedDistance;

        if (updateDefault)
        {
            defaultDistance = clampedDistance;
        }

        DistanceChanged?.Invoke(currentDistance);
    }

    public void SetFieldOfView(float fieldOfView, bool updateDefault = false)
    {
        if (orthographicCamera == null)
        {
            orthographicCamera = GetComponent<Camera>();
            if (orthographicCamera == null)
            {
                return;
            }
        }

        float clampedFieldOfView = Mathf.Clamp(fieldOfView, minFieldOfView, maxFieldOfView);
        clampedFieldOfView = Mathf.Max(clampedFieldOfView, 0.1f);
        ApplyFieldOfView(clampedFieldOfView, updateDefault);
        targetFieldOfView = orthographicCamera.fieldOfView;
        fieldOfViewVelocity = 0f;
        isFieldOfViewSmoothing = false;
    }

    public void ResetCamera()
    {
        targetOffset = Vector3.zero;
        currentRotationY = defaultAngleY;
        currentRotationX = Mathf.Clamp(defaultAngleX, minAngleX, maxAngleX);
        SetFieldOfView(defaultFieldOfView);
        SetDistance(defaultDistance);

        // 操作フラグもリセット
        isRotating = false;
        isPanning = false;
        startedOutsideUI = false;
    }

    public void SetDistanceRange(float min, float max)
    {
        float lower = Mathf.Max(0f, Mathf.Min(min, max));
        float upper = Mathf.Max(lower, Mathf.Max(min, max));

        minDistance = lower;
        maxDistance = upper;

        defaultDistance = Mathf.Clamp(defaultDistance, minDistance, maxDistance);
        SetDistance(Mathf.Clamp(currentDistance, minDistance, maxDistance));
    }

    public void SetFieldOfViewRange(float min, float max)
    {
        float lower = Mathf.Min(min, max);
        float upper = Mathf.Max(min, max);

        lower = Mathf.Max(lower, 0.1f);

        minFieldOfView = lower;
        maxFieldOfView = Mathf.Max(lower, upper);

        defaultFieldOfView = Mathf.Clamp(defaultFieldOfView, minFieldOfView, maxFieldOfView);
        float clampedCurrent = Mathf.Clamp(CurrentFieldOfView, minFieldOfView, maxFieldOfView);
        SetFieldOfView(clampedCurrent);
        targetFieldOfView = Mathf.Clamp(targetFieldOfView, minFieldOfView, maxFieldOfView);
    }

    // X軸角の範囲を動的に変更
    public void SetXAngleRange(float min, float max)
    {
        minAngleX = min;
        maxAngleX = max;
        currentRotationX = Mathf.Clamp(currentRotationX, minAngleX, maxAngleX);
    }

    void BeginFieldOfViewSmoothing(float fieldOfView)
    {
        if (orthographicCamera == null)
        {
            return;
        }

        targetFieldOfView = Mathf.Clamp(fieldOfView, minFieldOfView, maxFieldOfView);
        targetFieldOfView = Mathf.Max(targetFieldOfView, 0.1f);

        if (Mathf.Approximately(targetFieldOfView, orthographicCamera.fieldOfView))
        {
            return;
        }

        isFieldOfViewSmoothing = true;
    }

    void UpdateFieldOfViewSmoothing()
    {
        if (!isFieldOfViewSmoothing || orthographicCamera == null)
        {
            return;
        }

        float smoothTime = Mathf.Max(0.0001f, fieldOfViewSmoothTime);
        float currentFov = orthographicCamera.fieldOfView;
        float nextFov = Mathf.SmoothDamp(currentFov, targetFieldOfView, ref fieldOfViewVelocity, smoothTime, Mathf.Infinity, Time.deltaTime);

        if (Mathf.Abs(nextFov - targetFieldOfView) <= 0.01f)
        {
            nextFov = targetFieldOfView;
            isFieldOfViewSmoothing = false;
            fieldOfViewVelocity = 0f;
        }

        ApplyFieldOfView(nextFov, false);
    }

    void ApplyFieldOfView(float fieldOfView, bool updateDefault)
    {
        if (orthographicCamera == null)
        {
            return;
        }

        float clampedFieldOfView = Mathf.Clamp(fieldOfView, minFieldOfView, maxFieldOfView);
        clampedFieldOfView = Mathf.Max(clampedFieldOfView, 0.1f);

        orthographicCamera.fieldOfView = clampedFieldOfView;

        if (updateDefault)
        {
            defaultFieldOfView = clampedFieldOfView;
        }

        FieldOfViewChanged?.Invoke(orthographicCamera.fieldOfView);
    }

    // 特定の位置にフォーカス
    public void FocusOnPosition(Vector3 position)
    {
        if (cameraTarget != null)
        {
            Vector3 relativePos = position - cameraTarget.position;
            targetOffset = new Vector3(relativePos.x, 0, relativePos.z);

            // 制限を適用
            targetOffset.x = Mathf.Clamp(targetOffset.x, -panLimitX, panLimitX);
            targetOffset.z = Mathf.Clamp(targetOffset.z, -panLimitZ, panLimitZ);
        }
    }

    // カメラ操作の有効/無効を外部から制御
    public void SetCameraControlEnabled(bool enabled)
    {
        blockCameraWhenUIActive = !enabled;

        if (debugUIDetection)
        {
            Debug.Log($"[CameraController] Camera control: {(enabled ? "Enabled" : "Disabled")}");
        }
    }

    // インベントリの開閉をマニュアルで通知（必要に応じて使用）
    public void NotifyInventoryStateChanged(bool isOpen)
    {
        isInventoryOpen = isOpen;

        if (debugUIDetection)
        {
            Debug.Log($"[CameraController] Inventory state changed: {(isOpen ? "Open" : "Closed")}");
        }
    }

    // アプリケーションフォーカス時の処理
    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            // フォーカスを失った時は操作を中断
            isRotating = false;
            isPanning = false;
            startedOutsideUI = false;
        }
    }

    // アプリケーション一時停止時の処理
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // 一時停止時は操作を中断
            isRotating = false;
            isPanning = false;
            startedOutsideUI = false;
        }
    }

    void EnsureCameraTarget()
    {
        if (cameraTarget != null)
        {
            return;
        }

        GameObject existingTarget = GameObject.Find("CameraTarget");
        if (existingTarget != null)
        {
            cameraTarget = existingTarget.transform;
            return;
        }

        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null)
        {
            cameraTarget = playerController.transform;
            return;
        }

        GameObject target = new GameObject("CameraTarget");
        target.transform.position = Vector3.zero;
        cameraTarget = target.transform;
    }
}
