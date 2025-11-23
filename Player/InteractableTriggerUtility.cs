using UnityEngine;
#if UNITY_EDITOR
using UnityEditorInternal;
using UnityEditor;
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

#if UNITY_EDITOR
        EnsureInteractableTagExists();
#endif

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
        catch (System.Exception)
        {
            return false;
        }
#endif
    }

#if UNITY_EDITOR
    private static void EnsureInteractableTagExists()
    {
        if (TagExists(InteractableTag))
            return;

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0)
            return;

        SerializedObject tagManager = new SerializedObject(assets[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        if (tagsProp == null)
            return;

        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            SerializedProperty tagProp = tagsProp.GetArrayElementAtIndex(i);
            if (tagProp != null && tagProp.stringValue == InteractableTag)
            {
                return;
            }
        }

        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        SerializedProperty newTagProp = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
        if (newTagProp != null)
        {
            newTagProp.stringValue = InteractableTag;
        }

        tagManager.ApplyModifiedPropertiesWithoutUndo();
        tagManager.Update();
    }
#endif
}
