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

    private void OnEnable()
    {
        SaveGameManager.SaveApplied += OnSaveApplied;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (verboseLogging)
        {
            Debug.Log($"{LogPrefix} Subscribed to SaveApplied event.");
        }
#endif
    }

    private void OnDisable()
    {
        SaveGameManager.SaveApplied -= OnSaveApplied;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (verboseLogging)
        {
            Debug.Log($"{LogPrefix} Unsubscribed from SaveApplied event.");
        }
#endif
    }

    [Serializable]
    public class DevEntry
    {
        public string id;
        public int quantity;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [SerializeField] private bool enableInjection;
    [SerializeField] private bool verboseLogging = true;
    private const string LogPrefix = "[DevItemInjector]";
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

    private void OnSaveApplied()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (verboseLogging)
        {
            Debug.Log($"{LogPrefix} SaveApplied event received.");
        }
#endif
        InjectInternal(false);
    }

    private void InjectInternal(bool force)
    {
        if (!enableInjection)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (verboseLogging)
            {
                Debug.LogWarning($"{LogPrefix} Injection skipped: enableInjection is disabled.");
            }
#endif
            return;
        }

        if (!force && _hasInjected)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (verboseLogging)
            {
                Debug.LogWarning($"{LogPrefix} Injection skipped: items have already been injected.");
            }
#endif
            return;
        }

        if (InventoryManager.Instance == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (verboseLogging)
            {
                Debug.LogWarning($"{LogPrefix} Injection aborted: InventoryManager instance is unavailable.");
            }
#endif
            return;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (verboseLogging)
        {
            Debug.Log($"{LogPrefix} Injection starting{(force ? " (forced)" : string.Empty)}.");
        }
#endif

        int furnitureInjected = 0;
        int materialInjected = 0;

        if (furnitureItems != null)
        {
            foreach (var entry in furnitureItems)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (verboseLogging)
                {
                    Debug.Log($"{LogPrefix} Adding furniture '{entry.id}' x{entry.quantity}.");
                }
#endif
                InventoryManager.Instance.AddFurniture(entry.id, entry.quantity);
                furnitureInjected++;
            }
        }

        if (materialItems != null)
        {
            foreach (var entry in materialItems)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (verboseLogging)
                {
                    Debug.Log($"{LogPrefix} Adding material '{entry.id}' x{entry.quantity}.");
                }
#endif
                InventoryManager.Instance.AddMaterial(entry.id, entry.quantity);
                materialInjected++;
            }
        }

        InventoryManager.Instance.ForceInventoryUpdate();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (verboseLogging)
        {
            Debug.Log($"{LogPrefix} Injection complete. Furniture entries: {furnitureInjected}, Material entries: {materialInjected}.");
        }
#endif

        _hasInjected = true;
    }

    public static void ResetInjected()
    {
        _hasInjected = false;
    }
}
