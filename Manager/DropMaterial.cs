using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Component attached to material drop objects. Holds the material ID so other
/// systems can identify what material this pickup represents.
/// Implements <see cref="IInteractable"/> so the player can pick up the
/// material by interacting with it.
/// </summary>
public class DropMaterial : MonoBehaviour, IInteractable
{
    [SerializeField] private string materialID;
    [SerializeField] private SpriteRenderer iconRenderer;
    [SerializeField] private string anchorID;
    [SerializeField] private Transform collectAnimationRoot;
    [SerializeField] private float collectAnimationDuration = 0.5f;
    [SerializeField] private float collectFadeDuration = 0.5f;
    [SerializeField] private AnimationCurve collectYOffsetCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.3f, -0.05f),
        new Keyframe(1f, 0.2f));
    [SerializeField] private AnimationCurve collectAlphaCurve = new AnimationCurve(
        new Keyframe(0f, 1f),
        new Keyframe(1f, 0f));

    private string materialName;
    private Sprite materialIcon;
    private bool isCollecting;
    private SpriteRenderer[] cachedSpriteRenderers = System.Array.Empty<SpriteRenderer>();
    private Color[] initialSpriteColors = System.Array.Empty<Color>();

    /// <summary>
    /// Material identifier associated with this drop.
    /// </summary>
    public string MaterialID
    {
        get => materialID;
        set
        {
            materialID = value;
            Debug.Log($"MaterialID set to {materialID}");
            LoadMaterialInfo();
        }
    }

    /// <summary>
    /// Identifier for the anchor that spawned this drop, if any.
    /// </summary>
    public string AnchorID
    {
        get => anchorID;
        set => anchorID = value;
    }

    /// <summary>
    /// Adds this material to the player's inventory and destroys the drop.
    /// </summary>
    public void Interact()
    {
        if (isCollecting)
        {
            return;
        }

        if (string.IsNullOrEmpty(materialID))
        {
            Debug.LogWarning("Attempted to collect a drop material without a valid ID.");
            return;
        }

        // Ensure material data is loaded before adding to inventory
        if (string.IsNullOrEmpty(materialName))
        {
            LoadMaterialInfo();
        }

        var inventory = InventoryManager.Instance;
        if (inventory == null)
        {
            Debug.LogWarning("InventoryManager instance not found when collecting material drop.");
            return;
        }

        isCollecting = true;

        Debug.Log($"Adding material to inventory: ID={materialID}, Name={materialName}");
        inventory.AddMaterial(materialID);
        Debug.Log($"Added material to inventory: ID={materialID}, Name={materialName}");

        var sceneName = SceneManager.GetActiveScene().name;
        var worldPosition = transform.position;
        var dropAnchorID = anchorID;

        var saveManager = DropMaterialSaveManager.Instance;
        if (saveManager != null)
        {
            saveManager.RemoveDrop(sceneName, materialID, worldPosition, dropAnchorID);
        }

        foreach (var collider in GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }

        StartCoroutine(PlayCollectAnimation());
    }

    void Awake()
    {
        if (iconRenderer == null)
        {
            iconRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (collectAnimationRoot == null)
        {
            collectAnimationRoot = transform;
        }
        LoadMaterialInfo();
        CacheSpriteRendererColors();
    }

    void Start()
    {
        var manager = DropMaterialSaveManager.Instance;
        if (manager == null)
        {
            return;
        }

        string sceneName = SceneManager.GetActiveScene().name;
        if (!manager.IsDropRegistered(sceneName, materialID, transform.position, anchorID))
        {
            manager.RegisterDrop(sceneName, materialID, transform.position, anchorID);
        }
    }

    void LateUpdate()
    {
        if (Camera.main != null)
        {
            transform.forward = Camera.main.transform.forward;
        }
    }

    private void LoadMaterialInfo()
    {
        var dataManager = FurnitureDataManager.Instance;
        if (dataManager == null)
        {
            Debug.LogWarning($"FurnitureDataManager not available for material ID {materialID}");
            return;
        }

        var dataSO = dataManager.GetMaterialDataSO(materialID);
        if (dataSO == null)
        {
            Debug.LogWarning($"Material data not found for ID {materialID}");
            return;
        }

        materialName = dataSO.materialName;
        materialIcon = dataSO.icon;

        if (iconRenderer != null && materialIcon != null)
        {
            iconRenderer.sprite = materialIcon;
        }

        Debug.Log($"Loaded material info: Name={materialName}, Sprite={iconRenderer?.sprite}");
    }

    private IEnumerator PlayCollectAnimation()
    {
        var targetTransform = collectAnimationRoot != null ? collectAnimationRoot : transform;
        Vector3 startPosition = targetTransform.position;
        float duration = Mathf.Max(collectAnimationDuration, Mathf.Epsilon);
        float fadeDuration = Mathf.Max(collectFadeDuration, Mathf.Epsilon);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            float offset = collectYOffsetCurve != null ? collectYOffsetCurve.Evaluate(normalizedTime) : 0f;
            targetTransform.position = startPosition + Vector3.up * offset;

            float normalizedFadeTime = Mathf.Clamp01(elapsed / fadeDuration);
            float alphaMultiplier = collectAlphaCurve != null ? collectAlphaCurve.Evaluate(normalizedFadeTime) : 1f;
            ApplyAlphaToSpriteRenderers(alphaMultiplier);

            elapsed += Time.deltaTime;
            yield return null;
        }

        float finalOffset = collectYOffsetCurve != null ? collectYOffsetCurve.Evaluate(1f) : 0f;
        targetTransform.position = startPosition + Vector3.up * finalOffset;

        float finalAlphaMultiplier = collectAlphaCurve != null ? collectAlphaCurve.Evaluate(1f) : 0f;
        ApplyAlphaToSpriteRenderers(finalAlphaMultiplier);

        Destroy(gameObject);
    }

    private void CacheSpriteRendererColors()
    {
        var spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

        if (spriteRenderers == null)
        {
            spriteRenderers = System.Array.Empty<SpriteRenderer>();
        }

        if (iconRenderer != null && System.Array.IndexOf(spriteRenderers, iconRenderer) < 0)
        {
            var extendedRenderers = new SpriteRenderer[spriteRenderers.Length + 1];
            spriteRenderers.CopyTo(extendedRenderers, 0);
            extendedRenderers[extendedRenderers.Length - 1] = iconRenderer;
            spriteRenderers = extendedRenderers;
        }

        cachedSpriteRenderers = spriteRenderers;

        if (cachedSpriteRenderers == null || cachedSpriteRenderers.Length == 0)
        {
            cachedSpriteRenderers = System.Array.Empty<SpriteRenderer>();
            initialSpriteColors = System.Array.Empty<Color>();
            return;
        }

        initialSpriteColors = new Color[cachedSpriteRenderers.Length];
        for (int i = 0; i < cachedSpriteRenderers.Length; i++)
        {
            if (cachedSpriteRenderers[i] != null)
            {
                initialSpriteColors[i] = cachedSpriteRenderers[i].color;
            }
        }
    }

    private void ApplyAlphaToSpriteRenderers(float alphaMultiplier)
    {
        if (cachedSpriteRenderers == null || initialSpriteColors == null)
        {
            return;
        }

        float clampedAlphaMultiplier = Mathf.Clamp01(alphaMultiplier);
        int count = Mathf.Min(cachedSpriteRenderers.Length, initialSpriteColors.Length);
        for (int i = 0; i < count; i++)
        {
            var renderer = cachedSpriteRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            Color initialColor = initialSpriteColors[i];
            initialColor.a *= clampedAlphaMultiplier;
            renderer.color = initialColor;
        }
    }
}
