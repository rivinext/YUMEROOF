using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class WardrobeCardGridPopulator : MonoBehaviour
{
    public enum SortMode
    {
        None,
        NameId,
        DisplayName
    }

    [SerializeField] private WardrobeCatalog catalog;
    [SerializeField] private WardrobeCard cardPrefab;
    [SerializeField] private Transform container;
    [SerializeField] private bool filterByTabType;
    [SerializeField] private WardrobeTabType tabFilter;
    [SerializeField] private SortMode sortMode = SortMode.None;
    [SerializeField] private bool sortDescending;

    private readonly List<GameObject> generatedInstances = new List<GameObject>();

    public WardrobeCatalog Catalog
    {
        get { return catalog; }
        set
        {
            if (catalog == value)
            {
                return;
            }

            catalog = value;
            AutoPopulateIfActive();
        }
    }

    public WardrobeCard CardPrefab
    {
        get { return cardPrefab; }
        set
        {
            if (cardPrefab == value)
            {
                return;
            }

            cardPrefab = value;
            AutoPopulateIfActive();
        }
    }

    public Transform Container
    {
        get { return container; }
        set
        {
            if (container == value)
            {
                return;
            }

            container = value;
            AutoPopulateIfActive();
        }
    }

    public bool FilterByTabType
    {
        get { return filterByTabType; }
        set
        {
            if (filterByTabType == value)
            {
                return;
            }

            filterByTabType = value;
            AutoPopulateIfActive();
        }
    }

    public WardrobeTabType TabFilter
    {
        get { return tabFilter; }
        set
        {
            if (tabFilter == value)
            {
                return;
            }

            tabFilter = value;
            AutoPopulateIfActive();
        }
    }

    public SortMode Sorting
    {
        get { return sortMode; }
        set
        {
            if (sortMode == value)
            {
                return;
            }

            sortMode = value;
            AutoPopulateIfActive();
        }
    }

    public bool SortDescending
    {
        get { return sortDescending; }
        set
        {
            if (sortDescending == value)
            {
                return;
            }

            sortDescending = value;
            AutoPopulateIfActive();
        }
    }

    private void Reset()
    {
        if (container == null)
        {
            container = transform;
        }
    }

    private void Awake()
    {
        AutoPopulateIfActive();
    }

    private void OnEnable()
    {
        AutoPopulateIfActive();
    }

    public void Populate()
    {
        ClearGeneratedInstances();

        if (catalog == null || cardPrefab == null || container == null)
        {
            return;
        }

        IReadOnlyList<WardrobeCatalogEntry> entries = catalog.Entries;
        if (entries == null || entries.Count == 0)
        {
            return;
        }

        List<WardrobeCatalogEntry> workingList = new List<WardrobeCatalogEntry>(entries.Count);
        for (int i = 0; i < entries.Count; i++)
        {
            WardrobeCatalogEntry entry = entries[i];
            if (filterByTabType && entry.TabType != tabFilter)
            {
                continue;
            }

            workingList.Add(entry);
        }

        if (workingList.Count == 0)
        {
            return;
        }

        SortWorkingList(workingList);

        for (int i = 0; i < workingList.Count; i++)
        {
            WardrobeCatalogEntry entry = workingList[i];
            WardrobeCard cardInstance = Instantiate(cardPrefab, container);
            if (cardInstance == null)
            {
                continue;
            }

            generatedInstances.Add(cardInstance.gameObject);
            cardInstance.Apply(entry);
        }
    }

    [ContextMenu("Populate Now")]
    private void PopulateContextMenu()
    {
        Populate();
    }

    private void SortWorkingList(List<WardrobeCatalogEntry> workingList)
    {
        Comparison<WardrobeCatalogEntry> comparison = null;
        switch (sortMode)
        {
            case SortMode.NameId:
                comparison = (a, b) => string.Compare(a.NameId, b.NameId, StringComparison.OrdinalIgnoreCase);
                break;
            case SortMode.DisplayName:
                comparison = (a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
                break;
        }

        if (comparison == null)
        {
            return;
        }

        workingList.Sort(comparison);
        if (sortDescending)
        {
            workingList.Reverse();
        }
    }

    private void AutoPopulateIfActive()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        Populate();
    }

    private void ClearGeneratedInstances()
    {
        for (int i = 0; i < generatedInstances.Count; i++)
        {
            GameObject instance = generatedInstances[i];
            if (instance == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(instance);
            }
            else
            {
                DestroyImmediate(instance);
            }
        }

        generatedInstances.Clear();
    }
}
