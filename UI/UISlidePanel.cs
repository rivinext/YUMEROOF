using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 各UIパネルのスライドイン/アウトアニメーションを制御
/// </summary>
public class UISlidePanel : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private AnimationCurve slideInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve slideOutCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Position Settings")]
    [SerializeField] private float offScreenXOffset = 1920f; // 画面外のX座標オフセット（解像度に応じて調整）

    [Header("Close Behaviour")]
    [SerializeField] private bool closeOnEscape = true;
    [SerializeField] private bool closeOnClickOutside = true;
    [SerializeField] private Camera uiCamera;

    [Header("Pointer Detection")]
    [Tooltip("クリックを内部扱いにしたい追加のパネル。登録したパネルおよびその子孫も内部として判定されます。")]
    [SerializeField] private List<RectTransform> exclusionPanels = new();

    private RectTransform rectTransform;
    private Vector2 onScreenPosition;
    private Vector2 offScreenPosition;
    private bool isOpen = false;

    // アニメーション完了時のコールバック
    public Action OnSlideInComplete;
    public Action OnSlideOutComplete;

    // パネルが開いているかどうかを取得
    public bool IsOpen => isOpen;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        // オンスクリーン位置を保存（デザイン時の位置）
        onScreenPosition = rectTransform.anchoredPosition;

        // オフスクリーン位置を計算（画面右外）
        offScreenPosition = new Vector2(offScreenXOffset, onScreenPosition.y);

        // 初期位置を画面外に設定
        rectTransform.anchoredPosition = offScreenPosition;

        // 念のため非アクティブ化
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!isOpen)
            return;

        if (closeOnEscape && Input.GetKeyDown(KeyCode.Escape))
        {
            SlideOut();
            return;
        }

        if (closeOnClickOutside && Input.GetMouseButtonDown(0) && !IsPointerInsidePanel())
        {
            SlideOut();
        }
    }

    /// <summary>
    /// パネルをスライドインで表示
    /// </summary>
    public void SlideIn()
    {
        if (isOpen) return;

        gameObject.SetActive(true);
        isOpen = true;

        // 位置を確実に画面外に設定
        rectTransform.anchoredPosition = offScreenPosition;

        // DOTweenでアニメーション
        rectTransform.DOAnchorPos(onScreenPosition, animationDuration)
            .SetEase(slideInCurve)
            .OnComplete(() =>
            {
                OnSlideInComplete?.Invoke();
            });
    }

    /// <summary>
    /// パネルをスライドアウトで非表示
    /// </summary>
    public void SlideOut()
    {
        if (!isOpen) return;

        isOpen = false;

        // DOTweenでアニメーション
        rectTransform.DOAnchorPos(offScreenPosition, animationDuration)
            .SetEase(slideOutCurve)
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
                OnSlideOutComplete?.Invoke();
            });
    }

    private bool IsPointerInsidePanel()
    {
        if (rectTransform == null)
            return false;

        if (EventSystem.current != null)
        {
            var pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            var raycastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, raycastResults);

            foreach (var result in raycastResults)
            {
                var targetTransform = result.gameObject != null ? result.gameObject.transform : null;

                if (targetTransform == null)
                    continue;

                if (IsWithinPanelHierarchy(targetTransform, rectTransform))
                {
                    return true;
                }

                if (IsWithinExclusionPanels(targetTransform))
                {
                    return true;
                }
            }

            return false;
        }

        var cameraToUse = uiCamera != null ? uiCamera : Camera.main;
        if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition, cameraToUse))
        {
            return true;
        }

        foreach (var panel in exclusionPanels)
        {
            if (panel == null)
                continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(panel, Input.mousePosition, cameraToUse))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsWithinPanelHierarchy(Transform target, RectTransform panel)
    {
        if (target == null || panel == null)
            return false;

        return target == panel.transform || target.IsChildOf(panel);
    }

    private bool IsWithinExclusionPanels(Transform target)
    {
        if (target == null || exclusionPanels == null)
            return false;

        foreach (var panel in exclusionPanels)
        {
            if (panel == null)
                continue;

            if (IsWithinPanelHierarchy(target, panel))
                return true;
        }

        return false;
    }

    private void OnValidate()
    {
        if (exclusionPanels == null)
        {
            exclusionPanels = new List<RectTransform>();
            return;
        }

        var seen = new HashSet<RectTransform>();

        for (int i = exclusionPanels.Count - 1; i >= 0; i--)
        {
            var panel = exclusionPanels[i];
            if (panel == null || !seen.Add(panel))
            {
                exclusionPanels.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// アニメーションなしで即座に閉じる
    /// </summary>
    public void CloseImmediate()
    {
        isOpen = false;
        rectTransform.anchoredPosition = offScreenPosition;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// エディタ上でオフスクリーン位置を設定するためのヘルパー
    /// </summary>
    [ContextMenu("Set to Off-Screen Position")]
    private void SetToOffScreenPosition()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        rectTransform.anchoredPosition = new Vector2(offScreenXOffset, rectTransform.anchoredPosition.y);
    }

    /// <summary>
    /// エディタ上でオンスクリーン位置を設定するためのヘルパー
    /// </summary>
    [ContextMenu("Set to On-Screen Position")]
    private void SetToOnScreenPosition()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        rectTransform.anchoredPosition = new Vector2(0, rectTransform.anchoredPosition.y);
    }
}
