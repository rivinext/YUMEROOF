using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "YumeRoof/Initial Furniture Placement Config")]
public class InitialFurniturePlacementConfig : ScriptableObject
{
    public const string ResourcePath = "Data/ScriptableObjects/InitialFurniturePlacements";
    public List<InitialFurniturePlacement> placements = new();

    public static InitialFurniturePlacementConfig Load()
    {
        return Resources.Load<InitialFurniturePlacementConfig>(ResourcePath);
    }
}

[Serializable]
public class InitialFurniturePlacement
{
    public string furnitureID;
    public Vector3 position;
    public Vector3 rotationEuler;
    public string sceneName;
    public int layer = -1;
    public string parentUID;
    public string uniqueID;
    public int wallParentId;
    public string wallParentName;
    public string wallParentPath;
}
