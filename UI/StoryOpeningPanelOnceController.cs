using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StoryOpeningPanelOnceController : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;
    [SerializeField] private CanvasGroup tmpCanvasGroup;

    // SaveGameManager が ApplyManagers(Story) でスロットごとに復元してくれる想定の値
    public bool HasSeenOpeningPanel { get; set; }

    private bool isWaitingForSlideOut;
    private Coroutine waitCoroutine;
    private Coroutine tmpFadeCoroutine;

    void OnEnable()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }

        InitializeTmpFadeState();

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

        StopTmpFadeCoroutine();
        ResetTmpFadeState();
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
        if (slideManager != null)
        {
            if (slideManager.IsAnyPanelOpen())
            {
                Debug.Log("[StoryOpeningPanelOnceController] Slide panels already open or idle; showing panel immediately.");
                ShowPanel();
                return;
            }

            // ✅ スライドアウト完了後に出したい
            slideManager.SlideOutCompleted += HandleSlideOutCompleted;
            isWaitingForSlideOut = true;
        }
        else
        {
            // スライドマネージャが無いなら即表示
            ShowPanel();
        }
    }

    private void HandleSlideOutCompleted()
    {
        // ここで再判定（ロード状況のズレ対策）
        UnsubscribeFromSlideOut();

        if (!ShouldShowPanel())
            return;

        ShowPanel();
        StartTmpFadeSequence();
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

        InitializeTmpFadeState();
    }

    private void ClosePanel()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

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

    private void StartTmpFadeSequence()
    {
        StopTmpFadeCoroutine();

        if (tmpCanvasGroup == null)
        {
            return;
        }

        tmpFadeCoroutine = StartCoroutine(WaitAndFadeInTmp());
    }

    private IEnumerator WaitAndFadeInTmp()
    {
        yield return new WaitForSecondsRealtime(1f);

        float elapsed = 0f;
        const float fadeDuration = 0.4f;

        tmpCanvasGroup.alpha = 0f;
        tmpCanvasGroup.interactable = false;
        tmpCanvasGroup.blocksRaycasts = false;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            tmpCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        tmpCanvasGroup.alpha = 1f;
        tmpCanvasGroup.interactable = true;
        tmpCanvasGroup.blocksRaycasts = true;
        tmpFadeCoroutine = null;
    }

    private void StopTmpFadeCoroutine()
    {
        if (tmpFadeCoroutine == null)
        {
            return;
        }

        StopCoroutine(tmpFadeCoroutine);
        tmpFadeCoroutine = null;
    }

    private void InitializeTmpFadeState()
    {
        if (tmpCanvasGroup == null)
        {
            return;
        }

        tmpCanvasGroup.alpha = 0f;
        tmpCanvasGroup.interactable = false;
        tmpCanvasGroup.blocksRaycasts = false;
    }

    private void ResetTmpFadeState()
    {
        if (tmpCanvasGroup == null)
        {
            return;
        }

        tmpCanvasGroup.alpha = 0f;
        tmpCanvasGroup.interactable = false;
        tmpCanvasGroup.blocksRaycasts = false;
    }
}
