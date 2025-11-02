using UnityEngine;

/// <summary>
/// Controls manual mouth blend shape weights during emote playback.
/// </summary>
public class PlayerMouthBlendShapeController : MonoBehaviour
{
    [Header("Blend Shape Source")]
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;

    [Header("Vertical Movement")]
    [SerializeField] private string verticalBlendShapeName;
    [SerializeField] private int verticalBlendShapeIndex = -1;

    [Header("Horizontal Movement")]
    [SerializeField] private string horizontalBlendShapeName;
    [SerializeField] private int horizontalBlendShapeIndex = -1;

    private int resolvedVerticalIndex = -1;
    private int resolvedHorizontalIndex = -1;
    private bool manualControlActive;
    private float cachedVerticalWeight;
    private float cachedHorizontalWeight;

    public float VerticalWeight => cachedVerticalWeight;
    public float HorizontalWeight => cachedHorizontalWeight;

    private void Awake()
    {
        if (skinnedMeshRenderer == null)
        {
            skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        }

        ResolveBlendShapeIndices();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (skinnedMeshRenderer == null)
        {
            skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        }

        ResolveBlendShapeIndices();

        if (manualControlActive)
        {
            ApplyVerticalWeight(cachedVerticalWeight);
            ApplyHorizontalWeight(cachedHorizontalWeight);
        }
        else
        {
            ApplyVerticalWeight(0f);
            ApplyHorizontalWeight(0f);
        }
    }
#endif

    /// <summary>
    /// Enables or disables manual control of the mouth blend shapes.
    /// </summary>
    public void SetManualControlActive(bool active)
    {
        manualControlActive = active;

        if (manualControlActive)
        {
            ResolveBlendShapeIndices();
        }

        if (!manualControlActive)
        {
            cachedVerticalWeight = 0f;
            cachedHorizontalWeight = 0f;
            ApplyVerticalWeight(0f);
            ApplyHorizontalWeight(0f);
        }
        else
        {
            ApplyVerticalWeight(cachedVerticalWeight);
            ApplyHorizontalWeight(cachedHorizontalWeight);
        }
    }

    /// <summary>
    /// Sets the vertical mouth blend shape weight while manual control is active.
    /// </summary>
    public void SetVerticalWeight(float weight)
    {
        cachedVerticalWeight = Mathf.Clamp(weight, 0f, 100f);
        if (!manualControlActive)
        {
            return;
        }

        ApplyVerticalWeight(cachedVerticalWeight);
    }

    /// <summary>
    /// Sets the horizontal mouth blend shape weight while manual control is active.
    /// </summary>
    public void SetHorizontalWeight(float weight)
    {
        cachedHorizontalWeight = Mathf.Clamp(weight, 0f, 100f);
        if (!manualControlActive)
        {
            return;
        }

        ApplyHorizontalWeight(cachedHorizontalWeight);
    }

    private void ResolveBlendShapeIndices()
    {
        if (skinnedMeshRenderer == null)
        {
            resolvedVerticalIndex = -1;
            resolvedHorizontalIndex = -1;
            return;
        }

        Mesh sharedMesh = skinnedMeshRenderer.sharedMesh;
        if (sharedMesh == null)
        {
            resolvedVerticalIndex = -1;
            resolvedHorizontalIndex = -1;
            return;
        }

        resolvedVerticalIndex = ResolveIndex(sharedMesh, verticalBlendShapeName, verticalBlendShapeIndex);
        resolvedHorizontalIndex = ResolveIndex(sharedMesh, horizontalBlendShapeName, horizontalBlendShapeIndex);
    }

    private int ResolveIndex(Mesh mesh, string blendShapeName, int fallbackIndex)
    {
        if (mesh == null)
        {
            return -1;
        }

        if (!string.IsNullOrEmpty(blendShapeName))
        {
            int nameIndex = mesh.GetBlendShapeIndex(blendShapeName);
            if (nameIndex != -1)
            {
                return nameIndex;
            }
        }

        if (fallbackIndex >= 0 && fallbackIndex < mesh.blendShapeCount)
        {
            return fallbackIndex;
        }

        return -1;
    }

    private void ApplyVerticalWeight(float weight)
    {
        EnsureIndicesResolved();
        if (skinnedMeshRenderer == null)
        {
            return;
        }
        if (resolvedVerticalIndex == -1)
        {
            return;
        }

        skinnedMeshRenderer.SetBlendShapeWeight(resolvedVerticalIndex, Mathf.Clamp(weight, 0f, 100f));
    }

    private void ApplyHorizontalWeight(float weight)
    {
        EnsureIndicesResolved();
        if (skinnedMeshRenderer == null)
        {
            return;
        }
        if (resolvedHorizontalIndex == -1)
        {
            return;
        }

        skinnedMeshRenderer.SetBlendShapeWeight(resolvedHorizontalIndex, Mathf.Clamp(weight, 0f, 100f));
    }

    private void EnsureIndicesResolved()
    {
        if (resolvedVerticalIndex == -1 || resolvedHorizontalIndex == -1)
        {
            ResolveBlendShapeIndices();
        }
    }
}
