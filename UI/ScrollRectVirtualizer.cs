using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScrollRectVirtualizer : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content;
    [SerializeField] private RectTransform viewport;
    [SerializeField] private float itemHeight = 100f;
    [SerializeField] private float spacing = 0f;
    [SerializeField] private float paddingTop = 0f;
    [SerializeField] private float paddingBottom = 0f;
    [SerializeField] private int bufferItems = 2;

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

        NormalizeRectTransforms();
        DisableLayoutComponents();

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

        var totalHeight = paddingTop + paddingBottom;
        if (itemCount > 0)
        {
            totalHeight += itemCount * itemHeight;
            totalHeight += Mathf.Max(0, itemCount - 1) * spacing;
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

        var itemSpan = itemHeight + spacing;
        var scrollY = Mathf.Max(0f, content.anchoredPosition.y - paddingTop);
        var firstIndex = Mathf.FloorToInt(scrollY / itemSpan);
        firstIndex = Mathf.Max(0, firstIndex - bufferItems);

        var viewportHeight = viewport.rect.height;
        var visibleCount = Mathf.CeilToInt(viewportHeight / itemSpan) + bufferItems * 2;
        var lastIndex = Mathf.Min(itemCount - 1, firstIndex + visibleCount - 1);

        var indicesToRelease = new List<int>();
        foreach (var index in activeItems.Keys)
        {
            if (index < firstIndex || index > lastIndex)
            {
                indicesToRelease.Add(index);
            }
        }

        foreach (var index in indicesToRelease)
        {
            ReleaseItem(index);
        }

        for (var index = firstIndex; index <= lastIndex; index++)
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
        item.anchorMin = new Vector2(0f, 1f);
        item.anchorMax = new Vector2(1f, 1f);
        item.pivot = new Vector2(0.5f, 1f);
        item.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, itemHeight);
        item.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, content.rect.width);
    }

    private void SetItemPosition(RectTransform item, int index)
    {
        var itemSpan = itemHeight + spacing;
        var y = paddingTop + index * itemSpan;
        item.anchoredPosition = new Vector2(item.anchoredPosition.x, -y);
    }
}
