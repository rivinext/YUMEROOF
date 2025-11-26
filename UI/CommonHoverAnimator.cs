using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class CommonHoverAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private const float MinHoverScaleValue = 0.01f;
    private const float MinHoverDurationValue = 0.01f;

    [SerializeField] private RectTransform hoverTarget;
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float hoverTilt = 5f;
    [SerializeField] private float hoverDuration = 0.18f;
    [SerializeField] private bool disableHoverAnimation = false;

    private RectTransform resolvedHoverTarget;
    private Vector3 baseScale = Vector3.one;
    private Vector3 baseEulerAngles = Vector3.zero;
    private Tween hoverTween;

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
    }

    private void OnValidate()
    {
        hoverScale = Mathf.Max(hoverScale, MinHoverScaleValue);
        hoverDuration = Mathf.Max(hoverDuration, MinHoverDurationValue);
    }

    private void OnDisable()
    {
        KillHoverTween();
        ResetHoverTargetTransform();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (disableHoverAnimation || resolvedHoverTarget == null)
        {
            return;
        }

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
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (disableHoverAnimation || resolvedHoverTarget == null)
        {
            return;
        }

        KillHoverTween();
        resolvedHoverTarget.localEulerAngles = baseEulerAngles;
        hoverTween = resolvedHoverTarget.DOScale(baseScale, SafeHoverDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => hoverTween = null);
    }

    public void HandlePointerEnter(PointerEventData eventData)
    {
        OnPointerEnter(eventData);
    }

    public void HandlePointerExit(PointerEventData eventData)
    {
        OnPointerExit(eventData);
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
}
