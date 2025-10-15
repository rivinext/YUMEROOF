using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Components;

public class FurnitureDescriptionPanel : MonoBehaviour
{
    [Header("Description Elements")]
    public TMP_Text descriptionText;           // 説明文
    public Image rarityCornerMark;            // レアリティコーナーマーク（右上）

    [Header("Required Materials Section")]
    public GameObject requiredMaterialsContainer; // 必要素材コンテナ
    public Transform requiredMaterialsList;    // 必要素材リスト
    public GameObject materialItemPrefab;      // 素材アイテムプレハブ（互換性のため残す）
    public GameObject requiredMaterialPrefab;  // 必要素材専用プレハブ（推奨）

    [Header("Dropped Materials Section")]
    public GameObject droppedMaterialsContainer;  // ドロップ素材コンテナ
    public Transform droppedMaterialsList;     // ドロップ素材リスト
    public GameObject droppedMaterialPrefab;   // ドロップ素材専用プレハブ（推奨）

    [Header("Sell Section")]
    public GameObject sellSection;             // 売却セクション全体
    public Button sellButton;                  // 売却ボタン
    public TMP_Text sellPriceText;             // 売却価格テキスト

    [Header("Button Sprites")]
    public Sprite sellButtonNormal;            // 売却ボタン通常
    public Sprite sellButtonHover;             // 売却ボタンホバー
    public Sprite sellButtonPressed;           // 売却ボタン押下

    [Header("Rarity Corner Images")]
    public Sprite commonCorner;                // Common用コーナー画像
    public Sprite uncommonCorner;              // Uncommon用コーナー画像
    public Sprite rareCorner;                  // Rare用コーナー画像

    [Header("Material Colors")]
    public Color insufficientMaterialColor = new Color(1f, 0.267f, 0.267f, 1f); // #FF4444

    // 現在表示中のアイテム
    private InventoryItem currentItem;
    private FurnitureDataSO currentFurnitureDataSO;

    void Start()
    {
        // 売却ボタンイベント設定
        if (sellButton != null)
        {
            sellButton.onClick.AddListener(SellItem);

            // ボタン状態ハンドラー追加
            var handler = sellButton.gameObject.AddComponent<SellButtonHandler>();
            handler.normalSprite = sellButtonNormal;
            handler.hoverSprite = sellButtonHover;
            handler.pressedSprite = sellButtonPressed;
        }
    }

    // 家具の詳細を表示
    public void ShowFurnitureDetail(InventoryItem item)
    {
        if (item == null || item.itemType != InventoryItem.ItemType.Furniture) return;

        currentItem = item;

        // ScriptableObjectを直接取得
        currentFurnitureDataSO = FurnitureDataManager.Instance?.GetFurnitureDataSO(item.itemID);

        if (currentFurnitureDataSO == null) return;

        // レシピの有無を確認
        bool hasRecipe = currentFurnitureDataSO.HasRecipe;

        // 説明文（常に表示）
        if (descriptionText != null)
        {
            descriptionText.text = GetDescription(currentFurnitureDataSO.descriptionID);

            var localizeEvent = descriptionText.GetComponent<LocalizeStringEvent>();
            if (localizeEvent == null)
                localizeEvent = descriptionText.gameObject.AddComponent<LocalizeStringEvent>();
            localizeEvent.StringReference.TableReference = "ItemDesc";
            localizeEvent.StringReference.TableEntryReference = currentFurnitureDataSO.descriptionID;
        }

        // レシピがない場合は説明文のみ表示
        if (!hasRecipe)
        {
            // レアリティコーナーマークを非表示
            if (rarityCornerMark != null)
                rarityCornerMark.gameObject.SetActive(false);

            // 各セクションを非表示
            if (requiredMaterialsContainer != null)
                requiredMaterialsContainer.SetActive(false);
            if (droppedMaterialsContainer != null)
                droppedMaterialsContainer.SetActive(false);
            if (sellSection != null)
                sellSection.SetActive(false);

            return;
        }

        // レシピがある場合は通常表示
        // レアリティコーナーマーク（右上）
        if (rarityCornerMark != null)
        {
            rarityCornerMark.gameObject.SetActive(true);
            SetRarityCorner(currentFurnitureDataSO.rarity);
        }

        // 必要素材表示
        ShowRequiredMaterials();

        // ドロップ素材表示
        ShowDroppedMaterials();

        // 売却価格（所有数が1以上の場合のみ表示）
        if (sellSection != null)
        {
            sellSection.SetActive(currentItem.quantity > 0);
            if (sellPriceText != null)
            {
                sellPriceText.text = $"Price: {currentFurnitureDataSO.sellPrice}";
            }
            if (sellButton != null)
            {
                sellButton.interactable = currentItem.quantity > 0;
            }
        }
    }

    // 必要素材を表示
    void ShowRequiredMaterials()
    {
        // 既存のアイテムをクリア
        if (requiredMaterialsList != null)
        {
            foreach (Transform child in requiredMaterialsList)
            {
                Destroy(child.gameObject);
            }
        }

        // レシピがない場合は非表示
        if (currentFurnitureDataSO == null || !currentFurnitureDataSO.HasRecipe)
        {
            if (requiredMaterialsContainer != null)
                requiredMaterialsContainer.SetActive(false);
            return;
        }

        // 有効な素材が一つでもあるかチェック
        bool hasValidMaterials = false;
        for (int i = 0; i < currentFurnitureDataSO.recipeMaterialIDs.Length; i++)
        {
            if (!string.IsNullOrEmpty(currentFurnitureDataSO.recipeMaterialIDs[i]) &&
                currentFurnitureDataSO.recipeMaterialIDs[i] != "None" &&
                currentFurnitureDataSO.recipeMaterialQuantities[i] > 0)
            {
                hasValidMaterials = true;
                break;
            }
        }

        if (!hasValidMaterials)
        {
            if (requiredMaterialsContainer != null)
                requiredMaterialsContainer.SetActive(false);
            return;
        }

        if (requiredMaterialsContainer != null)
            requiredMaterialsContainer.SetActive(true);

        bool allMaterialsSufficient = true;

        // レシピ素材を表示
        for (int i = 0; i < currentFurnitureDataSO.recipeMaterialIDs.Length; i++)
        {
            string materialID = currentFurnitureDataSO.recipeMaterialIDs[i];
            int requiredQuantity = currentFurnitureDataSO.recipeMaterialQuantities[i];

            // 空のID、"None"、または数量0の場合はスキップ
            if (string.IsNullOrEmpty(materialID) ||
                materialID == "None" ||
                materialID.Trim() == "" ||
                requiredQuantity <= 0)
                continue;

            bool isSufficient = CreateRequiredMaterialItem(materialID, requiredQuantity);
            if (!isSufficient)
                allMaterialsSufficient = false;
        }

        // クラフト可能状態を更新
        currentItem.canCraft = allMaterialsSufficient;

        // InventoryItemCardの更新をトリガー
        var inventoryUI = FindObjectOfType<InventoryUI>();
        if (inventoryUI != null)
        {
            var cardManager = inventoryUI.GetComponent<InventoryCardManager>();
            cardManager?.UpdateAllCraftableStatus();
        }
    }

    // 必要素材アイテムを作成
    bool CreateRequiredMaterialItem(string materialID, int requiredQuantity)
    {
        // MaterialIDの追加検証
        if (string.IsNullOrEmpty(materialID) ||
            materialID == "None" ||
            materialID.Trim() == "" ||
            requiredQuantity <= 0)
        {
            Debug.LogWarning($"Invalid material data: ID={materialID}, Quantity={requiredQuantity}");
            return true; // 無効なデータは充足扱いにする
        }

        // 使用するプレハブを決定（専用プレハブ優先）
        GameObject prefabToUse = requiredMaterialPrefab != null ? requiredMaterialPrefab : materialItemPrefab;

        if (prefabToUse == null || requiredMaterialsList == null) return false;

        GameObject item = Instantiate(prefabToUse, requiredMaterialsList);

        // MaterialDataSOを取得
        var materialDataSO = FurnitureDataManager.Instance?.GetMaterialDataSO(materialID);
        if (materialDataSO == null)
        {
            Debug.LogWarning($"MaterialDataSO not found for ID: {materialID}");
            Destroy(item); // 無効なアイテムは削除
            return true;
        }

        // 所持数を取得
        int owned = InventoryManager.Instance.GetItemCount(InventoryItem.ItemType.Material, materialID);
        bool isSufficient = owned >= requiredQuantity;

        // UI要素の取得と設定（複数の名前パターンに対応）
        // アイコン設定
        Image iconImage = item.transform.Find("MaterialIcon")?.GetComponent<Image>();
        if (iconImage == null) iconImage = item.transform.Find("Icon")?.GetComponent<Image>();
        if (iconImage != null && materialDataSO.icon != null)
        {
            iconImage.sprite = materialDataSO.icon;
        }

        // 名前設定
        TMP_Text nameText = item.transform.Find("MaterialName")?.GetComponent<TMP_Text>();
        if (nameText == null) nameText = item.transform.Find("Name")?.GetComponent<TMP_Text>();
        if (nameText != null)
        {
            nameText.text = LocalizationSettings.StringDatabase.GetLocalizedString("MaterialNames", materialDataSO.nameID);

            var localizeEvent = nameText.GetComponent<LocalizeStringEvent>();
            if (localizeEvent == null)
                localizeEvent = nameText.gameObject.AddComponent<LocalizeStringEvent>();
            localizeEvent.StringReference.TableReference = "MaterialNames";
            localizeEvent.StringReference.TableEntryReference = materialDataSO.nameID;
        }

        // 数量設定（所有数/必要数の形式）
        TMP_Text countText = item.transform.Find("RequiredCount")?.GetComponent<TMP_Text>();
        if (countText == null) countText = item.transform.Find("Count")?.GetComponent<TMP_Text>();
        if (countText == null) countText = item.transform.Find("Quantity")?.GetComponent<TMP_Text>();
        if (countText != null)
        {
            countText.text = $"{owned}/{requiredQuantity}";

            // 不足している場合は赤色で表示
            if (!isSufficient)
            {
                countText.color = insufficientMaterialColor;
            }
            // 充足している場合はPrefabのデフォルト色を使用（何もしない）
        }

        return isSufficient;
    }

    // ドロップ素材を表示
    void ShowDroppedMaterials()
    {
        // 既存のアイテムをクリア
        if (droppedMaterialsList != null)
        {
            foreach (Transform child in droppedMaterialsList)
            {
                Destroy(child.gameObject);
            }
        }

        // ドロップ素材がない場合は非表示
        if (currentFurnitureDataSO == null || !currentFurnitureDataSO.CanDropMaterial)
        {
            if (droppedMaterialsContainer != null)
                droppedMaterialsContainer.SetActive(false);
            return;
        }

        // 有効な素材が一つでもあるかチェック
        bool hasValidDrops = false;
        for (int i = 0; i < currentFurnitureDataSO.dropMaterialIDs.Length; i++)
        {
            if (!string.IsNullOrEmpty(currentFurnitureDataSO.dropMaterialIDs[i]) &&
                currentFurnitureDataSO.dropMaterialIDs[i] != "None" &&
                currentFurnitureDataSO.dropRates[i] > 0)
            {
                hasValidDrops = true;
                break;
            }
        }

        if (!hasValidDrops)
        {
            if (droppedMaterialsContainer != null)
                droppedMaterialsContainer.SetActive(false);
            return;
        }

        if (droppedMaterialsContainer != null)
            droppedMaterialsContainer.SetActive(true);

        // ドロップ素材を表示
        for (int i = 0; i < currentFurnitureDataSO.dropMaterialIDs.Length; i++)
        {
            string materialID = currentFurnitureDataSO.dropMaterialIDs[i];
            float dropRate = currentFurnitureDataSO.dropRates[i];

            // 空のID、"None"、またはドロップ率0以下の場合はスキップ
            if (string.IsNullOrEmpty(materialID) ||
                materialID == "None" ||
                materialID.Trim() == "" ||
                dropRate <= 0)
                continue;

            CreateDroppedMaterialItem(materialID, dropRate);
        }
    }

    // ドロップ素材アイテムを作成
    void CreateDroppedMaterialItem(string materialID, float dropRate)
    {
        // MaterialIDの追加検証
        if (string.IsNullOrEmpty(materialID) ||
            materialID == "None" ||
            materialID.Trim() == "" ||
            dropRate <= 0)
        {
            Debug.LogWarning($"Invalid drop data: ID={materialID}, Rate={dropRate}");
            return;
        }

        // 使用するプレハブを決定（専用プレハブ優先）
        GameObject prefabToUse = droppedMaterialPrefab != null ? droppedMaterialPrefab : materialItemPrefab;

        if (prefabToUse == null || droppedMaterialsList == null) return;

        GameObject item = Instantiate(prefabToUse, droppedMaterialsList);

        // MaterialDataSOを取得
        var materialDataSO = FurnitureDataManager.Instance?.GetMaterialDataSO(materialID);
        if (materialDataSO == null)
        {
            Debug.LogWarning($"MaterialDataSO not found for ID: {materialID}");
            Destroy(item); // 無効なアイテムは削除
            return;
        }

        // UI要素の取得と設定（複数の名前パターンに対応）
        // アイコン設定
        Image iconImage = item.transform.Find("MaterialIcon")?.GetComponent<Image>();
        if (iconImage == null) iconImage = item.transform.Find("Icon")?.GetComponent<Image>();
        if (iconImage != null && materialDataSO.icon != null)
        {
            iconImage.sprite = materialDataSO.icon;
        }

        // 名前設定（ドロップ率は表示しない）
        TMP_Text nameText = item.transform.Find("MaterialName")?.GetComponent<TMP_Text>();
        if (nameText == null) nameText = item.transform.Find("Name")?.GetComponent<TMP_Text>();
        if (nameText != null)
        {
            nameText.text = LocalizationSettings.StringDatabase.GetLocalizedString("MaterialNames", materialDataSO.nameID);

            var localizeEvent = nameText.GetComponent<LocalizeStringEvent>();
            if (localizeEvent == null)
                localizeEvent = nameText.gameObject.AddComponent<LocalizeStringEvent>();
            localizeEvent.StringReference.TableReference = "MaterialNames";
            localizeEvent.StringReference.TableEntryReference = materialDataSO.nameID;
        }

        // 数量欄は非表示または削除
        TMP_Text countText = item.transform.Find("RequiredCount")?.GetComponent<TMP_Text>();
        if (countText == null) countText = item.transform.Find("Count")?.GetComponent<TMP_Text>();
        if (countText == null) countText = item.transform.Find("Quantity")?.GetComponent<TMP_Text>();
        if (countText != null)
        {
            countText.gameObject.SetActive(false);
        }
    }

    // レアリティコーナー設定
    void SetRarityCorner(Rarity rarity)
    {
        if (rarityCornerMark == null) return;

        switch (rarity)
        {
            case Rarity.Common:
                if (commonCorner != null)
                    rarityCornerMark.sprite = commonCorner;
                break;
            case Rarity.Uncommon:
                if (uncommonCorner != null)
                    rarityCornerMark.sprite = uncommonCorner;
                break;
            case Rarity.Rare:
                if (rareCorner != null)
                    rarityCornerMark.sprite = rareCorner;
                break;
        }
    }

    // 説明文を取得
    string GetDescription(string descriptionID)
    {
        if (string.IsNullOrEmpty(descriptionID))
            return "No description available.";

        return LocalizationSettings.StringDatabase.GetLocalizedString("ItemDesc", descriptionID);
    }

    // 売却アイテム
    void SellItem()
    {
        if (currentItem == null || currentFurnitureDataSO == null || currentItem.quantity <= 0) return;

        // インベントリから削除
        if (InventoryManager.Instance.RemoveFurniture(currentItem.itemID, 1))
        {
            MoneyManager.Instance?.AddMoney(currentFurnitureDataSO.sellPrice);
            Debug.Log($"Sold {currentFurnitureDataSO.nameID} for {currentFurnitureDataSO.sellPrice} coins");

            // 数量が0になったら売却ボタンを無効化
            if (currentItem.quantity <= 0)
            {
                if (sellButton != null)
                    sellButton.interactable = false;
                if (sellSection != null)
                    sellSection.SetActive(false);
            }

            // インベントリ表示を更新
            var inventoryUI = FindObjectOfType<InventoryUI>();
            if (inventoryUI != null)
            {
                inventoryUI.RefreshInventoryDisplay();
            }

            // 表示を更新
            ShowFurnitureDetail(currentItem);
        }
    }

    // 詳細要素を非表示にする（Material選択時用）
    public void HideDetailElements()
    {
        // 必要素材セクションを非表示
        if (requiredMaterialsContainer != null)
            requiredMaterialsContainer.SetActive(false);

        // ドロップ素材セクションを非表示
        if (droppedMaterialsContainer != null)
            droppedMaterialsContainer.SetActive(false);

        // 売却セクションを非表示
        if (sellSection != null)
            sellSection.SetActive(false);

        // レアリティコーナーを非表示
        if (rarityCornerMark != null)
            rarityCornerMark.gameObject.SetActive(false);

        // 説明文をクリア
        if (descriptionText != null)
            descriptionText.text = "";
    }

    // 詳細要素を表示する（Furniture選択時用）
    public void ShowDetailElements()
    {
        // レアリティコーナーを表示
        if (rarityCornerMark != null)
            rarityCornerMark.gameObject.SetActive(true);

        // 売却セクションを表示（所有数による）
        if (sellSection != null && currentItem != null)
            sellSection.SetActive(currentItem.quantity > 0);
    }

    // 説明をクリア
    public void ClearDescription()
    {
        currentItem = null;
        currentFurnitureDataSO = null;

        if (descriptionText != null)
            descriptionText.text = "";

        // 必要素材とドロップ素材をクリア
        if (requiredMaterialsList != null)
        {
            foreach (Transform child in requiredMaterialsList)
            {
                Destroy(child.gameObject);
            }
        }

        if (droppedMaterialsList != null)
        {
            foreach (Transform child in droppedMaterialsList)
            {
                Destroy(child.gameObject);
            }
        }
    }

    // 現在選択中のアイテムを取得（クラフト用）
    public InventoryItem GetCurrentItem()
    {
        return currentItem;
    }

    // 現在選択中のFurnitureDataSOを取得（クラフト用）
    public FurnitureDataSO GetCurrentFurnitureDataSO()
    {
        return currentFurnitureDataSO;
    }

    // クラフト可能状態を取得（クラフト用）
    public bool CanCraftCurrentItem()
    {
        return currentItem != null && currentItem.canCraft;
    }
}

// 売却ボタンの状態変更ハンドラー
public class SellButtonHandler : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler, UnityEngine.EventSystems.IPointerDownHandler, UnityEngine.EventSystems.IPointerUpHandler
{
    public Sprite normalSprite;
    public Sprite hoverSprite;
    public Sprite pressedSprite;

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
}
