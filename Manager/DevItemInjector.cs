using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DevItemInjector : MonoBehaviour
{
    [Serializable]
    public class DevEntry
    {
        public string id;
        public int quantity = 1;
    }

    [SerializeField] private bool enableInjection = true;
    [SerializeField] private List<DevEntry> furnitureItems = new List<DevEntry>();
    [SerializeField] private List<DevEntry> materialItems = new List<DevEntry>();

    private static bool _hasInjected;
    private Coroutine _waitCoroutine;
    private bool _saveApplied;

    private void OnEnable()
    {
        SaveGameManager.SaveApplied += OnSaveApplied;
        _saveApplied = SaveGameManager.Instance == null;
        RequestInjection();
    }

    private void OnDisable()
    {
        SaveGameManager.SaveApplied -= OnSaveApplied;
        StopWaiting();
    }

    private void OnDestroy()
    {
        StopWaiting();
    }

    public static void ResetInjected()
    {
        _hasInjected = false;
    }

    private void OnSaveApplied()
    {
        _saveApplied = true;
        RequestInjection();
    }

    private void RequestInjection()
    {
        if (!enableInjection || _hasInjected || _waitCoroutine != null)
        {
            return;
        }

        _waitCoroutine = StartCoroutine(WaitAndInject());
    }

    private IEnumerator WaitAndInject()
    {
        while (!_saveApplied || InventoryManager.Instance == null)
        {
            yield return null;
        }

        InjectItems();
        _waitCoroutine = null;
    }

    private void InjectItems()
    {
        if (!enableInjection || _hasInjected)
        {
            return;
        }

        if (furnitureItems != null)
        {
            foreach (var entry in furnitureItems)
            {
                if (entry == null || string.IsNullOrEmpty(entry.id) || entry.quantity <= 0)
                {
                    continue;
                }

                InventoryManager.Instance.AddFurniture(entry.id, entry.quantity);
            }
        }

        if (materialItems != null)
        {
            foreach (var entry in materialItems)
            {
                if (entry == null || string.IsNullOrEmpty(entry.id) || entry.quantity <= 0)
                {
                    continue;
                }

                InventoryManager.Instance.AddMaterial(entry.id, entry.quantity);
            }
        }

        InventoryManager.Instance.ForceInventoryUpdate();
        _hasInjected = true;
    }

    private void StopWaiting()
    {
        if (_waitCoroutine == null)
        {
            return;
        }

        StopCoroutine(_waitCoroutine);
        _waitCoroutine = null;
    }
}
