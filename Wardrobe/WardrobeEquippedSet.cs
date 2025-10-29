using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds instantiated wardrobe parts for a category, organized by their resolved attachment anchor.
/// </summary>
[Serializable]
public class WardrobeEquippedSet
{
    private static readonly GameObject[] EmptyInstances = Array.Empty<GameObject>();

    private readonly Dictionary<WardrobeBodyAnchor, List<GameObject>> anchorInstances = new Dictionary<WardrobeBodyAnchor, List<GameObject>>();

    /// <summary>
    /// Gets whether this set does not contain any equipped instances.
    /// </summary>
    public bool IsEmpty => anchorInstances.Count == 0;

    /// <summary>
    /// Adds an equipped instance for the specified anchor.
    /// </summary>
    public void Add(WardrobeBodyAnchor anchor, GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        if (!anchorInstances.TryGetValue(anchor, out List<GameObject> instances) || instances == null)
        {
            instances = new List<GameObject>();
            anchorInstances[anchor] = instances;
        }

        instances.Add(instance);
    }

    /// <summary>
    /// Gets the first non-null instance across all anchors.
    /// </summary>
    public GameObject GetFirstInstance()
    {
        foreach (KeyValuePair<WardrobeBodyAnchor, List<GameObject>> pair in anchorInstances)
        {
            List<GameObject> instances = pair.Value;
            if (instances == null)
            {
                continue;
            }

            for (int i = 0; i < instances.Count; i++)
            {
                GameObject instance = instances[i];
                if (instance != null)
                {
                    return instance;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Gets all instances registered for the given anchor.
    /// </summary>
    public IReadOnlyList<GameObject> GetInstances(WardrobeBodyAnchor anchor)
    {
        if (anchorInstances.TryGetValue(anchor, out List<GameObject> instances) && instances != null)
        {
            return instances;
        }

        return EmptyInstances;
    }

    /// <summary>
    /// Enumerates all anchors and their equipped instances.
    /// </summary>
    public IEnumerable<KeyValuePair<WardrobeBodyAnchor, IReadOnlyList<GameObject>>> GetAnchoredInstances()
    {
        foreach (KeyValuePair<WardrobeBodyAnchor, List<GameObject>> pair in anchorInstances)
        {
            IReadOnlyList<GameObject> instances = pair.Value ?? EmptyInstances;
            yield return new KeyValuePair<WardrobeBodyAnchor, IReadOnlyList<GameObject>>(pair.Key, instances);
        }
    }

    /// <summary>
    /// Removes and returns all instances registered for the specified anchor.
    /// </summary>
    public List<GameObject> Remove(WardrobeBodyAnchor anchor)
    {
        List<GameObject> removed = new List<GameObject>();
        if (anchorInstances.TryGetValue(anchor, out List<GameObject> instances) && instances != null)
        {
            removed.AddRange(instances);
        }

        anchorInstances.Remove(anchor);
        return removed;
    }

    /// <summary>
    /// Removes and returns all stored instances.
    /// </summary>
    public List<GameObject> RemoveAll()
    {
        List<GameObject> removed = new List<GameObject>();
        foreach (KeyValuePair<WardrobeBodyAnchor, List<GameObject>> pair in anchorInstances)
        {
            List<GameObject> instances = pair.Value;
            if (instances != null)
            {
                removed.AddRange(instances);
            }
        }

        anchorInstances.Clear();
        return removed;
    }
}
