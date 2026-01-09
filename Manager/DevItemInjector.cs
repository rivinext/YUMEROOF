using System;
using System.Collections.Generic;
using UnityEngine;

public class DevItemInjector : MonoBehaviour
{
    [Serializable]
    public class DevEntry
    {
        public string id;
        public int quantity;
    }

#if DISABLE_DEV_ITEM_INJECTION
    public const bool BuildDisablesInjection = true;
#else
    public const bool BuildDisablesInjection = false;
#endif

    [SerializeField, Tooltip("Use this even in release builds to seed inventories for demos or QA.")]
    private bool enableInjection = true;
    [SerializeField] private List<DevEntry> furnitureItems;
    [SerializeField] private List<DevEntry> materialItems;

    public void Inject()
    {
        if (BuildDisablesInjection || !enableInjection) return;

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

    }
}
