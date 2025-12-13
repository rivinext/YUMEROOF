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
        public string parentUID;
        public string uniqueID;
    }
}

[Serializable]
public class BaseSaveData
{
    public string saveDate;
    public float playTime;
    public string chapterName;
    public string location;
    public string screenshotFilename;

    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }
}

[Serializable]
public class StorySaveData : BaseSaveData
{
    public PlayerManager.PlayerData player;
    public List<string> inventory = new();
    public List<SaveSystem.PlacedFurniture> furniture = new();
    public GameClock.ClockData clock;
    public int money;
    public int milestoneIndex;
    public int cozy;
    public int nature;
    public MaterialHueSaveData materialHue;
    public List<IndependentMaterialColorSlotEntry> independentMaterialColorSlots = new();
    public IndependentMaterialColorSaveData independentMaterialColors = new();
    public List<WardrobeSelectionEntry> wardrobeSelections = new();
    public bool hasWardrobeSelections;

    public static StorySaveData FromJson(string json)
    {
        return JsonUtility.FromJson<StorySaveData>(json);
    }
}

[Serializable]
public class CreativeSaveData : BaseSaveData
{
    public PlayerManager.PlayerData player;
    public List<string> ownedItems = new();
    public List<SaveSystem.PlacedFurniture> furniture = new();
    public GameClock.ClockData clock;
    public int money;
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
        return JsonUtility.FromJson<CreativeSaveData>(json);
    }
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
