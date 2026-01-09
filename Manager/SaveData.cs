using System;
using System.Collections.Generic;
using UnityEngine;

namespace SaveSystem
{
    [Serializable]
    public class PlacedFurniture
    {
        public string id;
        public Vector3 position;
        public Quaternion rotation;
        public string sceneName;
        public int layer;
        public string parentUID;
        public string uniqueID;
        public int wallParentId;
        public string wallParentName;
        public string wallParentPath;
    }
}

[Serializable]
public class InventoryEntry
{
    public string itemID;
    public int quantity;
    public bool isMaterial;
}

[Serializable]
public class BaseSaveData
{
    public string saveDate;
    public float playTime;
    public string chapterName;
    public string location;
    public bool applied_0_1_6_seed;

    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }
}

[Serializable]
public class StorySaveData : BaseSaveData
{
    public const int DefaultMoney = 2026;
    public PlayerManager.PlayerData player;
    public List<InventoryEntry> inventory = new();
    [NonSerialized] public List<string> legacyInventory;
    public List<SaveSystem.PlacedFurniture> furniture = new();
    public GameClock.ClockData clock;
    public int money = DefaultMoney;
    public int milestoneIndex;
    public int cozy;
    public int nature;
    public MaterialHueSaveData materialHue;
    public List<IndependentMaterialColorSlotEntry> independentMaterialColorSlots = new();
    public IndependentMaterialColorSaveData independentMaterialColors = new();
    public List<WardrobeSelectionEntry> wardrobeSelections = new();
    public bool hasWardrobeSelections;
    public bool hasSeenOpeningPanel;

    public static StorySaveData FromJson(string json)
    {
        var data = JsonUtility.FromJson<StorySaveData>(json);
        if (data == null)
        {
            return null;
        }

        if (data.inventory == null || data.inventory.Count == 0)
        {
            var legacy = JsonUtility.FromJson<StorySaveDataLegacy>(json);
            if (legacy != null && legacy.inventory != null && legacy.inventory.Count > 0)
            {
                data.legacyInventory = new List<string>(legacy.inventory);
            }
        }

        return data;
    }
}

[Serializable]
public class CreativeSaveData : BaseSaveData
{
    public PlayerManager.PlayerData player;
    public List<InventoryEntry> ownedItems = new();
    [NonSerialized] public List<string> legacyOwnedItems;
    public List<SaveSystem.PlacedFurniture> furniture = new();
    public GameClock.ClockData clock;
    public int money;
    public bool unlimitedMoney = true;
    public int milestoneIndex;
    public int cozy;
    public int nature;
    public MaterialHueSaveData materialHue;
    public List<IndependentMaterialColorSlotEntry> independentMaterialColorSlots = new();
    public IndependentMaterialColorSaveData independentMaterialColors = new();
    public List<WardrobeSelectionEntry> wardrobeSelections = new();
    public bool hasWardrobeSelections;

    public static CreativeSaveData FromJson(string json)
    {
        var data = JsonUtility.FromJson<CreativeSaveData>(json);
        if (data == null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(json) && !json.Contains("\"unlimitedMoney\""))
        {
            data.unlimitedMoney = true;
        }

        if (data.ownedItems == null || data.ownedItems.Count == 0)
        {
            var legacy = JsonUtility.FromJson<CreativeSaveDataLegacy>(json);
            if (legacy != null && legacy.ownedItems != null && legacy.ownedItems.Count > 0)
            {
                data.legacyOwnedItems = new List<string>(legacy.ownedItems);
            }
        }

        return data;
    }
}

[Serializable]
internal class StorySaveDataLegacy
{
    public List<string> inventory = new();
}

[Serializable]
internal class CreativeSaveDataLegacy
{
    public List<string> ownedItems = new();
}

[Serializable]
public class MaterialHueSaveData
{
    // Legacy support for single manager saves
    public int selectedSlotIndex;
    public List<HSVColor> controllerColors = new();

    public List<MaterialHueManagerSaveData> managers = new();
}

[Serializable]
public class MaterialHueManagerSaveData
{
    public string keyPrefix;
    public int selectedSlotIndex;
    public List<HSVColor> controllerColors = new();
}

[Serializable]
public class IndependentMaterialColorEntry
{
    public string key;
    public HSVColor color;
}

[Serializable]
public class IndependentMaterialColorSlotEntry
{
    public string slotKey;
    public IndependentMaterialColorSaveData colors = new();
}

[Serializable]
public class IndependentMaterialColorSaveData
{
    public List<IndependentMaterialColorEntry> colors = new();

    public IndependentMaterialColorSaveData()
    {
    }

    public IndependentMaterialColorSaveData(IndependentMaterialColorSaveData other)
    {
        if (other?.colors == null)
        {
            return;
        }

        foreach (var entry in other.colors)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.key))
            {
                continue;
            }

            SetColor(entry.key, entry.color);
        }
    }

    public void SetColor(string key, HSVColor color)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        string trimmedKey = key.Trim();
        for (int i = 0; i < colors.Count; i++)
        {
            if (colors[i] != null && colors[i].key == trimmedKey)
            {
                colors[i].color = color;
                return;
            }
        }

        colors.Add(new IndependentMaterialColorEntry
        {
            key = trimmedKey,
            color = color
        });
    }

    public bool TryGetColor(string key, out HSVColor color)
    {
        color = default;

        if (string.IsNullOrWhiteSpace(key) || colors == null)
        {
            return false;
        }

        string trimmedKey = key.Trim();
        foreach (var entry in colors)
        {
            if (entry != null && entry.key == trimmedKey)
            {
                color = entry.color;
                return true;
            }
        }

        return false;
    }
}
