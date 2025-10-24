using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using TMPro;
using System.Collections;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

public class MilestonePanel : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panel;
    public Button toggleButton;
#if UNITY_EDITOR
    [SerializeField] private Button advanceButton;
#endif
    public TMP_Text cozyText;
    public TMP_Text natureText;
    public TMP_Text itemText;
    public TMP_Text rarityProgressText;
    [FormerlySerializedAs("rewardText")]
    public TMP_Text rewardItemText;
    public TMP_Text rewardMoneyText;
    [SerializeField] private Image rewardImage;
    [SerializeField] private Sprite rewardFallbackSprite;
    public TMP_Text milestoneIdText;
    [Header("Tooltip")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TMP_Text tooltipText;
    [SerializeField] private TMP_Text tooltipCozyText;
    [SerializeField] private TMP_Text tooltipNatureText;
    [SerializeField] private TMP_Text tooltipItemText;
    [SerializeField] private TMP_Text tooltipRarityText;
    [FormerlySerializedAs("tooltipRewardText")]
    [SerializeField] private TMP_Text tooltipRewardItemText;
    [SerializeField] private TMP_Text tooltipRewardMoneyText;
    [SerializeField] private Image tooltipRewardImage;
    [SerializeField] private CanvasGroup tooltipCanvasGroup;
    [SerializeField] private float tooltipFadeDuration = 0.2f;
    [SerializeField] private Vector2 tooltipOffset = new Vector2(0f, -10f);

    private RectTransform tooltipRectTransform;
    private RectTransform tooltipParentRectTransform;
    private Canvas tooltipParentCanvas;
    private Camera tooltipCanvasCamera;
    private readonly Vector3[] milestoneCornerBuffer = new Vector3[4];

    [SerializeField] private GameObject milestonePrefab;
    [SerializeField] private RectTransform milestoneContainer;
    [SerializeField] private Image progressLine;
    private readonly System.Collections.Generic.List<Image> milestoneImages = new System.Collections.Generic.List<Image>();
    private readonly System.Collections.Generic.List<Sprite> milestoneDefaultSprites = new System.Collections.Generic.List<Sprite>();

    [Header("Localization")]
    [SerializeField] private string cozyProgressKey = "CozyProgress";
    [SerializeField] private string natureProgressKey = "NatureProgress";
    [SerializeField] private string itemProgressKey = "ItemProgress";

    [Header("Milestone Sprites")]
    [SerializeField] private Sprite completedSprite;
    [SerializeField] private Sprite inProgressSprite;
    [SerializeField] private Sprite incompleteSprite;

    [Header("Notifications")]
    [SerializeField] private GameObject milestoneNotificationIndicator;
    private int lastKnownMilestoneIndex = 0;
    private bool hasProcessedInitialProgressUpdate;

    [Header("Animation")]
    [SerializeField] private float closedPositionX = 0f;
    [SerializeField] private float openPositionX = 0f;
    [SerializeField] private float anchoredY = 0f;
    [SerializeField]
    private AnimationCurve slideInXCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField]
    private AnimationCurve slideOutXCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private bool isOpen = false;
    private Coroutine slideCoroutine;
    private Coroutine progressCoroutine;
    private Coroutine tooltipFadeCoroutine;
    private int currentProgressIndex = 0;
    private bool tooltipActive;

    void Awake()
    {
        CacheTooltipReferences();
        UIPanelExclusionManager.Instance?.Register(this);
    }

    void Start()
    {
        isOpen = false;
        CacheTooltipReferences();
        if (rewardImage != null)
        {
            rewardImage.enabled = false;
            rewardImage.sprite = rewardFallbackSprite;
            if (rewardImage.gameObject.activeSelf)
            {
                rewardImage.gameObject.SetActive(false);
            }
        }
        if (tooltipRewardImage != null)
        {
            tooltipRewardImage.enabled = false;
            tooltipRewardImage.sprite = rewardFallbackSprite;
            if (tooltipRewardImage.gameObject.activeSelf)
            {
                tooltipRewardImage.gameObject.SetActive(false);
            }
        }
        if (panel != null)
        {
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(closedPositionX, anchoredY);
        }

        if (tooltipCanvasGroup != null)
        {
            tooltipCanvasGroup.alpha = 0f;
            tooltipCanvasGroup.interactable = false;
            tooltipCanvasGroup.blocksRaycasts = false;
            if (tooltipPanel != null && tooltipPanel.activeSelf)
            {
                tooltipPanel.SetActive(false);
            }
        }
        else if (tooltipPanel != null && tooltipPanel.activeSelf)
        {
            tooltipPanel.SetActive(false);
        }

        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(TogglePanel);
        }

#if UNITY_EDITOR
        if (advanceButton != null)
        {
            advanceButton.onClick.AddListener(() =>
                MilestoneManager.Instance?.AdvanceMilestoneDebug());
        }
#endif

        if (MilestoneManager.Instance != null)
        {
            MilestoneManager.Instance.OnMilestoneProgress += HandleMilestoneProgress;
        }

        int initialMilestoneIndex = MilestoneManager.Instance?.CurrentMilestoneIndex ?? 0;
        currentProgressIndex = initialMilestoneIndex;
        lastKnownMilestoneIndex = initialMilestoneIndex;
        hasProcessedInitialProgressUpdate = false;

        CreateMilestones(MilestoneManager.Instance != null ? MilestoneManager.Instance.MilestoneCount : 0);
        SetProgress(initialMilestoneIndex);
        SetNotificationVisible(false);
        MilestoneManager.Instance?.RequestProgressUpdate();
    }

    void OnDestroy()
    {
        if (MilestoneManager.Instance != null)
        {
            MilestoneManager.Instance.OnMilestoneProgress -= HandleMilestoneProgress;
        }
    }

    public bool IsOpen => isOpen;

    public void OpenPanel()
    {
        if (isOpen)
        {
            return;
        }

        UIPanelExclusionManager.Instance?.NotifyOpened(this);
        isOpen = true;
        ClearMilestoneNotification();

        if (slideCoroutine != null)
        {
            StopCoroutine(slideCoroutine);
        }

        slideCoroutine = StartCoroutine(SlidePanel(true));
    }

    public void ClosePanel()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;

        if (slideCoroutine != null)
        {
            StopCoroutine(slideCoroutine);
        }

        slideCoroutine = StartCoroutine(SlidePanel(false));
    }

    void TogglePanel()
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

    IEnumerator SlidePanel(bool open)
    {
        if (panel == null) yield break;

        RectTransform rect = panel.GetComponent<RectTransform>();
        float anchoredYValue = anchoredY;

        AnimationCurve activeCurve = open ? slideInXCurve : slideOutXCurve;
        bool hasCurve = TryGetSlideParameters(activeCurve, out float duration);
        float endTime = open ? duration : 0f;
        float targetX = open ? openPositionX : closedPositionX;

        if (!hasCurve)
        {
            rect.anchoredPosition = new Vector2(targetX, anchoredYValue);
            yield break;
        }

        float currentTime = Mathf.Clamp(
            GetTimeForPosition(rect.anchoredPosition.x, closedPositionX, openPositionX, activeCurve, duration),
            0f,
            duration);
        float direction = open ? 1f : -1f;

        rect.anchoredPosition = new Vector2(
            Mathf.LerpUnclamped(closedPositionX, openPositionX, activeCurve.Evaluate(currentTime)),
            anchoredYValue);

        while ((direction > 0f && currentTime < endTime) || (direction < 0f && currentTime > endTime))
        {
            currentTime += Time.unscaledDeltaTime * direction;
            currentTime = Mathf.Clamp(currentTime, 0f, duration);

            float normalized = activeCurve.Evaluate(currentTime);
            float lerpedX = Mathf.LerpUnclamped(closedPositionX, openPositionX, normalized);
            rect.anchoredPosition = new Vector2(lerpedX, anchoredYValue);
            yield return null;
        }

        rect.anchoredPosition = new Vector2(
            Mathf.LerpUnclamped(closedPositionX, openPositionX, activeCurve.Evaluate(endTime)),
            anchoredYValue);
    }

    private bool TryGetSlideParameters(AnimationCurve curve, out float duration)
    {
        duration = 0f;

        if (curve == null)
        {
            return false;
        }

        int keyCount = curve.length;
        if (keyCount == 0)
        {
            return false;
        }

        duration = curve.keys[keyCount - 1].time;
        if (duration <= 0f)
        {
            return false;
        }

        return keyCount >= 2;
    }

    private float GetTimeForPosition(float positionX, float closedX, float openX, AnimationCurve curve, float duration)
    {
        if (curve == null)
        {
            return 0f;
        }

        int keyCount = curve.length;
        if (keyCount == 0)
        {
            return 0f;
        }

        Keyframe[] keys = curve.keys;
        if (keyCount == 1)
        {
            return keys[0].time;
        }

        float denominator = openX - closedX;
        float normalized = denominator != 0f ? (positionX - closedX) / denominator : 0f;

        for (int i = 0; i < keyCount - 1; i++)
        {
            float startValue = keys[i].value;
            float endValue = keys[i + 1].value;

            if (Mathf.Approximately(startValue, endValue))
            {
                if (Mathf.Approximately(normalized, startValue))
                {
                    return Mathf.Lerp(keys[i].time, keys[i + 1].time, 0.5f);
                }
                continue;
            }

            bool between = (normalized >= startValue && normalized <= endValue) ||
                           (normalized <= startValue && normalized >= endValue);
            if (between)
            {
                float segmentProgress = (normalized - startValue) / (endValue - startValue);
                return Mathf.Lerp(keys[i].time, keys[i + 1].time, segmentProgress);
            }
        }

        float startValueAtZero = curve.Evaluate(0f);
        float endValueAtDuration = curve.Evaluate(duration);
        return Mathf.Abs(normalized - startValueAtZero) <= Mathf.Abs(normalized - endValueAtDuration) ? 0f : duration;
    }

    public void CreateMilestones(int count)
    {
        if (milestoneContainer == null || milestonePrefab == null) return;

        for (int i = milestoneContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(milestoneContainer.GetChild(i).gameObject);
        }

        milestoneImages.Clear();
        milestoneDefaultSprites.Clear();
        for (int i = 0; i < count; i++)
        {
            var obj = Instantiate(milestonePrefab, milestoneContainer);
            var img = obj.GetComponent<Image>();
            if (img != null)
            {
                img.color = Color.white;
                milestoneImages.Add(img);
                milestoneDefaultSprites.Add(img.sprite);
            }

            var trigger = obj.GetComponent<MilestoneTooltipTrigger>();
            if (trigger == null)
            {
                trigger = obj.AddComponent<MilestoneTooltipTrigger>();
            }
            trigger.Initialize(this, i);
        }
    }

    public void SetProgress(int currentIndex)
    {
        if (milestoneImages.Count == 0 || progressLine == null) return;

        for (int i = 0; i < milestoneImages.Count; i++)
        {
            if (milestoneImages[i] == null) continue;

            var image = milestoneImages[i];
            Sprite defaultSprite = i < milestoneDefaultSprites.Count ? milestoneDefaultSprites[i] : null;
            Sprite targetSprite = null;
            Color fallbackColor;

            if (i < currentIndex)
            {
                targetSprite = completedSprite;
                fallbackColor = Color.blue;
            }
            else if (i == currentIndex)
            {
                targetSprite = inProgressSprite;
                fallbackColor = Color.yellow;
            }
            else
            {
                targetSprite = incompleteSprite;
                fallbackColor = Color.gray;
            }

            if (targetSprite != null)
            {
                if (image.sprite != targetSprite)
                {
                    image.sprite = targetSprite;
                }
                image.color = Color.white;
            }
            else
            {
                if (image.sprite != defaultSprite)
                {
                    image.sprite = defaultSprite;
                }
                image.color = fallbackColor;
            }
        }

        currentIndex = Mathf.Clamp(currentIndex, 0, milestoneImages.Count - 1);
        var target = milestoneImages[currentIndex];
        progressLine.rectTransform.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            target.rectTransform.anchoredPosition.x);
    }

    void HandleMilestoneProgress(MilestoneManager.Milestone milestone, int cozy, int nature, int itemCount)
    {
        UpdateDisplay(milestone, cozy, nature, itemCount);

        if (tooltipActive)
        {
            HideMilestoneTooltip();
        }

        int targetIndex = GetMilestoneIndex(milestone.id);
        AnimateProgress(targetIndex, 0.5f);

        int currentMilestoneIndex = MilestoneManager.Instance?.CurrentMilestoneIndex ?? lastKnownMilestoneIndex;
        bool isRestoringState = MilestoneManager.Instance?.IsRestoringState ?? false;

        if (!hasProcessedInitialProgressUpdate || isRestoringState)
        {
            hasProcessedInitialProgressUpdate = true;
            lastKnownMilestoneIndex = currentMilestoneIndex;
            return;
        }

        if (currentMilestoneIndex > lastKnownMilestoneIndex)
        {
            if (isOpen)
            {
                ClearMilestoneNotification();
            }
            else
            {
                ShowMilestoneNotification();
            }
        }

        lastKnownMilestoneIndex = currentMilestoneIndex;
    }

    void UpdateDisplay(MilestoneManager.Milestone milestone, int cozy, int nature, int itemCount)
    {
        if (cozyText != null)
        {
            cozyText.text = GetProgressString(cozyProgressKey, "Cozy", cozy, milestone.cozyRequirement);
        }
        if (natureText != null)
        {
            natureText.text = GetProgressString(natureProgressKey, "Nature", nature, milestone.natureRequirement);
        }
        if (itemText != null)
        {
            itemText.text = GetProgressString(itemProgressKey, "Items", itemCount, milestone.itemCountRequirement);
        }
        UpdateRarityRequirementText(rarityProgressText, milestone);
        if (rewardItemText != null)
        {
            rewardItemText.text = milestone.reward ?? string.Empty;
        }
        if (rewardMoneyText != null)
        {
            rewardMoneyText.text = milestone.moneyReward.ToString();
        }
        UpdateRewardIcon(milestone, rewardImage);
        if (milestoneIdText != null)
        {
            milestoneIdText.text = $"Milestone: {milestone.id}";
        }
    }

    private void UpdateRewardIcon(MilestoneManager.Milestone milestone, Image targetImage)
    {
        if (targetImage == null)
        {
            return;
        }

        bool hasReward = !string.IsNullOrEmpty(milestone.reward) &&
                         !string.Equals(milestone.reward, "None", System.StringComparison.OrdinalIgnoreCase);

        if (hasReward)
        {
            var dataManager = FurnitureDataManager.Instance;
            var icon = dataManager?.GetFurnitureIcon(milestone.reward);
            if (icon != null)
            {
                targetImage.sprite = icon;
                targetImage.enabled = true;
                if (!targetImage.gameObject.activeSelf)
                {
                    targetImage.gameObject.SetActive(true);
                }
                return;
            }

            targetImage.sprite = rewardFallbackSprite;
            targetImage.enabled = false;
            if (targetImage.gameObject.activeSelf)
            {
                targetImage.gameObject.SetActive(false);
            }

            if (dataManager == null)
            {
                Debug.LogWarning("MilestonePanel: FurnitureDataManager instance not found. Unable to load reward icon.");
            }
            else
            {
                Debug.LogWarning($"MilestonePanel: Reward icon not found for '{milestone.reward}'.");
            }
            return;
        }

        targetImage.sprite = rewardFallbackSprite;
        targetImage.enabled = false;
        if (targetImage.gameObject.activeSelf)
        {
            targetImage.gameObject.SetActive(false);
        }
    }

    int GetMilestoneIndex(string id)
    {
        if (string.IsNullOrEmpty(id)) return 0;

        int underscoreIndex = id.LastIndexOf('_');
        if (underscoreIndex >= 0 && underscoreIndex < id.Length - 1)
        {
            string numberPart = id.Substring(underscoreIndex + 1);
            if (int.TryParse(numberPart, out int result))
            {
                return Mathf.Max(0, result);
            }
        }
        return 0;
    }

    public void AnimateProgress(int targetIndex, float duration)
    {
        if (milestoneImages.Count == 0 || progressLine == null) return;

        targetIndex = Mathf.Clamp(targetIndex, 0, milestoneImages.Count - 1);
        if (progressCoroutine != null)
        {
            StopCoroutine(progressCoroutine);
        }
        progressCoroutine = StartCoroutine(AnimateProgressRoutine(targetIndex, duration));
    }

    IEnumerator AnimateProgressRoutine(int targetIndex, float duration)
    {
        int startIndex = currentProgressIndex;
        float startX = milestoneImages[Mathf.Clamp(startIndex, 0, milestoneImages.Count - 1)].rectTransform.anchoredPosition.x;
        float endX = milestoneImages[targetIndex].rectTransform.anchoredPosition.x;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float current = Mathf.Lerp(startIndex, targetIndex, t);
            int displayIndex = Mathf.RoundToInt(current);
            SetProgress(displayIndex);
            float x = Mathf.Lerp(startX, endX, t);
            progressLine.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, x);
            yield return null;
        }

        SetProgress(targetIndex);
        currentProgressIndex = targetIndex;
    }

    public void ShowMilestoneTooltip(int index, RectTransform milestoneRectTransform)
    {
        if (MilestoneManager.Instance == null)
        {
            HideMilestoneTooltip();
            return;
        }

        if (!MilestoneManager.Instance.TryGetMilestone(index, out var milestone) ||
            index >= MilestoneManager.Instance.CurrentMilestoneIndex)
        {
            HideMilestoneTooltip();
            return;
        }

        tooltipActive = true;

        CacheTooltipReferences();

        if (tooltipRectTransform != null &&
            TryGetTooltipAnchoredPosition(milestoneRectTransform, out var anchoredPosition))
        {
            tooltipRectTransform.anchoredPosition = anchoredPosition + tooltipOffset;
        }

        FadeTooltip(true);
        if (tooltipText != null)
        {
            tooltipText.text = milestone.id;
        }

        UpdateTooltipFields();

        void UpdateTooltipFields()
        {
            if (tooltipCozyText != null)
            {
                tooltipCozyText.text = GetProgressString(cozyProgressKey, "Cozy",
                    milestone.cozyRequirement, milestone.cozyRequirement);
            }
            if (tooltipNatureText != null)
            {
                tooltipNatureText.text = GetProgressString(natureProgressKey, "Nature",
                    milestone.natureRequirement, milestone.natureRequirement);
            }
            if (tooltipItemText != null)
            {
                tooltipItemText.text = GetProgressString(itemProgressKey, "Items",
                    milestone.itemCountRequirement, milestone.itemCountRequirement);
            }
            UpdateRarityRequirementText(tooltipRarityText, milestone);
            if (tooltipRewardItemText != null)
            {
                tooltipRewardItemText.text = milestone.reward ?? string.Empty;
            }
            if (tooltipRewardMoneyText != null)
            {
                tooltipRewardMoneyText.text = milestone.moneyReward.ToString();
            }
            UpdateRewardIcon(milestone, tooltipRewardImage);
        }
    }

    private void UpdateRarityRequirementText(TMP_Text targetText, MilestoneManager.Milestone milestone)
    {
        if (targetText == null || milestone == null)
        {
            return;
        }

        string rarityName = GetRarityDisplayName(milestone.rarityRequirement);
        int currentCount = MilestoneManager.Instance?.GetPlacedCountForRarity(milestone.rarityRequirement) ?? 0;
        targetText.text = $"{rarityName}: {currentCount}/{milestone.rarityCountRequirement}";
    }

    private string GetRarityDisplayName(Rarity rarity)
    {
        string rarityKey = null;

        switch (rarity)
        {
            case Rarity.Common:
                rarityKey = "Rarity_Common";
                break;
            case Rarity.Uncommon:
                rarityKey = "Rarity_Uncommon";
                break;
            case Rarity.Rare:
                rarityKey = "Rarity_Rare";
                break;
        }

        if (!string.IsNullOrEmpty(rarityKey) && LocalizationSettings.HasSettings &&
            LocalizationSettings.StringDatabase != null)
        {
            try
            {
                string localizedString = LocalizationSettings.StringDatabase.GetLocalizedString(
                    "StandardText",
                    rarityKey);

                if (!string.IsNullOrEmpty(localizedString))
                {
                    return localizedString;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"MilestonePanel: Failed to get localized rarity for key '{rarityKey}'. {ex.Message}");
            }
        }

        return rarity.ToString();
    }

    private string GetProgressString(string key, string fallbackLabel, int current, int required)
    {
        if (!string.IsNullOrEmpty(key) && LocalizationSettings.HasSettings &&
            LocalizationSettings.StringDatabase != null)
        {
            try
            {
                var tableReference = new TableReference("StandardText");
                var entryReference = new TableEntryReference(key);
                return LocalizationSettings.StringDatabase.GetLocalizedString(
                    tableReference,
                    entryReference,
                    null,
                    FallbackBehavior.UseProjectSettings,
                    current,
                    required);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"MilestonePanel: Failed to get localized string for key '{key}'. {ex.Message}");
            }
        }

        return $"{fallbackLabel}: {current}/{required}";
    }

    public void HideMilestoneTooltip()
    {
        tooltipActive = false;
        FadeTooltip(false);
    }

    private void ShowMilestoneNotification()
    {
        SetNotificationVisible(true);
    }

    private void ClearMilestoneNotification()
    {
        SetNotificationVisible(false);
    }

    private void SetNotificationVisible(bool visible)
    {
        if (milestoneNotificationIndicator == null)
        {
            return;
        }

        if (milestoneNotificationIndicator.activeSelf != visible)
        {
            milestoneNotificationIndicator.SetActive(visible);
        }
    }

    private void FadeTooltip(bool show)
    {
        if (tooltipCanvasGroup == null)
        {
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(show);
            }
            return;
        }

        if (tooltipFadeCoroutine != null)
        {
            StopCoroutine(tooltipFadeCoroutine);
            tooltipFadeCoroutine = null;
        }

        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            CompleteTooltipFadeImmediately(show);
            return;
        }

        tooltipFadeCoroutine = StartCoroutine(FadeTooltipRoutine(show));
    }

    private void CacheTooltipReferences()
    {
        if (tooltipPanel == null)
        {
            tooltipRectTransform = null;
            tooltipParentRectTransform = null;
            tooltipParentCanvas = null;
            tooltipCanvasCamera = null;
            return;
        }

        tooltipRectTransform = tooltipPanel.GetComponent<RectTransform>();
        tooltipParentRectTransform = tooltipRectTransform != null ? tooltipRectTransform.parent as RectTransform : null;
        tooltipParentCanvas = tooltipRectTransform != null ? tooltipRectTransform.GetComponentInParent<Canvas>() : null;

        tooltipCanvasCamera = tooltipParentCanvas != null && tooltipParentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? (tooltipParentCanvas.worldCamera != null ? tooltipParentCanvas.worldCamera : Camera.main)
            : null;
    }

    private bool TryGetTooltipAnchoredPosition(RectTransform milestoneRectTransform, out Vector2 anchoredPosition)
    {
        anchoredPosition = Vector2.zero;

        CacheTooltipReferences();

        if (milestoneRectTransform == null || tooltipRectTransform == null)
        {
            return false;
        }

        var parentRect = tooltipParentRectTransform != null
            ? tooltipParentRectTransform
            : tooltipRectTransform.parent as RectTransform;

        if (parentRect == null)
        {
            return false;
        }

        milestoneRectTransform.GetWorldCorners(milestoneCornerBuffer);
        Vector3 bottomCenter = (milestoneCornerBuffer[0] + milestoneCornerBuffer[3]) * 0.5f;

        Camera camera = null;
        if (tooltipParentCanvas != null)
        {
            switch (tooltipParentCanvas.renderMode)
            {
                case RenderMode.ScreenSpaceOverlay:
                    camera = null;
                    break;
                case RenderMode.ScreenSpaceCamera:
                case RenderMode.WorldSpace:
                    camera = tooltipParentCanvas.worldCamera != null ? tooltipParentCanvas.worldCamera : tooltipCanvasCamera;
                    if (camera == null)
                    {
                        camera = Camera.main;
                    }
                    break;
            }
            tooltipCanvasCamera = camera;
        }
        else
        {
            camera = tooltipCanvasCamera;
        }

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, bottomCenter);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, camera, out var localPoint))
        {
            anchoredPosition = localPoint;
            return true;
        }

        return false;
    }

    private IEnumerator FadeTooltipRoutine(bool show)
    {
        if (tooltipPanel != null && !tooltipPanel.activeSelf && show)
        {
            tooltipPanel.SetActive(true);
        }

        float duration = Mathf.Max(tooltipFadeDuration, 0f);
        float elapsed = 0f;
        float startAlpha = tooltipCanvasGroup.alpha;
        float endAlpha = show ? 1f : 0f;

        tooltipCanvasGroup.interactable = false;
        tooltipCanvasGroup.blocksRaycasts = false;

        if (duration <= 0f)
        {
            tooltipCanvasGroup.alpha = endAlpha;
        }
        else
        {
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                tooltipCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
                yield return null;
            }
        }

        tooltipCanvasGroup.alpha = endAlpha;
        tooltipCanvasGroup.interactable = show;
        tooltipCanvasGroup.blocksRaycasts = show;

        if (!show && tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }

        tooltipFadeCoroutine = null;
    }

    private void CompleteTooltipFadeImmediately(bool show)
    {
        if (tooltipPanel != null)
        {
            if (show)
            {
                if (!tooltipPanel.activeSelf)
                {
                    tooltipPanel.SetActive(true);
                }
            }
            else if (tooltipPanel.activeSelf)
            {
                tooltipPanel.SetActive(false);
            }
        }

        if (tooltipCanvasGroup != null)
        {
            tooltipCanvasGroup.alpha = show ? 1f : 0f;
            tooltipCanvasGroup.interactable = show;
            tooltipCanvasGroup.blocksRaycasts = show;
        }
    }
}
