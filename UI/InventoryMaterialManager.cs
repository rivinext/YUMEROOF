using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

// マテリアルアイコンの管理を担当
public class InventoryMaterialManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject materialIconPrefab;

    [SerializeField]
    [Tooltip("Enable detailed debug logs for material icon refresh operations.")]
    private bool enableDebugLogging = false;

    [SerializeField]
    public MaterialDescriptionPanel materialDescPanel;

    // アイコンのキャッシュ
    private Dictionary<Transform, GameObject> materialIcons = new Dictionary<Transform, GameObject>();
    private Queue<GameObject> materialIconPool = new Queue<GameObject>();

    // UI参照
    private GameObject materialContent;
    private Transform[] materialSlots;

    // 選択管理
    private InventoryItem selectedMaterialItem;

    public void Initialize(GameObject content, Transform[] slots)
    {
        materialContent = content;
        materialSlots = slots;

        // materialSlotsが空の場合、自動取得
        if (materialSlots == null || materialSlots.Length == 0)
        {
            if (materialContent != null)
            {
                // MaterialContent の子要素（ItemSlot）を取得
                materialSlots = new Transform[materialContent.transform.childCount];
                for (int i = 0; i < materialContent.transform.childCount; i++)
                {
                    materialSlots[i] = materialContent.transform.GetChild(i);
                }
                Debug.Log($"Auto-detected {materialSlots.Length} material slots");
            }
        }

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += HandleInventoryChanged;
        }
    }

    void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= HandleInventoryChanged;
        }
    }

    public void RefreshMaterialIcons(List<InventoryItem> items)
    {
        // 既存のアイコンをクリア
        foreach (var kvp in materialIcons)
        {
            if (kvp.Value != null)
            {
                kvp.Value.SetActive(false);
                materialIconPool.Enqueue(kvp.Value);
            }
        }
        materialIcons.Clear();

        // 各スロットにアイコンを配置
        for (int i = 0; i < materialSlots.Length && i < items.Count; i++)
        {
            var icon = GetOrCreateMaterialIcon();
            if (icon == null) continue;

            icon.transform.SetParent(materialSlots[i], false);
            icon.transform.localPosition = Vector3.zero;
            icon.transform.localScale = Vector3.one;

            SetupMaterialIcon(icon, items[i]);
            materialIcons[materialSlots[i]] = icon;
            icon.SetActive(true);
        }
        // 各アイテムの詳細をログ出力
        if (ShouldLogDebug())
        {
            foreach (var item in items)
            {
                Debug.Log($"Material: {item.itemID}, Quantity: {item.quantity}");
            }
        }
    }

    private bool ShouldLogDebug()
    {
        return enableDebugLogging || Debug.isDebugBuild;
    }

    GameObject GetOrCreateMaterialIcon()
    {
        GameObject icon;

        if (materialIconPool.Count > 0)
        {
            icon = materialIconPool.Dequeue();
            Button oldButton = icon.GetComponent<Button>();
            if (oldButton != null)
            {
                oldButton.onClick.RemoveAllListeners();
            }
        }
        else
        {
            if (materialIconPrefab != null)
            {
                icon = Instantiate(materialIconPrefab);
            }
            else
            {
                Debug.LogError("MaterialIconPrefab is not assigned!");
                return null;
            }
        }

        return icon;
    }

    void SetupMaterialIcon(GameObject iconObj, InventoryItem item)
    {
        if (item == null || iconObj == null) return;

        var materialDataSO = FurnitureDataManager.Instance?.GetMaterialDataSO(item.itemID);

        // アイコン設定
        Transform iconTransform = iconObj.transform.Find("Icon");
        if (iconTransform == null) iconTransform = iconObj.transform.Find("IconImage");

        if (iconTransform != null)
        {
            Image iconImage = iconTransform.GetComponent<Image>();
            if (iconImage != null && materialDataSO != null && materialDataSO.icon != null)
            {
                iconImage.sprite = materialDataSO.icon;
                iconImage.color = Color.white;
            }
        }

        // 数量設定
        Transform quantityTransform = null;
        string[] quantityNames = { "Quantity", "Count", "QuantityText", "CountText" };
        foreach (var name in quantityNames)
        {
            quantityTransform = iconObj.transform.Find(name);
            if (quantityTransform != null) break;
        }

        if (quantityTransform != null)
        {
            Text quantityText = quantityTransform.GetComponent<Text>();
            if (quantityText != null)
            {
                quantityText.text = item.quantity.ToString();
            }
            else
            {
                TMP_Text tmpText = quantityTransform.GetComponent<TMP_Text>();
                if (tmpText != null)
                {
                    tmpText.text = item.quantity.ToString();
                }
            }
        }

        // ボタン設定
        Button button = iconObj.GetComponent<Button>();
        if (button == null)
        {
            button = iconObj.AddComponent<Button>();
        }
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnMaterialClicked(item));
    }

    void OnMaterialClicked(InventoryItem item)
    {
        selectedMaterialItem = item;
        Debug.Log($"Material clicked: {item.itemID}");

        // 説明エリア更新（必要に応じて）
        UpdateMaterialDescription(item);
    }

    void UpdateMaterialDescription(InventoryItem item)
    {
        if (materialDescPanel != null)
        {
            materialDescPanel.ShowMaterialDetail(item);
            materialDescPanel.gameObject.SetActive(true);
        }
    }

    public void ClearSelection()
    {
        selectedMaterialItem = null;
        if (materialDescPanel != null)
        {
            materialDescPanel.ClearDescription();
        }
    }

    public void Cleanup()
    {
        foreach (var kvp in materialIcons)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value);
            }
        }
        materialIcons.Clear();

        while (materialIconPool.Count > 0)
        {
            var icon = materialIconPool.Dequeue();
            if (icon != null)
            {
                Destroy(icon);
            }
        }
        materialIconPool.Clear();
    }

    private void HandleInventoryChanged()
    {
        var items = InventoryManager.Instance?.GetMaterialList();
        if (items != null)
        {
            RefreshMaterialIcons(items);
        }
    }
}
