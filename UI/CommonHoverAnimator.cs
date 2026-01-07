using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class CommonHoverAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private const float MinHoverScaleValue = 0.01f;
    private const float MinHoverDurationValue = 0.01f;

    [SerializeField] private RectTransform hoverTarget;
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float hoverTilt = 5f;
    [SerializeField] private float hoverDuration = 0.18f;
    [SerializeField] private bool disableHoverAnimation = false;
    [SerializeField] private List<GameObject> hideGameObjects = new List<GameObject>();
    [SerializeField] private List<Canvas> hideCanvases = new List<Canvas>();
    [SerializeField] private List<CanvasGroup> hideCanvasGroups = new List<CanvasGroup>();

    [Header("Hover Audio")]
    [SerializeField] private AudioClip hoverSfx;
    [SerializeField] private AudioSource hoverAudioSource;
    [SerializeField, Range(0f, 1f)] private float hoverSfxVolume = 1f;
    [SerializeField, Min(0f)] private float hoverSfxCooldown = 0.1f;
    [SerializeField] private bool disableHoverSfx = false;

    [Header("Click Audio")]
    [SerializeField] private AudioClip clickSfx;
    [SerializeField] private AudioSource clickAudioSource;
    [SerializeField, Range(0f, 1f)] private float clickSfxVolume = 1f;

    private RectTransform resolvedHoverTarget;
    private Vector3 baseScale = Vector3.one;
    private Vector3 baseEulerAngles = Vector3.zero;
    private Tween hoverTween;
    private float lastHoverSfxTime = -10f;
    private readonly Dictionary<GameObject, bool> originalGameObjectStates = new Dictionary<GameObject, bool>();
    private readonly Dictionary<Canvas, bool> originalCanvasStates = new Dictionary<Canvas, bool>();
    private readonly Dictionary<CanvasGroup, CanvasGroupState> originalCanvasGroupStates = new Dictionary<CanvasGroup, CanvasGroupState>();

    private struct CanvasGroupState
    {
        public float Alpha;
        public bool BlocksRaycasts;
        public bool Interactable;
    }

    private float SafeHoverScale => Mathf.Max(hoverScale, MinHoverScaleValue);
    private float SafeHoverDuration => Mathf.Max(hoverDuration, MinHoverDurationValue);

    private void Awake()
    {
        resolvedHoverTarget = hoverTarget != null ? hoverTarget : transform as RectTransform;

        if (resolvedHoverTarget != null)
        {
            baseScale = resolvedHoverTarget.localScale;
            baseEulerAngles = resolvedHoverTarget.localEulerAngles;
        }

        KillHoverTween();
        ResetHoverTargetTransform();

        SetupHoverAudioSource();
        SetupClickAudioSource();
    }

    private void OnValidate()
    {
        hoverScale = Mathf.Max(hoverScale, MinHoverScaleValue);
        hoverDuration = Mathf.Max(hoverDuration, MinHoverDurationValue);
        hoverSfxVolume = Mathf.Clamp01(hoverSfxVolume);
        hoverSfxCooldown = Mathf.Max(hoverSfxCooldown, 0f);
        clickSfxVolume = Mathf.Clamp01(clickSfxVolume);
    }

    private void OnDisable()
    {
        KillHoverTween();
        ResetHoverTargetTransform();
        RestoreHiddenTargets();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!disableHoverAnimation && resolvedHoverTarget != null)
        {
            KillHoverTween();
            ResetHoverTargetTransform();

            Vector3 targetScale = baseScale * SafeHoverScale;
            Vector3 tiltedRotation = baseEulerAngles + new Vector3(0f, 0f, hoverTilt);
            float duration = SafeHoverDuration;

            Sequence sequence = DOTween.Sequence();
            sequence.Join(resolvedHoverTarget.DOScale(targetScale, duration).SetEase(Ease.OutQuad));
            sequence.Join(resolvedHoverTarget.DOLocalRotate(tiltedRotation, duration * 0.5f).SetEase(Ease.OutQuad));
            sequence.Append(resolvedHoverTarget.DOLocalRotate(baseEulerAngles, duration * 0.5f).SetEase(Ease.OutQuad));
            sequence.OnComplete(() => hoverTween = null);
            hoverTween = sequence;

            PlayHoverSfx();
        }

        HideTargets();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!disableHoverAnimation && resolvedHoverTarget != null)
        {
            KillHoverTween();
            resolvedHoverTarget.localEulerAngles = baseEulerAngles;
            hoverTween = resolvedHoverTarget.DOScale(baseScale, SafeHoverDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => hoverTween = null);
        }

        RestoreHiddenTargets();
    }

    public void HandlePointerEnter(PointerEventData eventData)
    {
        OnPointerEnter(eventData);
    }

    public void HandlePointerExit(PointerEventData eventData)
    {
        OnPointerExit(eventData);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        PlayClickSfx();
    }

    private void KillHoverTween()
    {
        hoverTween?.Kill();
        hoverTween = null;
    }

    private void ResetHoverTargetTransform()
    {
        if (resolvedHoverTarget == null)
        {
            return;
        }

        resolvedHoverTarget.localScale = baseScale;
        resolvedHoverTarget.localEulerAngles = baseEulerAngles;
    }

    private void SetupHoverAudioSource()
    {
        if (hoverAudioSource == null)
        {
            hoverAudioSource = GetComponent<AudioSource>();
            if (hoverAudioSource == null)
            {
                hoverAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (hoverAudioSource != null)
        {
            hoverAudioSource.playOnAwake = false;
            hoverAudioSource.loop = false;
            hoverAudioSource.spatialBlend = 0f;
        }
    }

    private void SetupClickAudioSource()
    {
        if (clickAudioSource == null)
        {
            clickAudioSource = hoverAudioSource != null ? hoverAudioSource : GetComponent<AudioSource>();
            if (clickAudioSource == null)
            {
                clickAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (clickAudioSource != null)
        {
            clickAudioSource.playOnAwake = false;
            clickAudioSource.loop = false;
            clickAudioSource.spatialBlend = 0f;
        }
    }

    private void PlayHoverSfx()
    {
        if (disableHoverSfx)
        {
            return;
        }

        if (hoverSfx == null || hoverAudioSource == null)
        {
            return;
        }

        float elapsed = Time.unscaledTime - lastHoverSfxTime;
        if (elapsed < hoverSfxCooldown)
        {
            return;
        }

        float volume = hoverSfxVolume * AudioManager.CurrentSfxVolume;
        if (volume <= 0f)
        {
            return;
        }

        hoverAudioSource.PlayOneShot(hoverSfx, volume);
        lastHoverSfxTime = Time.unscaledTime;
    }

    private void PlayClickSfx()
    {
        if (clickSfx == null || clickAudioSource == null)
        {
            return;
        }

        float volume = clickSfxVolume * AudioManager.CurrentSfxVolume;
        if (volume <= 0f)
        {
            return;
        }

        clickAudioSource.PlayOneShot(clickSfx, volume);
    }

    private void HideTargets()
    {
        CacheAndHideGameObjects();
        CacheAndHideCanvases();
        CacheAndHideCanvasGroups();
    }

    private void RestoreHiddenTargets()
    {
        foreach (KeyValuePair<GameObject, bool> entry in originalGameObjectStates)
        {
            if (entry.Key != null)
            {
                entry.Key.SetActive(entry.Value);
            }
        }

        foreach (KeyValuePair<Canvas, bool> entry in originalCanvasStates)
        {
            if (entry.Key != null)
            {
                entry.Key.enabled = entry.Value;
            }
        }

        foreach (KeyValuePair<CanvasGroup, CanvasGroupState> entry in originalCanvasGroupStates)
        {
            if (entry.Key != null)
            {
                entry.Key.alpha = entry.Value.Alpha;
                entry.Key.blocksRaycasts = entry.Value.BlocksRaycasts;
                entry.Key.interactable = entry.Value.Interactable;
            }
        }

        originalGameObjectStates.Clear();
        originalCanvasStates.Clear();
        originalCanvasGroupStates.Clear();
    }

    private void CacheAndHideGameObjects()
    {
        if (hideGameObjects == null)
        {
            return;
        }

        foreach (GameObject target in hideGameObjects)
        {
            if (target == null || originalGameObjectStates.ContainsKey(target))
            {
                continue;
            }

            originalGameObjectStates[target] = target.activeSelf;
            target.SetActive(false);
        }
    }

    private void CacheAndHideCanvases()
    {
        if (hideCanvases == null)
        {
            return;
        }

        foreach (Canvas target in hideCanvases)
        {
            if (target == null || originalCanvasStates.ContainsKey(target))
            {
                continue;
            }

            originalCanvasStates[target] = target.enabled;
            target.enabled = false;
        }
    }

    private void CacheAndHideCanvasGroups()
    {
        if (hideCanvasGroups == null)
        {
            return;
        }

        foreach (CanvasGroup target in hideCanvasGroups)
        {
            if (target == null || originalCanvasGroupStates.ContainsKey(target))
            {
                continue;
            }

            originalCanvasGroupStates[target] = new CanvasGroupState
            {
                Alpha = target.alpha,
                BlocksRaycasts = target.blocksRaycasts,
                Interactable = target.interactable
            };
            target.alpha = 0f;
            target.blocksRaycasts = false;
            target.interactable = false;
        }
    }
}
