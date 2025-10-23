using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ConfirmationPopup : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;
    [SerializeField] private Image backdropImage;
    [SerializeField] private Sprite backdropSprite;
    [SerializeField] private RectTransform panelRectTransform;
    [SerializeField] private AnimationCurve slideCurve = AnimationCurve.Linear(0f, -500f, 1f, 0f);

    private Action onYes;
    private Button backdropButton;
    private Canvas parentCanvas;
    private bool isOpen;
    private Vector2 onScreenPosition;
    private float currentCurveTime;
    private Coroutine slideCoroutine;

    private void Awake()
    {
        // Ensure required UI references exist even if they were not set in the inspector
        if (messageText == null)
            messageText = GetComponentInChildren<TMP_Text>(true);

        if (yesButton == null || noButton == null)
        {
            var buttons = GetComponentsInChildren<Button>(true);
            if (buttons.Length > 0 && yesButton == null)
                yesButton = buttons[0];
            if (buttons.Length > 1 && noButton == null)
                noButton = buttons[1];
        }

        if (yesButton != null)
            yesButton.onClick.AddListener(HandleYes);
        if (noButton != null)
            noButton.onClick.AddListener(HandleNo);

        if (panelRectTransform == null)
        {
            panelRectTransform = GetComponent<RectTransform>();
        }

        if (panelRectTransform != null)
        {
            onScreenPosition = panelRectTransform.anchoredPosition;
            currentCurveTime = 0f;
            ApplySlidePosition(0f);
        }

        if (backdropImage != null)
        {
            backdropButton = backdropImage.GetComponent<Button>();
            parentCanvas = backdropImage.canvas;
            if (backdropSprite != null)
                backdropImage.sprite = backdropSprite;
            SetBackdropActive(false);
            if (backdropButton != null && !backdropButton.TryGetComponent<UIButtonSoundBlocker>(out _))
            {
                backdropButton.gameObject.AddComponent<UIButtonSoundBlocker>();
            }
        }
        else
        {
            parentCanvas = GetComponentInParent<Canvas>();
        }
    }

    private void OnDestroy()
    {
        if (yesButton != null)
            yesButton.onClick.RemoveListener(HandleYes);
        if (noButton != null)
            noButton.onClick.RemoveListener(HandleNo);
        if (backdropButton != null)
            backdropButton.onClick.RemoveListener(HandleBackdropClicked);
    }

    public void Open(string message, Action onYes)
    {
        isOpen = true;
        gameObject.SetActive(true);
        StartSlide(true);
        if (messageText != null)
            messageText.text = message;
        this.onYes = onYes;
        if (backdropSprite != null && backdropImage != null)
            backdropImage.sprite = backdropSprite;
        SetBackdropActive(true);
        RegisterBackdropListener();
    }

    private void HandleYes()
    {
        if (!isOpen)
            return;
        onYes?.Invoke();
        Close();
    }

    private void HandleNo()
    {
        if (!isOpen)
            return;
        Close();
    }

    public void Close()
    {
        if (!isOpen)
            return;

        isOpen = false;
        UnregisterBackdropListener();
        SetBackdropActive(false);

        StartSlide(false);
        onYes = null;
    }

    private void Update()
    {
        if (!isOpen)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleNo();
            return;
        }

        if (backdropButton != null)
            return;

        if (panelRectTransform == null)
            return;

        bool pointerDown = false;
        Vector2 pointerPosition = Vector2.zero;

        if (Input.GetMouseButtonDown(0))
        {
            pointerDown = true;
            pointerPosition = Input.mousePosition;
        }
        else if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                pointerDown = true;
                pointerPosition = touch.position;
            }
        }

        if (!pointerDown)
            return;

        Camera uiCamera = parentCanvas != null ? parentCanvas.worldCamera : null;
        if (!RectTransformUtility.RectangleContainsScreenPoint(panelRectTransform, pointerPosition, uiCamera))
            HandleNo();
    }

    private void RegisterBackdropListener()
    {
        if (backdropButton == null)
            return;
        backdropButton.onClick.RemoveListener(HandleBackdropClicked);
        backdropButton.onClick.AddListener(HandleBackdropClicked);
    }

    private void UnregisterBackdropListener()
    {
        if (backdropButton == null)
            return;
        backdropButton.onClick.RemoveListener(HandleBackdropClicked);
    }

    private void HandleBackdropClicked()
    {
        HandleNo();
    }

    private void SetBackdropActive(bool isActive)
    {
        if (backdropImage == null)
            return;

        if (backdropImage.gameObject.activeSelf != isActive)
        {
            backdropImage.gameObject.SetActive(isActive);
        }
        else
        {
            backdropImage.enabled = isActive;
        }
    }

    private void StartSlide(bool opening)
    {
        if (panelRectTransform == null)
        {
            currentCurveTime = 0f;
            if (!opening)
                OnSlideOutComplete();
            return;
        }

        if (slideCoroutine != null)
        {
            StopCoroutine(slideCoroutine);
            slideCoroutine = null;
        }

        bool hasSlide = TryGetSlideParameters(out float duration, out float closedOffset, out float openOffset);
        float targetTime = opening ? duration : 0f;

        if (!hasSlide)
        {
            currentCurveTime = Mathf.Max(0f, targetTime);

            if (panelRectTransform != null)
            {
                float offset = opening ? openOffset : closedOffset;
                panelRectTransform.anchoredPosition = new Vector2(onScreenPosition.x + offset, onScreenPosition.y);
            }

            if (!opening)
                OnSlideOutComplete();
            return;
        }

        slideCoroutine = StartCoroutine(SlideRoutine(opening, duration));
    }

    private System.Collections.IEnumerator SlideRoutine(bool opening, float duration)
    {
        float targetTime = opening ? duration : 0f;
        float direction = opening ? 1f : -1f;
        float currentTime = Mathf.Clamp(currentCurveTime, 0f, duration);

        ApplySlidePosition(currentTime);

        while ((direction > 0f && currentTime < targetTime) || (direction < 0f && currentTime > targetTime))
        {
            yield return null;

            currentTime += Time.unscaledDeltaTime * direction;
            currentTime = Mathf.Clamp(currentTime, 0f, duration);

            ApplySlidePosition(currentTime);
        }

        ApplySlidePosition(targetTime);
        slideCoroutine = null;

        if (!opening)
            OnSlideOutComplete();
    }

    private void ApplySlidePosition(float curveTimeSeconds)
    {
        if (panelRectTransform == null)
            return;

        float time = Mathf.Max(0f, curveTimeSeconds);
        if (TryGetSlideParameters(out float duration, out _, out _))
        {
            time = Mathf.Clamp(time, 0f, duration);
        }

        currentCurveTime = time;

        float offset = slideCurve != null ? slideCurve.Evaluate(currentCurveTime) : 0f;
        panelRectTransform.anchoredPosition = new Vector2(onScreenPosition.x + offset, onScreenPosition.y);
    }

    private void OnSlideOutComplete()
    {
        currentCurveTime = 0f;

        if (panelRectTransform != null)
        {
            float offscreenOffset = 0f;
            if (TryGetSlideParameters(out _, out float closedOffset, out _))
            {
                offscreenOffset = closedOffset;
            }
            else if (slideCurve != null)
            {
                offscreenOffset = slideCurve.Evaluate(currentCurveTime);
            }
            panelRectTransform.anchoredPosition = new Vector2(onScreenPosition.x + offscreenOffset, onScreenPosition.y);
        }

        gameObject.SetActive(false);
    }

    private bool TryGetSlideParameters(out float duration, out float closedOffset, out float openOffset)
    {
        duration = 0f;
        closedOffset = 0f;
        openOffset = 0f;

        if (slideCurve == null)
            return false;

        Keyframe[] keys = slideCurve.keys;
        if (keys == null || keys.Length == 0)
            return false;

        closedOffset = slideCurve.Evaluate(0f);

        Keyframe lastKey = keys[keys.Length - 1];
        duration = lastKey.time;
        openOffset = slideCurve.Evaluate(duration);

        if (keys.Length < 2 || duration <= 0f)
            return false;

        return true;
    }
}
