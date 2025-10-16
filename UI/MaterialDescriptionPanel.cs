using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization.Settings;

/// <summary>
/// Material用のシンプルな説明パネル
/// 拡張しやすい構造で基本機能のみ実装
/// </summary>
public class MaterialDescriptionPanel : MonoBehaviour
{
    [Header("Description Elements")]
    public TMP_Text itemNameText;        // アイテム名
    public TMP_Text descriptionText;     // 説明文
    public Image rarityCornerMark;       // レアリティコーナーマーク

    [Header("Rarity Sprites")]
    public Sprite commonSprite;
    public Sprite uncommonSprite;
    public Sprite rareSprite;

    // 現在表示中の素材
    private InventoryItem currentMaterialItem;
    private MaterialDataSO currentMaterialDataSO;

    void Start()
    {
        // 初期状態では非表示または空にする
        ClearDescription();
    }

    /// <summary>
    /// 素材の詳細を表示
    /// </summary>
    public void ShowMaterialDetail(InventoryItem item)
    {
        if (item == null || item.itemType != InventoryItem.ItemType.Material)
        {
            ClearDescription();
            return;
        }

        currentMaterialItem = item;

        // MaterialDataSOを取得
        currentMaterialDataSO = FurnitureDataManager.Instance?.GetMaterialDataSO(item.itemID);

        if (currentMaterialDataSO == null)
        {
            Debug.LogWarning($"MaterialDataSO not found for ID: {item.itemID}");
            ClearDescription();
            return;
        }

        // 基本情報の表示
        UpdateBasicInfo();
    }

    /// <summary>
    /// 基本情報を更新
    /// </summary>
    private void UpdateBasicInfo()
    {
        // アイテム名
        if (itemNameText != null)
            itemNameText.text = LocalizationSettings.StringDatabase.GetLocalizedString("MaterialNames", currentMaterialDataSO.nameID);

        // 説明文
        if (descriptionText != null)
            descriptionText.text = LocalizationSettings.StringDatabase.GetLocalizedString("MaterialDesc", currentMaterialDataSO.descriptionID);

        // レアリティ表示
        if (rarityCornerMark != null)
            UpdateRarityIndicator(currentMaterialDataSO.rarity);
    }

    /// <summary>
    /// レアリティインジケーターを更新
    /// </summary>
    private void UpdateRarityIndicator(Rarity rarity)
    {
        if (rarityCornerMark == null) return;

        // Ensure no tint is applied to the sprite
        rarityCornerMark.color = Color.white;

        switch (rarity)
        {
            case Rarity.Common:
                rarityCornerMark.sprite = commonSprite;
                break;
            case Rarity.Uncommon:
                rarityCornerMark.sprite = uncommonSprite;
                break;
            case Rarity.Rare:
                rarityCornerMark.sprite = rareSprite;
                break;
            default:
                rarityCornerMark.sprite = commonSprite;
                break;
        }

        rarityCornerMark.gameObject.SetActive(true);
    }

    /// <summary>
    /// 説明文を取得
    /// </summary>
    private string GetDescription(string descriptionID)
    {
        // TODO: ローカライゼーションシステムから説明文を取得
        // 仮実装：descriptionIDをそのまま返す
        if (string.IsNullOrEmpty(descriptionID))
            return "説明がありません。";

        // 実際にはローカライゼーションテーブルから取得
        return descriptionID;
    }

    /// <summary>
    /// 説明をクリア
    /// </summary>
    public void ClearDescription()
    {
        currentMaterialItem = null;
        currentMaterialDataSO = null;

        if (itemNameText != null)
            itemNameText.text = "";

        if (descriptionText != null)
            descriptionText.text = "";

        if (rarityCornerMark != null)
            rarityCornerMark.gameObject.SetActive(false);
    }

    /// <summary>
    /// 選択中のアイテムを取得
    /// </summary>
    public InventoryItem GetCurrentItem()
    {
        return currentMaterialItem;
    }

    /// <summary>
    /// 選択中のMaterialDataSOを取得
    /// </summary>
    public MaterialDataSO GetCurrentMaterialDataSO()
    {
        return currentMaterialDataSO;
    }
}
