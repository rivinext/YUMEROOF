using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
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
    [SerializeField] private Material foregroundMaterial;
    [SerializeField] private Material foregroundTMPMaterial;

    private const string ForegroundMaterialAssetPath = "Assets/UI/Materials/PromptAlwaysOnTop.mat";
    private const string ForegroundTMPMaterialAssetPath = "Assets/UI/Materials/PromptTMPAlwaysOnTop.mat";
    private const string ForegroundTMPShaderName = "TextMeshPro/Distance Field Overlay";

    private Object currentOwner;
    private InteractionPromptData currentData;

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

        EnsureForegroundMaterial();
        EnsureForegroundTMPMaterial();
        ApplyForegroundMaterial();

        HideImmediate();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureForegroundMaterial();
        EnsureForegroundTMPMaterial();
        ApplyForegroundMaterial();
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
        ApplyForegroundMaterial();
    }

    private void SetVisible(bool visible)
    {
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
    }

    private void ApplyForegroundMaterial()
    {
        if (promptRoot == null && promptBillboard != null)
        {
            promptRoot = promptBillboard.gameObject;
        }

        if (promptRoot == null)
        {
            return;
        }

        var textComponents = promptRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < textComponents.Length; i++)
        {
            ApplyTMPForeground(textComponents[i]);
        }

        if (foregroundMaterial == null)
        {
            return;
        }

        var graphics = promptRoot.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] is TextMeshProUGUI || graphics[i] is TMP_SubMeshUI)
            {
                continue;
            }

            graphics[i].material = foregroundMaterial;
        }
    }

    private void EnsureForegroundMaterial()
    {
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

    private void EnsureForegroundTMPMaterial()
    {
        if (foregroundTMPMaterial != null)
        {
            return;
        }

#if UNITY_EDITOR
        foregroundTMPMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(ForegroundTMPMaterialAssetPath);
        if (foregroundTMPMaterial != null)
        {
            return;
        }
#endif

        var shader = Shader.Find(ForegroundTMPShaderName);
        if (shader == null)
        {
            return;
        }

        foregroundTMPMaterial = new Material(shader)
        {
            name = "PromptTMPAlwaysOnTop (Runtime)"
        };
    }

    private void ApplyTMPForeground(TextMeshProUGUI textComponent)
    {
        if (textComponent == null)
        {
            return;
        }

        if (foregroundTMPMaterial != null)
        {
            textComponent.fontSharedMaterial = foregroundTMPMaterial;
        }
        else
        {
            var instance = CreateOverlayMaterialInstance(textComponent.fontMaterial);
            if (instance != null)
            {
                textComponent.fontMaterial = instance;
            }
        }

        var subMeshes = textComponent.GetComponentsInChildren<TMP_SubMeshUI>(true);
        for (int i = 0; i < subMeshes.Length; i++)
        {
            var subMesh = subMeshes[i];
            if (subMesh == null)
            {
                continue;
            }

            if (foregroundTMPMaterial != null)
            {
                subMesh.fontMaterial = foregroundTMPMaterial;
            }
            else
            {
                var instance = CreateOverlayMaterialInstance(subMesh.fontMaterial);
                if (instance != null)
                {
                    subMesh.fontMaterial = instance;
                }
            }
        }
    }

    private Material CreateOverlayMaterialInstance(Material source)
    {
        if (source == null)
        {
            return null;
        }

        if (!source.HasProperty(ShaderUtilities.ID_ZTestMode))
        {
            return new Material(source);
        }

        if (source.GetInt(ShaderUtilities.ID_ZTestMode) == (int)CompareFunction.Always)
        {
            return source;
        }

        var overlayInstance = new Material(source)
        {
            name = source.name + " (Overlay)"
        };
        overlayInstance.SetInt(ShaderUtilities.ID_ZTestMode, (int)CompareFunction.Always);
        return overlayInstance;
    }
}
