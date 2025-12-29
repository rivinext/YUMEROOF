using UnityEngine;

public class WallDeactivationHandler : MonoBehaviour
{
    [SerializeField] private Transform wallTransform;

    public void SetWallTransform(Transform target)
    {
        wallTransform = target;
    }

    private void Awake()
    {
        if (wallTransform == null)
        {
            wallTransform = transform;
        }
    }

    private void OnDisable()
    {
        if (wallTransform == null)
        {
            wallTransform = transform;
        }

        var placedFurnitures = transform.GetComponentsInChildren<PlacedFurniture>(true);
        foreach (var placedFurniture in placedFurnitures)
        {
            if (placedFurniture == null || placedFurniture.furnitureData == null)
            {
                continue;
            }

            if (placedFurniture.furnitureData.placementRules != PlacementRule.Wall)
            {
                continue;
            }

            if (!placedFurniture.transform.IsChildOf(wallTransform))
            {
                continue;
            }

            placedFurniture.StoreToInventory();
        }
    }
}
