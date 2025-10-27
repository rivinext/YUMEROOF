using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wardrobe/Catalog", fileName = "WardrobeCatalog")]
public class WardrobeCatalog : ScriptableObject
{
    [SerializeField]
    private List<WardrobeCatalogEntry> entries = new List<WardrobeCatalogEntry>();

    public IReadOnlyList<WardrobeCatalogEntry> Entries
    {
        get { return entries; }
    }

    public void SetEntries(List<WardrobeCatalogEntry> newEntries)
    {
        entries = newEntries != null
            ? new List<WardrobeCatalogEntry>(newEntries)
            : new List<WardrobeCatalogEntry>();
    }
}

[Serializable]
public struct WardrobeCatalogEntry
{
    [SerializeField] private string displayName;
    [SerializeField] private string categoryName;
    [SerializeField] private string itemId;
    [SerializeField] private string nameId;
    [SerializeField] private string image2D;
    [SerializeField] private string model3D;
    [SerializeField] private string descriptionId;
    [SerializeField] private WardrobeTabType tabType;

    public string DisplayName
    {
        get { return displayName; }
        set { displayName = value; }
    }

    public string CategoryName
    {
        get { return categoryName; }
        set { categoryName = value; }
    }

    public string ItemId
    {
        get { return itemId; }
        set { itemId = value; }
    }

    public string NameId
    {
        get { return nameId; }
        set { nameId = value; }
    }

    public string Image2D
    {
        get { return image2D; }
        set { image2D = value; }
    }

    public string Model3D
    {
        get { return model3D; }
        set { model3D = value; }
    }

    public string DescriptionId
    {
        get { return descriptionId; }
        set { descriptionId = value; }
    }

    public WardrobeTabType TabType
    {
        get { return tabType; }
        set { tabType = value; }
    }
}
