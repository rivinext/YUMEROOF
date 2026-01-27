using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class ShadowOptimizationTools
{
    private const float DefaultShadowDistance = 30f;
    private const float SmallObjectMaxSize = 0.5f;
    private const string FurniturePrefabFolder = "Assets/Resources/Furniture";

    [MenuItem("Tools/Rendering/Shadow Optimization/Apply Default Shadow Distance")]
    private static void ApplyDefaultShadowDistance()
    {
        ApplyShadowDistance(DefaultShadowDistance);
    }

    [MenuItem("Tools/Rendering/Shadow Optimization/Optimize Furniture Prefabs")]
    private static void OptimizeFurniturePrefabs()
    {
        OptimizePrefabsInFolder(FurniturePrefabFolder);
    }

    [MenuItem("Tools/Rendering/Shadow Optimization/Optimize Selected Objects")]
    private static void OptimizeSelectedObjects()
    {
        var selectedObjects = Selection.gameObjects;
        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            Debug.LogWarning("[ShadowOptimizationTools] No objects selected.");
            return;
        }

        var prefabPaths = new HashSet<string>();
        var sceneRoots = new List<GameObject>();

        foreach (var obj in selectedObjects)
        {
            if (obj == null)
            {
                continue;
            }

            if (PrefabUtility.IsPartOfPrefabAsset(obj))
            {
                var path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path))
                {
                    prefabPaths.Add(path);
                }
            }
            else
            {
                sceneRoots.Add(obj);
            }
        }

        foreach (var path in prefabPaths)
        {
            OptimizePrefabAtPath(path);
        }

        foreach (var root in sceneRoots)
        {
            ApplyShadowSettingsToHierarchy(root);
        }
    }

    private static void ApplyShadowDistance(float distance)
    {
        var qualityCount = QualitySettings.names.Length;
        if (qualityCount == 0)
        {
            Debug.LogWarning("[ShadowOptimizationTools] No quality levels found.");
            return;
        }

        var originalQuality = QualitySettings.GetQualityLevel();
        for (var i = 0; i < qualityCount; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.shadowDistance = distance;
        }

        QualitySettings.SetQualityLevel(originalQuality, false);

        var qualitySettingsAsset = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/QualitySettings.asset");
        if (qualitySettingsAsset != null)
        {
            EditorUtility.SetDirty(qualitySettingsAsset);
            AssetDatabase.SaveAssets();
        }

        Debug.Log($"[ShadowOptimizationTools] Shadow distance set to {distance} for all quality levels.");
    }

    private static void OptimizePrefabsInFolder(string folderPath)
    {
        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
        if (guids == null || guids.Length == 0)
        {
            Debug.LogWarning($"[ShadowOptimizationTools] No prefabs found in {folderPath}.");
            return;
        }

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            OptimizePrefabAtPath(path);
        }
    }

    private static void OptimizePrefabAtPath(string path)
    {
        var prefabRoot = PrefabUtility.LoadPrefabContents(path);
        if (prefabRoot == null)
        {
            return;
        }

        ApplyShadowSettingsToHierarchy(prefabRoot);
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }

    private static void ApplyShadowSettingsToHierarchy(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            ApplyShadowSettings(renderer);
        }
    }

    private static void ApplyShadowSettings(Renderer renderer)
    {
        if (renderer == null)
        {
            return;
        }

        var target = renderer.gameObject;
        if (target.isStatic)
        {
            var flags = GameObjectUtility.GetStaticEditorFlags(target);
            GameObjectUtility.SetStaticEditorFlags(target, flags | StaticEditorFlags.ContributeGI);
            renderer.receiveGI = ReceiveGI.Lightmaps;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return;
        }

        if (ShouldDisableShadowCasting(renderer))
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }
    }

    private static bool ShouldDisableShadowCasting(Renderer renderer)
    {
        var boundsSize = renderer.bounds.size;
        var maxSize = Mathf.Max(boundsSize.x, boundsSize.y, boundsSize.z);
        return maxSize <= SmallObjectMaxSize;
    }
}
