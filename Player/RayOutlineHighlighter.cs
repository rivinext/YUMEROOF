using UnityEngine;

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
    private int originalLayer;

    void Awake()
    {
        outlineLayer = LayerMask.NameToLayer(outlineLayerName);
        if (outlineLayer == -1)
        {
            Debug.LogWarning($"Layer '{outlineLayerName}' not found. Highlighting will be disabled.");
        }
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
        originalLayer = obj.layer;
        SetLayerRecursively(highlightedObject, outlineLayer);
    }

    /// <summary>
    /// Restore the original layer of the currently highlighted object, if any.
    /// </summary>
    public void Clear()
    {
        if (highlightedObject == null)
            return;

        SetLayerRecursively(highlightedObject, originalLayer);
        highlightedObject = null;
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}
