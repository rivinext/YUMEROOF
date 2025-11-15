using UnityEngine;

/// <summary>
/// Simple interactable that opens the shop UI.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ShopTrigger : MonoBehaviour, IInteractable
{
    public ShopUIManager shopUIManager;

    void Start()
    {
        if (shopUIManager == null)
        {
            shopUIManager = FindFirstObjectByType<ShopUIManager>();
        }
    }

    public void Interact()
    {
        shopUIManager?.OpenShop();
    }
}
