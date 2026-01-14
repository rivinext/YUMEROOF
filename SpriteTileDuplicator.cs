using UnityEngine;

/// <summary>
/// 指定したスプライトを上下左右にタイル状に複製するコンポーネント。
/// </summary>
[ExecuteAlways]
public class SpriteTileDuplicator : MonoBehaviour
{
    [Header("画像設定")]
    [Tooltip("タイル化に使用するスプライトをアタッチしてください。")]
    [SerializeField] private Sprite sprite;

    [Tooltip("このGameObjectのSpriteRendererにも同じスプライトを設定する")]
    [SerializeField] private bool applyToSelfRenderer = true;

    [Header("タイル数")]
    [Tooltip("左右方向に複製する枚数（中心を含まない片側の枚数）")]
    [Min(0)]
    [SerializeField] private int tilesX = 1;

    [Tooltip("上下方向に複製する枚数（中心を含まない片側の枚数）")]
    [Min(0)]
    [SerializeField] private int tilesY = 1;

    [Header("生成先")]
    [Tooltip("タイルを生成する親Transform（空なら自動生成）")]
    [SerializeField] private Transform tileRoot;

    private void Reset()
    {
        var renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            sprite = renderer.sprite;
        }
    }

    private void Start()
    {
        RebuildTiles();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            RebuildTiles();
        }
    }

    /// <summary>
    /// タイルを再生成します。
    /// </summary>
    public void RebuildTiles()
    {
        if (sprite == null)
        {
            return;
        }

        EnsureTileRoot();
        ClearTiles();

        if (applyToSelfRenderer)
        {
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.sprite = sprite;
            }
        }

        Vector2 size = sprite.bounds.size;
        for (int y = -tilesY; y <= tilesY; y++)
        {
            for (int x = -tilesX; x <= tilesX; x++)
            {
                if (x == 0 && y == 0)
                {
                    continue;
                }

                CreateTile(new Vector3(size.x * x, size.y * y, 0f));
            }
        }
    }

    private void EnsureTileRoot()
    {
        if (tileRoot != null)
        {
            return;
        }

        var rootObject = new GameObject("TileRoot");
        rootObject.transform.SetParent(transform, false);
        tileRoot = rootObject.transform;
    }

    private void ClearTiles()
    {
        if (tileRoot == null)
        {
            return;
        }

        for (int i = tileRoot.childCount - 1; i >= 0; i--)
        {
            var child = tileRoot.GetChild(i);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    private void CreateTile(Vector3 localPosition)
    {
        var tileObject = new GameObject("Tile");
        tileObject.transform.SetParent(tileRoot, false);
        tileObject.transform.localPosition = localPosition;

        var renderer = tileObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerID = GetSortingLayerID();
        renderer.sortingOrder = GetSortingOrder();
    }

    private int GetSortingLayerID()
    {
        var renderer = GetComponent<SpriteRenderer>();
        return renderer != null ? renderer.sortingLayerID : 0;
    }

    private int GetSortingOrder()
    {
        var renderer = GetComponent<SpriteRenderer>();
        return renderer != null ? renderer.sortingOrder : 0;
    }
}
