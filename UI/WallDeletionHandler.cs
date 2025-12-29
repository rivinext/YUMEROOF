using UnityEngine;

public class WallDeletionHandler : MonoBehaviour
{
    [SerializeField] private Transform wallTransform;

    private void OnDestroy()
    {
        ReturnWallFurnitureToInventory();
    }

    public void ReturnWallFurnitureToInventory()
    {
        Transform targetTransform = wallTransform != null ? wallTransform : transform;
        var furnitures = targetTransform.GetComponentsInChildren<PlacedFurniture>(true);

        foreach (var furniture in furnitures)
        {
            if (furniture == null || furniture.furnitureData == null)
            {
                continue;
            }

            if (furniture.furnitureData.placementRules != PlacementRule.Wall)
            {
                continue;
            }

            if (!furniture.transform.IsChildOf(targetTransform))
            {
                continue;
            }

            if (furniture.wallParentTransform != targetTransform)
            {
                continue;
            }

            if (furniture.parentFurniture != null)
            {
                continue;
            }

            furniture.StoreToInventory();
        }
    }
}
