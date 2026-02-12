using DG.Tweening;
using UnityEngine;

/// <summary>
/// フェードイン/アウトで表示するインタラクション用パネルを制御します。
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(CanvasGroup))]
public class InteractionFadePanel : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private CanvasGroup target;

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.25f;
    [SerializeField] private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Tween activeTween;
    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        EnsureTarget();
        ApplyState(0f, false, true);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureTarget();
        if (target == null)
            return;

        if (!Application.isPlaying)
        {
            ApplyState(isOpen ? 1f : 0f, isOpen, false);
        }
    }
#endif

    public void FadeIn()
    {
        if (target == null)
            return;

        activeTween?.Kill();
        isOpen = true;
        ApplyState(0f, true, true);

        if (animationDuration <= 0f)
        {
            ApplyState(1f, true, true);
            return;
        }

        activeTween = target.DOFade(1f, animationDuration)
            .SetEase(fadeInCurve)
            .OnKill(() => activeTween = null);
    }

    public void FadeOut()
    {
        if (target == null || !isOpen)
            return;

        activeTween?.Kill();
        isOpen = false;

        if (animationDuration <= 0f)
        {
            ApplyState(0f, false, true);
            return;
        }

        activeTween = target.DOFade(0f, animationDuration)
            .SetEase(fadeOutCurve)
            .OnComplete(() =>
            {
                ApplyState(0f, false, true);
                activeTween = null;
            });
    }

    public void CloseImmediate()
    {
        if (target == null)
            return;

        activeTween?.Kill();
        isOpen = false;
        ApplyState(0f, false, true);
    }

    private void ApplyState(float alpha, bool visible, bool allowDeactivate)
    {
        if (target == null)
            return;

        if (!target.gameObject.activeSelf)
        {
            target.gameObject.SetActive(true);
        }

        target.alpha = alpha;
        target.interactable = visible;
        target.blocksRaycasts = visible;

        if (!visible && allowDeactivate)
        {
            target.gameObject.SetActive(false);
        }
    }

    private void EnsureTarget()
    {
        if (target == null)
        {
            target = GetComponent<CanvasGroup>();
        }
    }
}
