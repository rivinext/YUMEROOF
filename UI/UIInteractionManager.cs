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
    private Canvas canvas;

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
        cameraController = FindObjectOfType<OrthographicCameraController>();

        // Canvas取得（UIのルート）
        if (backgroundImage != null)
        {
            canvas = backgroundImage.GetComponentInParent<Canvas>();
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
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCamera,
            out localMousePosition
        );

        bool isInside = bgRect.rect.Contains(localMousePosition);

        return isInside;
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
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCamera,
                out localMousePosition
            );

            if (rect.rect.Contains(localMousePosition))
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

        return false;
    }

    /// <summary>
    /// カメラ操作をブロックすべきかチェック
    /// </summary>
    public bool ShouldBlockCameraControl()
    {
        // インベントリパネルが開いていない場合はブロックしない
        if (inventoryPanel != null && !inventoryPanel.activeInHierarchy)
            return false;

        // Background画像の範囲内かチェック
        if (blockCameraInBackgroundArea && IsMouseOverBackgroundArea())
            return true;

        // 追加のブロックエリアをチェック
        if (IsMouseOverAdditionalBlockingArea())
            return true;

        return false;
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
