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
        [HideInInspector] public Material fadeMaterial;
        [HideInInspector] public Color originalColor;
        [HideInInspector] public float currentAlpha;
        [HideInInspector] public float targetAlpha;
        [HideInInspector] public bool layerSetToInvisible;
        [HideInInspector] public bool hasModeProperty;
        [HideInInspector] public bool hasBlendStateProperties;
        [HideInInspector] public float originalMode;
        [HideInInspector] public BlendMode originalSrcBlend;
        [HideInInspector] public BlendMode originalDstBlend;
        [HideInInspector] public bool originalZWrite;
        [HideInInspector] public string originalRenderType;
        [HideInInspector] public int originalRenderQueue;
        [HideInInspector] public bool originalAlphaTestKeyword;
        [HideInInspector] public bool originalAlphaBlendKeyword;
        [HideInInspector] public bool originalAlphaPremultiplyKeyword;
    }

    public Wall[] walls;
    public Camera cam;
    public string invisibleLayerName = "InvisibleWall";
    public float fadeSpeed = 4f;

    private int invisibleLayer;

    void Start()
    {
        invisibleLayer = LayerMask.NameToLayer(invisibleLayerName);

        // 元レイヤーを保存
        foreach (var wall in walls)
        {
            if (wall == null || wall.renderer == null)
            {
                continue;
            }

            wall.originalLayer = wall.renderer.gameObject.layer;
            wall.originalShadowCastingMode = wall.renderer.shadowCastingMode;

            var materialInstance = new Material(wall.renderer.sharedMaterial);
            wall.renderer.material = materialInstance;
            wall.fadeMaterial = materialInstance;

            wall.originalColor = materialInstance.color;
            wall.currentAlpha = wall.originalColor.a;
            wall.targetAlpha = wall.currentAlpha;

            wall.hasModeProperty = materialInstance.HasProperty("_Mode");
            if (wall.hasModeProperty)
            {
                wall.originalMode = materialInstance.GetFloat("_Mode");
            }

            wall.hasBlendStateProperties = materialInstance.HasProperty("_SrcBlend") && materialInstance.HasProperty("_DstBlend") && materialInstance.HasProperty("_ZWrite");
            if (wall.hasBlendStateProperties)
            {
                wall.originalSrcBlend = (BlendMode)materialInstance.GetInt("_SrcBlend");
                wall.originalDstBlend = (BlendMode)materialInstance.GetInt("_DstBlend");
                wall.originalZWrite = materialInstance.GetInt("_ZWrite") == 1;
            }
            wall.originalRenderType = materialInstance.GetTag("RenderType", false);
            wall.originalRenderQueue = materialInstance.renderQueue;
            wall.originalAlphaTestKeyword = materialInstance.IsKeywordEnabled("_ALPHATEST_ON");
            wall.originalAlphaBlendKeyword = materialInstance.IsKeywordEnabled("_ALPHABLEND_ON");
            wall.originalAlphaPremultiplyKeyword = materialInstance.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON");
        }
    }

    void Update()
    {
        float camY = cam.transform.eulerAngles.y;

        foreach (var wall in walls)
        {
            if (wall == null || wall.renderer == null)
            {
                continue;
            }

            // ID→角度変換 (1=0,2=90,3=180,4=270)
            float wallAngle = (wall.id - 1) * 90f;
            float diff = Mathf.DeltaAngle(camY, wallAngle);

            bool isFront = Mathf.Abs(diff) < 90f; // 前面か判定

            if (isFront)
            {
                if (!Mathf.Approximately(wall.targetAlpha, 0f))
                {
                    BeginFadeOut(wall);
                }
            }
            else
            {
                if (!Mathf.Approximately(wall.targetAlpha, wall.originalColor.a))
                {
                    BeginFadeIn(wall);
                }
            }

            UpdateFade(wall);
        }
    }

    private void BeginFadeOut(Wall wall)
    {
        if (wall.fadeMaterial == null)
        {
            return;
        }

        SetMaterialTransparent(wall);
        wall.targetAlpha = 0f;
    }

    private void BeginFadeIn(Wall wall)
    {
        if (wall.fadeMaterial == null)
        {
            return;
        }

        wall.layerSetToInvisible = false;
        wall.renderer.gameObject.layer = wall.originalLayer;
        wall.renderer.shadowCastingMode = wall.originalShadowCastingMode;
        SetMaterialTransparent(wall);
        wall.targetAlpha = wall.originalColor.a;
    }

    private void UpdateFade(Wall wall)
    {
        if (wall.fadeMaterial == null)
        {
            return;
        }

        if (Mathf.Approximately(wall.currentAlpha, wall.targetAlpha))
        {
            return;
        }

        float nextAlpha = Mathf.MoveTowards(wall.currentAlpha, wall.targetAlpha, fadeSpeed * Time.deltaTime);
        var color = wall.fadeMaterial.color;
        color.a = nextAlpha;
        wall.fadeMaterial.color = color;
        wall.currentAlpha = nextAlpha;

        if (Mathf.Approximately(nextAlpha, 0f) && Mathf.Approximately(wall.targetAlpha, 0f) && !wall.layerSetToInvisible)
        {
            if (invisibleLayer != -1)
            {
                wall.renderer.gameObject.layer = invisibleLayer;
            }
            wall.renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            wall.layerSetToInvisible = true;
        }

        if (Mathf.Approximately(nextAlpha, wall.originalColor.a) && Mathf.Approximately(wall.targetAlpha, wall.originalColor.a))
        {
            RestoreOriginalMaterialSettings(wall);
        }
    }

    private void SetMaterialTransparent(Wall wall)
    {
        var mat = wall.fadeMaterial;
        if (mat == null)
        {
            return;
        }

        if (wall.hasModeProperty)
        {
            mat.SetFloat("_Mode", 3f);
        }

        mat.SetOverrideTag("RenderType", "Transparent");
        if (wall.hasBlendStateProperties)
        {
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
        }
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = (int)RenderQueue.Transparent;
    }

    private void RestoreOriginalMaterialSettings(Wall wall)
    {
        var mat = wall.fadeMaterial;
        if (mat == null)
        {
            return;
        }

        if (wall.hasModeProperty)
        {
            mat.SetFloat("_Mode", wall.originalMode);
        }

        mat.SetOverrideTag("RenderType", wall.originalRenderType);
        if (wall.hasBlendStateProperties)
        {
            mat.SetInt("_SrcBlend", (int)wall.originalSrcBlend);
            mat.SetInt("_DstBlend", (int)wall.originalDstBlend);
            mat.SetInt("_ZWrite", wall.originalZWrite ? 1 : 0);
        }

        if (wall.originalAlphaTestKeyword)
        {
            mat.EnableKeyword("_ALPHATEST_ON");
        }
        else
        {
            mat.DisableKeyword("_ALPHATEST_ON");
        }

        if (wall.originalAlphaBlendKeyword)
        {
            mat.EnableKeyword("_ALPHABLEND_ON");
        }
        else
        {
            mat.DisableKeyword("_ALPHABLEND_ON");
        }

        if (wall.originalAlphaPremultiplyKeyword)
        {
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        }
        else
        {
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        }

        mat.renderQueue = wall.originalRenderQueue;
        mat.color = wall.originalColor;
        wall.currentAlpha = wall.originalColor.a;
        wall.targetAlpha = wall.originalColor.a;
        wall.layerSetToInvisible = false;
    }
}
