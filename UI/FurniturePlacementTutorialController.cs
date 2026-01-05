using UnityEngine;

public class FurniturePlacementTutorialController : MonoBehaviour
{
    [SerializeField] private bool debugMode = false;

    private InventoryPlacementBridge placementBridge;

    public static bool IsTutorialDisabled { get; private set; }

    public static void DisableTutorial()
    {
        SetTutorialDisabled(true);
    }

    public static void SetTutorialDisabled(bool disabled)
    {
        IsTutorialDisabled = disabled;
    }

    private void OnEnable()
    {
        placementBridge = InventoryPlacementBridge.Instance;
        if (placementBridge != null)
        {
            placementBridge.OnPlacementComplete += HandlePlacementComplete;
        }
    }

    private void OnDisable()
    {
        if (placementBridge != null)
        {
            placementBridge.OnPlacementComplete -= HandlePlacementComplete;
        }
    }

    private void HandlePlacementComplete(string itemId)
    {
        if (IsTutorialDisabled)
        {
            return;
        }

        DisableTutorial();

        if (debugMode)
        {
            Debug.Log($"[FurniturePlacementTutorialController] Disabled tutorial after placing {itemId}.");
        }
    }
}
