using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Controls a single shared interaction prompt billboard that can be reused by any interactable.
/// </summary>
public class SharedInteractionPromptController : MonoBehaviour
{
    public static SharedInteractionPromptController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private InteractionPromptBillboard promptBillboard;
    [SerializeField] private CanvasGroup promptCanvasGroup;
    [SerializeField] private DynamicLocalizer promptLocalizer;
    [SerializeField] private string promptLocalizerField = "Prompt";
    [Header("Hierarchy Order")]
    [Tooltip("Optional helper that keeps the prompt text/icon container in front of the background.")]
    [SerializeField] private PromptSiblingOrderController promptSiblingOrderController;
    [Tooltip("Prompt background container (moved to the first sibling when Move Background First On Show is enabled).")]
    [SerializeField] private RectTransform promptBackgroundContainer;
    [Tooltip("Prompt text or icon container that should render in front of the background.")]
    [SerializeField] private RectTransform promptContentContainer;
    [Tooltip("When enabled, the prompt content container is moved to the end of its sibling list whenever the prompt is shown, ensuring it renders above the background.")]
    [SerializeField] private bool reorderContentOnShow = true;
    [Tooltip("Set this if the prompt background must stay behind the text. It calls SetAsFirstSibling on the background container when the prompt is shown.")]
    [SerializeField] private bool moveBackgroundFirstOnShow;
    [Header("Sorting")]
    [SerializeField] private bool overrideSorting;
    [SerializeField] private string sortingLayerName;
    [SerializeField] private int sortingOrder;
    [Header("Prompt Foreground Material (Optional)")]
    [Tooltip("When enabled, replaces all Graphics under Prompt Root with a foreground material so the prompt renders on top. Disable to keep the default UI materials for cases where the prompt should blend with the rest of the UI.")]
    [SerializeField] private bool useForegroundMaterial = true;
    [Tooltip("Material applied to prompt Graphics when foreground rendering is enabled. Ignored when Use Foreground Material is disabled.")]
    [SerializeField] private Material foregroundMaterial;

    private const string ForegroundMaterialAssetPath = "Assets/UI/Materials/PromptAlwaysOnTop.mat";

    private Object currentOwner;
    private InteractionPromptData currentData;
    private Canvas cachedCanvas;
    private bool hasOriginalCanvasSettings;
    private bool originalOverrideSorting;
    private string originalSortingLayerName;
    private int originalSortingOrder;
    [SerializeField, HideInInspector] private bool sortingSettingsInitialized;
    private bool hasLoggedMissingCanvasWarning;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"Duplicate {nameof(SharedInteractionPromptController)} detected. Destroying the new instance on {gameObject.name}.");
            Destroy(this);
            return;
        }

        Instance = this;

        if (promptRoot == null && promptBillboard != null)
        {
            promptRoot = promptBillboard.gameObject;
        }

        CacheCanvas(true);
        ApplyCanvasSorting();
        RefreshForegroundMaterial();
        ConfigurePromptSiblingOrderController();

        HideImmediate();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (promptRoot == null && promptBillboard != null)
        {
            promptRoot = promptBillboard.gameObject;
        }

        CacheCanvas(true);
        ApplyCanvasSorting();
        RefreshForegroundMaterial();
        ConfigurePromptSiblingOrderController();
    }
#endif

    /// <summary>
    /// Displays the prompt for the provided owner. If a different owner was active, it will be replaced.
    /// </summary>
    public void ShowPrompt(Object owner, InteractionPromptData data)
    {
        if (owner == null)
            return;

        if (!data.IsValid)
        {
            HidePrompt(owner);
            return;
        }

        if (!ReferenceEquals(currentOwner, owner))
        {
            HideImmediate();
            currentOwner = owner;
        }

        currentData = data;

        if (promptBillboard != null)
        {
            promptBillboard.SetTarget(data.Anchor, data.HeightOffset);
        }

        if (promptLocalizer != null)
        {
            promptLocalizer.SetFieldByName(promptLocalizerField, data.LocalizationKey);
        }

        SetVisible(true);
    }

    /// <summary>
    /// Hides the prompt if the caller currently owns it.
    /// </summary>
    public void HidePrompt(Object owner)
    {
        if (owner != null && !ReferenceEquals(currentOwner, owner))
            return;

        HideImmediate();
    }

    /// <summary>
    /// Forces the prompt to hide regardless of the current owner.
    /// </summary>
    public void HideAll()
    {
        HideImmediate();
    }

    private void HideImmediate()
    {
        currentOwner = null;
        currentData = default;

        if (promptBillboard != null)
        {
            promptBillboard.SetTarget(null);
        }

        if (promptLocalizer != null)
        {
            promptLocalizer.SetFieldByName(promptLocalizerField, string.Empty);
        }

        SetVisible(false);
    }

    [ContextMenu("Refresh Foreground Material")]
    public void RefreshForegroundMaterial()
    {
        if (!useForegroundMaterial)
        {
            ResetForegroundMaterialToDefault();
            return;
        }

        EnsureForegroundMaterial();
        ApplyForegroundMaterial();
    }

    private void SetVisible(bool visible)
    {
        ApplyCanvasSorting();

        if (promptRoot != null)
        {
            promptRoot.SetActive(visible);
        }

        if (promptCanvasGroup != null)
        {
            promptCanvasGroup.alpha = visible ? 1f : 0f;
            promptCanvasGroup.interactable = visible;
            promptCanvasGroup.blocksRaycasts = visible;
        }

        if (visible)
        {
            EnsurePromptHierarchyOrder();
        }
    }

    private void EnsurePromptHierarchyOrder()
    {
        if (!reorderContentOnShow && !moveBackgroundFirstOnShow)
        {
            return;
        }

        ConfigurePromptSiblingOrderController();

        if (promptSiblingOrderController != null)
        {
            if (moveBackgroundFirstOnShow)
            {
                promptSiblingOrderController.MoveBackgroundsToBack();
            }

            if (reorderContentOnShow)
            {
                promptSiblingOrderController.ApplyForegroundOrder();
            }

            return;
        }

        if (moveBackgroundFirstOnShow && promptBackgroundContainer != null)
        {
            promptBackgroundContainer.SetAsFirstSibling();
        }

        if (reorderContentOnShow && promptContentContainer != null)
        {
            promptContentContainer.SetAsLastSibling();
        }
    }

    private void ConfigurePromptSiblingOrderController()
    {
        if (promptSiblingOrderController == null)
        {
            return;
        }

        promptSiblingOrderController.ConfigureTargets(promptContentContainer, promptBackgroundContainer);
    }

    private void ApplyForegroundMaterial()
    {
        if (!useForegroundMaterial)
        {
            return;
        }

        if (promptRoot == null && promptBillboard != null)
        {
            promptRoot = promptBillboard.gameObject;
        }

        if (promptRoot == null || foregroundMaterial == null)
        {
            return;
        }

        var graphics = promptRoot.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            graphics[i].material = foregroundMaterial;
        }
    }

    private void EnsureForegroundMaterial()
    {
        if (!useForegroundMaterial)
        {
            return;
        }

        if (foregroundMaterial != null)
        {
            return;
        }

#if UNITY_EDITOR
        foregroundMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(ForegroundMaterialAssetPath);
        if (foregroundMaterial != null)
        {
            return;
        }
#endif

        var shader = Shader.Find("UI/UIAlwaysOnTop");
        if (shader == null)
        {
            return;
        }

        foregroundMaterial = new Material(shader)
        {
            name = "PromptAlwaysOnTop (Runtime)"
        };
    }

    private void ResetForegroundMaterialToDefault()
    {
        if (promptRoot == null && promptBillboard != null)
        {
            promptRoot = promptBillboard.gameObject;
        }

        if (promptRoot == null)
        {
            return;
        }

        var graphics = promptRoot.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            graphics[i].material = graphics[i].defaultMaterial;
        }
    }

    private Canvas CacheCanvas(bool forceRefresh = false)
    {
        if (forceRefresh)
        {
            cachedCanvas = null;
            hasOriginalCanvasSettings = false;
        }

        if (cachedCanvas != null)
        {
            return cachedCanvas;
        }

        if (promptRoot == null && promptBillboard != null)
        {
            promptRoot = promptBillboard.gameObject;
        }

        if (promptRoot != null)
        {
            cachedCanvas = promptRoot.GetComponent<Canvas>();
            if (cachedCanvas == null)
            {
                cachedCanvas = promptRoot.GetComponentInChildren<Canvas>(true);
            }
        }

        if (cachedCanvas != null)
        {
            if (!hasOriginalCanvasSettings)
            {
                originalOverrideSorting = cachedCanvas.overrideSorting;
                originalSortingLayerName = cachedCanvas.sortingLayerName;
                originalSortingOrder = cachedCanvas.sortingOrder;
                hasOriginalCanvasSettings = true;
            }

            if (!sortingSettingsInitialized)
            {
                sortingLayerName = cachedCanvas.sortingLayerName;
                sortingOrder = cachedCanvas.sortingOrder;
                sortingSettingsInitialized = true;
            }

            hasLoggedMissingCanvasWarning = false;
        }

        return cachedCanvas;
    }

    private void ApplyCanvasSorting()
    {
        var canvas = CacheCanvas();
        if (canvas == null)
        {
            if (Application.isPlaying && !hasLoggedMissingCanvasWarning)
            {
                Debug.LogWarning($"{nameof(SharedInteractionPromptController)} on {gameObject.name} could not find a Canvas under the assigned {nameof(promptRoot)}.", this);
                hasLoggedMissingCanvasWarning = true;
            }

            return;
        }

        if (overrideSorting)
        {
            canvas.overrideSorting = true;
            if (!string.IsNullOrEmpty(sortingLayerName))
            {
                canvas.sortingLayerName = sortingLayerName;
            }
            canvas.sortingOrder = sortingOrder;
        }
        else if (hasOriginalCanvasSettings)
        {
            canvas.overrideSorting = originalOverrideSorting;
            canvas.sortingLayerName = originalSortingLayerName;
            canvas.sortingOrder = originalSortingOrder;
        }
    }
}
