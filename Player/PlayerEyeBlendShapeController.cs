using UnityEngine;

/// <summary>
/// Controls manual eye blend shape weights during emote playback.
/// </summary>
public class PlayerEyeBlendShapeController : MonoBehaviour
{
    [Header("Blend Shape Source")]
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;

    [Header("Left Eye")]
    [SerializeField] private string leftEyeBlendShapeName;
    [SerializeField] private int leftEyeBlendShapeIndex = -1;

    [Header("Right Eye")]
    [SerializeField] private string rightEyeBlendShapeName;
    [SerializeField] private int rightEyeBlendShapeIndex = -1;

    private int resolvedLeftIndex = -1;
    private int resolvedRightIndex = -1;
    private bool manualControlActive;
    private float cachedLeftWeight;
    private float cachedRightWeight;

    public float LeftEyeWeight => cachedLeftWeight;
    public float RightEyeWeight => cachedRightWeight;

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
            ApplyLeftWeight(cachedLeftWeight);
            ApplyRightWeight(cachedRightWeight);
        }
        else
        {
            ApplyLeftWeight(0f);
            ApplyRightWeight(0f);
        }
    }
#endif

    /// <summary>
    /// Enables or disables manual control of the eye blend shapes.
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
            cachedLeftWeight = 0f;
            cachedRightWeight = 0f;
            ApplyLeftWeight(0f);
            ApplyRightWeight(0f);
        }
        else
        {
            ApplyLeftWeight(cachedLeftWeight);
            ApplyRightWeight(cachedRightWeight);
        }
    }

    /// <summary>
    /// Sets the left eye blend shape weight while manual control is active.
    /// </summary>
    public void SetLeftEyeWeight(float weight)
    {
        cachedLeftWeight = Mathf.Clamp(weight, 0f, 100f);
        if (!manualControlActive)
        {
            return;
        }

        ApplyLeftWeight(cachedLeftWeight);
    }

    /// <summary>
    /// Sets the right eye blend shape weight while manual control is active.
    /// </summary>
    public void SetRightEyeWeight(float weight)
    {
        cachedRightWeight = Mathf.Clamp(weight, 0f, 100f);
        if (!manualControlActive)
        {
            return;
        }

        ApplyRightWeight(cachedRightWeight);
    }

    private void ResolveBlendShapeIndices()
    {
        if (skinnedMeshRenderer == null)
        {
            resolvedLeftIndex = -1;
            resolvedRightIndex = -1;
            return;
        }

        Mesh sharedMesh = skinnedMeshRenderer.sharedMesh;
        if (sharedMesh == null)
        {
            resolvedLeftIndex = -1;
            resolvedRightIndex = -1;
            return;
        }

        resolvedLeftIndex = ResolveIndex(sharedMesh, leftEyeBlendShapeName, leftEyeBlendShapeIndex);
        resolvedRightIndex = ResolveIndex(sharedMesh, rightEyeBlendShapeName, rightEyeBlendShapeIndex);
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

    private void ApplyLeftWeight(float weight)
    {
        EnsureIndicesResolved();
        if (skinnedMeshRenderer == null)
        {
            return;
        }
        if (resolvedLeftIndex == -1)
        {
            return;
        }

        skinnedMeshRenderer.SetBlendShapeWeight(resolvedLeftIndex, Mathf.Clamp(weight, 0f, 100f));
    }

    private void ApplyRightWeight(float weight)
    {
        EnsureIndicesResolved();
        if (skinnedMeshRenderer == null)
        {
            return;
        }
        if (resolvedRightIndex == -1)
        {
            return;
        }

        skinnedMeshRenderer.SetBlendShapeWeight(resolvedRightIndex, Mathf.Clamp(weight, 0f, 100f));
    }

    private void EnsureIndicesResolved()
    {
        if (resolvedLeftIndex == -1 || resolvedRightIndex == -1)
        {
            ResolveBlendShapeIndices();
        }
    }
}
