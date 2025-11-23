using UnityEngine;

public static class InteractableTriggerUtility
{
    public const string InteractableLayerName = "Interactable";
    public const string InteractableTag = "Interactable";

    public static void EnsureTriggerCollider(MonoBehaviour behaviour, ref Collider cachedCollider)
    {
        if (behaviour == null)
            return;

        if (cachedCollider == null)
        {
            cachedCollider = behaviour.GetComponent<Collider>();
        }

        if (cachedCollider == null)
        {
            cachedCollider = behaviour.gameObject.AddComponent<BoxCollider>();
        }

        cachedCollider.isTrigger = true;

        int interactableLayer = LayerMask.NameToLayer(InteractableLayerName);
        if (interactableLayer >= 0)
        {
            behaviour.gameObject.layer = interactableLayer;
        }

        if (TagExists(InteractableTag) && behaviour.gameObject.tag != InteractableTag)
        {
            behaviour.gameObject.tag = InteractableTag;
        }
    }

    private static bool TagExists(string tag)
    {
        try
        {
            GameObject temp = new GameObject();
            bool exists = temp.CompareTag(tag);
            Object.Destroy(temp);
            return exists;
        }
        catch (UnityException)
        {
            return false;
        }
    }
}
