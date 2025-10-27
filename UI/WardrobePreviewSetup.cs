using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sets up the wardrobe preview scene by spawning a dedicated preview player and
/// wiring a camera, render texture, and UI output together.
/// </summary>
[DisallowMultipleComponent]
public class WardrobePreviewSetup : MonoBehaviour
{
    [Header("Preview Player")]
    [SerializeField] private GameObject previewPlayerPrefab;
    [SerializeField] private Transform previewParent;
    [SerializeField] private bool spawnOnAwake = true;
    [SerializeField] private Vector3 previewPlayerLocalPosition = Vector3.zero;
    [SerializeField] private Vector3 previewPlayerLocalEulerAngles = Vector3.zero;
    [SerializeField] private Vector3 previewPlayerLocalScale = Vector3.one;

    [Header("Preview Camera")]
    [SerializeField] private Camera previewCamera;

    [Header("Preview Output")]
    [SerializeField] private RawImage previewImage;
    [SerializeField] private RenderTexture renderTexture;
    [SerializeField] private Vector2Int renderTextureSize = new Vector2Int(512, 512);
    [SerializeField] private int renderTextureDepth = 16;
    [SerializeField] private RenderTextureFormat renderTextureFormat = RenderTextureFormat.ARGB32;
    [SerializeField] private FilterMode renderTextureFilterMode = FilterMode.Bilinear;
    [SerializeField] private bool renderTextureMipMaps = false;

    private GameObject previewPlayerInstance;
    private bool ownsRenderTexture;

    /// <summary>
    /// Gets the current preview player instance.
    /// </summary>
    public GameObject PreviewPlayerInstance => previewPlayerInstance;

    /// <summary>
    /// Gets the active camera used for rendering the preview.
    /// </summary>
    public Camera PreviewCamera => previewCamera;

    /// <summary>
    /// Gets the render texture that the preview camera outputs to.
    /// </summary>
    public RenderTexture PreviewTexture => renderTexture;

    /// <summary>
    /// Gets the UI image receiving the render texture.
    /// </summary>
    public RawImage PreviewImage => previewImage;

    private void Awake()
    {
        if (spawnOnAwake)
        {
            SpawnPreviewPlayer();
        }

        UpdatePreviewOutput();
    }

    private void OnEnable()
    {
        UpdatePreviewOutput();
    }

    private void OnDestroy()
    {
        ReleaseOwnedRenderTexture();
        CleanupPreviewPlayer();
    }

    private void OnValidate()
    {
        renderTextureSize = new Vector2Int(Mathf.Max(1, renderTextureSize.x), Mathf.Max(1, renderTextureSize.y));

        if (!Application.isPlaying)
        {
            return;
        }

        UpdatePreviewOutput();
    }

    /// <summary>
    /// Spawns a new preview player instance from the configured prefab.
    /// </summary>
    public GameObject SpawnPreviewPlayer()
    {
        if (previewPlayerPrefab == null)
        {
            Debug.LogWarning("Preview player prefab is not assigned.", this);
            return null;
        }

        CleanupPreviewPlayer();

        Transform targetParent = GetPreviewParent();
        previewPlayerInstance = Instantiate(previewPlayerPrefab, targetParent);
        AlignPreviewPlayerTransform(previewPlayerInstance.transform);
        return previewPlayerInstance;
    }

    /// <summary>
    /// Assigns an existing player instance to be used for the preview.
    /// </summary>
    public void SetPreviewPlayer(GameObject playerInstance, bool parentToPreviewRoot = true)
    {
        if (previewPlayerInstance == playerInstance)
        {
            return;
        }

        CleanupPreviewPlayer();
        previewPlayerInstance = playerInstance;

        if (previewPlayerInstance == null)
        {
            return;
        }

        if (parentToPreviewRoot)
        {
            Transform targetParent = GetPreviewParent();
            previewPlayerInstance.transform.SetParent(targetParent, worldPositionStays: false);
        }

        AlignPreviewPlayerTransform(previewPlayerInstance.transform);
    }

    /// <summary>
    /// Replaces the preview player prefab and optionally respawns the instance.
    /// </summary>
    public void SetPreviewPlayerPrefab(GameObject prefab, bool respawnInstance)
    {
        previewPlayerPrefab = prefab;

        if (respawnInstance)
        {
            SpawnPreviewPlayer();
        }
    }

    /// <summary>
    /// Assigns a camera to render the preview.
    /// </summary>
    public void SetPreviewCamera(Camera camera)
    {
        if (previewCamera == camera)
        {
            return;
        }

        if (previewCamera != null && previewCamera.targetTexture == renderTexture)
        {
            previewCamera.targetTexture = null;
        }

        previewCamera = camera;
        UpdatePreviewOutput();
    }

    /// <summary>
    /// Assigns the RawImage that displays the preview.
    /// </summary>
    public void SetPreviewImage(RawImage image)
    {
        if (previewImage == image)
        {
            return;
        }

        if (previewImage != null && previewImage.texture == renderTexture)
        {
            previewImage.texture = null;
        }

        previewImage = image;
        UpdatePreviewOutput();
    }

    /// <summary>
    /// Assigns a render texture to be used by the preview camera and image.
    /// </summary>
    public void SetRenderTexture(RenderTexture texture, bool takeOwnership)
    {
        if (renderTexture == texture && ownsRenderTexture == takeOwnership)
        {
            return;
        }

        ReleaseOwnedRenderTexture();

        renderTexture = texture;
        ownsRenderTexture = takeOwnership;

        if (ownsRenderTexture && renderTexture != null && !renderTexture.IsCreated())
        {
            renderTexture.Create();
        }

        UpdatePreviewOutput();
    }

    /// <summary>
    /// Forces the preview output to be refreshed.
    /// </summary>
    public void RefreshPreview()
    {
        UpdatePreviewOutput();
    }

    private void UpdatePreviewOutput()
    {
        EnsureCameraReference();
        EnsureRenderTexture();
        ApplyCameraOutput();
        ApplyImageOutput();
    }

    private void EnsureCameraReference()
    {
        if (previewCamera == null)
        {
            previewCamera = GetComponentInChildren<Camera>(includeInactive: true);
        }
    }

    private void EnsureRenderTexture()
    {
        if (renderTexture == null)
        {
            renderTexture = CreateRenderTexture();
            ownsRenderTexture = renderTexture != null;
        }
        else if (!renderTexture.IsCreated())
        {
            renderTexture.Create();
        }

        if (renderTexture != null)
        {
            renderTexture.filterMode = renderTextureFilterMode;
            renderTexture.useMipMap = renderTextureMipMaps;
            renderTexture.autoGenerateMips = renderTextureMipMaps;
        }
    }

    private RenderTexture CreateRenderTexture()
    {
        if (renderTextureSize.x <= 0 || renderTextureSize.y <= 0)
        {
            Debug.LogWarning("Render texture size must be greater than zero.", this);
            return null;
        }

        var texture = new RenderTexture(renderTextureSize.x, renderTextureSize.y, renderTextureDepth, renderTextureFormat)
        {
            name = nameof(WardrobePreviewSetup) + "_Preview",
            filterMode = renderTextureFilterMode,
            useMipMap = renderTextureMipMaps,
            autoGenerateMips = renderTextureMipMaps
        };

        texture.Create();
        return texture;
    }

    private void ApplyCameraOutput()
    {
        if (previewCamera == null)
        {
            return;
        }

        previewCamera.targetTexture = renderTexture;
    }

    private void ApplyImageOutput()
    {
        if (previewImage == null)
        {
            return;
        }

        previewImage.texture = renderTexture;
        previewImage.enabled = renderTexture != null;
    }

    private void CleanupPreviewPlayer()
    {
        if (previewPlayerInstance == null)
        {
            return;
        }

        Destroy(previewPlayerInstance);
        previewPlayerInstance = null;
    }

    private void ReleaseOwnedRenderTexture()
    {
        if (!ownsRenderTexture || renderTexture == null)
        {
            return;
        }

        if (previewCamera != null && previewCamera.targetTexture == renderTexture)
        {
            previewCamera.targetTexture = null;
        }

        if (previewImage != null && previewImage.texture == renderTexture)
        {
            previewImage.texture = null;
        }

        if (renderTexture.IsCreated())
        {
            renderTexture.Release();
        }

        Destroy(renderTexture);
        renderTexture = null;
        ownsRenderTexture = false;
    }

    private Transform GetPreviewParent()
    {
        return previewParent != null ? previewParent : transform;
    }

    private void AlignPreviewPlayerTransform(Transform target)
    {
        if (target == null)
        {
            return;
        }

        target.localPosition = previewPlayerLocalPosition;
        target.localEulerAngles = previewPlayerLocalEulerAngles;
        target.localScale = previewPlayerLocalScale;
    }
}
