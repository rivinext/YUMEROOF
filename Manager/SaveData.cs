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

    public static CreativeSaveData FromJson(string json)
    {
        return JsonUtility.FromJson<CreativeSaveData>(json);
    }
}
