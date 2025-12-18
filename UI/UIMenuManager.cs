using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DG.Tweening;
using Steamworks;

/// <summary>
/// メインメニューのUIパネル管理とPost Processing制御
/// </summary>
public class UIMenuManager : MonoBehaviour
{
    [System.Serializable]
    private struct ParallaxLayer
    {
        public RectTransform target;
        public Vector2 maxOffset;
        public float smoothTime;

        [HideInInspector] public Vector2 baseAnchoredPosition;
        [HideInInspector] public Vector2 velocity;
    }

    [Header("UI Panels")]
    [SerializeField] private MonoBehaviour creditPanel;
    [SerializeField] private PanelScaleAnimator optionPanel;
    [SerializeField] private UISlidePanel storySlotPanel;
    [SerializeField] private UISlidePanel creativeSlotPanel;

    [Header("Menu Buttons")]
    [SerializeField] private Button creditButton;
    [SerializeField] private Button optionButton;
    [SerializeField] private Button storyButton;
    [SerializeField] private Button creativeButton;
    [SerializeField] private Button exitButton;

    [Header("Wishlist")]
    [SerializeField] private Button wishlistButton;
    [SerializeField] private uint steamAppId;
    [SerializeField] private string fallbackStorePageUrl;

    [Header("Close Buttons")]
    [SerializeField] private Button creditCloseButton;
    [SerializeField] private Button optionCloseButton;
    [SerializeField] private Button storyCloseButton;
    [SerializeField] private Button creativeCloseButton;

    [Header("Save Slots")]
    [SerializeField] private SaveSlotUI storySlot;
    [SerializeField] private SaveSlotUI[] creativeSlots;

    [SerializeField] private ConfirmationPopup confirmPopup;

    [Header("Scene Names")]
    [SerializeField] private string storySceneName;
    [SerializeField] private string creativeSceneName;
    private string nextSceneName;
    public static string SelectedSlotKey { get; private set; }

    public static void ClearSelectedSlot()
    {
        SelectedSlotKey = null;
    }

    [Header("Parallax")]
    [SerializeField] private ParallaxLayer[] parallaxLayers;

    [Header("Post Processing")]
    [SerializeField] private Volume postProcessVolume;
    [SerializeField] private float blurTransitionDuration = 0.3f;
    [SerializeField] private float maxBlurDistance = 10f;

    private DepthOfField depthOfField;
    private MonoBehaviour currentOpenPanel;
    private bool isTransitioning = false;

    private void Awake()
    {
        InitializeParallaxLayers();
    }

    private void Start()
    {
        ClearSelectedSlot();

        if (confirmPopup == null)
            confirmPopup = FindFirstObjectByType<ConfirmationPopup>(FindObjectsInactive.Include);

        confirmPopup?.ValidateAnimator();

        // ✅ Post Processingを使わない設定
        if (postProcessVolume != null && postProcessVolume.profile != null)
        {
            postProcessVolume.profile.TryGet(out depthOfField);
            if (depthOfField != null)
            {
                depthOfField.active = false;
                depthOfField.focusDistance.value = maxBlurDistance;
            }
        }
        else
        {
            // Volumeが未設定でも警告を出さない
            depthOfField = null;
        }

#if DEMO_VERSION
        // DEMO ビルドではストーリーモードを無効化し、クリエイティブ専用メニューとしてセットアップ
        if (storyButton != null)
            storyButton.interactable = false;
        if (creativeButton != null)
            creativeButton.interactable = true;

        SetupButtonEvents();
        SetupPanelCallbacks();
        InitializeSlots();
#else
        SetupButtonEvents();
        SetupPanelCallbacks();
        InitializeSlots();
#endif
        CloseAllPanelsImmediate();

    }

    private void Update()
    {
        UpdateParallax();
    }


    /// <summary>
    /// ボタンイベントの設定
    /// </summary>
    private void SetupButtonEvents()
    {
        // メインメニューボタン
        if (creditButton != null)
            creditButton.onClick.AddListener(() => OpenPanel(creditPanel));

        if (optionButton != null)
            optionButton.onClick.AddListener(() => OpenPanel(optionPanel));

#if !DEMO_VERSION
        if (storyButton != null)
            storyButton.onClick.AddListener(() => OpenPanel(storySlotPanel));
#endif

        if (creativeButton != null)
            creativeButton.onClick.AddListener(() => OpenPanel(creativeSlotPanel));

        if (exitButton != null)
            exitButton.onClick.AddListener(ExitGame);

        if (wishlistButton != null)
            wishlistButton.onClick.AddListener(OpenWishlistPage);

        // 閉じるボタン
        if (creditCloseButton != null)
            creditCloseButton.onClick.AddListener(() => ClosePanel(creditPanel));

        if (optionCloseButton != null)
            optionCloseButton.onClick.AddListener(() => ClosePanel(optionPanel));

#if !DEMO_VERSION
        if (storyCloseButton != null)
            storyCloseButton.onClick.AddListener(() => ClosePanel(storySlotPanel));
#endif

        if (creativeCloseButton != null)
            creativeCloseButton.onClick.AddListener(() => ClosePanel(creativeSlotPanel));
    }

    /// <summary>
    /// パネルコールバックの設定
    /// </summary>
    private void SetupPanelCallbacks()
    {
        if (optionPanel != null)
        {
            optionPanel.OnCloseComplete = () => OnPanelClosed(optionPanel);
        }

        if (creditPanel != null)
        {
            switch (creditPanel)
            {
                case PanelScaleAnimator scaleAnimator:
                    scaleAnimator.OnCloseComplete = () => OnPanelClosed(scaleAnimator);
                    break;
                case UISlidePanel slidePanel:
                    slidePanel.OnSlideOutComplete = () => OnPanelClosed(slidePanel);
                    break;
            }
        }

#if !DEMO_VERSION
        if (storySlotPanel != null)
        {
            storySlotPanel.OnSlideOutComplete = () => OnPanelClosed(storySlotPanel);
        }
#endif

        if (creativeSlotPanel != null)
        {
            creativeSlotPanel.OnSlideOutComplete = () => OnPanelClosed(creativeSlotPanel);
        }
    }

    private void InitializeSlots()
    {
#if !DEMO_VERSION
        if (storySlot != null)
        {
            storySlot.Refresh();
            storySlot.OnSelected += HandleSlotSelected;
            storySlot.OnDeleteRequested += HandleDeleteRequested;
        }
#endif

        if (creativeSlots != null)
        {
            foreach (var slot in creativeSlots)
            {
                if (slot == null) continue;
                slot.Refresh();
                slot.OnSelected += HandleSlotSelected;
                slot.OnDeleteRequested += HandleDeleteRequested;
            }
        }
    }

    private void HandleSlotSelected(string slotKey)
    {
        SelectedSlotKey = slotKey;

        BaseSaveData data = null;
        if (SaveGameManager.Instance.HasSlot(slotKey))
            data = SaveGameManager.Instance.LoadMetadata(slotKey);

        if (data != null && !string.IsNullOrEmpty(data.location) && data.location != "MainMenu")
            nextSceneName = data.location;
        else if (storySlot != null && slotKey == storySlot.SlotKey)
            nextSceneName = storySceneName;
        else
            nextSceneName = creativeSceneName;

        SetMainMenuButtonsInteractable(false);
        SetSlotsInteractable(false);
        SlideTransitionManager.Instance?.LoadSceneWithSlide(nextSceneName);
    }

    private void HandleDeleteRequested(string slotKey)
    {
        if (confirmPopup == null) return;
        confirmPopup.Open("本当に削除しますか？", () =>
        {
            SaveGameManager.Instance.Delete(slotKey);
            RefreshSlots();
        });
    }

    private void RefreshSlots()
    {
        if (storySlot != null) storySlot.Refresh();
        if (creativeSlots != null)
        {
            foreach (var slot in creativeSlots)
            {
                if (slot == null) continue;
                slot.Refresh();
            }
        }
        if (confirmPopup != null)
            confirmPopup.Close();
    }

    /// <summary>
    /// パネルを開く
    /// </summary>
    private void OpenPanel(MonoBehaviour panel)
    {
        if (panel == null || isTransitioning) return;

        // 既に同じパネルが開いている場合は何もしない
        if (currentOpenPanel == panel && IsPanelOpen(panel)) return;

        isTransitioning = true;

        // 他のパネルが開いている場合は閉じる
        if (currentOpenPanel != null && currentOpenPanel != panel)
        {
            ClosePanelInternal(currentOpenPanel);
        }

        // 新しいパネルを開く
        currentOpenPanel = panel;
        OpenPanelInternal(panel);

        // ブラーエフェクトを有効化
        EnableBlur();

        // メインメニューボタンを無効化
        SetMainMenuButtonsInteractable(false);

        StartCoroutine(ResetTransitionFlag());
    }

    /// <summary>
    /// パネルを閉じる
    /// </summary>
    private void ClosePanel(MonoBehaviour panel)
    {
        if (panel == null || !IsPanelOpen(panel) || isTransitioning) return;

        isTransitioning = true;
        ClosePanelInternal(panel);

        StartCoroutine(ResetTransitionFlag());
    }

    /// <summary>
    /// パネルが閉じられた時の処理
    /// </summary>
    private void OnPanelClosed(MonoBehaviour panel)
    {
        if (currentOpenPanel == panel)
        {
            currentOpenPanel = null;

            // ブラーエフェクトを無効化
            DisableBlur();

            SetMainMenuButtonsInteractable(true);
            SetSlotsInteractable(true);
        }
    }

    /// <summary>
    /// すべてのパネルを即座に閉じる
    /// </summary>
    private void CloseAllPanelsImmediate()
    {
        switch (creditPanel)
        {
            case UISlidePanel slidePanel:
                slidePanel.CloseImmediate();
                break;
            case PanelScaleAnimator scaleAnimator:
                scaleAnimator.SnapClosed();
                break;
        }
        if (optionPanel != null) optionPanel.SnapClosed();
        if (storySlotPanel != null) storySlotPanel.CloseImmediate();
        if (creativeSlotPanel != null) creativeSlotPanel.CloseImmediate();

        currentOpenPanel = null;
        SetMainMenuButtonsInteractable(true);
        SetSlotsInteractable(true);
    }

    /// <summary>
    /// ブラーエフェクトを有効化
    /// </summary>
    private void EnableBlur()
    {
        if (depthOfField == null) return;

        depthOfField.active = true;

        // DOTweenでフォーカス距離をアニメーション
        DOTween.To(() => depthOfField.focusDistance.value,
                   x => depthOfField.focusDistance.value = x,
                   1f,
                   blurTransitionDuration)
                .SetEase(Ease.OutCubic);
    }

    /// <summary>
    /// ブラーエフェクトを無効化
    /// </summary>
    private void DisableBlur()
    {
        if (depthOfField == null) return;

        // DOTweenでフォーカス距離をアニメーション
        DOTween.To(() => depthOfField.focusDistance.value,
                   x => depthOfField.focusDistance.value = x,
                   maxBlurDistance,
                   blurTransitionDuration)
                .SetEase(Ease.OutCubic)
                .OnComplete(() =>
                {
                    depthOfField.active = false;
                });
    }

    /// <summary>
    /// メインメニューボタンの有効/無効を設定
    /// </summary>
    private void SetMainMenuButtonsInteractable(bool interactable)
    {
        if (creditButton != null) creditButton.interactable = interactable;
        if (optionButton != null) optionButton.interactable = interactable;
#if DEMO_VERSION
        // DEMO ではストーリーボタンを常に無効にしてクリエイティブ専用とする
        if (storyButton != null) storyButton.interactable = false;
#else
        if (storyButton != null) storyButton.interactable = interactable;
#endif
        if (creativeButton != null) creativeButton.interactable = interactable;
    }

    /// <summary>
    /// セーブスロットボタンの有効/無効を設定
    /// </summary>
    private void SetSlotsInteractable(bool interactable)
    {
#if DEMO_VERSION
        // DEMO ではストーリースロットを常に無効化して選択できないようにする
        if (storySlot != null) storySlot.SetInteractable(false);
#else
        if (storySlot != null) storySlot.SetInteractable(interactable);
#endif
        if (creativeSlots != null)
        {
            foreach (var slot in creativeSlots)
            {
                if (slot == null) continue;
                slot.SetInteractable(interactable);
            }
        }
    }

    /// <summary>
    /// トランジションフラグをリセット
    /// </summary>
    private IEnumerator ResetTransitionFlag()
    {
        yield return new WaitForSeconds(0.1f);
        isTransitioning = false;
    }

    private void OpenPanelInternal(MonoBehaviour panel)
    {
        switch (panel)
        {
            case UISlidePanel slidePanel:
                slidePanel.SlideIn();
                break;
            case PanelScaleAnimator scaleAnimator:
                scaleAnimator.Open();
                break;
        }
    }

    private void ClosePanelInternal(MonoBehaviour panel)
    {
        switch (panel)
        {
            case UISlidePanel slidePanel:
                slidePanel.SlideOut();
                break;
            case PanelScaleAnimator scaleAnimator:
                scaleAnimator.Close();
                break;
        }
    }

    private bool IsPanelOpen(MonoBehaviour panel)
    {
        switch (panel)
        {
            case UISlidePanel slidePanel:
                return slidePanel.IsOpen;
            case PanelScaleAnimator scaleAnimator:
                return scaleAnimator.IsOpen;
            default:
                return false;
        }
    }

    private void OpenWishlistPage()
    {
        if (steamAppId == 0 && string.IsNullOrEmpty(fallbackStorePageUrl))
        {
            Debug.LogWarning("Wishlist button clicked, but Steam App ID and fallback URL are not set.");
            return;
        }

        if (CanUseSteamOverlay())
        {
            SteamFriends.ActivateGameOverlayToStore(new AppId_t(steamAppId), EOverlayToStoreFlag.k_EOverlayToStoreFlag_None);
        }
        else
        {
            var targetUrl = string.IsNullOrEmpty(fallbackStorePageUrl)
                ? $"https://store.steampowered.com/app/{steamAppId}/"
                : fallbackStorePageUrl;
            Application.OpenURL(targetUrl);
        }
    }

    private bool CanUseSteamOverlay()
    {
        if (!SteamAPI.IsSteamRunning())
            return false;

        return SteamUtils.IsOverlayEnabled();
    }

    /// <summary>
    /// ゲームを終了する
    /// </summary>
    private void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDestroy()
    {
        ResetParallax();

        // イベントリスナーのクリーンアップ
        if (optionButton != null) optionButton.onClick.RemoveAllListeners();
        if (storyButton != null) storyButton.onClick.RemoveAllListeners();
        if (creativeButton != null) creativeButton.onClick.RemoveAllListeners();
        if (exitButton != null) exitButton.onClick.RemoveAllListeners();
        if (creditButton != null) creditButton.onClick.RemoveAllListeners();
        if (wishlistButton != null) wishlistButton.onClick.RemoveAllListeners();
        if (optionCloseButton != null) optionCloseButton.onClick.RemoveAllListeners();
        if (storyCloseButton != null) storyCloseButton.onClick.RemoveAllListeners();
        if (creativeCloseButton != null) creativeCloseButton.onClick.RemoveAllListeners();
        if (creditCloseButton != null) creditCloseButton.onClick.RemoveAllListeners();

        if (storySlot != null)
        {
            storySlot.OnSelected -= HandleSlotSelected;
            storySlot.OnDeleteRequested -= HandleDeleteRequested;
        }
        if (creativeSlots != null)
        {
            foreach (var slot in creativeSlots)
            {
                if (slot == null) continue;
                slot.OnSelected -= HandleSlotSelected;
                slot.OnDeleteRequested -= HandleDeleteRequested;
            }
        }
    }

    private void InitializeParallaxLayers()
    {
        if (parallaxLayers == null) return;

        for (int i = 0; i < parallaxLayers.Length; i++)
        {
            var layer = parallaxLayers[i];
            if (layer.target == null)
            {
                parallaxLayers[i] = layer;
                continue;
            }

            layer.baseAnchoredPosition = layer.target.anchoredPosition;
            layer.velocity = Vector2.zero;
            parallaxLayers[i] = layer;
        }
    }

    private void UpdateParallax()
    {
        if (parallaxLayers == null || parallaxLayers.Length == 0) return;
        if (Screen.width <= 0 || Screen.height <= 0) return;

        Vector2 mousePosition = Input.mousePosition;
        float halfWidth = Screen.width * 0.5f;
        float halfHeight = Screen.height * 0.5f;

        float normalizedX = halfWidth > 0f ? Mathf.Clamp((mousePosition.x - halfWidth) / halfWidth, -1f, 1f) : 0f;
        float normalizedY = halfHeight > 0f ? Mathf.Clamp((mousePosition.y - halfHeight) / halfHeight, -1f, 1f) : 0f;
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            normalizedX = 0f;
            normalizedY = 0f;
        }
        Vector2 normalizedOffset = new Vector2(normalizedX, normalizedY);

        for (int i = 0; i < parallaxLayers.Length; i++)
        {
            var layer = parallaxLayers[i];
            if (layer.target == null)
            {
                parallaxLayers[i] = layer;
                continue;
            }

            Vector2 targetPosition = layer.baseAnchoredPosition + Vector2.Scale(layer.maxOffset, normalizedOffset);

            if (layer.smoothTime > 0f)
            {
                Vector2 currentPosition = layer.target.anchoredPosition;
                Vector2 newPosition = Vector2.SmoothDamp(currentPosition, targetPosition, ref layer.velocity, layer.smoothTime);
                layer.target.anchoredPosition = newPosition;
            }
            else
            {
                layer.target.anchoredPosition = targetPosition;
                layer.velocity = Vector2.zero;
            }

            parallaxLayers[i] = layer;
        }
    }

    public void ResetParallax()
    {
        if (parallaxLayers == null) return;

        for (int i = 0; i < parallaxLayers.Length; i++)
        {
            var layer = parallaxLayers[i];
            if (layer.target == null)
            {
                parallaxLayers[i] = layer;
                continue;
            }

            layer.target.anchoredPosition = layer.baseAnchoredPosition;
            layer.velocity = Vector2.zero;
            parallaxLayers[i] = layer;
        }
    }
}
