using UnityEditor;
using UnityEngine;

public static class WardrobePartTagBatcher
{
    [MenuItem("Tools/Wardrobe/Add Part Tags To Selected Prefabs")]
    public static void AddTagsToSelectedPrefabs()
    {
        GameObject[] selectedPrefabs = Selection.GetFiltered<GameObject>(SelectionMode.Assets);
        if (selectedPrefabs == null || selectedPrefabs.Length == 0)
        {
            EditorUtility.DisplayDialog("Wardrobe Part Tag", "プレハブをプロジェクトから選択してください。", "OK");
            return;
        }

        int updatedPrefabCount = 0;
        int updatedTagCount = 0;

        for (int i = 0; i < selectedPrefabs.Length; i++)
        {
            string prefabPath = AssetDatabase.GetAssetPath(selectedPrefabs[i]);
            if (string.IsNullOrEmpty(prefabPath))
            {
                continue;
            }

            GameObject prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);
            if (prefabContents == null)
            {
                continue;
            }

            int tagsAddedForPrefab = 0;
            bool hasChanges = false;

            try
            {
                Transform[] transforms = prefabContents.GetComponentsInChildren<Transform>(true);
                for (int t = 0; t < transforms.Length; t++)
                {
                    Transform current = transforms[t];
                    if (current == null || current == prefabContents.transform)
                    {
                        continue;
                    }

                    WardrobePartTag tag = current.GetComponent<WardrobePartTag>();
                    if (tag == null)
                    {
                        tag = current.gameObject.AddComponent<WardrobePartTag>();
                        tagsAddedForPrefab++;
                        hasChanges = true;
                    }

                    string normalizedName = WardrobePartNameUtility.NormalizePartName(current.name);
                    if (tag.PartName != normalizedName)
                    {
                        tag.PartName = normalizedName;
                        EditorUtility.SetDirty(tag);
                        hasChanges = true;
                    }
                }

                if (hasChanges)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
                    updatedPrefabCount++;
                    updatedTagCount += tagsAddedForPrefab;
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabContents);
            }
        }

        Debug.Log($"Wardrobe Part Tag を追加しました。更新プレハブ数: {updatedPrefabCount}, 追加タグ数: {updatedTagCount}");
    }
}
