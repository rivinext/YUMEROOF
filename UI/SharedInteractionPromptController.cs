using UnityEngine;

/// <summary>
/// Controls a single shared interaction prompt billboard that can be reused by any interactable.
/// </summary>
public class SharedInteractionPromptController : MonoBehaviour
{
    public static SharedInteractionPromptController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private InteractionPromptBillboard promptBillboard;
    [SerializeField] private CanvasGroup promptCanvasGroup;
    [SerializeField] private DynamicLocalizer promptLocalizer;
    [SerializeField] private string promptLocalizerField = "Prompt";

    private Object currentOwner;
    private InteractionPromptData currentData;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"Duplicate {nameof(SharedInteractionPromptController)} detected. Destroying the new instance on {gameObject.name}.");
            Destroy(this);
            return;
        }

        Instance = this;

        if (promptRoot == null && promptBillboard != null)
        {
            promptRoot = promptBillboard.gameObject;
        }

        HideImmediate();
    }

    /// <summary>
    /// Displays the prompt for the provided owner. If a different owner was active, it will be replaced.
    /// </summary>
    public void ShowPrompt(Object owner, InteractionPromptData data)
    {
        if (owner == null)
            return;

        if (!data.IsValid)
        {
            HidePrompt(owner);
            return;
        }

        if (!ReferenceEquals(currentOwner, owner))
        {
            HideImmediate();
            currentOwner = owner;
        }

        currentData = data;

        if (promptBillboard != null)
        {
            promptBillboard.SetTarget(data.Anchor, data.HeightOffset);
        }

        if (promptLocalizer != null)
        {
            promptLocalizer.SetFieldByName(promptLocalizerField, data.LocalizationKey);
        }

        SetVisible(true);
    }

    /// <summary>
    /// Hides the prompt if the caller currently owns it.
    /// </summary>
    public void HidePrompt(Object owner)
    {
        if (owner != null && !ReferenceEquals(currentOwner, owner))
            return;

        HideImmediate();
    }

    /// <summary>
    /// Forces the prompt to hide regardless of the current owner.
    /// </summary>
    public void HideAll()
    {
        HideImmediate();
    }

    private void HideImmediate()
    {
        currentOwner = null;
        currentData = default;

        if (promptBillboard != null)
        {
            promptBillboard.SetTarget(null);
        }

        if (promptLocalizer != null)
        {
            promptLocalizer.SetFieldByName(promptLocalizerField, string.Empty);
        }

        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (promptRoot != null)
        {
            promptRoot.SetActive(visible);
        }

        if (promptCanvasGroup != null)
        {
            promptCanvasGroup.alpha = visible ? 1f : 0f;
            promptCanvasGroup.interactable = visible;
            promptCanvasGroup.blocksRaycasts = visible;
        }
    }
}
