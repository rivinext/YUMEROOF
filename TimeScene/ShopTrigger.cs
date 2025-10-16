using UnityEngine;

/// <summary>
/// Simple interactable that opens the shop UI.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ShopTrigger : MonoBehaviour, IInteractable
{
    public ShopUIManager shopUIManager;
    public GameObject pressEHint;

    void Start()
    {
        if (shopUIManager == null)
        {
            shopUIManager = FindObjectOfType<ShopUIManager>();
        }

        if (pressEHint != null)
            pressEHint.SetActive(false);
    }

    public void Interact()
    {
        shopUIManager?.OpenShop();
    }
}
