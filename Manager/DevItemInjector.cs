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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [SerializeField] private bool enableInjection;
#else
    private const bool enableInjection = false;
#endif
    [SerializeField] private List<DevEntry> furnitureItems;
    [SerializeField] private List<DevEntry> materialItems;

    public void Inject()
    {
        if (!enableInjection) return;

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
