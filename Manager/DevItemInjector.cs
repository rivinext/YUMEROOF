using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DevItemInjector : MonoBehaviour
{
    private static bool _hasInjected = false;
    private Coroutine _inventoryWaitCoroutine;
    private bool _pendingForce;

    [NonSerialized]
    private bool _spawnedByInitializer;

    public bool SpawnedByInitializer
    {
        get => _spawnedByInitializer;
        internal set => _spawnedByInitializer = value;
    }

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
        StopInventoryWaitCoroutine();
    }

    private void OnDestroy()
    {
        StopInventoryWaitCoroutine();
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"{LogPrefix} InjectInternal invoked. force={force}, enableInjection={enableInjection}, hasInjected={_hasInjected}, inventoryReady={InventoryManager.Instance != null}");
#endif
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
            Debug.LogWarning($"{LogPrefix} InventoryManager instance is null. Queueing injection and waiting.");
            if (verboseLogging)
            {
                Debug.LogWarning($"{LogPrefix} InventoryManager unavailable. Waiting before injection.");
            }
#endif

            _pendingForce |= force;

            if (_inventoryWaitCoroutine == null)
            {
                _inventoryWaitCoroutine = StartCoroutine(WaitForInventoryAndInject());
            }

            return;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (verboseLogging)
        {
            Debug.Log($"{LogPrefix} Injection starting{(force ? " (forced)" : string.Empty)}.");
        }
        else
        {
            Debug.Log($"{LogPrefix} Injection starting{(force ? " (forced)" : string.Empty)} with furniture count {furnitureItems?.Count ?? 0} and material count {materialItems?.Count ?? 0}.");
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
                else
                {
                    Debug.Log($"{LogPrefix} Adding furniture '{entry.id}' x{entry.quantity} (non-verbose).");
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
                else
                {
                    Debug.Log($"{LogPrefix} Adding material '{entry.id}' x{entry.quantity} (non-verbose).");
                }
#endif
                InventoryManager.Instance.AddMaterial(entry.id, entry.quantity);
                materialInjected++;
            }
        }

        InventoryManager.Instance.ForceInventoryUpdate();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"{LogPrefix} ForceInventoryUpdate called. furnitureInjected={furnitureInjected}, materialInjected={materialInjected}.");
        if (verboseLogging)
        {
            Debug.Log($"{LogPrefix} Injection complete. Furniture entries: {furnitureInjected}, Material entries: {materialInjected}.");
        }
#endif

        _hasInjected = true;
    }

    private IEnumerator WaitForInventoryAndInject()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (verboseLogging)
        {
            Debug.Log($"{LogPrefix} Waiting for InventoryManager to become available before injecting.");
        }
#endif

        while (InventoryManager.Instance == null)
        {
            yield return null;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (verboseLogging)
        {
            Debug.Log($"{LogPrefix} InventoryManager available. Resuming injection.");
        }
#endif

        var force = _pendingForce;
        _pendingForce = false;
        _inventoryWaitCoroutine = null;
        InjectInternal(force);
    }

    private void StopInventoryWaitCoroutine()
    {
        if (_inventoryWaitCoroutine != null)
        {
            StopCoroutine(_inventoryWaitCoroutine);
            _inventoryWaitCoroutine = null;
            _pendingForce = false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (verboseLogging)
            {
                Debug.Log($"{LogPrefix} Stopped waiting for InventoryManager.");
            }
#endif
        }
    }

    public static void ResetInjected()
    {
        _hasInjected = false;
    }
}
