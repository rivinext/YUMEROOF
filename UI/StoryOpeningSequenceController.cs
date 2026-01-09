using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StoryOpeningSequenceController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup openingPanelGroup;
    [SerializeField] private CanvasGroup openingTextGroup;
    [SerializeField] private TMP_Text openingText;

    [Header("Text")]
    [SerializeField, TextArea] private string openingMessage = "Welcome.";
    [SerializeField] private string pressAnyKeyMessage = "Press any key";

    [Header("Timing")]
    [SerializeField, Min(0f)] private float textFadeDuration = 1f;
    [SerializeField, Min(0f)] private float holdSeconds = 2f;
    [SerializeField, Min(0f)] private float panelFadeDuration = 1f;

    private Coroutine sequenceCoroutine;
    private bool hasPlayed;

    private void OnEnable()
    {
        if (SlideTransitionManager.Instance != null)
        {
            SlideTransitionManager.Instance.SlideOutCompleted += HandleSlideOutCompleted;
        }
    }

    private void OnDisable()
    {
        if (SlideTransitionManager.Instance != null)
        {
            SlideTransitionManager.Instance.SlideOutCompleted -= HandleSlideOutCompleted;
        }
    }

    private void HandleSlideOutCompleted()
    {
        if (hasPlayed || !ShouldPlaySequence())
            return;

        if (sequenceCoroutine != null)
            StopCoroutine(sequenceCoroutine);

        sequenceCoroutine = StartCoroutine(PlaySequence());
    }

    private bool ShouldPlaySequence()
    {
        var initializer = GameSessionInitializer.Instance;
        if (initializer == null || !initializer.CreatedNewSave)
            return false;

        if (string.IsNullOrEmpty(initializer.LoadedSlotKey))
            return false;

        if (!initializer.LoadedSlotKey.StartsWith("Story", StringComparison.OrdinalIgnoreCase))
            return false;

        if (SceneManager.GetActiveScene().name == "MainMenu")
            return false;

        return true;
    }

    private IEnumerator PlaySequence()
    {
        hasPlayed = true;

        if (openingPanelGroup != null)
        {
            openingPanelGroup.alpha = 1f;
            openingPanelGroup.blocksRaycasts = true;
            openingPanelGroup.interactable = true;
        }

        if (openingTextGroup != null)
        {
            openingTextGroup.alpha = 0f;
        }

        if (openingText != null)
        {
            openingText.text = openingMessage;
        }

        if (openingTextGroup != null)
        {
            yield return FadeCanvasGroup(openingTextGroup, 0f, 1f, textFadeDuration);
        }

        if (holdSeconds > 0f)
            yield return new WaitForSecondsRealtime(holdSeconds);

        if (openingText != null)
        {
            openingText.text = pressAnyKeyMessage;
        }

        yield return new WaitUntil(() => Input.anyKeyDown);

        if (openingTextGroup != null)
        {
            yield return FadeCanvasGroup(openingTextGroup, openingTextGroup.alpha, 0f, textFadeDuration);
        }

        if (openingPanelGroup != null)
        {
            yield return FadeCanvasGroup(openingPanelGroup, openingPanelGroup.alpha, 0f, panelFadeDuration);
            openingPanelGroup.blocksRaycasts = false;
            openingPanelGroup.interactable = false;
        }
    }

    private static IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null)
            yield break;

        if (duration <= 0f)
        {
            group.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        group.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        group.alpha = to;
    }
}
