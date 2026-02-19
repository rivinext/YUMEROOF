using UnityEngine;
using UnityEngine.SceneManagement;

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
        // シーン遷移/終了時にも OnDisable が呼ばれるため、
        // そのタイミングでは壁家具を「回収」しない。
        // （回収すると Cozy/Nature が減算され、次シーン読み込み後に値が欠落する）
        if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded)
        {
            return;
        }

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
