using DG.Tweening;
using UnityEngine;

/// <summary>
/// 左側からスライドインするインタラクション用パネルを制御します。
/// Inspector で開閉時の X 座標やアンカー Y、アニメーションカーブを設定できます。
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class InteractionSlidePanel : MonoBehaviour
{
    private enum TransitionMode
    {
        Slide,
        Fade
    }

    [Header("Panel")]
    [SerializeField] private RectTransform target;
    [SerializeField] private CanvasGroup targetCanvasGroup;

    [Header("Position Settings")]
    [SerializeField] private float openAnchoredX = 0f;
    [SerializeField] private float closedAnchoredX = -800f;
    [SerializeField] private float anchoredY = 0f;

    [Header("Animation Settings")]
    [SerializeField] private TransitionMode transitionMode = TransitionMode.Slide;
    [SerializeField] private float animationDuration = 0.35f;
    [SerializeField] private AnimationCurve slideInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve slideOutCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Tween activeTween;
    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        EnsureTarget();
        ApplyAnchoredPosition(GetClosedX(), anchoredY, true);
        ApplyAlpha(isOpen ? 1f : 0f);
        if (Application.isPlaying && target != null)
        {
            target.gameObject.SetActive(false);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureTarget();
        if (target == null)
            return;

        if (!Application.isPlaying)
        {
            ApplyAnchoredPosition(isOpen ? openAnchoredX : GetClosedX(), anchoredY, false);
            ApplyAlpha(isOpen ? 1f : 0f);
        }
    }
#endif

    /// <summary>
    /// アンカーポジションの Y を変更します。
    /// </summary>
    /// <param name="y">新しい Y 値。</param>
    public void SetAnchorY(float y)
    {
        anchoredY = y;
        ApplyAnchoredPosition(isOpen ? openAnchoredX : GetClosedX(), anchoredY, true);
    }

    /// <summary>
    /// パネルをスライドインさせます。
    /// </summary>
    public void SlideIn()
    {
        if (target == null)
            return;

        activeTween?.Kill();
        target.gameObject.SetActive(true);
        isOpen = true;
        Vector2 endPos = new(openAnchoredX, anchoredY);
        target.anchoredPosition = new Vector2(GetClosedX(), anchoredY);

        if (transitionMode == TransitionMode.Fade)
        {
            target.anchoredPosition = endPos;
            ApplyAlpha(0f);
        }

        if (animationDuration <= 0f)
        {
            target.anchoredPosition = endPos;
            ApplyAlpha(1f);
            return;
        }

        activeTween = transitionMode == TransitionMode.Fade
            ? targetCanvasGroup.DOFade(1f, animationDuration)
                .SetEase(slideInCurve)
                .OnKill(() => activeTween = null)
            : target.DOAnchorPos(endPos, animationDuration)
                .SetEase(slideInCurve)
                .OnKill(() => activeTween = null);
    }

    /// <summary>
    /// パネルをスライドアウトさせます。
    /// </summary>
    public void SlideOut()
    {
        if (target == null || !isOpen)
            return;

        activeTween?.Kill();
        isOpen = false;
        Vector2 endPos = new(GetClosedX(), anchoredY);

        if (animationDuration <= 0f)
        {
            target.anchoredPosition = endPos;
            ApplyAlpha(0f);
            target.gameObject.SetActive(false);
            return;
        }

        activeTween = transitionMode == TransitionMode.Fade
            ? targetCanvasGroup.DOFade(0f, animationDuration)
                .SetEase(slideOutCurve)
                .OnComplete(() =>
                {
                    target.gameObject.SetActive(false);
                    activeTween = null;
                })
            : target.DOAnchorPos(endPos, animationDuration)
                .SetEase(slideOutCurve)
                .OnComplete(() =>
                {
                    target.gameObject.SetActive(false);
                    activeTween = null;
                });
    }

    /// <summary>
    /// アニメーションを使わず即座に閉じます。
    /// </summary>
    public void CloseImmediate()
    {
        if (target == null)
            return;

        activeTween?.Kill();
        isOpen = false;
        ApplyAnchoredPosition(GetClosedX(), anchoredY, true);
        ApplyAlpha(0f);
        target.gameObject.SetActive(false);
    }

    private void ApplyAlpha(float alpha)
    {
        if (targetCanvasGroup == null)
            return;

        targetCanvasGroup.alpha = alpha;
    }

    private float GetClosedX()
    {
        return transitionMode == TransitionMode.Fade ? openAnchoredX : closedAnchoredX;
    }

    private void ApplyAnchoredPosition(float x, float y, bool respectGameObjectState)
    {
        if (target == null)
            return;

        Vector2 pos = target.anchoredPosition;
        pos.x = x;
        pos.y = y;

        bool wasInactive = !target.gameObject.activeSelf;
        if (!target.gameObject.activeSelf && !Application.isPlaying)
        {
            target.gameObject.SetActive(true);
        }

        target.anchoredPosition = pos;

        if (respectGameObjectState && wasInactive && Application.isPlaying)
        {
            target.gameObject.SetActive(false);
        }
    }

    private void EnsureTarget()
    {
        if (target == null)
        {
            target = GetComponent<RectTransform>();
        }

        if (targetCanvasGroup == null && target != null)
        {
            targetCanvasGroup = target.GetComponent<CanvasGroup>();
            if (targetCanvasGroup == null)
            {
                targetCanvasGroup = target.gameObject.AddComponent<CanvasGroup>();
            }
        }
    }
}
