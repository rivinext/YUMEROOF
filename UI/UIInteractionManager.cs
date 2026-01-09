using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// UI要素とカメラ操作の相互作用を管理
/// Background画像の正確な範囲を検出
/// </summary>
public class UIInteractionManager : MonoBehaviour
{
    private static UIInteractionManager instance;
    public static UIInteractionManager Instance => instance;

    [Header("UI References")]
    public GameObject inventoryPanel;
    public Image backgroundImage;  // Background画像への直接参照

    [Header("Settings")]
    public bool blockCameraInBackgroundArea = true;
    public bool debugMode = false;

    [Header("Additional Blocking Areas")]
    public List<RectTransform> additionalBlockingAreas = new List<RectTransform>();

    private Camera mainCamera;
    private OrthographicCameraController cameraController;
    private Canvas backgroundCanvas;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 参照を取得
        mainCamera = Camera.main;
        cameraController = FindFirstObjectByType<OrthographicCameraController>();

        // Canvas取得（UIのルート）
        if (backgroundImage != null)
        {
            backgroundCanvas = backgroundImage.GetComponentInParent<Canvas>();
        }

        // 自動的にBackground画像を探す（設定されていない場合）
        if (backgroundImage == null && inventoryPanel != null)
        {
            Transform bgTransform = inventoryPanel.transform.Find("Background");
            if (bgTransform != null)
            {
                backgroundImage = bgTransform.GetComponent<Image>();
                if (debugMode && backgroundImage != null)
                {
                    Debug.Log("[UIInteractionManager] Found Background image automatically");
                }
            }
        }
    }

    /// <summary>
    /// マウスがBackground画像の範囲内にあるかチェック
    /// </summary>
    public bool IsMouseOverBackgroundArea()
    {
        if (backgroundImage == null || !backgroundImage.gameObject.activeInHierarchy)
            return false;

        // RectTransformを使った精密な範囲チェック
        RectTransform bgRect = backgroundImage.GetComponent<RectTransform>();
        if (bgRect == null)
            return false;

        // マウス位置がRectTransform内にあるかチェック
        Vector2 localMousePosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            bgRect,
            Input.mousePosition,
            GetEventCamera(backgroundCanvas),
            out localMousePosition
        );

        bool isInside = bgRect.rect.Contains(localMousePosition);

        if (!isInside)
            return false;

        // CanvasGroupのblocksRaycastsが有効か確認
        if (!IsRaycastEnabled(backgroundImage.transform))
            return false;

        return true;
    }

    /// <summary>
    /// 追加のブロックエリアをチェック
    /// </summary>
    public bool IsMouseOverAdditionalBlockingArea()
    {
        foreach (var rect in additionalBlockingAreas)
        {
            if (rect == null || !rect.gameObject.activeInHierarchy)
                continue;

            Vector2 localMousePosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rect,
                Input.mousePosition,
                GetEventCamera(rect.GetComponentInParent<Canvas>()),
                out localMousePosition
            );

            if (rect.rect.Contains(localMousePosition))
            {
                if (IsRaycastEnabled(rect.transform))
                {
                    #if UNITY_EDITOR
                    if (debugMode)
                    {
                        Debug.Log($"[UIInteractionManager] Mouse over additional blocking area: {rect.name}");
                    }
                    #endif
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// カメラ操作をブロックすべきかチェック
    /// </summary>
    public bool ShouldBlockCameraControl()
    {
        // Background画像の範囲内かチェック
        if (blockCameraInBackgroundArea && IsMouseOverBackgroundArea())
            return true;

        // 追加のブロックエリアをチェック
        if (IsMouseOverAdditionalBlockingArea())
            return true;

        // EventSystemによる汎用的なUIヒットを確認
        if (EventSystem.current != null)
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return true;
            }
        }

        return false;
    }

    private Camera GetEventCamera(Canvas targetCanvas)
    {
        if (targetCanvas == null || targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        if (targetCanvas.worldCamera != null)
        {
            return targetCanvas.worldCamera;
        }

        return mainCamera;
    }

    private bool IsRaycastEnabled(Transform target)
    {
        var canvasGroups = target.GetComponentsInParent<CanvasGroup>(true);
        foreach (var canvasGroup in canvasGroups)
        {
            if (!canvasGroup.blocksRaycasts)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// EventSystemを使った汎用的なUI検出
    /// </summary>
    public bool IsPointerOverSpecificUI(string tagOrName = null)
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            // 特定のタグや名前でフィルタリング
            if (string.IsNullOrEmpty(tagOrName))
                return true;

            if (result.gameObject.name == tagOrName || result.gameObject.tag == tagOrName)
                return true;
        }

        return false;
    }

    /// <summary>
    /// デバッグ用：現在のマウス位置情報を表示
    /// </summary>
    public void DebugMousePosition()
    {
        if (!debugMode)
            return;

        Debug.Log("=== Mouse Position Debug ===");
        Debug.Log($"Screen Position: {Input.mousePosition}");
        Debug.Log($"Over Background: {IsMouseOverBackgroundArea()}");
        Debug.Log($"Over Additional Areas: {IsMouseOverAdditionalBlockingArea()}");
        Debug.Log($"Should Block Camera: {ShouldBlockCameraControl()}");
    }
}
