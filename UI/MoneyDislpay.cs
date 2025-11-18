using UnityEngine;
using TMPro;
using System.Collections;

public class MoneyDisplay : MonoBehaviour
{
    [SerializeField]
    private TMP_Text moneyText;

    [SerializeField]
    private RectTransform moneyTextTransform;

    [SerializeField]
    private AnimationCurve scaleCurve = new AnimationCurve(
        new Keyframe(0f, 1f),
        new Keyframe(0.9f, 1.2f),
        new Keyframe(1f, 1f));

    [SerializeField]
    private float animationDuration = 0.25f;

    private float displayedAmount;
    private Coroutine animationCoroutine;
    private Vector3 initialScale = Vector3.one;

    void Awake()
    {
        if (moneyTextTransform == null && moneyText != null)
        {
            moneyTextTransform = moneyText.rectTransform;
        }

        if (moneyTextTransform != null)
        {
            initialScale = moneyTextTransform.localScale;
        }
    }

    void Start()
    {
        if (MoneyManager.Instance != null)
        {
            displayedAmount = MoneyManager.Instance.CurrentMoney;
            UpdateText(Mathf.RoundToInt(displayedAmount));
            MoneyManager.Instance.OnMoneyChanged += AnimateDisplay;
        }
        else
        {
            displayedAmount = 0f;
            UpdateText(0);
        }
    }

    void OnDestroy()
    {
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.OnMoneyChanged -= AnimateDisplay;
        }

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
    }

    void AnimateDisplay(int targetAmount)
    {
        if (!gameObject.activeInHierarchy)
        {
            displayedAmount = targetAmount;
            UpdateText(targetAmount);
            ResetScale();
            return;
        }

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            ResetScale();
        }

        animationCoroutine = StartCoroutine(AnimateAmountRoutine(targetAmount));
    }

    IEnumerator AnimateAmountRoutine(int targetAmount)
    {
        float startingAmount = displayedAmount;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = animationDuration > 0f ? Mathf.Clamp01(elapsed / animationDuration) : 1f;
            displayedAmount = Mathf.Lerp(startingAmount, targetAmount, t);
            UpdateText(Mathf.RoundToInt(displayedAmount));

            if (moneyTextTransform != null && scaleCurve != null && scaleCurve.length > 0)
            {
                float evaluatedScale = scaleCurve.Evaluate(t);
                moneyTextTransform.localScale = initialScale * evaluatedScale;
            }
            yield return null;
        }

        displayedAmount = targetAmount;
        UpdateText(targetAmount);
        ResetScale();
        animationCoroutine = null;
    }

    void UpdateText(int amount)
    {
        if (moneyText != null)
        {
            moneyText.text = amount.ToString();
        }
    }

    void ResetScale()
    {
        if (moneyTextTransform != null)
        {
            moneyTextTransform.localScale = initialScale;
        }
    }
}
