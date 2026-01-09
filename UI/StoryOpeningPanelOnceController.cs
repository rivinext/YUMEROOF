using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoryOpeningPanelOnceController : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;
    [Header("Text Fade In")]
    [SerializeField] private TMP_Text openingText;
    [SerializeField] private CanvasGroup openingTextCanvasGroup;
    [SerializeField, Min(0f)] private float textFadeDelaySeconds = 1f;
    [SerializeField, Min(0f)] private float textFadeDurationSeconds = 0.5f;
    [Header("Panel Fade Out")]
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField, Min(0f)] private float panelFadeOutDurationSeconds = 0.35f;

    // SaveGameManager が ApplyManagers(Story) でスロットごとに復元してくれる想定の値
    public bool HasSeenOpeningPanel { get; set; }

    private bool isWaitingForSlideOut;
    private Coroutine waitCoroutine;
    private Coroutine textFadeCoroutine;
    private Tween textFadeTween;
    private Tween panelFadeTween;

    void OnEnable()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }

        ResetVisualState();

        // ✅ slotKey が空のタイミングで判定してしまうのを防ぐ
        if (waitCoroutine != null)
        {
            StopCoroutine(waitCoroutine);
            waitCoroutine = null;
        }
        waitCoroutine = StartCoroutine(WaitForSlotAndTryShow());
    }

    void OnDisable()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
        }

        UnsubscribeFromSlideOut();

        if (waitCoroutine != null)
        {
            StopCoroutine(waitCoroutine);
            waitCoroutine = null;
        }

        StopTextFade();
        StopPanelFade();
    }

    private IEnumerator WaitForSlotAndTryShow()
    {
        // SaveGameManager とスロットキーが確定するまで待つ
        while (SaveGameManager.Instance == null ||
               string.IsNullOrEmpty(SaveGameManager.Instance.CurrentSlotKey))
        {
            yield return null;
        }

        // ここに来た時点で ApplyManagers が走って HasSeenOpeningPanel が復元されている可能性が高い
        TryShowPanelNow();
    }

    private void TryShowPanelNow()
    {
        var slotKey = SaveGameManager.Instance != null ? SaveGameManager.Instance.CurrentSlotKey : null;
        Debug.Log($"[StoryOpeningPanelOnceController] TryShowPanelNow: slotKey='{slotKey}', hasSeenOpeningPanel={HasSeenOpeningPanel}");

        if (!ShouldShowPanel())
        {
            Debug.Log($"[StoryOpeningPanelOnceController] willShow=false (slotKey='{slotKey}', hasSeenOpeningPanel={HasSeenOpeningPanel})");
            return;
        }

        Debug.Log($"[StoryOpeningPanelOnceController] willShow=true (slotKey='{slotKey}', hasSeenOpeningPanel={HasSeenOpeningPanel})");

        var slideManager = SlideTransitionManager.Instance;
        if (slideManager == null || slideManager.IsAnyPanelOpen)
        {
            // スライドマネージャが無い or パネルが開いているなら即表示
            ShowPanel();
            return;
        }

        // ✅ スライドアウト完了後に出したい（スライドアウトが発生しうる場合のみ）
        slideManager.SlideOutCompleted += HandleSlideOutCompleted;
        isWaitingForSlideOut = true;
    }

    private void HandleSlideOutCompleted()
    {
        // ここで再判定（ロード状況のズレ対策）
        UnsubscribeFromSlideOut();

        if (!ShouldShowPanel())
            return;

        ShowPanel();
    }

    private void UnsubscribeFromSlideOut()
    {
        if (!isWaitingForSlideOut)
            return;

        var slideManager = SlideTransitionManager.Instance;
        if (slideManager != null)
        {
            slideManager.SlideOutCompleted -= HandleSlideOutCompleted;
        }

        isWaitingForSlideOut = false;
    }

    private bool ShouldShowPanel()
    {
        var slotKey = SaveGameManager.Instance != null ? SaveGameManager.Instance.CurrentSlotKey : null;

        // Story スロット以外は出さない
        if (string.IsNullOrEmpty(slotKey) || !slotKey.StartsWith("Story", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // すでに見ていたら出さない
        return !HasSeenOpeningPanel;
    }

    private void ShowPanel()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        ResetPanelFadeState();
        StartTextFadeIn();
    }

    private void ClosePanel()
    {
        StopTextFade();
        FadeOutPanel();

        // ✅ 閉じたら「見た」扱いにしてスロットに保存
        Debug.Log($"[StoryOpeningPanelOnceController] Before setting hasSeenOpeningPanel=true (current={HasSeenOpeningPanel})");
        HasSeenOpeningPanel = true;
        Debug.Log($"[StoryOpeningPanelOnceController] After setting hasSeenOpeningPanel=true (current={HasSeenOpeningPanel})");

        var slotKey = SaveGameManager.Instance != null ? SaveGameManager.Instance.CurrentSlotKey : null;
        if (string.IsNullOrEmpty(slotKey))
        {
            Debug.Log("[StoryOpeningPanelOnceController] CurrentSlotKey is empty. Skipping SaveCurrentSlot.");
            return;
        }

        Debug.Log($"[StoryOpeningPanelOnceController] CurrentSlotKey='{slotKey}'. Saving current slot.");
        SaveGameManager.Instance.SaveCurrentSlot();
    }

    private void StartTextFadeIn()
    {
        if (openingText == null && openingTextCanvasGroup == null)
        {
            return;
        }

        StopTextFade();
        SetTextAlpha(0f);
        textFadeCoroutine = StartCoroutine(FadeTextAfterDelay());
    }

    private IEnumerator FadeTextAfterDelay()
    {
        if (textFadeDelaySeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(textFadeDelaySeconds);
        }

        if (openingTextCanvasGroup != null)
        {
            textFadeTween = openingTextCanvasGroup
                .DOFade(1f, textFadeDurationSeconds)
                .SetUpdate(true);
        }
        else if (openingText != null)
        {
            textFadeTween = DOTween.To(
                    () => openingText.alpha,
                    value => openingText.alpha = value,
                    1f,
                    textFadeDurationSeconds)
                .SetUpdate(true);
        }

        textFadeCoroutine = null;
    }

    private void StopTextFade()
    {
        if (textFadeCoroutine != null)
        {
            StopCoroutine(textFadeCoroutine);
            textFadeCoroutine = null;
        }

        if (textFadeTween != null && textFadeTween.IsActive())
        {
            textFadeTween.Kill();
            textFadeTween = null;
        }
    }

    private void FadeOutPanel()
    {
        if (panelRoot == null)
        {
            return;
        }

        StopPanelFade();

        if (panelCanvasGroup == null)
        {
            panelRoot.SetActive(false);
            return;
        }

        panelCanvasGroup.interactable = false;
        panelCanvasGroup.blocksRaycasts = false;
        panelFadeTween = panelCanvasGroup
            .DOFade(0f, panelFadeOutDurationSeconds)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                if (panelRoot != null)
                {
                    panelRoot.SetActive(false);
                }
            });
    }

    private void StopPanelFade()
    {
        if (panelFadeTween != null && panelFadeTween.IsActive())
        {
            panelFadeTween.Kill();
            panelFadeTween = null;
        }
    }

    private void ResetVisualState()
    {
        ResetPanelFadeState();
        SetTextAlpha(0f);
    }

    private void ResetPanelFadeState()
    {
        if (panelCanvasGroup == null)
        {
            return;
        }

        panelCanvasGroup.alpha = 1f;
        panelCanvasGroup.interactable = true;
        panelCanvasGroup.blocksRaycasts = true;
    }

    private void SetTextAlpha(float alpha)
    {
        if (openingTextCanvasGroup != null)
        {
            openingTextCanvasGroup.alpha = alpha;
        }
        else if (openingText != null)
        {
            openingText.alpha = alpha;
        }
    }
}
