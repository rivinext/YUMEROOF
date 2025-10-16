using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class SettingsPanelAnimator : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private float closedPositionX = 673.2f;
    [SerializeField] private float openPositionX = 0f;
    [SerializeField] private float anchoredY = 0f;
    [SerializeField] private AnimationCurve slideInXCurve = AnimationCurve.Linear(0f, 0f, 0.3f, 1f);
    [SerializeField] private AnimationCurve slideOutXCurve = AnimationCurve.Linear(0f, 0f, 0.3f, 1f);
    [SerializeField, HideInInspector] private bool isAnchoredYInitialized = false;

    [Header("Control")]
    [SerializeField] private Button toggleButton;
    [SerializeField] private bool startOpen = false;

    private RectTransform rectTransform;
    private Coroutine animationCoroutine;
    private bool isOpen;

    private const float DefaultDuration = 0.25f;

    private void Reset()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            return;
        }

        openPositionX = rectTransform.anchoredPosition.x;
        anchoredY = rectTransform.anchoredPosition.y;
        isAnchoredYInitialized = true;
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

        if (startOpen)
        {
            SnapOpen();
        }
        else
        {
            SnapClosed();
        }

        RegisterToggleButton();
    }

    private void OnEnable()
    {
        RegisterToggleButton();
    }

    private void OnDisable()
    {
        UnregisterToggleButton();
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

    public bool IsOpen => isOpen;

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

    private void RegisterToggleButton()
    {
        if (toggleButton == null)
        {
            return;
        }

        toggleButton.onClick.RemoveListener(TogglePanel);
        toggleButton.onClick.AddListener(TogglePanel);
    }

    private void UnregisterToggleButton()
    {
        if (toggleButton == null)
        {
            return;
        }

        toggleButton.onClick.RemoveListener(TogglePanel);
    }

    public bool IsOpen => isOpen;
}
