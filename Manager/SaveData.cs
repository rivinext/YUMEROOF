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
    public IndependentMaterialColorSaveData independentMaterialColors;

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
    public IndependentMaterialColorSaveData independentMaterialColors;

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
public class IndependentMaterialColorSaveData
{
    public List<IndependentMaterialColorSaveEntry> entries = new();
}

[Serializable]
public class IndependentMaterialColorSaveEntry
{
    public string identifier;
    public HSVColor color;
}
