using System.Collections;
using UnityEngine;

public class StoryOpeningSequenceController : MonoBehaviour
{
    [Header("Canvas Groups")]
    [SerializeField] private CanvasGroup panelGroup;
    [SerializeField] private CanvasGroup mainTextGroup;
    [SerializeField] private CanvasGroup pressAnyKeyGroup;

    [Header("Timings")]
    [SerializeField] private float textFadeDuration = 1f;
    [SerializeField] private float waitBeforePromptSeconds = 2f;
    [SerializeField] private float promptFadeDuration = 0.5f;
    [SerializeField] private float panelFadeDuration = 1f;

    private Coroutine sequenceCoroutine;

    private void OnEnable()
    {
        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
        }

        sequenceCoroutine = StartCoroutine(RunSequence());
    }

    private void OnDisable()
    {
        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
            sequenceCoroutine = null;
        }
    }

    private IEnumerator RunSequence()
    {
        if (!IsNewGameSession())
        {
            HideAll();
            yield break;
        }

        if (panelGroup != null)
        {
            panelGroup.gameObject.SetActive(true);
            panelGroup.alpha = 1f;
            panelGroup.blocksRaycasts = true;
            panelGroup.interactable = true;
        }

        SetGroupAlpha(mainTextGroup, 0f);
        SetGroupAlpha(pressAnyKeyGroup, 0f);

        yield return FadeCanvasGroup(mainTextGroup, 0f, 1f, textFadeDuration);
        yield return new WaitForSeconds(waitBeforePromptSeconds);

        yield return FadeCanvasGroup(pressAnyKeyGroup, 0f, 1f, promptFadeDuration);

        while (!Input.anyKeyDown)
        {
            yield return null;
        }

        yield return FadeCanvasGroup(mainTextGroup, mainTextGroup != null ? mainTextGroup.alpha : 1f, 0f, textFadeDuration);
        yield return FadeCanvasGroup(pressAnyKeyGroup, pressAnyKeyGroup != null ? pressAnyKeyGroup.alpha : 1f, 0f, promptFadeDuration);
        yield return FadeCanvasGroup(panelGroup, panelGroup != null ? panelGroup.alpha : 1f, 0f, panelFadeDuration);

        if (panelGroup != null)
        {
            panelGroup.blocksRaycasts = false;
            panelGroup.interactable = false;
            panelGroup.gameObject.SetActive(false);
        }
    }

    private bool IsNewGameSession()
    {
        var saveManager = SaveGameManager.Instance;
        if (saveManager == null)
        {
            return false;
        }

        var slotKey = saveManager.CurrentSlotKey;
        if (string.IsNullOrEmpty(slotKey))
        {
            return false;
        }

        if (saveManager.HasSlot(slotKey))
        {
            return false;
        }

        var metadata = saveManager.LoadMetadata(slotKey);
        return metadata == null;
    }

    private void HideAll()
    {
        SetGroupAlpha(mainTextGroup, 0f);
        SetGroupAlpha(pressAnyKeyGroup, 0f);
        SetGroupAlpha(panelGroup, 0f);

        if (panelGroup != null)
        {
            panelGroup.blocksRaycasts = false;
            panelGroup.interactable = false;
            panelGroup.gameObject.SetActive(false);
        }
    }

    private void SetGroupAlpha(CanvasGroup group, float alpha)
    {
        if (group == null)
        {
            return;
        }

        group.alpha = alpha;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            group.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        group.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        group.alpha = to;
    }
}
