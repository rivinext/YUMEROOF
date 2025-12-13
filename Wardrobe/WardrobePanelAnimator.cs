using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(RectTransform))]
public class WardrobePanelAnimator : MonoBehaviour
{
    [SerializeField] private RectTransform panel;
    [SerializeField] private bool startVisible;
    [SerializeField] private Vector2 hiddenAnchoredPosition = new Vector2(0f, -1080f);
    [SerializeField] private Vector2 shownAnchoredPosition = Vector2.zero;
    [SerializeField, Min(0f)] private float animationDuration = 0.35f;
    [SerializeField] private AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [System.Serializable]
    public class VisibilityChangedEvent : UnityEvent<bool> { }

    [SerializeField] private VisibilityChangedEvent onVisibilityChanged = new VisibilityChangedEvent();

    private Coroutine animationRoutine;
    private bool targetShown;
    private bool isShown;

    public bool IsShown
    {
        get { return isShown; }
    }

    public VisibilityChangedEvent VisibilityChanged
    {
        get { return onVisibilityChanged; }
    }

    private void Reset()
    {
        panel = GetComponent<RectTransform>();
        shownAnchoredPosition = panel.anchoredPosition;
    }

    private void Awake()
    {
        if (panel == null)
        {
            panel = GetComponent<RectTransform>();
        }

        isShown = startVisible;
        targetShown = isShown;
        SetAnchoredPosition(isShown ? shownAnchoredPosition : hiddenAnchoredPosition);
    }

    public void Show(bool instant = false)
    {
        PlayAnimation(true, instant);
    }

    public void Hide(bool instant = false)
    {
        PlayAnimation(false, instant);
    }

    public void Toggle(bool instant = false)
    {
        PlayAnimation(!targetShown, instant);
    }

    private void PlayAnimation(bool show, bool instant)
    {
        if (panel == null)
        {
            return;
        }

        targetShown = show;

        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        if (instant || animationDuration <= 0f)
        {
            isShown = targetShown;
            SetAnchoredPosition(isShown ? shownAnchoredPosition : hiddenAnchoredPosition);
            onVisibilityChanged.Invoke(isShown);
            return;
        }

        animationRoutine = StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        Vector2 start = panel.anchoredPosition;
        Vector2 end = targetShown ? shownAnchoredPosition : hiddenAnchoredPosition;
        float duration = Mathf.Max(0.0001f, animationDuration);
        float time = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(time / duration);
            float eased = easing != null ? easing.Evaluate(normalized) : normalized;
            panel.anchoredPosition = Vector2.LerpUnclamped(start, end, eased);
            yield return null;
        }

        panel.anchoredPosition = end;
        animationRoutine = null;
        isShown = targetShown;
        onVisibilityChanged.Invoke(isShown);
    }

    private void SetAnchoredPosition(Vector2 position)
    {
        panel.anchoredPosition = position;
    }
}
