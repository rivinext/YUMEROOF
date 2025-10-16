using System;
using System.Collections.Generic;
using UnityEngine;

public class DevItemInjector : MonoBehaviour
{
    private static bool _hasInjected = false;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    [Serializable]
    public class DevEntry
    {
        public string id;
        public int quantity;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [SerializeField] private bool enableInjection;
#else
    private const bool enableInjection = false;
#endif
    [SerializeField] private List<DevEntry> furnitureItems;
    [SerializeField] private List<DevEntry> materialItems;

    public void Inject()
    {
        InjectInternal(false);
    }

    /// <summary>
    /// Forces an injection even if it has already been performed. This can be bound to a UI button.
    /// </summary>
    public void InjectFromButton()
    {
        InjectInternal(true);
    }

    private void InjectInternal(bool force)
    {
        if (!enableInjection || (!force && _hasInjected)) return;

        if (InventoryManager.Instance == null) return;

        if (furnitureItems != null)
        {
            foreach (var entry in furnitureItems)
            {
                InventoryManager.Instance.AddFurniture(entry.id, entry.quantity);
            }
        }

        if (materialItems != null)
        {
            foreach (var entry in materialItems)
            {
                InventoryManager.Instance.AddMaterial(entry.id, entry.quantity);
            }
        }

        _hasInjected = true;
    }

    public static void ResetInjected()
    {
        _hasInjected = false;
    }
}
