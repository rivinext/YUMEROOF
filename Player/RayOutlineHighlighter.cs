using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles highlighting of raycast targets by moving them to a dedicated Outline layer.
/// Records the original layer so it can be restored when the highlight is cleared.
/// </summary>
public class RayOutlineHighlighter : MonoBehaviour
{
    [Tooltip("Name of the layer used for outlining highlighted objects.")]
    public string outlineLayerName = "Outline";

    private int outlineLayer;
    private GameObject highlightedObject;
    private readonly Dictionary<Transform, int> originalLayers = new();

    void Awake()
    {
        outlineLayer = LayerMask.NameToLayer(outlineLayerName);
        if (outlineLayer == -1)
        {
            Debug.LogWarning($"Layer '{outlineLayerName}' not found. Highlighting will be disabled.");
        }
    }

    void OnDisable()
    {
        Clear();
    }

    void OnDestroy()
    {
        Clear();
    }

    /// <summary>
    /// Highlight the provided object by switching it to the outline layer.
    /// Any previously highlighted object is restored first.
    /// </summary>
    public void Highlight(GameObject obj)
    {
        if (obj == highlightedObject)
            return;

        Clear();

        if (obj == null || outlineLayer == -1)
            return;

        highlightedObject = obj;
        originalLayers.Clear();
        SetLayerRecursively(highlightedObject, outlineLayer);
    }

    /// <summary>
    /// Restore the original layer of the currently highlighted object, if any.
    /// </summary>
    public void Clear()
    {
        if (highlightedObject == null)
            return;

        RestoreLayerRecursively(highlightedObject);
        originalLayers.Clear();
        highlightedObject = null;
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj.GetComponent<OutlineExclusion>() != null)
            return;

        if (!originalLayers.ContainsKey(obj.transform))
            originalLayers[obj.transform] = obj.layer;

        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private void RestoreLayerRecursively(GameObject obj)
    {
        if (obj.GetComponent<OutlineExclusion>() != null)
            return;

        if (originalLayers.TryGetValue(obj.transform, out int originalLayer))
        {
            obj.layer = originalLayer;
        }

        foreach (Transform child in obj.transform)
        {
            RestoreLayerRecursively(child.gameObject);
        }
    }
}
