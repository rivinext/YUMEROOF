using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ScrollRectVirtualizer : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content;
    [SerializeField] private RectTransform viewport;
    [SerializeField] private float itemHeight = 100f;
    [SerializeField] private float itemWidth = 0f;
    [SerializeField] private float spacing = 0f;
    [SerializeField] private float horizontalSpacing = 0f;
    [SerializeField] private float paddingTop = 0f;
    [SerializeField] private float paddingBottom = 0f;
    [SerializeField] private float paddingLeft = 0f;
    [SerializeField] private float paddingRight = 0f;
    [SerializeField] private int columnCount = 1;
    [FormerlySerializedAs("bufferItems")]
    [SerializeField] private int bufferRows = 2;
    [SerializeField] private bool disableLayoutComponents = true;

    public Func<RectTransform> OnCreateItem;
    public Action<RectTransform> OnReleaseItem;
    public Action<int, RectTransform> OnBindItem;

    private readonly Dictionary<int, RectTransform> activeItems = new Dictionary<int, RectTransform>();
    private bool isInitialized;
    private int itemCount;

    public void Initialize(ScrollRect targetScrollRect, float targetItemHeight, float targetSpacing, float topPadding, float bottomPadding)
    {
        scrollRect = targetScrollRect;
        content = scrollRect != null ? scrollRect.content : null;
        viewport = scrollRect != null ? scrollRect.viewport : null;
        itemHeight = targetItemHeight;
        spacing = targetSpacing;
        paddingTop = topPadding;
        paddingBottom = bottomPadding;

        if (scrollRect == null || content == null || viewport == null)
        {
            return;
        }

        EnsureViewportSetup();
        NormalizeRectTransforms();
        if (disableLayoutComponents)
        {
            DisableLayoutComponents();
        }

        scrollRect.onValueChanged.RemoveListener(HandleScrollValueChanged);
        scrollRect.onValueChanged.AddListener(HandleScrollValueChanged);

        isInitialized = true;
        UpdateContentHeight();
        UpdateVisibleItems(true);
    }

    public void SetItemCount(int count, bool resetScrollPosition)
    {
        itemCount = Mathf.Max(0, count);
        UpdateContentHeight();

        if (resetScrollPosition && scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }

        UpdateVisibleItems(true);
    }

    public void RefreshVisibleItems()
    {
        UpdateVisibleItems(true);
    }

    private void HandleScrollValueChanged(Vector2 position)
    {
        UpdateVisibleItems(false);
    }

    private void NormalizeRectTransforms()
    {
        if (content != null)
        {
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
        }
    }

    private void EnsureViewportSetup()
    {
        if (viewport == null)
        {
            return;
        }

        var existingMask = viewport.GetComponent<Mask>();
        if (existingMask != null)
        {
            if (Application.isPlaying)
            {
                Destroy(existingMask);
            }
            else
            {
                DestroyImmediate(existingMask);
            }
        }

        var rectMask = viewport.GetComponent<RectMask2D>();
        if (rectMask == null)
        {
            rectMask = viewport.gameObject.AddComponent<RectMask2D>();
        }

        rectMask.enabled = true;

        if (content != null && content.parent != viewport)
        {
            content.SetParent(viewport, false);
        }
    }

    private void DisableLayoutComponents()
    {
        if (content == null)
        {
            return;
        }

        var layoutGroup = content.GetComponent<LayoutGroup>();
        if (layoutGroup != null)
        {
            layoutGroup.enabled = false;
        }

        var contentSizeFitter = content.GetComponent<ContentSizeFitter>();
        if (contentSizeFitter != null)
        {
            contentSizeFitter.enabled = false;
        }
    }

    private void UpdateContentHeight()
    {
        if (!isInitialized || content == null)
        {
            return;
        }

        var columns = Mathf.Max(1, columnCount);
        var rowCount = itemCount > 0 ? Mathf.CeilToInt(itemCount / (float)columns) : 0;
        var totalHeight = paddingTop + paddingBottom;
        if (rowCount > 0)
        {
            totalHeight += rowCount * itemHeight;
            totalHeight += Mathf.Max(0, rowCount - 1) * spacing;
        }

        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, totalHeight);
    }

    private void UpdateVisibleItems(bool forceRebind)
    {
        if (!isInitialized || content == null || viewport == null)
        {
            return;
        }

        if (itemCount <= 0)
        {
            ReleaseAllItems();
            return;
        }

        var columns = Mathf.Max(1, columnCount);
        var rowHeight = itemHeight + spacing;
        var scrollY = Mathf.Max(0f, content.anchoredPosition.y - paddingTop);
        var firstRow = Mathf.FloorToInt(scrollY / rowHeight) - bufferRows;
        firstRow = Mathf.Max(0, firstRow);

        var viewportHeight = viewport.rect.height;
        var visibleRows = Mathf.CeilToInt(viewportHeight / rowHeight) + bufferRows * 2;
        var startIndex = Mathf.Max(0, firstRow * columns);
        var visibleCount = visibleRows * columns;
        var lastIndex = Mathf.Min(itemCount - 1, startIndex + visibleCount - 1);

        var requiredIndices = new HashSet<int>();
        for (var index = startIndex; index <= lastIndex; index++)
        {
            requiredIndices.Add(index);
        }

        var indicesToRelease = new List<int>();
        foreach (var index in activeItems.Keys)
        {
            if (!requiredIndices.Contains(index))
            {
                indicesToRelease.Add(index);
            }
        }

        foreach (var index in indicesToRelease)
        {
            ReleaseItem(index);
        }

        foreach (var index in requiredIndices)
        {
            if (!activeItems.TryGetValue(index, out var item))
            {
                item = GetItemFromPool();
                if (item == null)
                {
                    continue;
                }

                activeItems[index] = item;
                item.SetParent(content, false);
                SetupItemRectTransform(item);
                SetItemPosition(item, index);
                OnBindItem?.Invoke(index, item);
            }
            else
            {
                SetItemPosition(item, index);
                if (forceRebind)
                {
                    OnBindItem?.Invoke(index, item);
                }
            }
        }
    }

    private RectTransform GetItemFromPool()
    {
        return OnCreateItem?.Invoke();
    }

    private void ReleaseItem(int index)
    {
        if (!activeItems.TryGetValue(index, out var item))
        {
            return;
        }

        activeItems.Remove(index);
        OnReleaseItem?.Invoke(item);
    }

    private void ReleaseAllItems()
    {
        foreach (var index in new List<int>(activeItems.Keys))
        {
            ReleaseItem(index);
        }
    }

    private void SetupItemRectTransform(RectTransform item)
    {
        var calculatedItemWidth = GetItemWidth();
        item.anchorMin = new Vector2(0f, 1f);
        item.anchorMax = new Vector2(0f, 1f);
        item.pivot = new Vector2(0f, 1f);
        item.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, itemHeight);
        item.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, calculatedItemWidth);
    }

    private void SetItemPosition(RectTransform item, int index)
    {
        var columns = Mathf.Max(1, columnCount);
        var row = index / columns;
        var col = index % columns;
        var itemSpan = itemHeight + spacing;
        var y = paddingTop + row * itemSpan;
        var calculatedItemWidth = GetItemWidth();
        var x = paddingLeft + col * (calculatedItemWidth + horizontalSpacing);
        item.anchoredPosition = new Vector2(x, -y);
    }

    private float GetItemWidth()
    {
        if (itemWidth > 0f)
        {
            return itemWidth;
        }

        var columns = Mathf.Max(1, columnCount);
        var availableWidth = Mathf.Max(0f, content.rect.width - paddingLeft - paddingRight - horizontalSpacing * (columns - 1));
        return columns > 0 ? availableWidth / columns : content.rect.width;
    }
}
