using System;
using System.Collections.Generic;
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

    [Header("Anchoring")]
    [SerializeField] private Transform anchorRoot;
    [SerializeField] private bool autoAnchorToBounds = true;
    [SerializeField] private bool includeRenderers = true;
    [SerializeField] private bool includeColliders = true;
    [SerializeField, Min(0f)] private float verticalOffset = 1f;

    private Camera targetCamera;
    private bool targetVisible = true;
    private Renderer[] cachedAnchorRenderers = Array.Empty<Renderer>();
    private Collider[] cachedAnchorColliders = Array.Empty<Collider>();

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

        EnsureAnchorRoot();
        CacheAnchorTargets();

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
        EnsureAnchorRoot();
        CacheAnchorTargets();
        RefreshCameraReference();
    }

    private void LateUpdate()
    {
        if (TryGetAnchorBounds(out Bounds bounds))
        {
            Vector3 bottomCenter = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            transform.position = bottomCenter + Vector3.up * verticalOffset;
        }

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

    public void SetAnchorRoot(Transform root)
    {
        anchorRoot = root != null ? root : transform.parent;
        EnsureAnchorRoot();
        CacheAnchorTargets();
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

    private void EnsureAnchorRoot()
    {
        if (anchorRoot == null)
        {
            anchorRoot = transform.parent;
        }
    }

    private void CacheAnchorTargets()
    {
        if (!autoAnchorToBounds)
        {
            cachedAnchorRenderers = Array.Empty<Renderer>();
            cachedAnchorColliders = Array.Empty<Collider>();
            return;
        }

        if (anchorRoot == null)
        {
            cachedAnchorRenderers = Array.Empty<Renderer>();
            cachedAnchorColliders = Array.Empty<Collider>();
            return;
        }

        if (includeRenderers)
        {
            List<Renderer> renderers = new List<Renderer>();
            foreach (Renderer renderer in anchorRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                {
                    continue;
                }

                Transform rendererTransform = renderer.transform;
                if (rendererTransform != null && rendererTransform.IsChildOf(transform))
                {
                    continue;
                }

                renderers.Add(renderer);
            }

            cachedAnchorRenderers = renderers.ToArray();
        }
        else
        {
            cachedAnchorRenderers = Array.Empty<Renderer>();
        }

        if (includeColliders)
        {
            List<Collider> colliders = new List<Collider>();
            foreach (Collider collider in anchorRoot.GetComponentsInChildren<Collider>(true))
            {
                if (collider == null)
                {
                    continue;
                }

                Transform colliderTransform = collider.transform;
                if (colliderTransform != null && colliderTransform.IsChildOf(transform))
                {
                    continue;
                }

                colliders.Add(collider);
            }

            cachedAnchorColliders = colliders.ToArray();
        }
        else
        {
            cachedAnchorColliders = Array.Empty<Collider>();
        }
    }

    private bool TryGetAnchorBounds(out Bounds bounds)
    {
        bounds = new Bounds();

        if (!autoAnchorToBounds)
        {
            return false;
        }

        bool hasBounds = false;

        if (includeRenderers && cachedAnchorRenderers != null)
        {
            foreach (Renderer renderer in cachedAnchorRenderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                Transform rendererTransform = renderer.transform;
                if (rendererTransform != null && rendererTransform.IsChildOf(transform))
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
        }

        if (includeColliders && cachedAnchorColliders != null)
        {
            foreach (Collider collider in cachedAnchorColliders)
            {
                if (collider == null)
                {
                    continue;
                }

                Transform colliderTransform = collider.transform;
                if (colliderTransform != null && colliderTransform.IsChildOf(transform))
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = collider.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(collider.bounds);
                }
            }
        }

        return hasBounds;
    }
}
