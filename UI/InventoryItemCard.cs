using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization;
using DG.Tweening;

public class InventoryItemCard : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Elements")]
    public Image itemImage;
    public TMP_Text itemNameText;
    public TMP_Text quantityText;
    public Image backgroundImage;
    public Image rarityCornerMark;
    public Toggle favoriteToggle;
    public Image favoriteOffImage;
    public Image favoriteOnImage;
    public GameObject uncraftableOverlay;

    // Localization
    [SerializeField] private LocalizedString localizedName = new LocalizedString();

    [Header("Hover Animation")]
    [SerializeField] private RectTransform hoverTarget;
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float hoverTilt = 5f;
    [SerializeField] private float hoverDuration = 0.18f;

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private InventoryCardAudioProfile audioProfile;
    private float lastHoverTime;
    private float currentSfxVolume = 1f;

    private RectTransform resolvedHoverTarget;
    private Vector3 baseScale;
    private Vector3 baseEulerAngles;
    private Tween hoverTween;

    [Header("Attributes - Furniture Only")]
    public GameObject cozyContainer;
    public Image cozyIcon;
    public TMP_Text cozyText;
    public GameObject natureContainer;
    public Image natureIcon;
    public TMP_Text natureText;
    public Image weatherIcon;

    [Header("Card States")]
    public Sprite defaultBackground;
    public Sprite selectedBackground;
    public Sprite uncraftableBackground;

    [Header("Rarity Corner Images")]
    public Sprite commonCorner;
    public Sprite uncommonCorner;
    public Sprite rareCorner;

    [Header("Weather Icons")]
    public Sprite windIcon;
    public Sprite rainIcon;

    // イベント
    public event Action<InventoryItem> OnItemClicked;
    public event Action<InventoryItem> OnItemDragged;
    public event Action<InventoryItem> OnFavoriteToggled;

    // 現在のアイテム
    public InventoryItem currentItem;
    private bool isMaterialCard;
    private bool isDragging = false;
    private GameObject dragPreview;
    private bool dragBlocked = false;

    // 選択状態
    private bool isSelected = false;

    void Awake()
    {
        resolvedHoverTarget = hoverTarget != null ? hoverTarget : transform as RectTransform;

        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        if (sfxSource != null)
        {
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f;
            ApplyAudioProfileSettings();
        }

        if (resolvedHoverTarget != null)
        {
            baseScale = resolvedHoverTarget.localScale;
            baseEulerAngles = resolvedHoverTarget.localEulerAngles;
        }
        else
        {
            baseScale = Vector3.one;
            baseEulerAngles = Vector3.zero;
        }

        KillHoverTween();
        ResetHoverTargetTransform();

        // Toggleのイベント設定
        if (favoriteToggle != null)
        {
            favoriteToggle.onValueChanged.RemoveAllListeners();
            favoriteToggle.onValueChanged.AddListener(OnFavoriteChanged);

            // Toggleの構造から画像を自動取得（設定されていない場合）
            if (favoriteOffImage == null)
            {
                Transform bgTransform = favoriteToggle.transform.Find("Background");
                if (bgTransform != null)
                    favoriteOffImage = bgTransform.GetComponent<Image>();
            }

            if (favoriteOnImage == null)
            {
                Transform checkTransform = favoriteToggle.transform.Find("Checkmark");
                if (checkTransform != null)
                    favoriteOnImage = checkTransform.GetComponent<Image>();
            }
        }
    }

    void OnEnable()
    {
        AudioManager.OnSfxVolumeChanged += HandleSfxVolumeChanged;
        HandleSfxVolumeChanged(AudioManager.CurrentSfxVolume);
    }

    void OnDisable()
    {
        KillHoverTween();
        ResetHoverTargetTransform();

        AudioManager.OnSfxVolumeChanged -= HandleSfxVolumeChanged;

        if (sfxSource != null && sfxSource.isPlaying)
        {
            sfxSource.Stop();
        }
    }

    void OnDestroy()
    {
        localizedName.StringChanged -= OnNameChanged;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (resolvedHoverTarget == null) return;

        KillHoverTween();
        ResetHoverTargetTransform();

        Vector3 targetScale = baseScale * hoverScale;
        Vector3 tiltedRotation = baseEulerAngles + new Vector3(0f, 0f, hoverTilt);
        float duration = Mathf.Max(hoverDuration, 0.01f);

        Sequence sequence = DOTween.Sequence();
        sequence.Join(resolvedHoverTarget.DOScale(targetScale, duration).SetEase(Ease.OutQuad));
        sequence.Join(resolvedHoverTarget.DOLocalRotate(tiltedRotation, duration * 0.5f).SetEase(Ease.OutQuad));
        sequence.Append(resolvedHoverTarget.DOLocalRotate(baseEulerAngles, duration * 0.5f).SetEase(Ease.OutQuad));
        sequence.OnComplete(() => hoverTween = null);
        hoverTween = sequence;

        PlayHoverSound();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (resolvedHoverTarget == null) return;

        KillHoverTween();
        resolvedHoverTarget.localEulerAngles = baseEulerAngles;
        hoverTween = resolvedHoverTarget.DOScale(baseScale, Mathf.Max(hoverDuration, 0.01f))
            .SetEase(Ease.OutQuad)
            .OnComplete(() => hoverTween = null);
    }

    // アイテムを設定
    public void SetItem(InventoryItem item, bool isMaterial)
    {
        currentItem = item;
        isMaterialCard = isMaterial;

        // レシピがない家具はクラフト可能として扱う
        if (item.itemType == InventoryItem.ItemType.Furniture)
        {
            var furnitureData = FurnitureDataManager.Instance?.GetFurnitureData(item.itemID);
            if (furnitureData == null || string.IsNullOrEmpty(furnitureData.recipeID))
            {
                item.canCraft = true;
                item.isUnlocked = true;
            }
        }

        UpdateDisplay();
    }

    // 選択状態を設定
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateVisualState();
    }

    // カードの視覚状態を更新
    public void UpdateVisualState()
    {
        if (backgroundImage == null) return;

        if (currentItem != null && currentItem.itemType == InventoryItem.ItemType.Furniture)
        {
            bool hasRecipe = HasRecipe();
            bool canCraft = currentItem.canCraft;
            int ownedQuantity = currentItem.quantity;

            // 背景画像の設定（常に設定する）
            if (isSelected)
            {
                // 選択状態
                if (selectedBackground != null)
                {
                    backgroundImage.sprite = selectedBackground;
                }
            }
            else
            {
                // 通常状態（UncraftableOverlayが表示されていても背景は通常のものを使用）
                if (defaultBackground != null)
                {
                    backgroundImage.sprite = defaultBackground;
                }
            }

            // Uncraftableオーバーレイの表示制御
            // レシピあり & クラフト不可（材料不足） & 所有数0 の場合のみ表示
            if (uncraftableOverlay != null)
            {
                bool showOverlay = hasRecipe && !canCraft && ownedQuantity == 0;
                uncraftableOverlay.SetActive(showOverlay);

                if (showOverlay)
                {
                    AdjustOverlayOrder();
                }
            }
        }
        else
        {
            // Furniture以外はオーバーレイ非表示
            if (uncraftableOverlay != null)
            {
                uncraftableOverlay.SetActive(false);
            }

            // 通常の背景を設定
            if (defaultBackground != null)
            {
                backgroundImage.sprite = defaultBackground;
            }
        }
    }

    // オーバーレイの順序を調整
    void AdjustOverlayOrder()
    {
        if (uncraftableOverlay == null || favoriteToggle == null) return;

        // FavoriteToggleのSibling Indexを取得
        int favoriteIndex = favoriteToggle.transform.GetSiblingIndex();

        // UncraftableOverlayをFavoriteToggleの直前に配置
        uncraftableOverlay.transform.SetSiblingIndex(favoriteIndex - 1);
    }

    // レシピがあるかチェック
    bool HasRecipe()
    {
        if (currentItem == null || currentItem.itemType != InventoryItem.ItemType.Furniture)
            return false;

        var furnitureData = FurnitureDataManager.Instance?.GetFurnitureData(currentItem.itemID);
        return furnitureData != null && !string.IsNullOrEmpty(furnitureData.recipeID);
    }

    // 表示を更新
    void UpdateDisplay()
    {
        if (currentItem == null) return;

        if (currentItem.itemType == InventoryItem.ItemType.Furniture)
        {
            DisplayFurniture();
        }
        else
        {
            DisplayMaterial();
        }

        // お気に入り状態を更新
        UpdateFavoriteDisplay();
    }

    // お気に入り表示を更新
    void UpdateFavoriteDisplay()
    {
        if (favoriteToggle != null)
        {
            // Toggleの状態を設定（イベントを発火させない）
            favoriteToggle.SetIsOnWithoutNotify(currentItem.isFavorite);

            // Checkmark（On画像）の表示/非表示を明示的に設定
            if (favoriteOnImage != null)
            {
                favoriteOnImage.gameObject.SetActive(currentItem.isFavorite);
            }

            // デバッグログ
            Debug.Log($"Item: {currentItem.itemID}, Favorite: {currentItem.isFavorite}");
        }
    }

    // 家具を表示
    void DisplayFurniture()
    {
        localizedName.StringChanged -= OnNameChanged;
        var furnitureData = FurnitureDataManager.Instance?.GetFurnitureData(currentItem.itemID);
        if (furnitureData == null) return;

        // アイテム名
        if (itemNameText != null)
        {
            localizedName.TableReference = "ItemNames";
            localizedName.TableEntryReference = furnitureData.nameID;
            localizedName.StringChanged += OnNameChanged;
            localizedName.RefreshString();
        }

        // アイテム画像
        var icon = FurnitureDataManager.Instance?.GetFurnitureIcon(currentItem.itemID);
        if (itemImage != null && icon != null)
        {
            itemImage.sprite = icon;
        }

        // 数量表示（修正：常に表示）
        if (quantityText != null)
        {
            quantityText.gameObject.SetActive(true);
            quantityText.text = $"×{currentItem.quantity}";
        }

        // Cozy/Nature値表示（0の場合も"0"を表示）
        if (cozyContainer != null)
        {
            cozyContainer.SetActive(true);
            if (cozyText != null)
                cozyText.text = furnitureData.cozy.ToString();
        }
        if (natureContainer != null)
        {
            natureContainer.SetActive(true);
            if (natureText != null)
                natureText.text = furnitureData.nature.ToString();
        }

        // レアリティコーナーマーク
        SetRarityCorner(furnitureData.rarity);

        // 天候属性アイコン
        if (weatherIcon != null)
        {
            switch (furnitureData.weatherAttribute)
            {
                case WeatherAttribute.Wind:
                    weatherIcon.gameObject.SetActive(true);
                    weatherIcon.sprite = windIcon;
                    break;
                case WeatherAttribute.Rain:
                    weatherIcon.gameObject.SetActive(true);
                    weatherIcon.sprite = rainIcon;
                    break;
                default:
                    weatherIcon.gameObject.SetActive(false);
                    break;
            }
        }

        // クラフト可能状態をチェック（レシピがある場合）
        if (HasRecipe())
        {
            CheckCraftableStatus(furnitureData);
        }

        // 視覚状態を更新
        UpdateVisualState();

        // Favoriteボタンを最前面に
        EnsureFavoriteOnTop();
    }

    // クラフト可能状態をチェック
    void CheckCraftableStatus(FurnitureData furnitureData)
    {
        if (furnitureData.recipeMaterialIDs == null || furnitureData.recipeMaterialIDs.Length == 0)
        {
            currentItem.canCraft = true;
            return;
        }

        bool canCraft = true;
        for (int i = 0; i < furnitureData.recipeMaterialIDs.Length; i++)
        {
            string materialID = furnitureData.recipeMaterialIDs[i];
            int required = furnitureData.recipeMaterialQuantities[i];

            if (!string.IsNullOrEmpty(materialID) && required > 0)
            {
                int owned = InventoryManager.Instance.GetItemCount(InventoryItem.ItemType.Material, materialID);
                if (owned < required)
                {
                    canCraft = false;
                    break;
                }
            }
        }

        currentItem.canCraft = canCraft;
    }

    // Favoriteボタンを最前面に配置
    void EnsureFavoriteOnTop()
    {
        if (favoriteToggle != null)
        {
            favoriteToggle.transform.SetAsLastSibling();
        }
    }

    void ResetHoverTargetTransform()
    {
        if (resolvedHoverTarget == null) return;

        resolvedHoverTarget.localScale = baseScale;
        resolvedHoverTarget.localEulerAngles = baseEulerAngles;
    }

    void KillHoverTween()
    {
        if (hoverTween == null) return;

        hoverTween.Kill();
        hoverTween = null;
    }

    // 素材を表示
    void DisplayMaterial()
    {
        localizedName.StringChanged -= OnNameChanged;
        var materialData = InventoryManager.Instance?.GetMaterialData(currentItem.itemID);
        if (materialData == null) return;

        if (itemNameText != null)
        {
            localizedName.TableReference = "ItemNames";
            localizedName.TableEntryReference = materialData.nameID;
            localizedName.StringChanged += OnNameChanged;
            localizedName.RefreshString();
        }

        if (quantityText != null)
        {
            quantityText.gameObject.SetActive(true);
            quantityText.text = currentItem.quantity.ToString();
        }

        if (cozyContainer != null) cozyContainer.SetActive(false);
        if (natureContainer != null) natureContainer.SetActive(false);

        SetRarityCorner(materialData.rarity);

        if (weatherIcon != null)
        {
            switch (materialData.weatherAttribute)
            {
                case WeatherAttribute.Wind:
                    weatherIcon.gameObject.SetActive(true);
                    weatherIcon.sprite = windIcon;
                    break;
                case WeatherAttribute.Rain:
                    weatherIcon.gameObject.SetActive(true);
                    weatherIcon.sprite = rainIcon;
                    break;
                default:
                    weatherIcon.gameObject.SetActive(false);
                    break;
            }
        }

        if (uncraftableOverlay != null)
            uncraftableOverlay.SetActive(false);
    }

    void OnNameChanged(string value)
    {
        if (itemNameText != null)
        {
            itemNameText.text = value;
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

    // お気に入り変更時の処理（Toggle用）
    void OnFavoriteChanged(bool isOn)
    {
        if (currentItem == null) return;

        // アイテムのお気に入り状態を更新
        currentItem.isFavorite = isOn;

        // Checkmark（On画像）の表示を更新
        if (favoriteOnImage != null)
        {
            favoriteOnImage.gameObject.SetActive(isOn);
        }

        Debug.Log($"Favorite changed - Item: {currentItem.itemID}, IsOn: {isOn}");

        // イベントを発火
        OnFavoriteToggled?.Invoke(currentItem);
    }

    // クリックイベント
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isDragging && currentItem != null)
        {
            // Toggleとその子要素のクリックは除外
            GameObject clicked = eventData.pointerCurrentRaycast.gameObject;
            if (favoriteToggle != null)
            {
                if (clicked == favoriteToggle.gameObject ||
                    clicked.transform.IsChildOf(favoriteToggle.transform))
                {
                    return;
                }
            }

            PlayClickSound();
            OnItemClicked?.Invoke(currentItem);
        }
    }

    void HandleSfxVolumeChanged(float value)
    {
        currentSfxVolume = Mathf.Clamp01(value);
    }

    void PlayHoverSound()
    {
        if (sfxSource == null || audioProfile == null || audioProfile.hoverClip == null)
            return;

        float currentTime = Time.unscaledTime;
        float cooldown = Mathf.Max(0f, audioProfile.cooldownSeconds);
        if (currentTime - lastHoverTime < cooldown)
            return;

        lastHoverTime = currentTime;
        PlayProfileClip(audioProfile.hoverClip);
    }

    void PlayClickSound()
    {
        if (sfxSource == null || audioProfile == null || audioProfile.clickClip == null)
            return;

        PlayProfileClip(audioProfile.clickClip);
    }

    void ApplyAudioProfileSettings()
    {
        if (audioProfile == null || sfxSource == null)
            return;

        if (audioProfile.outputMixerGroup != null)
        {
            sfxSource.outputAudioMixerGroup = audioProfile.outputMixerGroup;
        }

        sfxSource.rolloffMode = audioProfile.rolloffMode;
    }

    void PlayProfileClip(AudioClip clip)
    {
        if (clip == null || sfxSource == null || audioProfile == null)
            return;

        float pitchOffset = Mathf.Max(0f, audioProfile.pitchRandomization);
        float pitch = 1f;
        if (pitchOffset > 0f)
        {
            pitch = UnityEngine.Random.Range(1f - pitchOffset, 1f + pitchOffset);
        }

        float originalPitch = sfxSource.pitch;
        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, audioProfile.baseVolume * currentSfxVolume);
        sfxSource.pitch = originalPitch;
    }

    // ドラッグ開始
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (InventoryPlacementBridge.Instance.IsPlacementDisabledScene())
        {
            dragBlocked = true;
            return;
        }
        if (currentItem == null || isMaterialCard) return;
        if (currentItem.itemType != InventoryItem.ItemType.Furniture) return;

        // 所有数が0の場合はドラッグ不可
        if (currentItem.quantity == 0) return;

        isDragging = true;

        dragPreview = new GameObject("DragPreview");
        Image previewImage = dragPreview.AddComponent<Image>();
        previewImage.sprite = itemImage.sprite;
        previewImage.raycastTarget = false;

        Canvas canvas = GetComponentInParent<Canvas>();
        dragPreview.transform.SetParent(canvas.transform, false);
        dragPreview.transform.SetAsLastSibling();

        RectTransform rect = dragPreview.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(80, 80);

        Color c = previewImage.color;
        c.a = 0.7f;
        previewImage.color = c;

        OnItemDragged?.Invoke(currentItem);
    }

    // ドラッグ中
    public void OnDrag(PointerEventData eventData)
    {
        if (dragBlocked) return;
        if (dragPreview != null)
        {
            dragPreview.transform.position = Input.mousePosition;
        }
    }

    // ドラッグ終了（デバッグ版）
    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragBlocked)
        {
            dragBlocked = false;
            return;
        }
        Debug.Log("OnEndDrag called");
        isDragging = false;

        if (dragPreview != null)
        {
            Destroy(dragPreview);
            dragPreview = null;
        }

        // デバッグ：現在のアイテム情報
        if (currentItem != null)
        {
            Debug.Log($"Current Item: ID={currentItem.itemID}, Quantity={currentItem.quantity}, Type={currentItem.itemType}");
        }
        else
        {
            Debug.Log("Current Item is null");
        }

        // インベントリパネル外にドロップしたかチェック
        bool isOutside = true;
        var inventoryUI = GetComponentInParent<InventoryUI>();
        if (inventoryUI != null)
        {
            bool isPointerOverInventory = inventoryUI.IsPointerOverInventoryWindow(eventData.position, eventData.pressEventCamera);
            isOutside = !isPointerOverInventory;
        }
        Debug.Log($"Dropped outside inventory panel: {isOutside}");

        // インベントリパネル外にドロップした場合、配置を開始
        if (isOutside && currentItem != null && currentItem.quantity > 0)
        {
            Debug.Log($"Starting placement for item: {currentItem.itemID}");

            // InventoryPlacementBridgeを使用して配置を開始
            var bridge = InventoryPlacementBridge.Instance;
            if (bridge != null)
            {
                Debug.Log("InventoryPlacementBridge found, starting placement");
                bridge.StartPlacementFromInventory(currentItem);
            }
            else
            {
                Debug.LogError("InventoryPlacementBridge.Instance is null!");
            }
        }
        else
        {
            Debug.Log($"Not starting placement - Outside: {isOutside}, Item: {currentItem != null}, Quantity: {currentItem?.quantity ?? 0}");
        }

        dragBlocked = false;
    }

    // ポインターがインベントリパネル上にあるかチェック（修正版）
    bool IsPointerOverInventoryPanel(PointerEventData eventData)
    {
        var inventoryUI = GetComponentInParent<InventoryUI>();
        if (inventoryUI == null)
        {
            return false;
        }

        return inventoryUI.IsPointerOverInventoryWindow(eventData.position, eventData.pressEventCamera);
    }
}
