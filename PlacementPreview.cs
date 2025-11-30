using UnityEngine;

public class PlacementPreview : MonoBehaviour
{
    private const string FurnitureLayerName = "Furniture";
    private const string PlacementBlockerLayerName = "PlacementBlocker";

    public Vector3 boxSize = new Vector3(1.2f, 1.2f, 1.2f);
    public LayerMask collisionMask;

    public Material okMaterial;
    public Material ngMaterial;

    public Sprite cornerSprite;
    public Vector3 cornerSpriteOffset = new Vector3(0f, 0.05f, 0f);
    public float cornerSpriteScale = 0.25f;

    // スケールのアニメーション用変数
    [Header("Scale Animation")]
    public float minScale = 0.25f;
    public float maxScale = 0.4f;
    public float period = 1.5f;

    private SpriteRenderer[] cornerRenderers = new SpriteRenderer[4];
    private bool isSelected = false;
    private Color originalColor;
    private MeshRenderer meshRenderer;

    private void Reset()
    {
        EnsureCollisionMaskIncludesRequiredLayers();
    }

    private void OnValidate()
    {
        EnsureCollisionMaskIncludesRequiredLayers();
    }

    void Start()
    {
        EnsureCollisionMaskIncludesRequiredLayers();
        CreateCornerSprites();
        UpdateCornerVisibility();

        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            originalColor = meshRenderer.material.color;
        }
    }

    void Update()
    {
        // 選択されている場合のみ collision check と色の更新を行う
        if (isSelected)
        {
            bool isOverlapping = Physics.CheckBox(
                transform.position,
                boxSize / 2,
                transform.rotation,
                collisionMask
            );

            foreach (var sr in cornerRenderers)
            {
                sr.color = isOverlapping ? Color.red : Color.white;
            }

            // スケールを周期的に変更
            float scale = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(Time.time * (2 * Mathf.PI / period)) + 1) / 2);
            foreach (var sr in cornerRenderers)
            {
                sr.transform.localScale = Vector3.one * scale;
            }
        }
        else
        {
            // 非選択状態の場合は元のスケールに戻す
            foreach (var sr in cornerRenderers)
            {
                if (sr != null)
                {
                    sr.transform.localScale = Vector3.one * cornerSpriteScale;
                }
            }
        }
    }

    private void EnsureCollisionMaskIncludesRequiredLayers()
    {
        collisionMask = AddLayerToMask(collisionMask, FurnitureLayerName);
        collisionMask = AddLayerToMask(collisionMask, PlacementBlockerLayerName);
    }

    private static LayerMask AddLayerToMask(LayerMask mask, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer >= 0)
        {
            int maskValue = mask;
            maskValue |= 1 << layer;
            mask = maskValue;
        }

        return mask;
    }

    void UpdateCornerVisibility()
    {
        foreach (var sr in cornerRenderers)
        {
            if (sr != null)
            {
                sr.enabled = isSelected;
            }
        }
    }

    void CreateCornerSprites()
    {
        // オブジェクトのRendererからバウンディングボックスを取得
        Renderer objectRenderer = GetComponent<Renderer>();
        Vector3 size;

        if (objectRenderer != null)
        {
            Bounds bounds = objectRenderer.bounds;
            size = bounds.size;
        }
        else
        {
            size = boxSize;
        }

        Vector3 half = size / 2f;
        Vector3[] localPositions = new Vector3[4];

        float bottomY = -half.y + cornerSpriteOffset.y;

        localPositions[0] = new Vector3(-half.x, bottomY, -half.z);
        localPositions[1] = new Vector3(-half.x, bottomY, half.z);
        localPositions[2] = new Vector3(half.x, bottomY, half.z);
        localPositions[3] = new Vector3(half.x, bottomY, -half.z);

        Quaternion[] rotations = new Quaternion[4];
        rotations[0] = Quaternion.Euler(90, 270, 0);
        rotations[1] = Quaternion.Euler(90, 0, 0);
        rotations[2] = Quaternion.Euler(90, 90, 0);
        rotations[3] = Quaternion.Euler(90, 180, 0);

        for (int i = 0; i < 4; i++)
        {
            GameObject cornerObj = new GameObject("CornerSprite_" + i);
            cornerObj.transform.SetParent(transform);
            cornerObj.transform.localPosition = localPositions[i];
            cornerObj.transform.localRotation = rotations[i];
            cornerObj.transform.localScale = Vector3.one * cornerSpriteScale;

            SpriteRenderer sr = cornerObj.AddComponent<SpriteRenderer>();
            sr.sprite = cornerSprite;
            sr.sortingOrder = 100;
            cornerRenderers[i] = sr;
        }
    }

    // パブリックメソッドで外部から選択状態を制御できるようにする
    public void SetSelected(bool selected)
    {
        if (isSelected == selected) return;

        isSelected = selected;
        UpdateCornerVisibility();

        // オブジェクト本体の透明度を切り替える
        if (meshRenderer != null)
        {
            if (isSelected)
            {
                Color newColor = originalColor;
                newColor.a = 0.5f; // 半透明
                meshRenderer.material.color = newColor;
            }
            else
            {
                meshRenderer.material.color = originalColor;
            }
        }
    }

    // 選択状態を取得するメソッド
    public bool IsSelected()
    {
        return isSelected;
    }

    // corner spriteの位置を再計算・更新するメソッド
    public void UpdateCornerPositions()
    {
        for (int i = 0; i < cornerRenderers.Length; i++)
        {
            if (cornerRenderers[i] != null)
            {
                DestroyImmediate(cornerRenderers[i].gameObject);
            }
        }

        CreateCornerSprites();
        UpdateCornerVisibility();
    }
}
