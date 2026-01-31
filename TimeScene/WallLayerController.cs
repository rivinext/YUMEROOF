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
    }

    public Wall[] walls;
    public Camera cam;
    public string invisibleLayerName = "InvisibleWall";
    [SerializeField]
    private bool visibilityControlEnabled = true;
    private bool initialized;

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
                EnsureInitialized();
                RestoreOriginalSettings();
            }
        }
    }

    private void Awake()
    {
        CacheOriginalSettings();
    }

    private void CacheOriginalSettings()
    {
        if (initialized)
        {
            return;
        }

        // 元レイヤーを保存
        foreach (var wall in walls)
        {
            if (wall?.renderer == null)
            {
                continue;
            }

            wall.originalLayer = wall.renderer.gameObject.layer;
            wall.originalShadowCastingMode = wall.renderer.shadowCastingMode;

            var deactivationHandler = wall.renderer.GetComponent<WallDeactivationHandler>();
            if (deactivationHandler == null)
            {
                deactivationHandler = wall.renderer.gameObject.AddComponent<WallDeactivationHandler>();
            }
            deactivationHandler.SetWallTransform(wall.renderer.transform);
        }

        initialized = true;
    }

    private void EnsureInitialized()
    {
        if (!initialized)
        {
            CacheOriginalSettings();
        }
    }

    void Update()
    {
        if (!visibilityControlEnabled)
        {
            EnsureInitialized();
            RestoreOriginalSettings();
            return;
        }

        EnsureInitialized();
        float camY = cam.transform.eulerAngles.y;

        foreach (var wall in walls)
        {
            if (wall?.renderer == null)
            {
                continue;
            }

            // ID→角度変換 (1=0,2=90,3=180,4=270)
            float wallAngle = (wall.id - 1) * 90f;
            float diff = Mathf.DeltaAngle(camY, wallAngle);

            bool isFront = Mathf.Abs(diff) < 90f; // 前面か判定

            if (isFront)
            {
                wall.renderer.gameObject.layer = LayerMask.NameToLayer(invisibleLayerName);
                wall.renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }
            else
            {
                wall.renderer.gameObject.layer = wall.originalLayer;
                wall.renderer.shadowCastingMode = wall.originalShadowCastingMode;
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
            if (wall?.renderer == null)
            {
                continue;
            }

            wall.renderer.gameObject.layer = wall.originalLayer;
            wall.renderer.shadowCastingMode = wall.originalShadowCastingMode;
        }
    }
}
