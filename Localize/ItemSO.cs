using UnityEngine;

[CreateAssetMenu(fileName = "Item_", menuName = "Game/Item")]
public class ItemSO : ScriptableObject
{
    [Header("Basic Info")]
    public string ItemID;
    public string NameID;  // ローカライズキー（ItemNamesテーブル用）
    public string DescriptionID;  // 説明のローカライズキー（DescNamesテーブル用）

    [Header("Stats")]
    public int Cozy;
    public int Nature;
    public int SellPrice;

    [Header("Settings")]
    public string Category;
    public string PlacementRules;
    public float SurfaceType;
    public string Rarity;
    public bool IsMovable = true;
    public string InteractionType;

    [Header("Visual")]
    public Sprite Icon2D;  // 2DImage
    public GameObject Model3D;  // 3DModel

    [Header("Crafting")]
    public string RecipeID;
    public string UnlockCondition;

    [Header("Recipe Materials")]
    public string[] RecipeMaterialIDs = new string[3];
    public int[] RecipeQuantities = new int[3];

    [Header("Drop Materials")]
    public string[] DropMaterialIDs = new string[3];
    public float[] DropRates = new float[3];
}
