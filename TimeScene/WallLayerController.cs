using UnityEngine;
using UnityEngine.Rendering;

public class WallLayerController : MonoBehaviour
{
    [System.Serializable]
    public class Wall
    {
        public MeshRenderer renderer; // 対象の壁
        public int id;                // 1～4 (1=0°,2=90°,3=180°,4=270°)
        // 1:右前　2:左前　3:左後　4:右後
        [HideInInspector] public int originalLayer;
        [HideInInspector] public ShadowCastingMode originalShadowCastingMode;
        [HideInInspector] public bool isHidden;
    }

    public Wall[] walls;
    public Camera cam;
    public string invisibleLayerName = "InvisibleWall";
    [SerializeField]
    private bool visibilityControlEnabled = true;

    public bool VisibilityControlEnabled
    {
        get => visibilityControlEnabled;
        set
        {
            if (visibilityControlEnabled == value)
            {
                return;
            }

            visibilityControlEnabled = value;

            if (!visibilityControlEnabled)
            {
                RestoreOriginalSettings();
            }
        }
    }

    void Start()
    {
        // 元レイヤーを保存
        foreach (var wall in walls)
        {
            wall.originalLayer = wall.renderer.gameObject.layer;
            wall.originalShadowCastingMode = wall.renderer.shadowCastingMode;
        }
    }

    void Update()
    {
        if (!visibilityControlEnabled)
        {
            RestoreOriginalSettings();
            return;
        }

        float camY = cam.transform.eulerAngles.y;

        foreach (var wall in walls)
        {
            // ID→角度変換 (1=0,2=90,3=180,4=270)
            float wallAngle = (wall.id - 1) * 90f;
            float diff = Mathf.DeltaAngle(camY, wallAngle);

            bool isFront = Mathf.Abs(diff) < 90f; // 前面か判定

            if (isFront)
            {
                if (!wall.isHidden)
                {
                    StoreWallMountedFurniture(wall.renderer.transform);
                }

                wall.renderer.gameObject.layer = LayerMask.NameToLayer(invisibleLayerName);
                wall.renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                wall.isHidden = true;
            }
            else
            {
                wall.renderer.gameObject.layer = wall.originalLayer;
                wall.renderer.shadowCastingMode = wall.originalShadowCastingMode;
                wall.isHidden = false;
            }
        }
    }

    public void ToggleVisibilityControl()
    {
        VisibilityControlEnabled = !VisibilityControlEnabled;
    }

    private void RestoreOriginalSettings()
    {
        foreach (var wall in walls)
        {
            wall.renderer.gameObject.layer = wall.originalLayer;
            wall.renderer.shadowCastingMode = wall.originalShadowCastingMode;
            wall.isHidden = false;
        }
    }

    private void StoreWallMountedFurniture(Transform wallTransform)
    {
        if (wallTransform == null)
        {
            return;
        }

        var placedFurnitures = wallTransform.GetComponentsInChildren<PlacedFurniture>(true);
        foreach (var furniture in placedFurnitures)
        {
            if (furniture == null)
            {
                continue;
            }

            if (furniture.furnitureData == null)
            {
                continue;
            }

            if (furniture.furnitureData.placementRules != PlacementRule.Wall)
            {
                continue;
            }

            if (!furniture.transform.IsChildOf(wallTransform))
            {
                continue;
            }

            furniture.StoreToInventory();
        }
    }
}
