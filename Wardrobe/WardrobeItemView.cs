using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

[DisallowMultipleComponent]
public class WardrobeItemView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private WardrobeTabType category;
    [SerializeField] private GameObject wearablePrefab;
    [SerializeField] private List<WearablePart> partPrefabs = new List<WearablePart>();
    [SerializeField] private Button button;
    [SerializeField] private GameObject selectionIndicator;
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image emptyStateImage;
    [SerializeField] private string itemId;

    [Header("Hover Animation")]
    [SerializeField] private RectTransform hoverTarget;
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float hoverTilt = 5f;
    [SerializeField] private float hoverDuration = 0.18f;

    [Header("Hover Audio")]
    [SerializeField] private AudioClip hoverSfx;
    [SerializeField] private AudioSource hoverAudioSource;
    [SerializeField, Range(0f, 1f)] private float hoverSfxVolume = 1f;
    [SerializeField, Min(0f)] private float hoverSfxCooldown = 0.1f;

    private string displayName;
    private string nameId;
    private string descriptionId;
    private Sprite iconSprite;

    private WardrobeUIController owner;
    private bool isSelected;
    private float lastHoverSfxTime = -10f;
    private float currentSfxVolume = 1f;
    private RectTransform resolvedHoverTarget;
    private Vector3 baseScale = Vector3.one;
    private Vector3 baseEulerAngles = Vector3.zero;
    private Tween hoverTween;

    public WardrobeTabType Category => category;
    public GameObject WearablePrefab => wearablePrefab;
    public bool IsEmpty => wearablePrefab == null;
    public string DisplayName => displayName;
    public string NameId => nameId;
    public string DescriptionId => descriptionId;
    public Sprite IconSprite => iconSprite;
    public string ItemId => itemId;
    public bool HasPartPrefabs => partPrefabs != null && partPrefabs.Count > 0;
    public IReadOnlyList<WearablePart> PartPrefabs
    {
        get
        {
            if (partPrefabs == null)
            {
                partPrefabs = new List<WearablePart>();
            }

            return partPrefabs;
        }
    }

    [Serializable]
    public class WearablePart
    {
        public string partName;
        public GameObject prefab;

        public string PartName => partName;
        public GameObject Prefab => prefab;
    }

    private void Reset()
    {
        button = GetComponent<Button>();
    }

    private void Awake()
    {
        // ✅ ここで TMP_Text のクリック貫通を設定
        TMP_Text[] tmpTexts = GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in tmpTexts)
        {
            tmp.raycastTarget = false; // ← 貫通ON
        }

        UpdateEmptyStateVisuals();
        SetupHoverAudioSource();

        resolvedHoverTarget = hoverTarget != null ? hoverTarget : transform as RectTransform;

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
    }

    private void OnEnable()
    {
        RefreshSelectionState();
        AudioManager.OnSfxVolumeChanged += HandleSfxVolumeChanged;
        HandleSfxVolumeChanged(AudioManager.CurrentSfxVolume);
    }

    private void OnDisable()
    {
        KillHoverTween();
        ResetHoverTargetTransform();
        AudioManager.OnSfxVolumeChanged -= HandleSfxVolumeChanged;
    }

    internal void Initialize(WardrobeUIController wardrobeUIController)
    {
        if (owner == wardrobeUIController)
        {
            RefreshSelectionState();
            return;
        }

        if (owner != null)
        {
            Unbind();
        }

        owner = wardrobeUIController;
        Bind();
        RefreshSelectionState();
    }

    public void SetWearablePrefab(GameObject prefab)
    {
        wearablePrefab = prefab;
    }

    public void ApplyCatalogEntry(WardrobeCatalogEntry entry)
    {
        category = entry.TabType;
        displayName = entry.DisplayName;
        SetNameId(entry.NameId);
        descriptionId = entry.DescriptionId;
        itemId = entry.ItemId;

        string viewName = !string.IsNullOrEmpty(entry.DisplayName) ? entry.DisplayName : entry.NameId;
        if (!string.IsNullOrEmpty(viewName))
        {
            gameObject.name = viewName;
        }

        GameObject prefab = entry.WearablePrefab;
        if (prefab == null && !string.IsNullOrEmpty(entry.Model3D))
        {
            prefab = Resources.Load<GameObject>(entry.Model3D);
        }

        SetWearablePrefab(prefab);

        Sprite sprite = entry.ImageSprite;
        if (sprite == null && !string.IsNullOrEmpty(entry.Image2D))
        {
            sprite = Resources.Load<Sprite>(entry.Image2D);
        }

        SetIcon(sprite);

        UpdateEmptyStateVisuals();
    }

    public void SetNameId(string value)
    {
        nameId = value;
        if (nameLabel != null)
        {
            nameLabel.text = value;
        }
    }

    public void SetIcon(Sprite sprite)
    {
        iconSprite = sprite;
        if (iconImage != null)
        {
            iconImage.sprite = sprite;
        }

        UpdateEmptyStateVisuals();
    }

    private void Bind()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }
    }

    private void Unbind()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClicked);
        }
    }

    private void OnDestroy()
    {
        Unbind();

        AudioManager.OnSfxVolumeChanged -= HandleSfxVolumeChanged;
        KillHoverTween();

        if (owner != null)
        {
            owner.HandleItemDestroyed(this);
            owner = null;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayHoverSfx();

        if (resolvedHoverTarget == null)
        {
            return;
        }

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
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (resolvedHoverTarget == null)
        {
            return;
        }

        KillHoverTween();
        resolvedHoverTarget.localEulerAngles = baseEulerAngles;
        hoverTween = resolvedHoverTarget.DOScale(baseScale, Mathf.Max(hoverDuration, 0.01f))
            .SetEase(Ease.OutQuad)
            .OnComplete(() => hoverTween = null);
    }

    private void OnButtonClicked()
    {
        if (owner != null)
        {
            owner.HandleItemSelected(this);
        }
    }

    internal void SetSelected(bool selected)
    {
        isSelected = selected;

        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(selected);
        }
    }

    private void RefreshSelectionState()
    {
        if (owner == null)
        {
            SetSelected(false);
            return;
        }

        WardrobeItemView currentSelection = owner.GetSelectedItem(category);
        SetSelected(currentSelection == this);
    }

    private void UpdateEmptyStateVisuals()
    {
        bool isEmptyItemId = !string.IsNullOrEmpty(itemId) && itemId.IndexOf("empty", StringComparison.OrdinalIgnoreCase) >= 0;

        if (emptyStateImage != null)
        {
            emptyStateImage.gameObject.SetActive(isEmptyItemId);
        }

        if (nameLabel != null)
        {
            nameLabel.gameObject.SetActive(!isEmptyItemId);
        }

        if (iconImage != null)
        {
            iconImage.gameObject.SetActive(!isEmptyItemId);
            iconImage.enabled = !isEmptyItemId && iconImage.sprite != null;
        }
    }

    private void SetupHoverAudioSource()
    {
        if (hoverAudioSource == null)
        {
            hoverAudioSource = GetComponent<AudioSource>();
            if (hoverAudioSource == null)
            {
                hoverAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (hoverAudioSource != null)
        {
            hoverAudioSource.playOnAwake = false;
            hoverAudioSource.loop = false;
            hoverAudioSource.spatialBlend = 0f;
        }
    }

    private void HandleSfxVolumeChanged(float value)
    {
        currentSfxVolume = Mathf.Clamp01(value);
    }

    private void PlayHoverSfx()
    {
        if (hoverSfx == null || hoverAudioSource == null)
        {
            return;
        }

        float elapsed = Time.unscaledTime - lastHoverSfxTime;
        if (elapsed < hoverSfxCooldown)
        {
            return;
        }

        float volume = hoverSfxVolume * currentSfxVolume;
        if (volume <= 0f)
        {
            return;
        }

        hoverAudioSource.PlayOneShot(hoverSfx, volume);
        lastHoverSfxTime = Time.unscaledTime;
    }

    private void ResetHoverTargetTransform()
    {
        if (resolvedHoverTarget == null)
        {
            return;
        }

        resolvedHoverTarget.localScale = baseScale;
        resolvedHoverTarget.localEulerAngles = baseEulerAngles;
    }

    private void KillHoverTween()
    {
        if (hoverTween == null)
        {
            return;
        }

        hoverTween.Kill();
        hoverTween = null;
    }
}
