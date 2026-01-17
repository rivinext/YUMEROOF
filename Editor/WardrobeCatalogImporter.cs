using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class WardrobeCatalogImporter
{
    private const string CsvAssetPath = "Assets/Resources/Data/YUME_ROOF - Wardrobe.csv";
    internal const string CatalogAssetPath = "Assets/Data/ScriptableObjects/Wardrobe/WardrobeCatalog.asset";

    [MenuItem("Tools/Wardrobe/Import Catalog")]
    public static void ImportCatalog()
    {
        TextAsset csvAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(CsvAssetPath);
        if (csvAsset == null)
        {
            Debug.LogError($"Wardrobe catalog CSV not found at '{CsvAssetPath}'.");
            return;
        }

        List<WardrobeCatalogEntry> parsedEntries = ParseCsv(csvAsset.text);

        WardrobeCatalog catalog = AssetDatabase.LoadAssetAtPath<WardrobeCatalog>(CatalogAssetPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<WardrobeCatalog>();
            catalog.SetEntries(parsedEntries);
            AssetDatabase.CreateAsset(catalog, CatalogAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            WardrobeCatalogAutoBinder.AutoBindCatalogAssets();
            Debug.Log($"Created WardrobeCatalog asset with {parsedEntries.Count} entries.");
            return;
        }

        PreserveExistingEntryData(parsedEntries, catalog);
        Undo.RecordObject(catalog, "Update Wardrobe Catalog");
        catalog.SetEntries(parsedEntries);
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Updated WardrobeCatalog asset with {parsedEntries.Count} entries.");

        WardrobeCatalogAutoBinder.AutoBindCatalogAssets();
    }

    private static void PreserveExistingEntryData(List<WardrobeCatalogEntry> parsedEntries, WardrobeCatalog catalog)
    {
        if (parsedEntries == null || catalog == null)
        {
            return;
        }

        IReadOnlyList<WardrobeCatalogEntry> existingEntries = catalog.Entries;
        if (existingEntries == null)
        {
            return;
        }

        Dictionary<string, WardrobeCatalogEntry> existingLookup = new Dictionary<string, WardrobeCatalogEntry>();
        for (int i = 0; i < existingEntries.Count; i++)
        {
            WardrobeCatalogEntry existing = existingEntries[i];
            if (!string.IsNullOrEmpty(existing.ItemId) && !existingLookup.ContainsKey(existing.ItemId))
            {
                existingLookup.Add(existing.ItemId, existing);
            }
        }

        for (int i = 0; i < parsedEntries.Count; i++)
        {
            WardrobeCatalogEntry entry = parsedEntries[i];
            if (string.IsNullOrEmpty(entry.ItemId))
            {
                continue;
            }

            WardrobeCatalogEntry existing;
            if (!existingLookup.TryGetValue(entry.ItemId, out existing))
            {
                continue;
            }

            entry.ImageSprite = existing.ImageSprite;
            entry.WearablePrefab = existing.WearablePrefab;
            parsedEntries[i] = entry;
        }
    }

    private static List<WardrobeCatalogEntry> ParseCsv(string csvText)
    {
        List<WardrobeCatalogEntry> entries = new List<WardrobeCatalogEntry>();
        if (string.IsNullOrEmpty(csvText))
        {
            return entries;
        }

        using (StringReader reader = new StringReader(csvText))
        {
            string header = reader.ReadLine();
            if (string.IsNullOrEmpty(header))
            {
                return entries;
            }

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                List<string> columns = SplitCsvLine(line);
                bool hasNonEmptyColumn = false;
                for (int i = 0; i < columns.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(columns[i]))
                    {
                        hasNonEmptyColumn = true;
                        break;
                    }
                }

                if (!hasNonEmptyColumn)
                {
                    continue;
                }

                if (columns.Count < 7)
                {
                    Debug.LogWarning($"Skipping wardrobe row because it does not contain enough columns: '{line}'.");
                    continue;
                }

                WardrobeCatalogEntry entry = new WardrobeCatalogEntry
                {
                    DisplayName = columns[0],
                    CategoryName = columns[1],
                    ItemId = columns[2],
                    NameId = columns[3],
                    Image2D = columns[4],
                    Model3D = columns[5],
                    DescriptionId = columns[6],
                    TabType = ParseCategory(columns[1])
                };

                entries.Add(entry);
            }
        }

        return entries;
    }

    private static List<string> SplitCsvLine(string line)
    {
        List<string> columns = new List<string>();
        if (line == null)
        {
            return columns;
        }

        StringBuilder currentValue = new StringBuilder();
        bool insideQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (insideQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    currentValue.Append('"');
                    i++;
                }
                else
                {
                    insideQuotes = !insideQuotes;
                }
            }
            else if (c == ',' && !insideQuotes)
            {
                columns.Add(currentValue.ToString().Trim());
                currentValue.Length = 0;
            }
            else
            {
                currentValue.Append(c);
            }
        }

        columns.Add(currentValue.ToString().Trim());
        return columns;
    }

    private static WardrobeTabType ParseCategory(string rawCategory)
    {
        if (string.IsNullOrEmpty(rawCategory))
        {
            return WardrobeTabType.Hair;
        }

        string normalized = rawCategory.Trim();
        string sanitized = normalized.Replace(" ", string.Empty).Replace("-", string.Empty).Replace("_", string.Empty);
        WardrobeTabType tabType;
        if (Enum.TryParse(sanitized, true, out tabType))
        {
            return tabType;
        }

        switch (normalized.ToLowerInvariant())
        {
            case "onepiece":
            case "one-piece":
            case "dress":
                return WardrobeTabType.OnePiece;
            case "pants":
            case "bottoms":
                return WardrobeTabType.Pants;
            case "tops":
            case "shirts":
                return WardrobeTabType.Tops;
            case "shoes":
            case "footwear":
                return WardrobeTabType.Shoes;
            case "hair":
            case "hairstyle":
                return WardrobeTabType.Hair;
            case "accessories":
            case "accesories":
            case "accessory":
                return WardrobeTabType.Accessories;
            case "eyewear":
            case "glasses":
                return WardrobeTabType.Eyewear;
            default:
                Debug.LogWarning($"Unknown wardrobe category '{rawCategory}'. Defaulting to Hair.");
                return WardrobeTabType.Hair;
        }
    }
}
