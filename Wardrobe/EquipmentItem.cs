using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Describes a wardrobe equipment item and links it to the assets used at runtime.
/// </summary>
[CreateAssetMenu(menuName = "Wardrobe/Equipment Item")]
public class EquipmentItem : ScriptableObject
{
    [Header("General Settings")]
    [Tooltip("Select the wardrobe category this item belongs to. For legacy data, map old categories to the closest WardrobeCategory value.")]
    [SerializeField]
    private WardrobeCategory category = WardrobeCategory.Tops;

    [Tooltip("Displayed name shown in UI and localization tables. When importing existing content, copy the legacy display string here.")]
    [SerializeField]
    private string displayName = string.Empty;

    [Tooltip("Icon used in wardrobe selection grids. Existing icon textures can be reused by assigning them here.")]
    [SerializeField]
    private Sprite icon;

    [Header("Runtime References")]
    [Tooltip("Prefab instantiated on the player when this item is equipped. When migrating, reference the existing prefab or model.")]
    [SerializeField]
    private GameObject equipmentPrefab;

    [Tooltip("Optional list of material overrides applied to the equipped prefab renderers. Use this to match legacy material variants.")]
    [SerializeField]
    private Material[] materialOverrides = System.Array.Empty<Material>();

    /// <summary>
    /// Gets the wardrobe category that determines how this item is slotted.
    /// </summary>
    public WardrobeCategory Category => category;

    /// <summary>
    /// Gets the localized display name shown in the wardrobe UI.
    /// </summary>
    public string DisplayName => displayName;

    /// <summary>
    /// Gets the icon displayed in selection menus.
    /// </summary>
    public Sprite Icon => icon;

    /// <summary>
    /// Gets the prefab to instantiate when equipping this item.
    /// </summary>
    public GameObject EquipmentPrefab => equipmentPrefab;

    /// <summary>
    /// Gets the optional material overrides for renderers on the equipped prefab.
    /// </summary>
    public IReadOnlyList<Material> MaterialOverrides => materialOverrides;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            Debug.LogWarning($"EquipmentItem '{name}' is missing a display name.", this);
        }

        if (icon == null)
        {
            Debug.LogWarning($"EquipmentItem '{name}' has no icon assigned.", this);
        }

        if (equipmentPrefab == null)
        {
            Debug.LogWarning($"EquipmentItem '{name}' has no equipment prefab assigned.", this);
        }
    }
}
