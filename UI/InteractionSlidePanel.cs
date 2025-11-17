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
    [Header("Panel")]
    [SerializeField] private RectTransform target;

    [Header("Position Settings")]
    [SerializeField] private float openAnchoredX = 0f;
    [SerializeField] private float closedAnchoredX = -800f;
    [SerializeField] private float anchoredY = 0f;

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.35f;
    [SerializeField] private AnimationCurve slideInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve slideOutCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Tween activeTween;
    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        EnsureTarget();
        ApplyAnchoredPosition(closedAnchoredX, anchoredY, true);
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
            ApplyAnchoredPosition(isOpen ? openAnchoredX : closedAnchoredX, anchoredY, false);
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
        ApplyAnchoredPosition(isOpen ? openAnchoredX : closedAnchoredX, anchoredY, true);
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
        target.anchoredPosition = new Vector2(closedAnchoredX, anchoredY);

        if (animationDuration <= 0f)
        {
            target.anchoredPosition = endPos;
            return;
        }

        activeTween = target.DOAnchorPos(endPos, animationDuration)
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
        Vector2 endPos = new(closedAnchoredX, anchoredY);

        if (animationDuration <= 0f)
        {
            target.anchoredPosition = endPos;
            target.gameObject.SetActive(false);
            return;
        }

        activeTween = target.DOAnchorPos(endPos, animationDuration)
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
        ApplyAnchoredPosition(closedAnchoredX, anchoredY, true);
        target.gameObject.SetActive(false);
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
    }
}
