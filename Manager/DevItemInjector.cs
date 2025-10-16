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
        if (verboseLogging)
        {
            Debug.Log($"{LogPrefix} Subscribed to SaveApplied event.");
        }
    }

    private void OnDisable()
    {
        SaveGameManager.SaveApplied -= OnSaveApplied;
        if (verboseLogging)
        {
            Debug.Log($"{LogPrefix} Unsubscribed from SaveApplied event.");
        }
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

    [SerializeField] private bool enableInjection;
    [SerializeField] private bool verboseLogging = true;
    private const string LogPrefix = "[DevItemInjector]";
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
        if (verboseLogging)
        {
            Debug.Log($"{LogPrefix} SaveApplied event received.");
        }
        InjectInternal(false);
    }

    private void InjectInternal(bool force)
    {
        Debug.Log($"{LogPrefix} InjectInternal invoked. force={force}, enableInjection={enableInjection}, hasInjected={_hasInjected}, inventoryReady={InventoryManager.Instance != null}");
        if (!enableInjection)
        {
            if (verboseLogging)
            {
                Debug.LogWarning($"{LogPrefix} Injection skipped: enableInjection is disabled.");
            }
            return;
        }

        if (!force && _hasInjected)
        {
            if (verboseLogging)
            {
                Debug.LogWarning($"{LogPrefix} Injection skipped: items have already been injected.");
            }
            return;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning($"{LogPrefix} InventoryManager instance is null. Queueing injection and waiting.");
            if (verboseLogging)
            {
                Debug.LogWarning($"{LogPrefix} InventoryManager unavailable. Waiting before injection.");
            }

            _pendingForce |= force;

            if (_inventoryWaitCoroutine == null)
            {
                _inventoryWaitCoroutine = StartCoroutine(WaitForInventoryAndInject());
            }

            return;
        }

        if (verboseLogging)
        {
            Debug.Log($"{LogPrefix} Injection starting{(force ? " (forced)" : string.Empty)}.");
        }
        else
        {
            Debug.Log($"{LogPrefix} Injection starting{(force ? " (forced)" : string.Empty)} with furniture count {furnitureItems?.Count ?? 0} and material count {materialItems?.Count ?? 0}.");
        }

        int furnitureInjected = 0;
        int materialInjected = 0;

        if (furnitureItems != null)
        {
            foreach (var entry in furnitureItems)
            {
                if (verboseLogging)
                {
                    Debug.Log($"{LogPrefix} Adding furniture '{entry.id}' x{entry.quantity}.");
                }
                else
                {
                    Debug.Log($"{LogPrefix} Adding furniture '{entry.id}' x{entry.quantity} (non-verbose).");
                }
                InventoryManager.Instance.AddFurniture(entry.id, entry.quantity);
                furnitureInjected++;
            }
        }

        if (materialItems != null)
        {
            foreach (var entry in materialItems)
            {
                if (verboseLogging)
                {
                    Debug.Log($"{LogPrefix} Adding material '{entry.id}' x{entry.quantity}.");
                }
                else
                {
                    Debug.Log($"{LogPrefix} Adding material '{entry.id}' x{entry.quantity} (non-verbose).");
                }
                InventoryManager.Instance.AddMaterial(entry.id, entry.quantity);
                materialInjected++;
            }
        }

        InventoryManager.Instance.ForceInventoryUpdate();

        Debug.Log($"{LogPrefix} ForceInventoryUpdate called. furnitureInjected={furnitureInjected}, materialInjected={materialInjected}.");
        if (verboseLogging)
        {
            Debug.Log($"{LogPrefix} Injection complete. Furniture entries: {furnitureInjected}, Material entries: {materialInjected}.");
        }

        _hasInjected = true;
    }

    private IEnumerator WaitForInventoryAndInject()
    {
        if (verboseLogging)
        {
            Debug.Log($"{LogPrefix} Waiting for InventoryManager to become available before injecting.");
        }

        while (InventoryManager.Instance == null)
        {
            yield return null;
        }

        if (verboseLogging)
        {
            Debug.Log($"{LogPrefix} InventoryManager available. Resuming injection.");
        }

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
            if (verboseLogging)
            {
                Debug.Log($"{LogPrefix} Stopped waiting for InventoryManager.");
            }
        }
    }

    public static void ResetInjected()
    {
        _hasInjected = false;
    }
}
