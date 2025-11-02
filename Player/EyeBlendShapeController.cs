using UnityEngine;

/// <summary>
/// Controls left and right eye blend shapes on a skinned mesh renderer using normalized slider values.
/// </summary>
public class EyeBlendShapeController : MonoBehaviour
{
    [Header("Renderer")]
    [SerializeField]
    private SkinnedMeshRenderer skinnedMeshRenderer;

    [Header("Blend Shape Names")]
    [SerializeField]
    private string rightEyeBlendShapeName = "Right";
    [SerializeField]
    private string leftEyeBlendShapeName = "Left";

    private int rightEyeBlendShapeIndex = -1;
    private int leftEyeBlendShapeIndex = -1;

    private void Awake()
    {
        CacheBlendShapeIndices();
    }

    private void OnValidate()
    {
        CacheBlendShapeIndices();
    }

    /// <summary>
    /// Sets the right eye blend shape weight using a normalized value between 0 and 1.
    /// </summary>
    public void SetRightEye(float normalized)
    {
        SetBlendShapeWeight(rightEyeBlendShapeIndex, normalized);
    }

    /// <summary>
    /// Sets the left eye blend shape weight using a normalized value between 0 and 1.
    /// </summary>
    public void SetLeftEye(float normalized)
    {
        SetBlendShapeWeight(leftEyeBlendShapeIndex, normalized);
    }

    /// <summary>
    /// Resets both eye blend shapes to zero weight.
    /// </summary>
    public void ResetEyes()
    {
        SetRightEye(0f);
        SetLeftEye(0f);
    }

    private void SetBlendShapeWeight(int index, float normalized)
    {
        if (skinnedMeshRenderer == null || index == -1)
        {
            return;
        }

        float weight = Mathf.Clamp01(normalized) * 100f;
        skinnedMeshRenderer.SetBlendShapeWeight(index, weight);
    }

    private void CacheBlendShapeIndices()
    {
        if (skinnedMeshRenderer == null)
        {
            rightEyeBlendShapeIndex = -1;
            leftEyeBlendShapeIndex = -1;
            Debug.LogWarning($"{nameof(EyeBlendShapeController)} on '{name}' requires a reference to a {nameof(SkinnedMeshRenderer)}.");
            return;
        }

        Mesh mesh = skinnedMeshRenderer.sharedMesh;
        if (mesh == null)
        {
            rightEyeBlendShapeIndex = -1;
            leftEyeBlendShapeIndex = -1;
            Debug.LogWarning($"{nameof(EyeBlendShapeController)} on '{name}' cannot cache blend shapes because the renderer has no mesh.");
            return;
        }

        rightEyeBlendShapeIndex = GetBlendShapeIndex(mesh, rightEyeBlendShapeName);
        leftEyeBlendShapeIndex = GetBlendShapeIndex(mesh, leftEyeBlendShapeName);
    }

    private int GetBlendShapeIndex(Mesh mesh, string blendShapeName)
    {
        if (string.IsNullOrEmpty(blendShapeName))
        {
            Debug.LogWarning($"{nameof(EyeBlendShapeController)} on '{name}' requires a non-empty blend shape name.");
            return -1;
        }

        int index = mesh.GetBlendShapeIndex(blendShapeName);
        if (index == -1)
        {
            Debug.LogWarning($"Blend shape '{blendShapeName}' was not found on mesh '{mesh.name}' for {nameof(EyeBlendShapeController)} on '{name}'.");
        }

        return index;
    }
}
