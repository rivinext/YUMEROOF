using UnityEngine;
using TMPro;

public class MoneyDisplay : MonoBehaviour
{
    [SerializeField]
    private TMP_Text moneyText;

    void Start()
    {
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.OnMoneyChanged += UpdateDisplay;
            UpdateDisplay(MoneyManager.Instance.CurrentMoney);
        }
    }

    void OnDestroy()
    {
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.OnMoneyChanged -= UpdateDisplay;
        }
    }

    void UpdateDisplay(int amount)
    {
        if (moneyText != null)
        {
            moneyText.text = amount.ToString();
        }
    }
}
