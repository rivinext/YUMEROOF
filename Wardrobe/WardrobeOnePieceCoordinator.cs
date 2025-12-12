using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ワンピースカテゴリと Tops / Pants の排他制御、および初期装備の適用を担当するコンポーネント。
/// </summary>
[DisallowMultipleComponent]
public class WardrobeOnePieceCoordinator : MonoBehaviour
{
    [Header("参照設定")]
    [Tooltip("操作対象の WardrobeUIController を指定します。未設定の場合は同一 GameObject から自動取得します。")]
    [SerializeField] private WardrobeUIController wardrobeUIController;

    [Header("初期装備に使用する itemId（WardrobeCatalog に登録された値）")]
    [Tooltip("初期状態で着用させる OnePiece カテゴリの itemId。空欄の場合は未装備。")]
    [SerializeField] private string initialOnePieceItemId;
    [Tooltip("初期状態で着用させる Hair カテゴリの itemId。空欄の場合は未装備。")]
    [SerializeField] private string initialHairItemId;
    [Tooltip("初期状態で着用させる Tops カテゴリの itemId。空欄の場合は未装備。")]
    [SerializeField] private string initialTopsItemId;
    [Tooltip("初期状態で着用させる Pants カテゴリの itemId。空欄の場合は未装備。")]
    [SerializeField] private string initialPantsItemId;
    [Tooltip("初期状態で着用させる Shoes カテゴリの itemId。空欄の場合は未装備。")]
    [SerializeField] private string initialShoesItemId;

    /// <summary>
    /// itemId から WardrobeItemView を逆引きするための辞書。
    /// </summary>
    private readonly Dictionary<string, WardrobeItemView> itemLookup = new Dictionary<string, WardrobeItemView>(StringComparer.OrdinalIgnoreCase);

    private bool isProcessingAutoChange;
    private bool isOnePieceEquipped;
    private bool hasWardrobeSave;

    public string InitialOnePieceItemId => initialOnePieceItemId;

    private void Reset()
    {
        if (wardrobeUIController == null)
        {
            wardrobeUIController = GetComponent<WardrobeUIController>();
        }
    }

    private void Awake()
    {
        if (wardrobeUIController == null)
        {
            wardrobeUIController = GetComponent<WardrobeUIController>();
        }
    }

    private void OnEnable()
    {
        if (wardrobeUIController != null)
        {
            wardrobeUIController.OnItemEquipped.AddListener(HandleItemEquipped);
        }
    }

    private void Start()
    {
        InitializeHasWardrobeSaveFromCurrentSlot();
        RebuildItemLookup();
        ApplyInitialEquipment();
    }

    private void OnDisable()
    {
        if (wardrobeUIController != null)
        {
            wardrobeUIController.OnItemEquipped.RemoveListener(HandleItemEquipped);
        }
    }

    /// <summary>
    /// WardrobeUIController 配下の WardrobeItemView を走査して itemId の辞書を再構築します。
    /// </summary>
    public void RebuildItemLookup()
    {
        itemLookup.Clear();

        if (wardrobeUIController == null)
        {
            return;
        }

        WardrobeItemView[] views = wardrobeUIController.GetComponentsInChildren<WardrobeItemView>(true);
        for (int i = 0; i < views.Length; i++)
        {
            WardrobeItemView view = views[i];
            if (view == null)
            {
                continue;
            }

            string itemId = view.ItemId;
            if (string.IsNullOrEmpty(itemId))
            {
                continue;
            }

            itemLookup[itemId] = view;
        }
    }

    /// <summary>
    /// 初期装備として指定された itemId を適用します。
    /// </summary>
    public void ApplyInitialEquipment()
    {
        if (ShouldSkipInitialEquipment())
        {
            return;
        }

        ApplyInitialEquipmentForCategory(WardrobeTabType.Hair, initialHairItemId);
        ApplyInitialEquipmentForCategory(WardrobeTabType.Tops, initialTopsItemId);
        ApplyInitialEquipmentForCategory(WardrobeTabType.Pants, initialPantsItemId);
        ApplyInitialEquipmentForCategory(WardrobeTabType.Shoes, initialShoesItemId);
    }

    private bool ShouldSkipInitialEquipment()
    {
        return hasWardrobeSave;
    }

    public void SetHasWardrobeSave(bool value)
    {
        hasWardrobeSave = value;
    }

    private void InitializeHasWardrobeSaveFromCurrentSlot()
    {
        try
        {
            SaveGameManager saveGameManager = SaveGameManager.Instance;
            if (saveGameManager == null)
            {
                return;
            }

            string slotKey = saveGameManager.CurrentSlotKey;
            if (string.IsNullOrEmpty(slotKey))
            {
                return;
            }

            BaseSaveData metadata = saveGameManager.LoadMetadata(slotKey);
            if (metadata is StorySaveData storyData)
            {
                SetHasWardrobeSave(storyData.hasWardrobeSelections);
                return;
            }

            if (metadata is CreativeSaveData creativeData)
            {
                SetHasWardrobeSave(creativeData.hasWardrobeSelections);
            }
        }
        catch
        {
            // Do not log errors to avoid noisy startup when metadata is unavailable.
        }
    }

    private void ApplyInitialEquipmentForCategory(WardrobeTabType category, string itemId)
    {
        if (wardrobeUIController == null)
        {
            return;
        }

        WardrobeItemView selectedItem = wardrobeUIController.GetSelectedItem(category);
        if (selectedItem != null)
        {
            return;
        }

        EquipByItemId(category, itemId);
    }

    private void HandleItemEquipped(WardrobeTabType category, GameObject instance, WardrobeItemView source)
    {
        if (isProcessingAutoChange)
        {
            return;
        }

        if (source != null && !string.IsNullOrEmpty(source.ItemId))
        {
            itemLookup[source.ItemId] = source;
        }

        if (category == WardrobeTabType.OnePiece)
        {
            bool hasOnePiece = source != null && !source.IsEmpty;
            if (hasOnePiece)
            {
                isOnePieceEquipped = true;

                try
                {
                    isProcessingAutoChange = true;
                    wardrobeUIController.ClearCategory(WardrobeTabType.Tops);
                    wardrobeUIController.ClearCategory(WardrobeTabType.Pants);
                }
                finally
                {
                    isProcessingAutoChange = false;
                }
            }
            else
            {
                isOnePieceEquipped = false;
            }

            return;
        }

        if (!isOnePieceEquipped)
        {
            return;
        }

        if (category == WardrobeTabType.Tops)
        {
            ExitOnePieceAndRestore(WardrobeTabType.Pants, initialPantsItemId);
        }
        else if (category == WardrobeTabType.Pants)
        {
            ExitOnePieceAndRestore(WardrobeTabType.Tops, initialTopsItemId);
        }
    }

    private void ExitOnePieceAndRestore(WardrobeTabType partnerCategory, string partnerItemId)
    {
        try
        {
            isProcessingAutoChange = true;
            wardrobeUIController.ClearCategory(WardrobeTabType.OnePiece);
            isOnePieceEquipped = false;
        }
        finally
        {
            isProcessingAutoChange = false;
        }

        EquipByItemId(partnerCategory, partnerItemId);
    }

    private void EquipByItemId(WardrobeTabType category, string itemId)
    {
        if (wardrobeUIController == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(itemId))
        {
            try
            {
                isProcessingAutoChange = true;
                wardrobeUIController.ClearCategory(category);
            }
            finally
            {
                isProcessingAutoChange = false;
            }

            return;
        }

        WardrobeItemView view = FindItemViewByItemId(itemId);
        if (view == null)
        {
            Debug.LogWarningFormat(this, "[WardrobeOnePieceCoordinator] itemId '{0}' に対応する WardrobeItemView が見つかりませんでした。", itemId);
            return;
        }

        if (view.Category != category)
        {
            Debug.LogWarningFormat(this, "[WardrobeOnePieceCoordinator] itemId '{0}' はカテゴリ '{1}' に属していません (実際: {2})。", itemId, category, view.Category);
            return;
        }

        try
        {
            isProcessingAutoChange = true;
            wardrobeUIController.HandleItemSelected(view);
        }
        finally
        {
            isProcessingAutoChange = false;
        }
    }

    private WardrobeItemView FindItemViewByItemId(string targetItemId)
    {
        WardrobeItemView view;
        if (itemLookup.TryGetValue(targetItemId, out view) && view != null)
        {
            return view;
        }

        RebuildItemLookup();

        if (itemLookup.TryGetValue(targetItemId, out view) && view != null)
        {
            return view;
        }

        return null;
    }
}
