using System;
using System.Collections;
using UnityEngine;
using TMPro;

public class StoryOpeningSequenceController : MonoBehaviour
{
    [Header("Opening Panel")]
    [SerializeField] private CanvasGroup openingPanel;
    [SerializeField] private TMP_Text openingText;
    [SerializeField] private TMP_Text pressAnyKeyText;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float textFadeInSeconds = 1f;
    [SerializeField, Min(0f)] private float waitBeforePromptSeconds = 2f;
    [SerializeField, Min(0f)] private float textFadeOutSeconds = 0.5f;
    [SerializeField, Min(0f)] private float panelFadeOutSeconds = 1f;

    private Coroutine sequenceRoutine;

    private void Awake()
    {
        SetCanvasGroupAlpha(openingPanel, 0f);
        SetTextAlpha(openingText, 0f);
        SetTextAlpha(pressAnyKeyText, 0f);
    }

    private void OnEnable()
    {
        var transitionManager = SlideTransitionManager.Instance;
        if (transitionManager != null)
        {
            transitionManager.SlideOutCompleted += HandleSlideOutCompleted;
        }
    }

    private void OnDisable()
    {
        var transitionManager = SlideTransitionManager.Instance;
        if (transitionManager != null)
        {
            transitionManager.SlideOutCompleted -= HandleSlideOutCompleted;
        }
    }

    private void HandleSlideOutCompleted()
    {
        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
        }

        sequenceRoutine = StartCoroutine(OpeningSequence());
    }

    private IEnumerator OpeningSequence()
    {
        PlayerController.SetGlobalInputEnabled(false);
        var cameraController = FindFirstObjectByType<OrthographicCameraController>();
        cameraController?.SetCameraControlEnabled(false);

        yield return FadeCanvasGroup(openingPanel, 0f, 1f, textFadeInSeconds);
        yield return FadeText(openingText, 0f, 1f, textFadeInSeconds);

        if (waitBeforePromptSeconds > 0f)
        {
            yield return new WaitForSeconds(waitBeforePromptSeconds);
        }

        yield return FadeText(pressAnyKeyText, 0f, 1f, textFadeInSeconds);

        yield return new WaitUntil(AnyInputTriggered);

        yield return FadeText(pressAnyKeyText, 1f, 0f, textFadeOutSeconds);
        yield return FadeText(openingText, 1f, 0f, textFadeOutSeconds);
        yield return FadeCanvasGroup(openingPanel, 1f, 0f, panelFadeOutSeconds);

        PlayerController.SetGlobalInputEnabled(true);
        cameraController?.SetCameraControlEnabled(true);
    }

    private static bool AnyInputTriggered()
    {
        return Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);
    }

    private static void SetCanvasGroupAlpha(CanvasGroup canvasGroup, float alpha)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = alpha;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private static void SetTextAlpha(TMP_Text text, float alpha)
    {
        if (text == null)
        {
            return;
        }

        var color = text.color;
        color.a = alpha;
        text.color = color;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float from, float to, float duration)
    {
        if (canvasGroup == null)
        {
            yield break;
        }

        canvasGroup.alpha = from;
        if (duration <= 0f)
        {
            canvasGroup.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        canvasGroup.alpha = to;
    }

    private IEnumerator FadeText(TMP_Text text, float from, float to, float duration)
    {
        if (text == null)
        {
            yield break;
        }

        var color = text.color;
        color.a = from;
        text.color = color;

        if (duration <= 0f)
        {
            color.a = to;
            text.color = color;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            color.a = Mathf.Lerp(from, to, t);
            text.color = color;
            yield return null;
        }

        color.a = to;
        text.color = color;
    }
}
