using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// 自動的にScriptableObjectとアセットを接続するツール
public class AutoAssetLinker : EditorWindow
{
    private string prefabFolder = "Assets/Resources/Furniture/";
    private string iconFolder = "Assets/Sprites/Icons/";
    private string furnitureSOFolder = "Assets/Data/ScriptableObjects/Furniture/";
    private string materialSOFolder = "Assets/Data/ScriptableObjects/Materials/";

    private List<LinkResult> linkResults = new List<LinkResult>();
    private Vector2 scrollPos;

    [MenuItem("Tools/Yume Roof/Auto Asset Linker")]
    public static void ShowWindow()
    {
        GetWindow<AutoAssetLinker>("Auto Asset Linker");
    }

    void OnGUI()
    {
        GUILayout.Label("自動アセットリンクツール", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("ScriptableObjectに3Dモデルと2Dアイコンを自動的に接続します", MessageType.Info);

        EditorGUILayout.Space();

        // フォルダパス設定
        EditorGUILayout.LabelField("フォルダ設定", EditorStyles.boldLabel);
        prefabFolder = EditorGUILayout.TextField("Prefabフォルダ", prefabFolder);
        iconFolder = EditorGUILayout.TextField("アイコンフォルダ", iconFolder);
        furnitureSOFolder = EditorGUILayout.TextField("家具SOフォルダ", furnitureSOFolder);
        materialSOFolder = EditorGUILayout.TextField("素材SOフォルダ", materialSOFolder);

        EditorGUILayout.Space();

        // 実行ボタン
        if (GUILayout.Button("家具アセットを自動リンク", GUILayout.Height(30)))
        {
            LinkFurnitureAssets();
        }

        if (GUILayout.Button("素材アセットを自動リンク", GUILayout.Height(30)))
        {
            LinkMaterialAssets();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("すべて自動リンク", GUILayout.Height(40)))
        {
            linkResults.Clear();
            LinkFurnitureAssets();
            LinkMaterialAssets();
        }

        // 結果表示
        if (linkResults.Count > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("リンク結果", EditorStyles.boldLabel);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));

            foreach (var result in linkResults)
            {
                EditorGUILayout.BeginHorizontal();

                // アイコン表示
                var icon = result.success ? EditorGUIUtility.IconContent("d_FilterSelectedOnly").image :
                                           EditorGUIUtility.IconContent("console.erroricon").image;
                GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(16));

                // メッセージ表示
                EditorGUILayout.LabelField(result.message);

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }
    }

    void LinkFurnitureAssets()
    {
        linkResults.Clear();

        // すべてのFurnitureDataSOを取得
        string[] guids = AssetDatabase.FindAssets("t:FurnitureDataSO", new[] { furnitureSOFolder });
        int successCount = 0;
        int totalCount = guids.Length;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            FurnitureDataSO data = AssetDatabase.LoadAssetAtPath<FurnitureDataSO>(path);

            if (data == null) continue;

            bool prefabLinked = false;
            bool iconLinked = false;

            // Prefabの自動リンク
            if (string.IsNullOrEmpty(data.modelName))
            {
                linkResults.Add(new LinkResult(false, $"{data.nameID}: modelNameが空です"));
            }
            else
            {
                // デバッグ情報を追加
                linkResults.Add(new LinkResult(false, $"検索中: ModelName={data.modelName}, NameID={data.nameID}"));

                string searchFolder = prefabFolder.TrimEnd('/', '\\');
                string[] prefabGuids;

                if (string.IsNullOrEmpty(searchFolder))
                {
                    prefabGuids = System.Array.Empty<string>();
                    linkResults.Add(new LinkResult(false, "  検索フォルダが未設定です"));
                }
                else
                {
                    prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { searchFolder });
                    linkResults.Add(new LinkResult(false, $"  検索対象フォルダ: {searchFolder} (ヒット数: {prefabGuids.Length})"));
                }

                foreach (string prefabGuid in prefabGuids)
                {
                    string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

                    if (prefab == null) continue;

                    if (prefab.name == data.modelName || prefab.name == data.nameID)
                    {
                        data.prefab = prefab;
                        prefabLinked = true;
                        linkResults.Add(new LinkResult(true, $"{data.nameID}: Prefab接続成功 ({prefab.name})"));
                        break;
                    }
                }

                if (!prefabLinked)
                {
                    // Prefabが見つからない場合、FBXから自動生成を試みる
                    string fbxPath = $"{prefabFolder}{data.modelName}.fbx";
                    GameObject fbxModel = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);

                    if (fbxModel != null)
                    {
                        // FBXからPrefabを自動生成
                        GameObject newPrefab = CreatePrefabFromFBX(fbxModel, data.modelName, data);
                        if (newPrefab != null)
                        {
                            data.prefab = newPrefab;
                            prefabLinked = true;
                            linkResults.Add(new LinkResult(true, $"{data.nameID}: FBXからPrefab自動生成"));
                        }
                    }
                    else
                    {
                        linkResults.Add(new LinkResult(false, $"{data.nameID}: 条件に一致するPrefabが見つかりません (検索フォルダ: {prefabFolder})"));
                    }
                }

                if (prefabLinked && data.interactionType == InteractionType.Sit && data.prefab != null)
                {
                    string prefabPath = AssetDatabase.GetAssetPath(data.prefab);
                    GameObject prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);
                    if (prefabContents.GetComponent<SitTrigger>() == null)
                    {
                        prefabContents.AddComponent<SitTrigger>();
                        PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
                        linkResults.Add(new LinkResult(true, $"{data.nameID}: SitTrigger追加"));
                    }
                    PrefabUtility.UnloadPrefabContents(prefabContents);
                }
            }

            // アイコンの自動リンク
            if (string.IsNullOrEmpty(data.iconName))
            {
                linkResults.Add(new LinkResult(false, $"{data.nameID}: iconNameが空です"));
            }
            else
            {
                // 複数の検索パターンを試す（サブフォルダも含めて探索）
                var iconNameCandidates = new string[]
                {
                    data.iconName,
                    $"icon_{data.nameID.ToLower()}",
                    data.nameID
                };

                Sprite icon = FindSpriteInFolder(iconFolder, iconNameCandidates);

                if (icon != null)
                {
                    data.icon = icon;
                    iconLinked = true;
                    linkResults.Add(new LinkResult(true, $"{data.nameID}: アイコン接続成功 ({icon.name})"));
                }

                if (!iconLinked)
                {
                    string texturePath = FindTexturePathInFolder(iconFolder, new[] { data.iconName });
                    if (!string.IsNullOrEmpty(texturePath))
                    {
                        ConvertTextureToSprite(texturePath);
                        Sprite convertedIcon = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
                        if (convertedIcon != null)
                        {
                            data.icon = convertedIcon;
                            iconLinked = true;
                            linkResults.Add(new LinkResult(true, $"{data.nameID}: テクスチャをSpriteに変換して接続"));
                        }
                    }

                    if (!iconLinked)
                    {
                        linkResults.Add(new LinkResult(false, $"{data.nameID}: アイコンが見つかりません ({data.iconName})"));
                    }
                }
            }

            // ScriptableObjectを保存
            if (prefabLinked || iconLinked)
            {
                EditorUtility.SetDirty(data);
                successCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        linkResults.Add(new LinkResult(true, $"===== 完了: {successCount}/{totalCount} 個の家具をリンク ====="));
        Debug.Log($"家具アセットリンク完了: {successCount}/{totalCount}");
    }

    void LinkMaterialAssets()
    {
        // すべてのMaterialDataSOを取得
        string[] guids = AssetDatabase.FindAssets("t:MaterialDataSO", new[] { materialSOFolder });
        int successCount = 0;
        int totalCount = guids.Length;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            MaterialDataSO data = AssetDatabase.LoadAssetAtPath<MaterialDataSO>(path);

            if (data == null) continue;

            bool iconLinked = false;

            // アイコンの自動リンク
            if (!string.IsNullOrEmpty(data.iconName))
            {
                var iconNameCandidates = new string[]
                {
                    data.iconName,
                    $"icon_{data.materialID.ToLower()}",
                    data.materialID
                };

                Sprite icon = FindSpriteInFolder(iconFolder, iconNameCandidates);
                if (icon != null)
                {
                    data.icon = icon;
                    iconLinked = true;
                    linkResults.Add(new LinkResult(true, $"{data.materialID}: アイコン接続成功"));
                }
                else
                {
                    linkResults.Add(new LinkResult(false, $"{data.materialID}: アイコンが見つかりません"));
                }
            }

            if (iconLinked)
            {
                EditorUtility.SetDirty(data);
                successCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        linkResults.Add(new LinkResult(true, $"===== 完了: {successCount}/{totalCount} 個の素材をリンク ====="));
        Debug.Log($"素材アセットリンク完了: {successCount}/{totalCount}");
    }

    // FBXからPrefabを自動生成
    GameObject CreatePrefabFromFBX(GameObject fbxModel, string prefabName, FurnitureDataSO data)
    {
        string prefabPath = $"{prefabFolder}{prefabName}.prefab";

        // FBXのインスタンスを作成
        GameObject instance = Instantiate(fbxModel);
        instance.name = prefabName;

        // PlacedFurnitureコンポーネントを追加
        instance.AddComponent<PlacedFurniture>();

        // インタラクションタイプに応じてSitTriggerを追加
        if (data != null && data.interactionType == InteractionType.Sit)
        {
            instance.AddComponent<SitTrigger>();
        }

        // Colliderを追加（なければ）
        if (instance.GetComponent<Collider>() == null)
        {
            instance.AddComponent<BoxCollider>();
        }

        // Prefabとして保存
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);

        // インスタンスを削除
        DestroyImmediate(instance);

        return prefab;
    }

    // テクスチャをSpriteに変換
    void ConvertTextureToSprite(string texturePath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
        }
    }

    // アイコンフォルダ配下（サブフォルダ含む）からSpriteを検索
    Sprite FindSpriteInFolder(string baseFolder, IEnumerable<string> candidateNames)
    {
        string searchFolder = baseFolder.TrimEnd('/', '\\');
        if (string.IsNullOrEmpty(searchFolder))
        {
            linkResults.Add(new LinkResult(false, "  アイコン検索フォルダが未設定です"));
            return null;
        }

        string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite", new[] { searchFolder });
        foreach (string guid in spriteGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            string assetName = Path.GetFileNameWithoutExtension(assetPath);

            if (candidateNames.Any(name => string.Equals(name, assetName, StringComparison.OrdinalIgnoreCase)))
            {
                return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            }
        }

        return null;
    }

    // アイコンフォルダ配下（サブフォルダ含む）からテクスチャパスを検索
    string FindTexturePathInFolder(string baseFolder, IEnumerable<string> candidateNames)
    {
        string searchFolder = baseFolder.TrimEnd('/', '\\');
        if (string.IsNullOrEmpty(searchFolder))
        {
            return null;
        }

        string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { searchFolder });
        foreach (string guid in textureGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            string assetName = Path.GetFileNameWithoutExtension(assetPath);

            if (candidateNames.Any(name => string.Equals(name, assetName, StringComparison.OrdinalIgnoreCase)))
            {
                return assetPath;
            }
        }

        return null;
    }

    // リンク結果を保持する構造体
    private struct LinkResult
    {
        public bool success;
        public string message;

        public LinkResult(bool success, string message)
        {
            this.success = success;
            this.message = message;
        }
    }

    // デバッグ用：リンク状態をチェック
    [MenuItem("Tools/Yume Roof/Check Asset Links")]
    public static void CheckAssetLinks()
    {
        string[] furnitureGuids = AssetDatabase.FindAssets("t:FurnitureDataSO");
        int linkedPrefabs = 0;
        int linkedIcons = 0;
        int total = furnitureGuids.Length;

        foreach (string guid in furnitureGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            FurnitureDataSO data = AssetDatabase.LoadAssetAtPath<FurnitureDataSO>(path);

            if (data.prefab != null) linkedPrefabs++;
            if (data.icon != null) linkedIcons++;
        }

        Debug.Log($"=== アセットリンク状態 ===");
        Debug.Log($"家具総数: {total}");
        Debug.Log($"Prefabリンク済み: {linkedPrefabs}/{total}");
        Debug.Log($"アイコンリンク済み: {linkedIcons}/{total}");
    }
}
