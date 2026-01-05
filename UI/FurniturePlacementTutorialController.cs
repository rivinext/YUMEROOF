using UnityEngine;

public class FurniturePlacementTutorialController : MonoBehaviour
{
    public const string DisabledPrefKey = "Tutorial.FurniturePlacement.Disabled";

    [SerializeField] private bool debugMode = false;

    private InventoryPlacementBridge placementBridge;

    public static bool IsTutorialDisabled => PlayerPrefs.GetInt(DisabledPrefKey, 0) == 1;

    public static void DisableTutorial()
    {
        PlayerPrefs.SetInt(DisabledPrefKey, 1);
        PlayerPrefs.Save();
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
