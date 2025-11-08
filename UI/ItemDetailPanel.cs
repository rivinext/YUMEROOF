using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ItemDetailPanel : MonoBehaviour
{
    [Header("Panel Elements")]
    public Text itemNameText;                  // アイテム名
    public Text descriptionText;               // 説明文
    public Image itemImage;                    // アイテム画像
    public Image rarityCornerMark;            // レアリティマーク

    [Header("Required Materials")]
    public GameObject requiredMaterialsSection; // 必要素材セクション
    public Transform requiredMaterialsList;    // 必要素材リスト
    public GameObject materialItemPrefab;      // 素材アイテムプレハブ

    [Header("Dropped Materials")]
    public GameObject droppedMaterialsSection;  // ドロップ素材セクション
    public Transform droppedMaterialsList;     // ドロップ素材リスト

    [Header("Buttons")]
    public Button craftButton;                 // クラフトボタン
    public Button sellButton;                  // 売却ボタン
    public Button closeButton;                 // 閉じるボタン
    public Text sellPriceText;                 // 売却価格テキスト

    [Header("Button States")]
    public Sprite craftButtonNormal;           // クラフトボタン通常
    public Sprite craftButtonHover;            // クラフトボタンホバー
    public Sprite craftButtonPressed;          // クラフトボタン押下
    public Sprite craftButtonDisabled;         // クラフトボタン無効

    public Sprite sellButtonNormal;            // 売却ボタン通常
    public Sprite sellButtonHover;             // 売却ボタンホバー
    public Sprite sellButtonPressed;           // 売却ボタン押下

    [Header("Rarity Colors")]
    public Color commonColor = Color.gray;
    public Color uncommonColor = Color.green;
    public Color rareColor = Color.blue;

    // 現在表示中のアイテム
    private InventoryItem currentItem;

    void Start()
    {
        // ボタンイベント設定
        if (craftButton != null)
            craftButton.onClick.AddListener(CraftItem);
        if (sellButton != null)
            sellButton.onClick.AddListener(SellItem);
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);

        // ボタンの状態変更設定
        SetupButtonStates();
    }

    // ボタンの状態変更設定
    void SetupButtonStates()
    {
        // クラフトボタン
        if (craftButton != null)
        {
            ButtonStateHandler craftHandler = craftButton.gameObject.AddComponent<ButtonStateHandler>();
            craftHandler.normalSprite = craftButtonNormal;
            craftHandler.hoverSprite = craftButtonHover;
            craftHandler.pressedSprite = craftButtonPressed;
            craftHandler.disabledSprite = craftButtonDisabled;
        }

        // 売却ボタン
        if (sellButton != null)
        {
            ButtonStateHandler sellHandler = sellButton.gameObject.AddComponent<ButtonStateHandler>();
            sellHandler.normalSprite = sellButtonNormal;
            sellHandler.hoverSprite = sellButtonHover;
            sellHandler.pressedSprite = sellButtonPressed;
        }
    }

    // アイテム詳細を表示
    public void ShowItemDetail(InventoryItem item)
    {
        currentItem = item;

        if (item.itemType == InventoryItem.ItemType.Furniture)
        {
            ShowFurnitureDetail(item);
        }
        else
        {
            ShowMaterialDetail(item);
        }
    }

    // 家具の詳細を表示
    void ShowFurnitureDetail(InventoryItem item)
    {
        var furnitureData = FurnitureDataManager.Instance?.GetFurnitureData(item.itemID);
        if (furnitureData == null) return;

        // 基本情報
        if (itemNameText != null)
            itemNameText.text = furnitureData.nameID;

        if (descriptionText != null)
            descriptionText.text = GetDescription(furnitureData.descriptionID);

        // アイコン
        var icon = FurnitureDataManager.Instance?.GetFurnitureIcon(item.itemID);
        if (itemImage != null && icon != null)
            itemImage.sprite = icon;

        // レアリティ
        SetRarityDisplay(furnitureData.rarity);

        // 必要素材表示（レシピがある場合）
        if (!string.IsNullOrEmpty(furnitureData.recipeID))
        {
            ShowRequiredMaterials(furnitureData.recipeID);
            requiredMaterialsSection?.SetActive(true);
        }
        else
        {
            requiredMaterialsSection?.SetActive(false);
        }

        // ドロップ素材表示（素材をドロップする家具の場合）
        if (furnitureData.CanDropMaterial)
        {
            ShowDroppedMaterials(furnitureData.dropMaterialIDs, furnitureData.dropRates);
            droppedMaterialsSection?.SetActive(true);
        }
        else
        {
            droppedMaterialsSection?.SetActive(false);
        }

        // 売却価格
        if (sellPriceText != null)
            sellPriceText.text = $"Price: {furnitureData.sellPrice}";

        // ボタンの有効/無効設定
        if (craftButton != null)
        {
            craftButton.interactable = item.canCraft && !string.IsNullOrEmpty(furnitureData.recipeID);
            craftButton.gameObject.SetActive(!string.IsNullOrEmpty(furnitureData.recipeID));
        }

        if (sellButton != null)
        {
            sellButton.interactable = item.quantity > 0;
        }
    }

    // 素材の詳細を表示
    void ShowMaterialDetail(InventoryItem item)
    {
        var materialData = InventoryManager.Instance?.GetMaterialData(item.itemID);
        if (materialData == null) return;

        // 基本情報
        if (itemNameText != null)
            itemNameText.text = materialData.materialName;

        if (descriptionText != null)
            descriptionText.text = GetDescription(materialData.descriptionID);

        // アイコン（仮）
        // TODO: 素材アイコンの実装

        // レアリティ
        SetRarityDisplay(materialData.rarity);

        // 素材には必要素材やドロップ素材のセクションは非表示
        requiredMaterialsSection?.SetActive(false);
        droppedMaterialsSection?.SetActive(false);

        // 売却価格
        if (sellPriceText != null)
            sellPriceText.text = $"Price: {materialData.sellPrice}";

        // クラフトボタンは非表示、売却ボタンのみ
        if (craftButton != null)
            craftButton.gameObject.SetActive(false);

        if (sellButton != null)
        {
            sellButton.interactable = item.quantity > 0;
        }
    }

    // 必要素材を表示
    void ShowRequiredMaterials(string recipeID)
    {
        // 既存のアイテムをクリア
        foreach (Transform child in requiredMaterialsList)
        {
            Destroy(child.gameObject);
        }

        // TODO: レシピシステムから必要素材を取得
        // 仮実装
        CreateMaterialItem(requiredMaterialsList, "M001", 3);  // Wood x3
        CreateMaterialItem(requiredMaterialsList, "M002", 2);  // Stone x2
    }

    // ドロップ素材を表示
    void ShowDroppedMaterials(string[] materialIDs, float[] dropRates)
    {
        // 既存のアイテムをクリア
        foreach (Transform child in droppedMaterialsList)
        {
            Destroy(child.gameObject);
        }

        // ドロップ素材を表示
        for (int i = 0; i < materialIDs.Length && i < dropRates.Length; i++)
        {
            CreateMaterialItem(droppedMaterialsList, materialIDs[i], 1, dropRates[i]);
        }
    }

    // 素材アイテム表示を作成
    void CreateMaterialItem(Transform parent, string materialID, int quantity, float dropRate = 0)
    {
        if (materialItemPrefab == null) return;

        GameObject item = Instantiate(materialItemPrefab, parent);

        // 素材アイコンと名前を設定
        var materialData = InventoryManager.Instance?.GetMaterialData(materialID);
        if (materialData != null)
        {
            Text nameText = item.GetComponentInChildren<Text>();
            if (nameText != null)
            {
                if (dropRate > 0)
                    nameText.text = $"{materialData.materialName} x{quantity} ({dropRate:P0})";
                else
                    nameText.text = $"{materialData.materialName} x{quantity}";
            }

            // TODO: アイコン設定
        }
    }

    // レアリティ表示設定
    void SetRarityDisplay(Rarity rarity)
    {
        if (rarityCornerMark == null) return;

        switch (rarity)
        {
            case Rarity.Common:
                rarityCornerMark.color = commonColor;
                break;
            case Rarity.Uncommon:
                rarityCornerMark.color = uncommonColor;
                break;
            case Rarity.Rare:
                rarityCornerMark.color = rareColor;
                break;
        }
    }

    // 説明文を取得（仮実装）
    string GetDescription(string descriptionID)
    {
        // TODO: ローカライゼーションシステムから説明文を取得
        return $"Description for {descriptionID}";
    }

    // アイテムをクラフト
    void CraftItem()
    {
        if (currentItem == null || currentItem.itemType != InventoryItem.ItemType.Furniture) return;

        // TODO: クラフトシステムの実装
        Debug.Log($"Crafting {currentItem.itemID}");

        // 仮実装：素材を消費して家具を追加
        // InventoryManager.Instance.RemoveMaterial("M001", 3);
        // InventoryManager.Instance.RemoveMaterial("M002", 2);
        // InventoryManager.Instance.AddFurniture(currentItem.itemID, 1);

        ClosePanel();
    }

    // アイテムを売却
    void SellItem()
    {
        if (currentItem == null) return;

        int sellPrice = 0;

        if (currentItem.itemType == InventoryItem.ItemType.Furniture)
        {
            var furnitureData = FurnitureDataManager.Instance?.GetFurnitureData(currentItem.itemID);
            if (furnitureData != null)
            {
                sellPrice = furnitureData.sellPrice;
                InventoryManager.Instance.RemoveFurniture(currentItem.itemID, 1);
            }
        }
        else
        {
            var materialData = InventoryManager.Instance?.GetMaterialData(currentItem.itemID);
            if (materialData != null)
            {
                sellPrice = materialData.sellPrice;
                InventoryManager.Instance.RemoveMaterial(currentItem.itemID, 1);
            }
        }

        MoneyManager.Instance?.AddMoney(sellPrice);
        Debug.Log($"Sold item for {sellPrice} coins");

        // 数量が0になったらパネルを閉じる
        if (currentItem.quantity <= 1)
        {
            ClosePanel();
        }
    }

    // パネルを閉じる
    void ClosePanel()
    {
        gameObject.SetActive(false);
        currentItem = null;
    }
}

// ボタンの状態変更ハンドラー
public class ButtonStateHandler : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler, UnityEngine.EventSystems.IPointerDownHandler, UnityEngine.EventSystems.IPointerUpHandler
{
    public Sprite normalSprite;
    public Sprite hoverSprite;
    public Sprite pressedSprite;
    public Sprite disabledSprite;

    private Button button;
    private Image buttonImage;
    private bool isPressed = false;

    void Start()
    {
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();

        if (buttonImage != null && normalSprite != null)
            buttonImage.sprite = normalSprite;
    }

    public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (button != null && button.interactable && !isPressed)
        {
            if (buttonImage != null && hoverSprite != null)
                buttonImage.sprite = hoverSprite;
        }
    }

    public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (button != null && button.interactable && !isPressed)
        {
            if (buttonImage != null && normalSprite != null)
                buttonImage.sprite = normalSprite;
        }
    }

    public void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (button != null && button.interactable)
        {
            isPressed = true;
            if (buttonImage != null && pressedSprite != null)
                buttonImage.sprite = pressedSprite;
        }
    }

    public void OnPointerUp(UnityEngine.EventSystems.PointerEventData eventData)
    {
        isPressed = false;
        if (button != null && button.interactable)
        {
            if (buttonImage != null && normalSprite != null)
                buttonImage.sprite = normalSprite;
        }
    }

    void Update()
    {
        // 無効状態の表示
        if (button != null && !button.interactable)
        {
            if (buttonImage != null && disabledSprite != null)
                buttonImage.sprite = disabledSprite;
        }
    }
}
