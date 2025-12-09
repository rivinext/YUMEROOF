using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ToggleFadePanelController : MonoBehaviour
{
    [SerializeField] private Toggle toggle;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField, Tooltip("Optional GameObject toggled alongside the fade.")]
    private GameObject contentRoot;
    [SerializeField, Tooltip("Seconds for the panel to fade in.")]
    private float fadeInDuration = 0.25f;
    [SerializeField, Tooltip("Seconds for the panel to fade out.")]
    private float fadeOutDuration = 0.2f;
    [SerializeField, Tooltip("When enabled, the content root will be deactivated after fading out.")]
    private bool deactivateOnHide = true;
    [SerializeField, Tooltip("Optional panel notified for exclusion handling when opened.")]
    private Object exclusionTarget;

    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
    }

    private void Start()
    {
        if (toggle == null)
        {
            return;
        }

        ApplyToggleState(toggle.isOn, true);
        toggle.onValueChanged.AddListener(HandleToggleValueChanged);
    }

    private void OnDestroy()
    {
        if (toggle != null)
        {
            toggle.onValueChanged.RemoveListener(HandleToggleValueChanged);
        }
    }

    private void HandleToggleValueChanged(bool isOn)
    {
        ApplyToggleState(isOn, false);
    }

    private void ApplyToggleState(bool isOn, bool instant)
    {
        if (canvasGroup == null)
        {
            ToggleContentActive(isOn);
            if (isOn)
            {
                NotifyExclusionManager();
            }
            return;
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = StartCoroutine(FadeRoutine(isOn, instant));
    }

    private IEnumerator FadeRoutine(bool show, bool instant)
    {
        float duration = Mathf.Max(0f, show ? fadeInDuration : fadeOutDuration);
        if (instant || duration <= 0f)
        {
            ApplyImmediateState(show);
            fadeRoutine = null;
            yield break;
        }

        if (contentRoot != null)
        {
            contentRoot.SetActive(true);
        }

        float startAlpha = canvasGroup.alpha;
        float endAlpha = show ? 1f : 0f;
        float elapsed = 0f;

        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        canvasGroup.alpha = endAlpha;
        canvasGroup.blocksRaycasts = show;
        canvasGroup.interactable = show;

        if (!show && deactivateOnHide && contentRoot != null)
        {
            contentRoot.SetActive(false);
        }

        if (show)
        {
            NotifyExclusionManager();
        }

        fadeRoutine = null;
    }

    private void ApplyImmediateState(bool show)
    {
        canvasGroup.alpha = show ? 1f : 0f;
        canvasGroup.blocksRaycasts = show;
        canvasGroup.interactable = show;

        ToggleContentActive(show || !deactivateOnHide);

        if (show)
        {
            NotifyExclusionManager();
        }
    }

    private void ToggleContentActive(bool active)
    {
        if (contentRoot != null)
        {
            contentRoot.SetActive(active);
        }
    }

    private void NotifyExclusionManager()
    {
        if (exclusionTarget == null)
        {
            return;
        }

        var manager = UIPanelExclusionManager.Instance;
        if (manager == null)
        {
            return;
        }

        switch (exclusionTarget)
        {
            case SettingsPanelAnimator settings:
                manager.NotifyOpened(settings);
                break;
            case CameraControlPanel camera:
                manager.NotifyOpened(camera);
                break;
            case MilestonePanel milestone:
                manager.NotifyOpened(milestone);
                break;
            case InventoryUI inventory:
                manager.NotifyOpened(inventory);
                break;
            case WardrobeUIController wardrobe:
                manager.NotifyOpened(wardrobe);
                break;
            case ColorPanelController colorPanel:
                manager.NotifyOpened(colorPanel);
                break;
        }
    }
}
