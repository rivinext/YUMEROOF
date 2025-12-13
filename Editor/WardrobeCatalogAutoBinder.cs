using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class WardrobeCatalogAutoBinder
{
    [MenuItem("Tools/Wardrobe/Auto Bind Assets")]
    public static void AutoBindCatalogAssets()
    {
        WardrobeCatalog catalog = AssetDatabase.LoadAssetAtPath<WardrobeCatalog>(WardrobeCatalogImporter.CatalogAssetPath);
        if (catalog == null)
        {
            Debug.LogError($"Wardrobe catalog asset not found at '{WardrobeCatalogImporter.CatalogAssetPath}'.");
            return;
        }

        IReadOnlyList<WardrobeCatalogEntry> entries = catalog.Entries;
        if (entries == null || entries.Count == 0)
        {
            Debug.LogWarning("Wardrobe catalog has no entries to auto bind.");
            return;
        }

        List<WardrobeCatalogEntry> updatedEntries = new List<WardrobeCatalogEntry>(entries.Count);
        for (int i = 0; i < entries.Count; i++)
        {
            WardrobeCatalogEntry entry = entries[i];
            WardrobeCatalogEntry updatedEntry = entry;

            Sprite imageSprite = FindSprite(entry.Image2D);
            if (imageSprite != null)
            {
                updatedEntry.ImageSprite = imageSprite;
            }

            GameObject wearablePrefab = FindPrefab(entry.Model3D);
            if (wearablePrefab != null)
            {
                updatedEntry.WearablePrefab = wearablePrefab;
            }

            updatedEntries.Add(updatedEntry);
        }

        Undo.RecordObject(catalog, "Auto Bind Wardrobe Catalog Assets");
        catalog.SetEntries(updatedEntries);
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Auto bound assets for {updatedEntries.Count} wardrobe catalog entries.");
    }

    private static Sprite FindSprite(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName))
        {
            return null;
        }

        string[] guids = AssetDatabase.FindAssets($"{spriteName} t:Sprite");
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite != null && sprite.name == spriteName)
            {
                return sprite;
            }
        }

        return null;
    }

    private static GameObject FindPrefab(string prefabName)
    {
        if (string.IsNullOrEmpty(prefabName))
        {
            return null;
        }

        string[] guids = AssetDatabase.FindAssets($"{prefabName} t:Prefab");
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab != null && prefab.name == prefabName)
            {
                return prefab;
            }
        }

        return null;
    }
}
