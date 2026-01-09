using UnityEngine;

public class GhostPreviewManager : MonoBehaviour
{
    public Material ghostMaterial;

    private GameObject ghostObject;

    public GameObject CreateGhost(GameObject source, Vector3 position, Quaternion rotation, bool enableFloating = false)
    {
        DestroyGhost();

        if (source == null)
        {
            return null;
        }

        ghostObject = Instantiate(source, position, rotation);
        if (enableFloating)
        {
            ghostObject.AddComponent<GhostFloatAnimator>();
        }
        PrepareGhostObject(ghostObject);
        return ghostObject;
    }

    public void DestroyGhost()
    {
        if (ghostObject != null)
        {
            Destroy(ghostObject);
            ghostObject = null;
        }
    }

    private void PrepareGhostObject(GameObject ghost)
    {
        if (ghost == null)
        {
            return;
        }

        var colliders = ghost.GetComponentsInChildren<Collider>();
        foreach (var collider in colliders)
        {
            collider.enabled = false;
        }

        var placedFurnitureComponents = ghost.GetComponentsInChildren<PlacedFurniture>();
        foreach (var placedFurniture in placedFurnitureComponents)
        {
            if (placedFurniture.cornerMarkers != null)
            {
                foreach (var marker in placedFurniture.cornerMarkers)
                {
                    if (marker != null)
                    {
                        Destroy(marker);
                    }
                }
            }

            Destroy(placedFurniture);
        }

        var inventoryButtons = ghost.GetComponentsInChildren<StoreToInventoryButton>();
        foreach (var button in inventoryButtons)
        {
            Destroy(button);
        }

        if (ghostMaterial != null)
        {
            var renderers = ghost.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                renderer.material = ghostMaterial;
            }
        }

        ghost.tag = "Untagged";
        SetLayerRecursively(ghost, LayerMask.NameToLayer("Ignore Raycast"));
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}
