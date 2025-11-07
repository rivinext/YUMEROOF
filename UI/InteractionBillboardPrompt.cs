using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractionBillboardPrompt : MonoBehaviour
{
    [Header("Canvas")] 
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private bool disableCanvasWhenHidden = true;
    [SerializeField] private bool hideOnStart = true;
    [SerializeField, Min(0f)] private float fadeSpeed = 10f;

    [Header("Content Overrides")]
    [SerializeField] private TextMeshProUGUI promptLabel;
    [SerializeField] private Image promptIcon;

    private Camera targetCamera;
    private bool targetVisible = true;

    private void Awake()
    {
        if (targetCanvas == null)
        {
            targetCanvas = GetComponentInChildren<Canvas>(true);
        }

        if (canvasGroup == null && targetCanvas != null)
        {
            canvasGroup = targetCanvas.GetComponent<CanvasGroup>();
        }

        if (hideOnStart)
        {
            ApplyVisibilityImmediate(false);
        }
        else
        {
            ApplyVisibilityImmediate(targetVisible);
        }
    }

    private void OnEnable()
    {
        RefreshCameraReference();
    }

    private void LateUpdate()
    {
        EnsureCamera();

        if (targetCamera != null)
        {
            Vector3 cameraPosition = targetCamera.transform.position;
            Vector3 direction = cameraPosition - transform.position;
            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }
        }

        if (canvasGroup != null)
        {
            float targetAlpha = targetVisible ? 1f : 0f;
            float newAlpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
            if (!Mathf.Approximately(newAlpha, canvasGroup.alpha))
            {
                canvasGroup.alpha = newAlpha;
            }

            if (disableCanvasWhenHidden && targetCanvas != null)
            {
                if (targetVisible && !targetCanvas.enabled)
                {
                    targetCanvas.enabled = true;
                }
                else if (!targetVisible && Mathf.Approximately(canvasGroup.alpha, 0f) && targetCanvas.enabled)
                {
                    targetCanvas.enabled = false;
                }
            }
        }
        else if (targetCanvas != null)
        {
            targetCanvas.enabled = targetVisible;
        }
    }

    public void SetVisible(bool visible)
    {
        targetVisible = visible;

        if (canvasGroup == null)
        {
            if (targetCanvas != null)
            {
                targetCanvas.enabled = visible;
            }
            else
            {
                gameObject.SetActive(visible);
            }
        }
        else if (visible && disableCanvasWhenHidden && targetCanvas != null && !targetCanvas.enabled)
        {
            targetCanvas.enabled = true;
        }
    }

    public void SetContent(string text, Sprite icon)
    {
        ApplyOverrides(text, true, icon, true);
    }

    public void ApplyOverrides(string text, bool applyText, Sprite icon, bool applyIcon)
    {
        if (applyText && promptLabel != null)
        {
            promptLabel.text = text;
        }

        if (applyIcon && promptIcon != null)
        {
            promptIcon.sprite = icon;
            promptIcon.enabled = icon != null;
        }
    }

    private void ApplyVisibilityImmediate(bool visible)
    {
        targetVisible = visible;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
        }

        if (targetCanvas != null)
        {
            targetCanvas.enabled = !disableCanvasWhenHidden || visible;
        }
    }

    private void RefreshCameraReference()
    {
        if (Camera.main != null)
        {
            targetCamera = Camera.main;
        }
    }

    private bool EnsureCamera()
    {
        if (targetCamera == null || !targetCamera.isActiveAndEnabled)
        {
            RefreshCameraReference();
        }

        return targetCamera != null;
    }
}
