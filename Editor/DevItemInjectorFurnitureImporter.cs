using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class DevItemInjectorFurnitureImporter : EditorWindow
{
    private DevItemInjector targetInjector;
    private DefaultAsset furnitureFolder;
    private int defaultQuantity = 1;
    private bool appendMode = true;

    [MenuItem("Tools/Dev Item Injector/Import Furniture")]
    public static void OpenWindow()
    {
        GetWindow<DevItemInjectorFurnitureImporter>(true, "Import Furniture Items", true);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Dev Item Injector", EditorStyles.boldLabel);
        targetInjector = (DevItemInjector)EditorGUILayout.ObjectField("Target Injector", targetInjector, typeof(DevItemInjector), true);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Furniture Source", EditorStyles.boldLabel);
        furnitureFolder = (DefaultAsset)EditorGUILayout.ObjectField("Furniture Folder", furnitureFolder, typeof(DefaultAsset), false);
        defaultQuantity = EditorGUILayout.IntField("Quantity", Mathf.Max(0, defaultQuantity));

        EditorGUILayout.Space();
        appendMode = EditorGUILayout.ToggleLeft("Append Mode (keep existing entries)", appendMode);

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(targetInjector == null || furnitureFolder == null || defaultQuantity <= 0))
        {
            if (GUILayout.Button("Import Furniture Items"))
            {
                ImportFurniture();
            }
        }
    }

    private void ImportFurniture()
    {
        if (targetInjector == null)
        {
            Debug.LogError("Target DevItemInjector is not set.");
            return;
        }

        string folderPath = AssetDatabase.GetAssetPath(furnitureFolder);
        if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.LogError("Furniture folder is not set or invalid.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:FurnitureDataSO", new[] { folderPath });
        if (guids == null || guids.Length == 0)
        {
            Debug.LogWarning($"No FurnitureDataSO assets found under '{folderPath}'.");
            return;
        }

        SerializedObject serializedInjector = new SerializedObject(targetInjector);
        serializedInjector.Update();
        SerializedProperty furnitureListProp = serializedInjector.FindProperty("furnitureItems");
        if (furnitureListProp == null || !furnitureListProp.isArray)
        {
            Debug.LogError("Target DevItemInjector does not contain a furnitureItems list.");
            return;
        }

        Undo.RecordObject(targetInjector, "Import Furniture Items");

        Dictionary<string, int> existingLookup = new Dictionary<string, int>();
        if (!appendMode)
        {
            furnitureListProp.ClearArray();
        }
        else
        {
            for (int i = 0; i < furnitureListProp.arraySize; i++)
            {
                SerializedProperty entryProp = furnitureListProp.GetArrayElementAtIndex(i);
                SerializedProperty idProp = entryProp.FindPropertyRelative("id");
                if (!string.IsNullOrEmpty(idProp.stringValue) && !existingLookup.ContainsKey(idProp.stringValue))
                {
                    existingLookup.Add(idProp.stringValue, i);
                }
            }
        }

        int added = 0;
        int updated = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            FurnitureDataSO furnitureData = AssetDatabase.LoadAssetAtPath<FurnitureDataSO>(path);
            if (furnitureData == null || string.IsNullOrEmpty(furnitureData.itemID))
            {
                continue;
            }

            int targetIndex;
            if (existingLookup.TryGetValue(furnitureData.itemID, out targetIndex))
            {
                SerializedProperty entryProp = furnitureListProp.GetArrayElementAtIndex(targetIndex);
                entryProp.FindPropertyRelative("quantity").intValue = defaultQuantity;
                updated++;
                continue;
            }

            targetIndex = furnitureListProp.arraySize;
            furnitureListProp.InsertArrayElementAtIndex(targetIndex);
            SerializedProperty newEntryProp = furnitureListProp.GetArrayElementAtIndex(targetIndex);
            newEntryProp.FindPropertyRelative("id").stringValue = furnitureData.itemID;
            newEntryProp.FindPropertyRelative("quantity").intValue = defaultQuantity;
            existingLookup[furnitureData.itemID] = targetIndex;
            added++;
        }

        serializedInjector.ApplyModifiedProperties();
        EditorUtility.SetDirty(targetInjector);
        AssetDatabase.SaveAssets();

        Debug.Log($"Furniture import completed. Added: {added}, Updated: {updated}, Total: {furnitureListProp.arraySize}.");
    }
}
