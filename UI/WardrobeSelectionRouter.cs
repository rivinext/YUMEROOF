using System.Collections.Generic;
using Player;
using UnityEngine;

/// <summary>
/// Routes wardrobe item selection events to a <see cref="PlayerOutfitSwitcher"/> by
/// resolving the selected item identifier into the configured outfit index.
/// </summary>
[DisallowMultipleComponent]
public class WardrobeSelectionRouter : MonoBehaviour
{
    [System.Serializable]
    private class CategoryOutfitMapping
    {
        [System.Serializable]
        private struct ItemToOutfitIndex
        {
            [Tooltip("Wardrobe item identifier provided by the selection group.")]
            [SerializeField] private string itemId;

            [Tooltip("Index passed to the PlayerOutfitSwitcher when this item is selected.")]
            [SerializeField] private int outfitIndex;

            public string ItemId => itemId;
            public int OutfitIndex => outfitIndex;
        }

        [Header("Category")]
        [SerializeField] private WardrobeCategory category = WardrobeCategory.Headwear;
        [SerializeField] private WardrobeItemSelectionGroup selectionGroup;

        [Header("Mappings")]
        [SerializeField] private List<ItemToOutfitIndex> itemMappings = new List<ItemToOutfitIndex>();

        [Header("Fallback")]
        [Tooltip("Outfit index used when no mapping is configured for the current selection. Set to -1 to disable.")]
        [SerializeField] private int fallbackOutfitIndex = -1;

        private Dictionary<string, int> lookup;

        public WardrobeCategory Category => category;
        public WardrobeItemSelectionGroup SelectionGroup => selectionGroup;
        public bool HasFallback => fallbackOutfitIndex >= 0;
        public int FallbackOutfitIndex => fallbackOutfitIndex;

        public void InitializeLookup(WardrobeSelectionRouter owner)
        {
            if (lookup == null)
            {
                lookup = new Dictionary<string, int>();
            }
            else
            {
                lookup.Clear();
            }

            foreach (ItemToOutfitIndex mapping in itemMappings)
            {
                if (string.IsNullOrEmpty(mapping.ItemId))
                {
                    continue;
                }

                if (lookup.ContainsKey(mapping.ItemId))
                {
                    Debug.LogWarning($"Duplicate outfit mapping for item '{mapping.ItemId}' in category {category}. Using the first entry only.", owner);
                    continue;
                }

                lookup.Add(mapping.ItemId, mapping.OutfitIndex);
            }
        }

        public bool TryGetOutfitIndex(string itemId, out int outfitIndex)
        {
            outfitIndex = default;

            if (lookup == null || string.IsNullOrEmpty(itemId))
            {
                return false;
            }

            return lookup.TryGetValue(itemId, out outfitIndex);
        }
    }

    [Header("References")]
    [SerializeField] private PlayerOutfitSwitcher playerOutfitSwitcher;

    [Header("Category Routes")]
    [SerializeField] private List<CategoryOutfitMapping> categoryMappings = new List<CategoryOutfitMapping>();

    private readonly Dictionary<WardrobeCategory, CategoryOutfitMapping> mappingLookup = new Dictionary<WardrobeCategory, CategoryOutfitMapping>();

    private void Awake()
    {
        BuildLookup();
    }

    private void OnEnable()
    {
        BuildLookup();
        RegisterListeners();
        ApplyInitialSelections();
    }

    private void OnDisable()
    {
        UnregisterListeners();
    }

    private void OnValidate()
    {
        BuildLookup();
    }

    private void BuildLookup()
    {
        mappingLookup.Clear();

        foreach (CategoryOutfitMapping mapping in categoryMappings)
        {
            if (mapping == null)
            {
                continue;
            }

            mapping.InitializeLookup(this);

            if (mappingLookup.ContainsKey(mapping.Category))
            {
                Debug.LogWarning($"Category {mapping.Category} has multiple mappings configured. Using the first mapping only.", this);
                continue;
            }

            mappingLookup.Add(mapping.Category, mapping);

            WardrobeItemSelectionGroup selectionGroup = mapping.SelectionGroup;
            if (selectionGroup == null)
            {
                Debug.LogWarning($"Selection group is not assigned for category {mapping.Category}.", this);
                continue;
            }

            if (selectionGroup.Category != mapping.Category)
            {
                Debug.LogWarning($"Selection group category mismatch for {mapping.Category}. Group reports {selectionGroup.Category}.", selectionGroup);
            }
        }
    }

    private void RegisterListeners()
    {
        foreach (CategoryOutfitMapping mapping in categoryMappings)
        {
            WardrobeItemSelectionGroup selectionGroup = mapping?.SelectionGroup;
            if (selectionGroup == null)
            {
                continue;
            }

            selectionGroup.OnSelectionChanged.AddListener(HandleSelectionChanged);
        }
    }

    private void UnregisterListeners()
    {
        foreach (CategoryOutfitMapping mapping in categoryMappings)
        {
            WardrobeItemSelectionGroup selectionGroup = mapping?.SelectionGroup;
            if (selectionGroup == null)
            {
                continue;
            }

            selectionGroup.OnSelectionChanged.RemoveListener(HandleSelectionChanged);
        }
    }

    private void ApplyInitialSelections()
    {
        foreach (CategoryOutfitMapping mapping in categoryMappings)
        {
            WardrobeItemSelectionGroup selectionGroup = mapping?.SelectionGroup;
            if (selectionGroup == null)
            {
                continue;
            }

            WardrobeItemButton selectedButton = selectionGroup.SelectedButton;
            if (selectedButton != null)
            {
                ApplyOutfitForSelection(selectionGroup.Category, selectedButton.ItemId, logWarnings: false);
            }
            else if (mapping.HasFallback)
            {
                ApplyOutfitIndex(mapping.FallbackOutfitIndex, selectionGroup.Category, isFallback: true, logWarnings: false);
            }
        }
    }

    private void HandleSelectionChanged(WardrobeCategory category, string itemId)
    {
        ApplyOutfitForSelection(category, itemId, logWarnings: true);
    }

    private void ApplyOutfitForSelection(WardrobeCategory category, string itemId, bool logWarnings)
    {
        if (playerOutfitSwitcher == null)
        {
            if (logWarnings)
            {
                Debug.LogWarning("PlayerOutfitSwitcher reference is not assigned.", this);
            }

            return;
        }

        if (!mappingLookup.TryGetValue(category, out CategoryOutfitMapping mapping) || mapping == null)
        {
            if (logWarnings)
            {
                Debug.LogWarning($"No outfit mapping configured for category {category}.", this);
            }

            return;
        }

        if (mapping.TryGetOutfitIndex(itemId, out int outfitIndex))
        {
            ApplyOutfitIndex(outfitIndex, category, isFallback: false, logWarnings: logWarnings);
            return;
        }

        if (logWarnings)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                Debug.LogWarning($"Selection for category {category} did not provide a valid item identifier.", this);
            }
            else
            {
                Debug.LogWarning($"No outfit index configured for item '{itemId}' in category {category}.", this);
            }
        }

        if (mapping.HasFallback)
        {
            ApplyOutfitIndex(mapping.FallbackOutfitIndex, category, isFallback: true, logWarnings: logWarnings);
        }
    }

    private void ApplyOutfitIndex(int outfitIndex, WardrobeCategory category, bool isFallback, bool logWarnings)
    {
        if (outfitIndex < 0)
        {
            if (logWarnings)
            {
                Debug.LogWarning($"Outfit index {outfitIndex} for category {category} is invalid.", this);
            }

            return;
        }

        playerOutfitSwitcher?.SetOutfit(outfitIndex);

        if (isFallback && logWarnings)
        {
            Debug.LogWarning($"Applied fallback outfit index {outfitIndex} for category {category}.", this);
        }
    }
}
