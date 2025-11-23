using UnityEngine;

public class GhostPreviewManager : MonoBehaviour
{
    public Material ghostMaterial;
    [SerializeField] private string interactionTriggerLayerName = "InteractableTrigger";
    [SerializeField, Tooltip("トリガー半径のデフォルト値。レンダラーが存在する場合はバウンズから自動計算されます。")]
    private float defaultTriggerRadius = 1.5f;
    [SerializeField, Tooltip("0 より大きい場合、トリガーがこの距離内でのみフォーカスを許可します。")]
    private float maxTriggerFocusDistance = 0f;

    private GameObject ghostObject;
    private int cachedTriggerLayer = int.MinValue;

    public GameObject CreateGhost(GameObject source, Vector3 position, Quaternion rotation)
    {
        DestroyGhost();

        if (source == null)
        {
            return null;
        }

        ghostObject = Instantiate(source, position, rotation);
        ghostObject.AddComponent<GhostFloatAnimator>();
        PrepareGhostObject(ghostObject);
        SetupGhostInteractionTrigger(ghostObject);
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

        var triggerObjects = ghost.GetComponentsInChildren<GhostInteractionTrigger>(true);

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

        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        SetLayerRecursively(ghost, ignoreRaycastLayer);

        int triggerLayer = GetInteractionTriggerLayer();
        if (triggerLayer >= 0 && triggerObjects != null && triggerObjects.Length > 0)
        {
            foreach (var trigger in triggerObjects)
            {
                if (trigger != null)
                {
                    SetLayerRecursively(trigger.gameObject, triggerLayer);
                }
            }
        }
    }

    private void SetupGhostInteractionTrigger(GameObject ghost)
    {
        if (ghost == null)
        {
            return;
        }

        int triggerLayer = GetInteractionTriggerLayer();

        var interactable = ghost.GetComponentInChildren<BuildingGhostInteractable>();
        var triggers = ghost.GetComponentsInChildren<GhostInteractionTrigger>(true);

        if (triggers == null || triggers.Length == 0)
        {
            var triggerObj = new GameObject("GhostInteractionTrigger");
            triggerObj.transform.SetParent(ghost.transform, false);
            triggerObj.layer = triggerLayer >= 0 ? triggerLayer : ghost.layer;

            var sphere = triggerObj.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = CalculateTriggerRadius(ghost);

            var triggerComponent = triggerObj.AddComponent<GhostInteractionTrigger>();
            triggerComponent.SetInteractable(interactable);
            if (maxTriggerFocusDistance > 0f)
            {
                triggerComponent.SetMaxFocusDistance(maxTriggerFocusDistance);
            }

            triggers = new[] { triggerComponent };
        }

        foreach (var trigger in triggers)
        {
            if (trigger == null)
            {
                continue;
            }

            var triggerCollider = trigger.GetComponent<Collider>();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }

            if (triggerLayer >= 0)
            {
                SetLayerRecursively(trigger.gameObject, triggerLayer);
            }

            if (triggerCollider is SphereCollider sphereCollider && sphereCollider.radius <= 0f)
            {
                sphereCollider.radius = CalculateTriggerRadius(ghost);
            }

            if (triggerCollider != null && interactable != null)
            {
                trigger.SetInteractable(interactable);
            }

            if (maxTriggerFocusDistance > 0f)
            {
                trigger.SetMaxFocusDistance(maxTriggerFocusDistance);
            }
        }
    }

    private float CalculateTriggerRadius(GameObject ghost)
    {
        var renderers = ghost.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
        {
            return Mathf.Max(0.1f, defaultTriggerRadius);
        }

        Bounds combined = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            combined.Encapsulate(renderers[i].bounds);
        }

        float radius = Mathf.Max(combined.extents.x, combined.extents.y, combined.extents.z);
        return Mathf.Max(radius, defaultTriggerRadius);
    }

    private int GetInteractionTriggerLayer()
    {
        if (cachedTriggerLayer != int.MinValue)
        {
            return cachedTriggerLayer;
        }

        cachedTriggerLayer = LayerMask.NameToLayer(interactionTriggerLayerName);
        if (cachedTriggerLayer < 0)
        {
            Debug.LogWarning($"[GhostPreviewManager] Layer '{interactionTriggerLayerName}' not found. Using current layer for triggers.");
        }

        return cachedTriggerLayer;
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (layer < 0)
        {
            return;
        }

        obj.layer = layer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}
