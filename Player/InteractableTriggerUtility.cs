using UnityEngine;
#if UNITY_EDITOR
using UnityEditorInternal;
#endif

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

        Collider triggerCollider = cachedCollider;

        if (triggerCollider is MeshCollider meshCollider)
        {
            if (!meshCollider.convex)
            {
                meshCollider.convex = true;
            }

            if (!meshCollider.convex)
            {
                triggerCollider = behaviour.gameObject.AddComponent<BoxCollider>();
            }
        }

        cachedCollider = triggerCollider;
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

    public static bool TagExists(string tag)
    {
#if UNITY_EDITOR
        foreach (string definedTag in InternalEditorUtility.tags)
        {
            if (definedTag == tag)
            {
                return true;
            }
        }

        return false;
#else
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
#endif
    }
}
