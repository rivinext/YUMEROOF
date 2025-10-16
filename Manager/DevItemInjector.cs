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
        LogVerbose("Subscribed to SaveApplied event.");
    }

    private void OnDisable()
    {
        SaveGameManager.SaveApplied -= OnSaveApplied;
        LogVerbose("Unsubscribed from SaveApplied event.");
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

    private void Log(string message)
    {
        Debug.Log($"{LogPrefix} {message}");
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"{LogPrefix} {message}");
    }

    private void LogVerbose(string message)
    {
        if (verboseLogging)
        {
            Log(message);
        }
    }

    private void LogVerboseWarning(string message)
    {
        if (verboseLogging)
        {
            LogWarning(message);
        }
    }

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
        LogVerbose("SaveApplied event received.");
        InjectInternal(false);
    }

    private void InjectInternal(bool force)
    {
        Log($"InjectInternal invoked. force={force}, enableInjection={enableInjection}, hasInjected={_hasInjected}, inventoryReady={InventoryManager.Instance != null}");
        if (!enableInjection)
        {
            LogVerboseWarning("Injection skipped: enableInjection is disabled.");
            return;
        }

        if (!force && _hasInjected)
        {
            LogVerboseWarning("Injection skipped: items have already been injected.");
            return;
        }

        if (InventoryManager.Instance == null)
        {
            LogWarning("InventoryManager instance is null. Queueing injection and waiting.");
            LogVerboseWarning("InventoryManager unavailable. Waiting before injection.");

            _pendingForce |= force;

            if (_inventoryWaitCoroutine == null)
            {
                _inventoryWaitCoroutine = StartCoroutine(WaitForInventoryAndInject());
            }

            return;
        }

        if (verboseLogging)
        {
            Log($"Injection starting{(force ? " (forced)" : string.Empty)}.");
        }
        else
        {
            Log($"Injection starting{(force ? " (forced)" : string.Empty)} with furniture count {furnitureItems?.Count ?? 0} and material count {materialItems?.Count ?? 0}.");
        }

        int furnitureInjected = 0;
        int materialInjected = 0;

        if (furnitureItems != null)
        {
            foreach (var entry in furnitureItems)
            {
                if (verboseLogging)
                {
                    Log($"Adding furniture '{entry.id}' x{entry.quantity}.");
                }
                else
                {
                    Log($"Adding furniture '{entry.id}' x{entry.quantity} (non-verbose).");
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
                    Log($"Adding material '{entry.id}' x{entry.quantity}.");
                }
                else
                {
                    Log($"Adding material '{entry.id}' x{entry.quantity} (non-verbose).");
                }
                InventoryManager.Instance.AddMaterial(entry.id, entry.quantity);
                materialInjected++;
            }
        }

        InventoryManager.Instance.ForceInventoryUpdate();

        Log($"ForceInventoryUpdate called. furnitureInjected={furnitureInjected}, materialInjected={materialInjected}.");
        LogVerbose($"Injection complete. Furniture entries: {furnitureInjected}, Material entries: {materialInjected}.");

        _hasInjected = true;
    }

    private IEnumerator WaitForInventoryAndInject()
    {
        LogVerbose("Waiting for InventoryManager to become available before injecting.");

        while (InventoryManager.Instance == null)
        {
            yield return null;
        }

        LogVerbose("InventoryManager available. Resuming injection.");

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
            LogVerbose("Stopped waiting for InventoryManager.");
        }
    }

    public static void ResetInjected()
    {
        _hasInjected = false;
    }
}
