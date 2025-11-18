using UnityEngine;
using TMPro;
using System.Collections;

public class MoneyDisplay : MonoBehaviour
{
    [SerializeField]
    private TMP_Text moneyText;

    [SerializeField]
    private float animationDuration = 0.25f;

    private float displayedAmount;
    private Coroutine animationCoroutine;

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
            return;
        }

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
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
            yield return null;
        }

        displayedAmount = targetAmount;
        UpdateText(targetAmount);
        animationCoroutine = null;
    }

    void UpdateText(int amount)
    {
        if (moneyText != null)
        {
            moneyText.text = amount.ToString();
        }
    }
}
