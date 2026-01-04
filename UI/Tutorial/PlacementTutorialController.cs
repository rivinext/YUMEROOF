using UnityEngine;

public class PlacementTutorialController : MonoBehaviour
{
    [SerializeField] private GameObject tutorialRoot;
    [SerializeField] private FreePlacementSystem placementSystem;
    [SerializeField] private InventoryUI inventoryUI;

    private void Awake()
    {
        if (tutorialRoot == null)
        {
            tutorialRoot = gameObject;
        }
    }

    private void OnEnable()
    {
        EnsurePlacementSystem();
        if (placementSystem != null)
        {
            placementSystem.OnPlacementCompleted += HandlePlacementCompleted;
        }

        EnsureInventoryUI();
        if (inventoryUI != null)
        {
            inventoryUI.OnInventoryOpened += HandleInventoryOpened;
            inventoryUI.OnInventoryClosed += HandleInventoryClosed;
        }
    }

    private void Start()
    {
        EvaluateTutorialVisibility();
    }

    private void OnDisable()
    {
        if (placementSystem != null)
        {
            placementSystem.OnPlacementCompleted -= HandlePlacementCompleted;
        }

        if (inventoryUI != null)
        {
            inventoryUI.OnInventoryOpened -= HandleInventoryOpened;
            inventoryUI.OnInventoryClosed -= HandleInventoryClosed;
        }
    }

    private void EnsurePlacementSystem()
    {
        if (placementSystem == null)
        {
            placementSystem = FindFirstObjectByType<FreePlacementSystem>();
        }
    }

    private void EnsureInventoryUI()
    {
        if (inventoryUI == null)
        {
            inventoryUI = FindFirstObjectByType<InventoryUI>();
        }
    }

    private void EvaluateTutorialVisibility()
    {
        if (inventoryUI != null && inventoryUI.IsOpen)
        {
            HideTutorial();
            return;
        }

        bool hasSeen = HasSeenTutorialFlag();
        bool hasPlacedFurniture = HasPlacedFurniture();

        if (hasPlacedFurniture && !hasSeen)
        {
            MarkTutorialSeen(persist: true);
            hasSeen = true;
        }

        if (hasSeen || hasPlacedFurniture)
        {
            HideTutorial();
        }
        else
        {
            ShowTutorial();
        }
    }

    private bool HasSeenTutorialFlag()
    {
        var saveManager = SaveGameManager.Instance;
        return saveManager != null && saveManager.HasSeenPlacementTutorial;
    }

    private bool HasPlacedFurniture()
    {
        var furnitureManager = FurnitureSaveManager.Instance;
        return furnitureManager != null && furnitureManager.GetAllFurniture().Count > 0;
    }

    private void MarkTutorialSeen(bool persist)
    {
        var saveManager = SaveGameManager.Instance;
        if (saveManager == null)
        {
            return;
        }

        saveManager.SetPlacementTutorialSeen(true);
        if (persist)
        {
            saveManager.SaveCurrentSlot();
        }
    }

    private void HandlePlacementCompleted()
    {
        MarkTutorialSeen(persist: true);
        HideTutorial();
    }

    private void HandleInventoryOpened()
    {
        EvaluateTutorialVisibility();
    }

    private void HandleInventoryClosed()
    {
        EvaluateTutorialVisibility();
    }

    private void HideTutorial()
    {
        if (tutorialRoot != null)
        {
            tutorialRoot.SetActive(false);
        }
    }

    private void ShowTutorial()
    {
        if (tutorialRoot != null)
        {
            tutorialRoot.SetActive(true);
        }
    }
}
