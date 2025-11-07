using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

/// <summary>
/// 単一のインタラクト用ビルボードUIを制御するコンポーネント。
/// 対象のTransformを追従しながら、アイコンとテキストを表示/非表示にできる。
/// </summary>
public class InteractableBillboardPrompt : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private GameObject contentRoot;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Image iconImage;

    [Header("Defaults")]
    [SerializeField] private LocalizedString defaultLocalizedMessage = new LocalizedString();
    [SerializeField, TextArea] private string defaultMessage;
    [SerializeField] private Sprite defaultIcon;
    [SerializeField] private Transform defaultAnchor;
    [SerializeField] private Vector3 defaultWorldOffset = Vector3.up;

    private Camera mainCamera;
    private bool isVisible;
    private MonoBehaviour currentInteractable;
    private Transform currentAnchor;
    private Vector3 currentOffset;
    private bool useDynamicAnchor;
    private MonoBehaviour boundsTarget;
    private AsyncOperationHandle<string> localizedHandle;
    private bool hasLocalizedHandle;
    private string currentFallbackMessage;

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponentInChildren<CanvasGroup>();
        }

        if (contentRoot == null && canvasGroup != null)
        {
            contentRoot = canvasGroup.gameObject;
        }

        if (contentRoot == null && transform.childCount > 0)
        {
            contentRoot = transform.GetChild(0).gameObject;
        }

        if (canvasGroup == null && contentRoot == null)
        {
            Debug.LogWarning($"[{nameof(InteractableBillboardPrompt)}] CanvasGroup または Content Root が設定されていません。表示切り替えが正しく動作しない可能性があります。", this);
        }

        SetVisible(false);
    }

    private void OnEnable()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void OnDisable()
    {
        ReleaseLocalizedHandle();
        SetVisible(false);
    }

    private void OnDestroy()
    {
        ReleaseLocalizedHandle();
    }

    private void LateUpdate()
    {
        if (!isVisible)
            return;

        UpdateWorldPosition();
        UpdateLookAt();
    }

    /// <summary>
    /// 指定された要求内容に基づきプロンプトを表示する。
    /// </summary>
    public void Show(in InteractableBillboardPromptRequest request)
    {
        if (request.Prompt != this)
            return;

        currentInteractable = request.Interactable;
        currentAnchor = request.Anchor != null ? request.Anchor : ResolveDefaultAnchor(request.Interactable);
        currentOffset = request.HasCustomOffset ? request.WorldOffset : defaultWorldOffset;

        useDynamicAnchor = ShouldUseDynamicAnchor(request);
        boundsTarget = useDynamicAnchor ? currentInteractable : null;

        ApplyIcon(request.Icon != null ? request.Icon : defaultIcon);
        ApplyMessage(request.LocalizedMessage.IsEmpty ? defaultLocalizedMessage : request.LocalizedMessage,
            request.HasFallbackMessage ? request.FallbackMessage : defaultMessage);

        SetVisible(true);
        UpdateWorldPosition();
    }

    /// <summary>
    /// 表示中のプロンプトを非表示にする。
    /// </summary>
    public void Hide(MonoBehaviour interactable)
    {
        if (currentInteractable != null && currentInteractable != interactable)
            return;

        ReleaseLocalizedHandle();
        currentInteractable = null;
        currentAnchor = null;
        currentOffset = defaultWorldOffset;
        currentFallbackMessage = defaultMessage;
        useDynamicAnchor = false;
        boundsTarget = null;
        SetVisible(false);
    }

    private Transform ResolveDefaultAnchor(MonoBehaviour interactable)
    {
        if (defaultAnchor != null)
            return defaultAnchor;

        return interactable != null ? interactable.transform : null;
    }

    private bool ShouldUseDynamicAnchor(in InteractableBillboardPromptRequest request)
    {
        if (request.Anchor != null || defaultAnchor != null)
            return false;

        if (request.Interactable == null)
            return false;

        return TryCalculateCombinedBounds(request.Interactable, out _);
    }

    private void UpdateWorldPosition()
    {
        Vector3 targetPosition = transform.position;
        bool hasPosition = false;

        if (useDynamicAnchor && boundsTarget != null && TryCalculateCombinedBounds(boundsTarget, out Bounds bounds))
        {
            targetPosition = CalculateAnchorFromBounds(bounds) + currentOffset;
            hasPosition = true;
        }

        if (!hasPosition)
        {
            Transform anchorTransform = currentAnchor;

            if (anchorTransform == null)
            {
                if (defaultAnchor != null)
                {
                    anchorTransform = defaultAnchor;
                }
                else if (currentInteractable != null)
                {
                    anchorTransform = currentInteractable.transform;
                }
            }

            if (anchorTransform != null)
            {
                targetPosition = anchorTransform.position + currentOffset;
                hasPosition = true;
            }
            else if (currentInteractable != null)
            {
                targetPosition = currentInteractable.transform.position + currentOffset;
                hasPosition = true;
            }
            else if (defaultAnchor != null)
            {
                targetPosition = defaultAnchor.position + currentOffset;
                hasPosition = true;
            }
            else
            {
                targetPosition = transform.position;
            }
        }

        transform.position = targetPosition;
    }

    private void UpdateLookAt()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
            return;

        Vector3 toCamera = transform.position - mainCamera.transform.position;
        if (toCamera.sqrMagnitude <= Mathf.Epsilon)
            return;

        transform.rotation = Quaternion.LookRotation(toCamera, Vector3.up);
    }

    private static bool TryCalculateCombinedBounds(MonoBehaviour target, out Bounds combinedBounds)
    {
        combinedBounds = default;

        if (target == null)
            return false;

        var renderers = target.GetComponentsInChildren<Renderer>();
        var colliders = target.GetComponentsInChildren<Collider>();

        bool hasBounds = false;

        void EncapsulateBounds(Bounds sourceBounds)
        {
            if (!hasBounds)
            {
                combinedBounds = sourceBounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(sourceBounds);
            }
        }

        foreach (var renderer in renderers)
        {
            if (renderer == null)
                continue;

            EncapsulateBounds(renderer.bounds);
        }

        foreach (var collider in colliders)
        {
            if (collider == null)
                continue;

            EncapsulateBounds(collider.bounds);
        }

        return hasBounds;
    }

    private static Vector3 CalculateAnchorFromBounds(in Bounds bounds)
    {
        return new Vector3(bounds.center.x, bounds.min.y + 1f, bounds.center.z);
    }

    private void ApplyIcon(Sprite sprite)
    {
        if (iconImage == null)
            return;

        iconImage.sprite = sprite;
        iconImage.enabled = sprite != null;
    }

    private void ApplyMessage(LocalizedString localized, string fallback)
    {
        ReleaseLocalizedHandle();

        currentFallbackMessage = fallback;
        SetMessage(fallback);

        if (!localized.IsEmpty)
        {
            try
            {
                localizedHandle = localized.GetLocalizedStringAsync();
                hasLocalizedHandle = true;
                if (localizedHandle.IsDone)
                {
                    SetMessage(localizedHandle.Status == AsyncOperationStatus.Succeeded ? localizedHandle.Result : fallback);
                    ReleaseLocalizedHandle();
                }
                else
                {
                    localizedHandle.Completed += OnHandleCompleted;
                }
            }
            catch (Exception)
            {
                SetMessage(fallback);
            }
        }
        else
        {
            SetMessage(fallback);
        }
    }

    private void SetMessage(string text)
    {
        if (messageText == null)
            return;

        messageText.text = text ?? string.Empty;
    }

    private void SetVisible(bool visible)
    {
        isVisible = visible;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = visible;
            canvasGroup.interactable = visible;
        }

        if (contentRoot != null && contentRoot != gameObject)
        {
            if (!contentRoot.activeSelf && visible)
                contentRoot.SetActive(true);
            else if (contentRoot.activeSelf && !visible)
                contentRoot.SetActive(false);
        }
    }

    private void ReleaseLocalizedHandle()
    {
        if (hasLocalizedHandle)
        {
            localizedHandle.Completed -= OnHandleCompleted;
            if (localizedHandle.IsValid())
            {
                localizedHandle.Release();
            }
            hasLocalizedHandle = false;
        }
    }

    private void OnHandleCompleted(AsyncOperationHandle<string> handle)
    {
        if (!hasLocalizedHandle)
            return;

        SetMessage(handle.Status == AsyncOperationStatus.Succeeded ? handle.Result : currentFallbackMessage);
        ReleaseLocalizedHandle();
    }
}

/// <summary>
/// プロンプト表示に必要な情報をまとめた構造体。
/// </summary>
public struct InteractableBillboardPromptRequest
{
    public InteractableBillboardPrompt Prompt;
    public MonoBehaviour Interactable;
    public Transform Anchor;
    public Vector3 WorldOffset;
    public LocalizedString LocalizedMessage;
    public string FallbackMessage;

    public Sprite Icon;

    public bool HasCustomOffset;
    public bool HasFallbackMessage;
}

/// <summary>
/// 対象がビルボードプロンプト用の情報を提供できる場合に実装するインターフェース。
/// </summary>
public interface IInteractableBillboardPromptSource
{
    bool TryGetPromptRequest(out InteractableBillboardPromptRequest request);
}
