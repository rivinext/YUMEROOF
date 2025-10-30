using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ワンピース着用時のトップス・ボトムス連動処理をまとめた制御クラス。
/// </summary>
[DisallowMultipleComponent]
public class WardrobeOnePieceCoordinator : MonoBehaviour
{
    [Header("参照設定")]
    [SerializeField] private WardrobeUIController wardrobeController;

    [Header("カテゴリ設定")]
    [SerializeField] private WardrobeTabType onePieceCategory = WardrobeTabType.OnePiece;
    [SerializeField] private WardrobeTabType topCategory = WardrobeTabType.Tops;
    [SerializeField] private WardrobeTabType pantsCategory = WardrobeTabType.Pants;

    [Header("初期＆デフォルト着用アイテム")]
    [SerializeField] private WardrobeItemView defaultTopItem;
    [SerializeField] private WardrobeItemView defaultPantsItem;
    [SerializeField] private WardrobeItemView defaultOnePieceItem;
    [SerializeField] private bool applyDefaultsOnStart = false;

    private bool isOnePieceEquipped;
    private bool suppressCallback;

    private UnityAction<WardrobeTabType, GameObject, WardrobeItemView> equippedHandler;

    private void Reset()
    {
        if (wardrobeController == null)
        {
            wardrobeController = GetComponent<WardrobeUIController>();
        }
    }

    private void Awake()
    {
        if (wardrobeController == null)
        {
            wardrobeController = GetComponent<WardrobeUIController>();
        }

        equippedHandler = HandleItemEquipped;
    }

    private void OnEnable()
    {
        if (wardrobeController != null)
        {
            wardrobeController.OnItemEquipped.AddListener(equippedHandler);
            RefreshOnePieceState();
        }
    }

    private void Start()
    {
        if (applyDefaultsOnStart)
        {
            ApplyInitialOutfit();
        }
    }

    private void OnDisable()
    {
        if (wardrobeController != null)
        {
            wardrobeController.OnItemEquipped.RemoveListener(equippedHandler);
        }
    }

    private void HandleItemEquipped(WardrobeTabType category, GameObject instance, WardrobeItemView view)
    {
        if (suppressCallback || wardrobeController == null)
        {
            return;
        }

        bool hasWearable = view != null && !view.IsEmpty;

        if (category == onePieceCategory)
        {
            isOnePieceEquipped = hasWearable;

            if (isOnePieceEquipped)
            {
                SuppressCallbacks(() =>
                {
                    ClearCategoryIfNeeded(topCategory);
                    ClearCategoryIfNeeded(pantsCategory);
                });
            }

            return;
        }

        if (category == topCategory || category == pantsCategory)
        {
            if (!hasWearable)
            {
                return;
            }

            if (isOnePieceEquipped)
            {
                WardrobeItemView counterpartDefault = category == topCategory ? defaultPantsItem : defaultTopItem;

                SuppressCallbacks(() =>
                {
                    ClearCategoryIfNeeded(onePieceCategory);
                    EquipDefault(counterpartDefault);
                });
            }

            isOnePieceEquipped = false;
        }
    }

    private void ApplyInitialOutfit()
    {
        if (wardrobeController == null)
        {
            return;
        }

        SuppressCallbacks(() =>
        {
            if (defaultOnePieceItem != null && !defaultOnePieceItem.IsEmpty)
            {
                wardrobeController.HandleItemSelected(defaultOnePieceItem);
                isOnePieceEquipped = true;
                return;
            }

            if (defaultTopItem != null && !defaultTopItem.IsEmpty)
            {
                wardrobeController.HandleItemSelected(defaultTopItem);
            }

            if (defaultPantsItem != null && !defaultPantsItem.IsEmpty)
            {
                wardrobeController.HandleItemSelected(defaultPantsItem);
            }
        });
    }

    private void EquipDefault(WardrobeItemView target)
    {
        if (target == null || target.IsEmpty)
        {
            return;
        }

        wardrobeController.HandleItemSelected(target);
    }

    private void ClearCategoryIfNeeded(WardrobeTabType category)
    {
        if (wardrobeController.GetSelectedItem(category) != null)
        {
            wardrobeController.ClearCategory(category);
        }
    }

    private void RefreshOnePieceState()
    {
        if (wardrobeController == null)
        {
            isOnePieceEquipped = false;
            return;
        }

        WardrobeItemView current = wardrobeController.GetSelectedItem(onePieceCategory);
        isOnePieceEquipped = current != null && !current.IsEmpty;
    }

    private void SuppressCallbacks(System.Action action)
    {
        if (action == null)
        {
            return;
        }

        bool previousState = suppressCallback;
        suppressCallback = true;
        try
        {
            action.Invoke();
        }
        finally
        {
            suppressCallback = previousState;
        }
    }
}
