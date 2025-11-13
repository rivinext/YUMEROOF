using UnityEngine;
using UnityEngine.Rendering;

public class CeilingLayerController : MonoBehaviour
{
    [System.Serializable]
    public class Ceiling
    {
        public MeshRenderer renderer;
        public Vector3 normal = Vector3.down;

        [HideInInspector] public int originalLayer;
        [HideInInspector] public ShadowCastingMode originalShadowCastingMode;
    }

    public Ceiling[] ceilings;
    public Camera cam;
    public string invisibleLayerName = "InvisibleCeiling";
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

    private void Start()
    {
        foreach (var ceiling in ceilings)
        {
            if (ceiling?.renderer == null)
            {
                continue;
            }

            ceiling.originalLayer = ceiling.renderer.gameObject.layer;
            ceiling.originalShadowCastingMode = ceiling.renderer.shadowCastingMode;
        }
    }

    private void Update()
    {
        if (!visibilityControlEnabled)
        {
            RestoreOriginalSettings();
            return;
        }

        if (cam == null)
        {
            return;
        }

        int invisibleLayer = LayerMask.NameToLayer(invisibleLayerName);

        foreach (var ceiling in ceilings)
        {
            if (ceiling?.renderer == null)
            {
                continue;
            }

            Vector3 worldNormal = ceiling.renderer.transform.TransformDirection(ceiling.normal).normalized;
            Vector3 toCamera = cam.transform.position - ceiling.renderer.bounds.center;

            bool isOnNormalSide = Vector3.Dot(toCamera.normalized, worldNormal) > 0f;

            if (isOnNormalSide && invisibleLayer != -1)
            {
                ceiling.renderer.gameObject.layer = invisibleLayer;
                ceiling.renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }
            else
            {
                RestoreOriginalSettings(ceiling);
            }
        }
    }

    public void ToggleVisibilityControl()
    {
        VisibilityControlEnabled = !VisibilityControlEnabled;
    }

    private void RestoreOriginalSettings()
    {
        foreach (var ceiling in ceilings)
        {
            RestoreOriginalSettings(ceiling);
        }
    }

    private void RestoreOriginalSettings(Ceiling ceiling)
    {
        if (ceiling?.renderer == null)
        {
            return;
        }

        ceiling.renderer.gameObject.layer = ceiling.originalLayer;
        ceiling.renderer.shadowCastingMode = ceiling.originalShadowCastingMode;
    }
}
