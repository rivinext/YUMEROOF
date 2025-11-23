using UnityEngine;

/// <summary>
/// Interactable for the shop keeper. Opens the conversation controller
/// which in turn shows the shop panels when appropriate.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ShopTrigger : MonoBehaviour, IInteractable
{
    [Tooltip("Optional direct reference to the conversation controller.")]
    public ShopConversationController conversationController;

    [Tooltip("Fallback reference to the shop UI manager if no conversation controller is available.")]
    public ShopUIManager shopUIManager;

    private Collider interactionCollider;

    void Start()
    {
        if (conversationController == null)
        {
            conversationController = FindFirstObjectByType<ShopConversationController>();
        }

        if (shopUIManager == null)
        {
            shopUIManager = FindFirstObjectByType<ShopUIManager>();
        }

        InteractableTriggerUtility.EnsureTriggerCollider(this, ref interactionCollider);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        InteractableTriggerUtility.EnsureTriggerCollider(this, ref interactionCollider);
    }
#endif

    public void Interact()
    {
        if (conversationController != null)
        {
            conversationController.BeginConversation();
        }
        else
        {
            // Fallback to the legacy behaviour in case the conversation controller is missing.
            shopUIManager?.OpenShop();
        }
    }
}
