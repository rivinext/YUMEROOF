using UnityEngine;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class SleepTransitionUIManager : MonoBehaviour
{
    [Serializable]
    private class SlidePanelAnimation
    {
        public RectTransform panel;
        public float closedPositionY;
        public float openPositionY;
        public float anchoredX;
        public float duration = 0.5f;
        public AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    }

    [SerializeField] private TMP_Text dayText;
    [SerializeField] private Canvas canvas;
    [SerializeField] private InventoryUI inventoryUI;

    [Header("Animation")]
    [SerializeField] private SlidePanelAnimation topSlide = new SlidePanelAnimation
    {
        closedPositionY = 0f,
        openPositionY = 1080f,
        anchoredX = 0f
    };

    [SerializeField] private SlidePanelAnimation topSlideSecondary = new SlidePanelAnimation
    {
        closedPositionY = 0f,
        openPositionY = 1080f,
        anchoredX = 0f
    };

    [SerializeField] private SlidePanelAnimation bottomSlide = new SlidePanelAnimation
    {
        closedPositionY = 0f,
        openPositionY = -1080f,
        anchoredX = 0f
    };

    [Header("Text Fade Settings")]
    [SerializeField] private float textFadeDuration = 0.5f;
    [SerializeField] private float textDisplayDelay = 1f;
    [SerializeField] private AnimationCurve textFadeEasing = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Unlock Panel Settings")]
    [SerializeField] private AvailableNotOwnedLogger availableLogger;
    [SerializeField] private RectTransform unlockPanel;
    [SerializeField] private RectTransform cardContainer;
    [SerializeField] private GameObject itemCardPrefab;
    [SerializeField] private Button unlockPanelButton;
    [SerializeField] private float unlockPanelDelay = 0f;
    [SerializeField] private float unlockFadeDuration = 0.5f;
    [SerializeField] private AnimationCurve unlockFadeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private float cardContainerDelay = 0f;
    [SerializeField] private float cardContainerFadeDuration = 0.5f;
    [SerializeField] private AnimationCurve cardContainerFadeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    private Coroutine transitionCoroutine;
    public bool IsTransitionRunning => transitionCoroutine != null;

    public event Action OnDayShown;

    private void Awake()
    {
        if (canvas != null)
            canvas.enabled = false;
        if (unlockPanel != null)
            unlockPanel.gameObject.SetActive(false);
        if (cardContainer != null)
            cardContainer.gameObject.SetActive(false);
        if (inventoryUI == null)
            inventoryUI = FindFirstObjectByType<InventoryUI>();

        SetInitialPanelPositions();
    }

    public void PlayTransition(int nextDay)
    {
        if (canvas != null)
            canvas.enabled = true;
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(TransitionRoutine(nextDay));
    }

    private void SetInitialPanelPositions()
    {
        SetPanelAnchoredPosition(topSlide, topSlide.openPositionY);
        SetPanelAnchoredPosition(topSlideSecondary, topSlideSecondary.openPositionY);
        SetPanelAnchoredPosition(bottomSlide, bottomSlide.openPositionY);
    }

    private void SetPanelAnchoredPosition(SlidePanelAnimation slide, float positionY)
    {
        if (slide?.panel == null)
            return;

        slide.panel.anchoredPosition = new Vector2(slide.anchoredX, positionY);
    }

    private void UpdateSlidePosition(SlidePanelAnimation slide, float startY, float endY, float displacement)
    {
        if (slide?.panel == null)
            return;

        float y = Mathf.LerpUnclamped(startY, endY, displacement);
        slide.panel.anchoredPosition = new Vector2(slide.anchoredX, y);
    }

    private float GetSlideDuration(SlidePanelAnimation slide)
    {
        return slide?.duration ?? 0f;
    }

    private float GetMaxDuration(params SlidePanelAnimation[] slides)
    {
        float max = 0f;
        foreach (SlidePanelAnimation slide in slides)
        {
            if (slide == null)
                continue;
            max = Mathf.Max(max, slide.duration);
        }
        return max;
    }

    private float EvaluateSlideCurve(SlidePanelAnimation slide, float t, bool clampResult = false)
    {
        if (slide == null)
            return 1f;

        if (slide.duration <= 0f)
            return 1f;

        float clampedT = Mathf.Clamp(t, 0f, slide.duration);

        if (slide.curve == null || slide.curve.length == 0)
        {
            float normalized = clampedT / slide.duration;
            return clampResult ? Mathf.Clamp01(normalized) : normalized;
        }

        float value = slide.curve.Evaluate(clampedT);
        return clampResult ? Mathf.Clamp01(value) : value;
    }

    private void OnValidate()
    {
        ValidateSlidePanel(topSlide, "Top");
        ValidateSlidePanel(topSlideSecondary, "Top (Secondary)");
        ValidateSlidePanel(bottomSlide, "Bottom");
    }

    private void ValidateSlidePanel(SlidePanelAnimation slide, string label)
    {
        if (slide == null)
            return;

        if (slide.curve == null)
        {
            slide.curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        }

        float maxDuration = Mathf.Max(0f, slide.duration);

        if (Mathf.Approximately(maxDuration, 0f))
        {
            slide.curve.keys = new[]
            {
                new Keyframe(0f, 0f),
                new Keyframe(0f, 1f)
            };
            return;
        }

        var keys = slide.curve.keys;
        bool hasStartKey = false;
        bool hasEndKey = false;
        int startKeyIndex = -1;
        bool startValueMatchesZero = true;
        bool endValueMatchesOne = true;

        for (int i = 0; i < keys.Length; i++)
        {
            Keyframe key = keys[i];

            key.time = Mathf.Clamp(key.time, 0f, maxDuration);

            if (Mathf.Approximately(key.time, 0f))
            {
                hasStartKey = true;
                startKeyIndex = i;
                key.time = 0f;
                startValueMatchesZero = Mathf.Approximately(key.value, 0f);
            }
            else if (Mathf.Approximately(key.time, maxDuration))
            {
                hasEndKey = true;
                key.time = maxDuration;
                endValueMatchesOne = Mathf.Approximately(key.value, 1f);
            }

            keys[i] = key;
        }

        if (!hasEndKey)
        {
            int candidateIndex = -1;
            float candidateTime = float.MinValue;
            for (int i = 0; i < keys.Length; i++)
            {
                if (i == startKeyIndex)
                    continue;

                if (keys[i].time > candidateTime)
                {
                    candidateTime = keys[i].time;
                    candidateIndex = i;
                }
            }

            if (candidateIndex >= 0)
            {
                Keyframe candidate = keys[candidateIndex];
                candidate.time = maxDuration;
                keys[candidateIndex] = candidate;
                hasEndKey = true;
                endValueMatchesOne = Mathf.Approximately(candidate.value, 1f);
            }
        }

        slide.curve.keys = keys;

        if (!hasStartKey)
        {
            float startValue = keys.Length > 0 ? keys[0].value : 0f;
            slide.curve.AddKey(new Keyframe(0f, startValue));
            startValueMatchesZero = Mathf.Approximately(startValue, 0f);
            hasStartKey = true;
        }

        if (!hasEndKey)
        {
            float endValue = keys.Length > 0 ? keys[keys.Length - 1].value : 1f;
            slide.curve.AddKey(new Keyframe(maxDuration, endValue));
            endValueMatchesOne = Mathf.Approximately(endValue, 1f);
            hasEndKey = true;
        }

        if (hasStartKey && !startValueMatchesZero)
            Debug.LogWarning($"{nameof(SleepTransitionUIManager)} ({label}): Slide curve start value is not 0. Overshoot is allowed; confirm this is intentional.", this);

        if (hasEndKey && !endValueMatchesOne)
            Debug.LogWarning($"{nameof(SleepTransitionUIManager)} ({label}): Slide curve end value is not 1. Overshoot is allowed; confirm this is intentional.", this);
    }

    private IEnumerator TransitionRoutine(int nextDay)
    {
        SetPanelAnchoredPosition(topSlide, topSlide.openPositionY);
        SetPanelAnchoredPosition(topSlideSecondary, topSlideSecondary.openPositionY);
        SetPanelAnchoredPosition(bottomSlide, bottomSlide.openPositionY);
        if (dayText != null)
        {
            Color textColor = dayText.color;
            textColor.a = 0f;
            dayText.color = textColor;
            dayText.text = $"{nextDay} Day";
        }

        if (inventoryUI != null)
        {
            inventoryUI.SwitchTab(false);
            inventoryUI.RefreshInventoryDisplay();
        }

        List<GameObject> spawnedCards = null;
        if (availableLogger != null && cardContainer != null && itemCardPrefab != null)
        {
            List<string> newItemIds = new List<string>(availableLogger.GetNewItemIds());
            if (newItemIds.Count > 0)
            {
                int displayCount = Mathf.Min(Random.Range(1, 4), newItemIds.Count);
                spawnedCards = new List<GameObject>();

                for (int i = 0; i < displayCount; i++)
                {
                    int index = Random.Range(0, newItemIds.Count);
                    string id = newItemIds[index];
                    newItemIds.RemoveAt(index);

                    GameObject card = Instantiate(itemCardPrefab, cardContainer);
                    var cardComp = card.GetComponent<InventoryItemCard>();

                    InventoryManager.Instance.AddFurniture(id, 0);
                    InventoryManager.Instance.ForceInventoryUpdate();
                    var item = InventoryManager.Instance.GetFurnitureItem(id);
                    cardComp.SetItem(item, false);   // 第2引数は家具なので false

                    card.transform.localScale = Vector3.one;
                    card.SetActive(true);
                    spawnedCards.Add(card);
                }
            }
        }

        float topTime = 0f;
        float bottomTime = 0f;
        float topDuration = GetMaxDuration(topSlide, topSlideSecondary);
        float bottomDuration = GetSlideDuration(bottomSlide);
        while (topTime < topDuration || bottomTime < bottomDuration)
        {
            float delta = Time.deltaTime;
            topTime += delta;
            bottomTime += delta;

            if (topSlide.panel != null)
            {
                float eased = EvaluateSlideCurve(topSlide, topTime);
                UpdateSlidePosition(topSlide, topSlide.openPositionY, topSlide.closedPositionY, eased);
            }

            if (topSlideSecondary.panel != null)
            {
                float eased = EvaluateSlideCurve(topSlideSecondary, topTime);
                UpdateSlidePosition(topSlideSecondary, topSlideSecondary.openPositionY, topSlideSecondary.closedPositionY, eased);
            }

            if (bottomSlide.panel != null)
            {
                float eased = EvaluateSlideCurve(bottomSlide, bottomTime);
                UpdateSlidePosition(bottomSlide, bottomSlide.openPositionY, bottomSlide.closedPositionY, eased);
            }
            yield return null;
        }

        SetPanelAnchoredPosition(topSlide, topSlide.closedPositionY);
        SetPanelAnchoredPosition(topSlideSecondary, topSlideSecondary.closedPositionY);
        SetPanelAnchoredPosition(bottomSlide, bottomSlide.closedPositionY);

        if (unlockPanelDelay > 0f)
            yield return new WaitForSeconds(unlockPanelDelay);

        if (spawnedCards != null && spawnedCards.Count > 0 && unlockPanel != null && cardContainer != null)
        {
            unlockPanel.gameObject.SetActive(true);
            cardContainer.gameObject.SetActive(true);

            CanvasGroup panelCg = unlockPanel.GetComponent<CanvasGroup>();
            if (panelCg == null)
                panelCg = unlockPanel.gameObject.AddComponent<CanvasGroup>();
            CanvasGroup containerCg = cardContainer.GetComponent<CanvasGroup>();
            if (containerCg == null)
                containerCg = cardContainer.gameObject.AddComponent<CanvasGroup>();

            panelCg.alpha = 0f;
            containerCg.alpha = 0f;

            float ut = 0f;
            while (ut < unlockFadeDuration)
            {
                ut += Time.deltaTime;
                float progress = Mathf.Clamp01(ut / unlockFadeDuration);
                float eased = unlockFadeCurve.Evaluate(progress);
                panelCg.alpha = Mathf.Lerp(0f, 1f, eased);
                yield return null;
            }

            if (cardContainerDelay > 0f)
                yield return new WaitForSeconds(cardContainerDelay);

            ut = 0f;
            while (ut < cardContainerFadeDuration)
            {
                ut += Time.deltaTime;
                float progress = Mathf.Clamp01(ut / cardContainerFadeDuration);
                float eased = cardContainerFadeCurve.Evaluate(progress);
                containerCg.alpha = Mathf.Lerp(0f, 1f, eased);
                yield return null;
            }

            bool clicked = false;
            UnityEngine.Events.UnityAction clickAction = () => clicked = true;
            if (unlockPanelButton != null)
                unlockPanelButton.onClick.AddListener(clickAction);

            yield return new WaitUntil(() => clicked);

            if (unlockPanelButton != null)
                unlockPanelButton.onClick.RemoveListener(clickAction);

            ut = 0f;
            while (ut < cardContainerFadeDuration)
            {
                ut += Time.deltaTime;
                float progress = Mathf.Clamp01(ut / cardContainerFadeDuration);
                float eased = cardContainerFadeCurve.Evaluate(progress);
                containerCg.alpha = Mathf.Lerp(1f, 0f, eased);
                yield return null;
            }

            ut = 0f;
            while (ut < unlockFadeDuration)
            {
                ut += Time.deltaTime;
                float progress = Mathf.Clamp01(ut / unlockFadeDuration);
                float eased = unlockFadeCurve.Evaluate(progress);
                panelCg.alpha = Mathf.Lerp(1f, 0f, eased);
                yield return null;
            }

            unlockPanel.gameObject.SetActive(false);
            cardContainer.gameObject.SetActive(false);

            foreach (GameObject card in spawnedCards)
            {
                if (card != null)
                    Destroy(card);
            }
        }

        float t = 0f;
        while (t < textFadeDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / textFadeDuration);
            float eased = textFadeEasing.Evaluate(progress);
            if (dayText != null)
            {
                Color textColor = dayText.color;
                textColor.a = Mathf.Lerp(0f, 1f, eased);
                dayText.color = textColor;
            }
            yield return null;
        }

        OnDayShown?.Invoke();

        if (textDisplayDelay > 0f)
            yield return new WaitForSeconds(textDisplayDelay);

        t = 0f;
        while (t < textFadeDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / textFadeDuration);
            float eased = textFadeEasing.Evaluate(progress);
            if (dayText != null)
            {
                Color textColor = dayText.color;
                textColor.a = Mathf.Lerp(1f, 0f, eased);
                dayText.color = textColor;
            }
            yield return null;
        }

        topTime = 0f;
        bottomTime = 0f;
        topDuration = GetMaxDuration(topSlide, topSlideSecondary);
        bottomDuration = GetSlideDuration(bottomSlide);
        while (topTime < topDuration || bottomTime < bottomDuration)
        {
            float delta = Time.deltaTime;
            topTime += delta;
            bottomTime += delta;

            if (topSlide.panel != null)
            {
                float eased = EvaluateSlideCurve(topSlide, topTime);
                UpdateSlidePosition(topSlide, topSlide.closedPositionY, topSlide.openPositionY, eased);
            }

            if (topSlideSecondary.panel != null)
            {
                float eased = EvaluateSlideCurve(topSlideSecondary, topTime);
                UpdateSlidePosition(topSlideSecondary, topSlideSecondary.closedPositionY, topSlideSecondary.openPositionY, eased);
            }

            if (bottomSlide.panel != null)
            {
                float eased = EvaluateSlideCurve(bottomSlide, bottomTime);
                UpdateSlidePosition(bottomSlide, bottomSlide.closedPositionY, bottomSlide.openPositionY, eased);
            }
            yield return null;
        }

        SetPanelAnchoredPosition(topSlide, topSlide.openPositionY);
        SetPanelAnchoredPosition(topSlideSecondary, topSlideSecondary.openPositionY);
        SetPanelAnchoredPosition(bottomSlide, bottomSlide.openPositionY);

        if (canvas != null)
            canvas.enabled = false;
        transitionCoroutine = null;
    }
}
