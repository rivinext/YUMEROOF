using UnityEngine;
using UnityEngine.Rendering;

public class WallLayerController : MonoBehaviour
{
    private const string PlayerPrefsKey = "wall_visibility_control_enabled";

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

    private static WallLayerController instance;

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

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        visibilityControlEnabled = LoadSavedVisibilityState();
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

    private bool LoadSavedVisibilityState()
    {
        if (PlayerPrefs.HasKey(PlayerPrefsKey))
        {
            return PlayerPrefs.GetInt(PlayerPrefsKey) == 1;
        }

        return visibilityControlEnabled;
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
        }
    }
}
