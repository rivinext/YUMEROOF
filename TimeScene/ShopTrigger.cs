using UnityEngine;

/// <summary>
/// Simple interactable that opens the shop UI.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ShopTrigger : MonoBehaviour, IInteractable, IInteractionPromptDataProvider
{
    public ShopUIManager shopUIManager;

    [Header("Interaction Prompt")]
    [SerializeField] private Transform promptAnchor;
    [SerializeField] private float promptOffset = 1f;
    [SerializeField] private string promptLocalizationKey = string.Empty;

    void Start()
    {
        if (shopUIManager == null)
        {
            shopUIManager = FindFirstObjectByType<ShopUIManager>();
        }

        if (promptAnchor == null)
            promptAnchor = transform;
    }

    public void Interact()
    {
        shopUIManager?.OpenShop();
    }

    public bool TryGetInteractionPromptData(out InteractionPromptData data)
    {
        var anchor = promptAnchor != null ? promptAnchor : transform;
        data = new InteractionPromptData(anchor, promptOffset, promptLocalizationKey);
        return true;
    }
}
