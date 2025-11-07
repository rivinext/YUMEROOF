using UnityEngine;

/// <summary>
/// Keeps prompt UI elements in the expected foreground/background sibling order.
/// </summary>
[AddComponentMenu("UI/Prompt Sibling Order Controller")]
public class PromptSiblingOrderController : MonoBehaviour
{
    [Header("Targets")]
    [Tooltip("RectTransforms that should stay in front of the prompt background (text, icon, etc.). Assign the prompt's content container here.")]
    [SerializeField] private RectTransform[] defaultForegroundTargets = new RectTransform[0];

    [Tooltip("Optional RectTransforms that should stay behind the foreground content. Call MoveBackgroundsToBack or enable Move Backgrounds On Enable to keep them at the bottom.")]
    [SerializeField] private RectTransform[] defaultBackgroundTargets = new RectTransform[0];

    [Header("Behaviour")]
    [Tooltip("Automatically re-apply the foreground ordering when the component is enabled.")]
    [SerializeField] private bool applyForegroundOnEnable = true;

    [Tooltip("When enabled, background targets will also be moved to the first sibling index when this component is enabled. Use this if your prompt background needs to render behind other elements.")]
    [SerializeField] private bool moveBackgroundsOnEnable;

    private RectTransform[] runtimeForegroundTargets;
    private RectTransform[] runtimeBackgroundTargets;

    private RectTransform[] ForegroundTargets => runtimeForegroundTargets ?? defaultForegroundTargets;
    private RectTransform[] BackgroundTargets => runtimeBackgroundTargets ?? defaultBackgroundTargets;

    private void OnEnable()
    {
        if (moveBackgroundsOnEnable)
        {
            MoveBackgroundsToBack();
        }

        if (applyForegroundOnEnable)
        {
            ApplyForegroundOrder();
        }
    }

    /// <summary>
    /// Overrides the targets for the current session. Call with <c>null</c> to fall back to the serialized defaults.
    /// </summary>
    public void ConfigureTargets(RectTransform foreground, RectTransform background)
    {
        runtimeForegroundTargets = foreground != null ? new[] { foreground } : null;
        runtimeBackgroundTargets = background != null ? new[] { background } : null;
    }

    /// <summary>
    /// Applies the foreground order by moving all targets to the end of their sibling lists.
    /// </summary>
    public void ApplyForegroundOrder()
    {
        var targets = ForegroundTargets;
        if (targets == null)
            return;

        for (int i = 0; i < targets.Length; i++)
        {
            var target = targets[i];
            if (target == null)
                continue;

            target.SetAsLastSibling();
        }
    }

    /// <summary>
    /// Moves all configured background targets to the beginning of their sibling lists.
    /// </summary>
    public void MoveBackgroundsToBack()
    {
        var targets = BackgroundTargets;
        if (targets == null)
            return;

        for (int i = 0; i < targets.Length; i++)
        {
            var target = targets[i];
            if (target == null)
                continue;

            target.SetAsFirstSibling();
        }
    }

    /// <summary>
    /// Sets the sibling index for all foreground targets. Useful if you need a specific ordering instead of always moving them to the front.
    /// </summary>
    /// <param name="index">Desired sibling index within the parent.</param>
    public void SetForegroundSiblingIndex(int index)
    {
        var targets = ForegroundTargets;
        if (targets == null)
            return;

        for (int i = 0; i < targets.Length; i++)
        {
            var target = targets[i];
            if (target == null || target.parent == null)
                continue;

            int clampedIndex = Mathf.Clamp(index, 0, target.parent.childCount - 1);
            target.SetSiblingIndex(clampedIndex);
        }
    }

    /// <summary>
    /// Clears any runtime overrides so the serialized defaults are used again.
    /// </summary>
    public void ResetRuntimeTargets()
    {
        runtimeForegroundTargets = null;
        runtimeBackgroundTargets = null;
    }

    [ContextMenu("Apply Foreground Order Now")]
    private void ContextApplyForegroundOrder()
    {
        ApplyForegroundOrder();
    }

    [ContextMenu("Move Backgrounds To First Sibling")]
    private void ContextMoveBackgroundsToBack()
    {
        MoveBackgroundsToBack();
    }
}
