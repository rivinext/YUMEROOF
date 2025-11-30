using UnityEngine;

public class PlacementPreview : MonoBehaviour
{
    private const string FurnitureLayerName = "Furniture";
    private const string PlacementBlockerLayerName = "PlacementBlocker";

    public Vector3 boxSize = new Vector3(1.2f, 1.2f, 1.2f);
    public LayerMask collisionMask;

    public Material okMaterial;
    public Material ngMaterial;

    [Header("Marker Prefabs")]
    [Tooltip("Marker prefab used when previewing placement on floors or ceilings. The prefab must include a SpriteRenderer so existing visibility and scale animation logic applies.")]
    public GameObject floorOrCeilingMarkerPrefab;

    [Tooltip("Marker prefab used when previewing placement on walls. The prefab must include a SpriteRenderer so existing visibility and scale animation logic applies.")]
    public GameObject wallMarkerPrefab;

    [Tooltip("Fallback sprite used when no marker prefab is assigned. Also used to initialize SpriteRenderer components on marker prefabs when they do not have a sprite set.")]
    public Sprite cornerSprite;
    public Vector3 cornerSpriteOffset = new Vector3(0f, 0.05f, 0f);
    public float cornerSpriteScale = 0.25f;

    public enum PlacementMode
    {
        FloorOrCeiling,
        Wall
    }

    [Header("Placement Mode")]
    [Tooltip("Determines which marker prefab and rotation rules are applied. FloorOrCeiling rotates around the Y axis, Wall rotates around the Z axis.")]
    public PlacementMode placementMode = PlacementMode.FloorOrCeiling;

    [Header("Corner Rotation Offsets")]
    [Tooltip("Clockwise Y-axis offsets for floor or ceiling placement (starting from bottom-left corner).")]
    public float[] floorOrCeilingCornerYRotations = new float[4] { 270f, 0f, 90f, 180f };

    [Tooltip("Clockwise Z-axis offsets for wall placement (starting from bottom-left corner).")]
    public float[] wallCornerZRotations = new float[4] { 0f, 90f, 180f, 270f };

    // スケールのアニメーション用変数
    [Header("Scale Animation")]
    public float minScale = 0.25f;
    public float maxScale = 0.4f;
    public float period = 1.5f;

    private SpriteRenderer[] cornerRenderers = new SpriteRenderer[4];
    private Vector3[] cornerBaseScales = new Vector3[4];
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
                if (sr != null)
                {
                    int index = System.Array.IndexOf(cornerRenderers, sr);
                    Vector3 baseScale = (index >= 0 && index < cornerBaseScales.Length && cornerBaseScales[index] != Vector3.zero)
                        ? cornerBaseScales[index]
                        : Vector3.one;
                    sr.transform.localScale = baseScale * scale;
                }
            }
        }
        else
        {
            // 非選択状態の場合は元のスケールに戻す
            foreach (var sr in cornerRenderers)
            {
                if (sr != null)
                {
                    int index = System.Array.IndexOf(cornerRenderers, sr);
                    Vector3 baseScale = (index >= 0 && index < cornerBaseScales.Length && cornerBaseScales[index] != Vector3.zero)
                        ? cornerBaseScales[index]
                        : Vector3.one;
                    sr.transform.localScale = baseScale * cornerSpriteScale;
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
        for (int i = 0; i < rotations.Length; i++)
        {
            rotations[i] = GetCornerRotation(i);
        }

        for (int i = 0; i < 4; i++)
        {
            GameObject cornerObj = CreateCornerObject(i);
            cornerObj.transform.localPosition = localPositions[i];
            cornerObj.transform.localRotation = rotations[i];
            cornerObj.transform.localScale = Vector3.one;

            SpriteRenderer sr = GetOrCreateSpriteRenderer(cornerObj);
            if (sr.sprite == null && cornerSprite != null)
            {
                sr.sprite = cornerSprite;
            }

            sr.sortingOrder = 100;

            cornerBaseScales[i] = sr.transform.localScale == Vector3.zero ? Vector3.one : sr.transform.localScale;
            sr.transform.localScale = cornerBaseScales[i] * cornerSpriteScale;

            cornerRenderers[i] = sr;
        }
    }

    private Quaternion GetCornerRotation(int index)
    {
        if (placementMode == PlacementMode.Wall)
        {
            float zRotation = (wallCornerZRotations != null && wallCornerZRotations.Length > index)
                ? wallCornerZRotations[index]
                : 0f;
            return Quaternion.Euler(0f, 0f, zRotation);
        }
        else
        {
            float yRotation = (floorOrCeilingCornerYRotations != null && floorOrCeilingCornerYRotations.Length > index)
                ? floorOrCeilingCornerYRotations[index]
                : 0f;
            return Quaternion.Euler(90f, yRotation, 0f);
        }
    }

    private GameObject CreateCornerObject(int index)
    {
        GameObject selectedPrefab = placementMode == PlacementMode.Wall ? wallMarkerPrefab : floorOrCeilingMarkerPrefab;

        if (selectedPrefab != null)
        {
            GameObject instance = Instantiate(selectedPrefab, transform);
            instance.name = $"{selectedPrefab.name}_Corner_{index}";
            return instance;
        }

        GameObject cornerObj = new GameObject("CornerSprite_" + index);
        cornerObj.transform.SetParent(transform);
        return cornerObj;
    }

    private SpriteRenderer GetOrCreateSpriteRenderer(GameObject cornerObj)
    {
        SpriteRenderer sr = cornerObj.GetComponentInChildren<SpriteRenderer>();

        if (sr == null)
        {
            sr = cornerObj.AddComponent<SpriteRenderer>();
        }

        return sr;
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
                if (Application.isPlaying)
                {
                    Destroy(cornerRenderers[i].gameObject);
                }
                else
                {
                    DestroyImmediate(cornerRenderers[i].gameObject);
                }

                cornerRenderers[i] = null;
            }
        }

        cornerRenderers = new SpriteRenderer[4];
        cornerBaseScales = new Vector3[4];
        CreateCornerSprites();
        UpdateCornerVisibility();
    }
}
