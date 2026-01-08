using UnityEngine;

public class MoneyCostInteractable : MonoBehaviour, IInteractable
{
    [Header("Money Settings")]
    [SerializeField] private int cost = 100; // インスペクターで設定する消費額
    [SerializeField] private bool consumeOnce = false; // 1回きりで無効化するか
    [SerializeField] private bool blockIfInsufficient = true; // 所持金不足なら無効化

    private bool consumed = false;

    public void Interact()
    {
        if (consumed && consumeOnce) return;

        var money = MoneyManager.Instance;
        if (money == null)
        {
            Debug.LogWarning("[MoneyCostInteractable] MoneyManager が見つかりません。");
            return;
        }

        if (blockIfInsufficient && money.CurrentMoney < cost)
        {
            Debug.Log("[MoneyCostInteractable] 所持金不足のため実行できません。");
            return;
        }

        money.AddMoney(-cost);

        if (consumeOnce)
        {
            consumed = true;
            // 必要ならコライダー無効化など
            // GetComponent<Collider>()?.enabled = false;
        }
    }
}
