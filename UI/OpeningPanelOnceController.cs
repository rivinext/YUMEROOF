using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class OpeningPanelOnceController : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;
    [SerializeField] private CanvasGroup openingTextCanvasGroup;
    [SerializeField, Min(0f)] private float openingTextFadeDuration = 0.5f;

    private Coroutine openingTextCoroutine;

    void OnEnable()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }

        var slideManager = SlideTransitionManager.Instance;
        if (slideManager != null)
        {
            slideManager.SlideOutCompleted += HandleSlideOutCompleted;
        }
    }

    void OnDisable()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
        }

        var slideManager = SlideTransitionManager.Instance;
        if (slideManager != null)
        {
            slideManager.SlideOutCompleted -= HandleSlideOutCompleted;
        }

        StopOpeningTextFade();
    }

    public void ClosePanel()
    {
        var saveGameManager = SaveGameManager.Instance;
        saveGameManager.HasSeenOpeningPanel = true;
        var slotKey = saveGameManager.CurrentSlotKey;
        if (!string.IsNullOrEmpty(slotKey))
        {
            saveGameManager.Save(slotKey);
        }

        SetPanelVisible(false);
    }

    public void ShowIfNewSave(bool createdNewSave)
    {
        SetPanelVisible(createdNewSave);

        if (!createdNewSave)
        {
            return;
        }

        PrepareOpeningTextCanvasGroup();

        var slideManager = SlideTransitionManager.Instance;
        if (slideManager == null || !slideManager.IsAnyPanelOpen)
        {
            StartOpeningTextFade();
        }
    }

    void SetPanelVisible(bool isVisible)
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(isVisible);
        }

        if (!isVisible)
        {
            StopOpeningTextFade();
            ResetOpeningTextCanvasGroup();
        }
    }

    void HandleSlideOutCompleted()
    {
        StartOpeningTextFade();
    }

    void PrepareOpeningTextCanvasGroup()
    {
        if (openingTextCanvasGroup == null)
        {
            return;
        }

        openingTextCanvasGroup.DOKill();
        openingTextCanvasGroup.alpha = 0f;
        openingTextCanvasGroup.blocksRaycasts = false;
        openingTextCanvasGroup.interactable = false;
    }

    void ResetOpeningTextCanvasGroup()
    {
        if (openingTextCanvasGroup == null)
        {
            return;
        }

        openingTextCanvasGroup.DOKill();
        openingTextCanvasGroup.alpha = 0f;
        openingTextCanvasGroup.blocksRaycasts = false;
        openingTextCanvasGroup.interactable = false;
    }

    void StartOpeningTextFade()
    {
        if (openingTextCanvasGroup == null)
        {
            return;
        }

        StopOpeningTextFade();
        openingTextCoroutine = StartCoroutine(FadeInOpeningText());
    }

    void StopOpeningTextFade()
    {
        if (openingTextCoroutine != null)
        {
            StopCoroutine(openingTextCoroutine);
            openingTextCoroutine = null;
        }

        if (openingTextCanvasGroup != null)
        {
            openingTextCanvasGroup.DOKill();
        }
    }

    IEnumerator FadeInOpeningText()
    {
        yield return new WaitForSecondsRealtime(1f);

        if (openingTextCanvasGroup == null)
        {
            yield break;
        }

        openingTextCanvasGroup.DOKill();
        openingTextCanvasGroup.blocksRaycasts = true;
        openingTextCanvasGroup.interactable = true;
        openingTextCanvasGroup.DOFade(1f, openingTextFadeDuration).SetUpdate(true);
        openingTextCoroutine = null;
    }
}
