using System.Collections;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class CameraControlPanelAnimator : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private float closedPositionX = 1030f;
    [SerializeField] private float openPositionX = 0f;
    [SerializeField] private float anchoredY = 0f;
    [SerializeField] private AnimationCurve slideInXCurve = AnimationCurve.Linear(0f, 0f, 0.25f, 1f);
    [SerializeField] private AnimationCurve slideOutXCurve = AnimationCurve.Linear(0f, 0f, 0.25f, 1f);

    [SerializeField, HideInInspector] private bool isAnchoredYInitialized = false;

    private RectTransform rectTransform;
    private Coroutine animationCoroutine;
    private bool isOpen;

    private const float DefaultDuration = 0.25f;

    private void Reset()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            openPositionX = rectTransform.anchoredPosition.x;
            anchoredY = rectTransform.anchoredPosition.y;
            isAnchoredYInitialized = true;
        }
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (!isAnchoredYInitialized && rectTransform != null)
        {
            anchoredY = rectTransform.anchoredPosition.y;
            isAnchoredYInitialized = true;
        }

        UIPanelExclusionManager.Instance?.Register(this);
        SnapToTarget();
    }

    private void OnEnable()
    {
        SnapToTarget();
    }

    private void OnDisable()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
    }

    public void TogglePanel()
    {
        if (isOpen)
        {
            ClosePanel();
        }
        else
        {
            OpenPanel();
        }
    }

    public void OpenPanel()
    {
        if (rectTransform == null)
        {
            return;
        }

        if (isOpen && animationCoroutine == null)
        {
            return;
        }

        UIPanelExclusionManager.Instance?.NotifyOpened(this);
        PlayAnimation(slideInXCurve, openPositionX, true);
    }

    public void ClosePanel()
    {
        if (rectTransform == null)
        {
            return;
        }

        if (!isOpen && animationCoroutine == null)
        {
            return;
        }

        PlayAnimation(slideOutXCurve, closedPositionX, false);
    }

    public void SnapOpen()
    {
        isOpen = true;
        SetPosition(openPositionX);
    }

    public void SnapClosed()
    {
        isOpen = false;
        SetPosition(closedPositionX);
    }

    private void PlayAnimation(AnimationCurve curve, float targetX, bool targetState)
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }

        float startX = rectTransform.anchoredPosition.x;
        animationCoroutine = StartCoroutine(AnimatePanel(curve, startX, targetX, targetState));
    }

    private IEnumerator AnimatePanel(AnimationCurve curve, float startX, float endX, bool targetState)
    {
        isOpen = targetState;

        float duration = GetCurveDuration(curve);
        if (duration <= Mathf.Epsilon)
        {
            SetPosition(endX);
            animationCoroutine = null;
            yield break;
        }

        float time = 0f;
        while (time < duration)
        {
            float curveValue = EvaluateCurve(curve, time, duration);
            float newX = Mathf.LerpUnclamped(startX, endX, curveValue);
            SetPosition(newX);

            yield return null;
            time += Time.unscaledDeltaTime;
        }

        SetPosition(endX);
        animationCoroutine = null;
    }

    private void SetPosition(float x)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchoredPosition = new Vector2(x, anchoredY);
    }

    private void SnapToTarget()
    {
        SetPosition(isOpen ? openPositionX : closedPositionX);
    }

    private static float GetCurveDuration(AnimationCurve curve)
    {
        if (curve == null || curve.length == 0)
        {
            return DefaultDuration;
        }

        return Mathf.Max(curve[curve.length - 1].time, 0f);
    }

    private static float EvaluateCurve(AnimationCurve curve, float time, float duration)
    {
        if (curve == null || curve.length == 0)
        {
            return Mathf.Clamp01(duration <= Mathf.Epsilon ? 1f : time / duration);
        }

        float clampedTime = Mathf.Clamp(time, 0f, duration);
        return curve.Evaluate(clampedTime);
    }

    public bool IsOpen => isOpen;
}
