using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public enum WardrobeTabType
{
    Hair,
    Accessories,
    Eyewear,
    Tops,
    Pants,
    OnePiece,
    Shoes
}

public class WardrobeUIController : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private WardrobePanelAnimator panelAnimator;
    [SerializeField] private Button openCloseButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private ToggleGroup tabToggleGroup;
    [SerializeField] private List<CategoryTab> categoryTabs = new List<CategoryTab>();
    [SerializeField] private List<AttachmentPoint> attachmentPoints = new List<AttachmentPoint>();
    [SerializeField] private List<AttachmentPoint> gameAttachmentPoints = new List<AttachmentPoint>();
    [SerializeField] private Transform previewPlayerRoot;
    [SerializeField] private Transform gamePlayerRoot;
    [SerializeField] private Camera previewCamera;
    [SerializeField] private RawImage previewTargetImage;
    [SerializeField] private float previewRotateSpeed = 0.25f;
    [SerializeField] private bool previewUseDeltaTime = false;
    [SerializeField] private bool autoRegisterItemsOnAwake = true;
    [SerializeField] private WardrobeCatalog wardrobeCatalog;
    [SerializeField] private WardrobeItemView wardrobeItemViewPrefab;
    [SerializeField] private TMP_Text descriptionText;

    [Serializable]
    private class CategoryTab
    {
        public WardrobeTabType category;
        public Toggle toggle;
        public GameObject content;
    }

    [Serializable]
    public class AttachmentPoint
    {
        public WardrobeTabType category;
        public WardrobeBodyAnchor anchor = WardrobeBodyAnchor.None;
        public Transform mountPoint;
        public bool resetLocalTransform = true;

        public WardrobeBodyAnchor GetResolvedAnchor()
        {
            if (anchor != WardrobeBodyAnchor.None)
            {
                return anchor;
            }

            return GetDefaultAnchorForCategory(category);
        }

        public void EnsureAnchorInitialized()
        {
            if (anchor == WardrobeBodyAnchor.None)
            {
                WardrobeBodyAnchor defaultAnchor = GetDefaultAnchorForCategory(category);
                if (defaultAnchor != WardrobeBodyAnchor.None)
                {
                    anchor = defaultAnchor;
                }
            }
        }
    }

    [Serializable]
    public class WardrobeEquipEvent : UnityEvent<WardrobeTabType, WardrobeEquippedSet, WardrobeItemView> { }

    [SerializeField] private WardrobeEquipEvent onItemEquipped = new WardrobeEquipEvent();

    private readonly List<UnityAction<bool>> toggleHandlers = new List<UnityAction<bool>>();
    private readonly Dictionary<WardrobeTabType, Dictionary<WardrobeBodyAnchor, AttachmentPoint>> attachmentLookup = new Dictionary<WardrobeTabType, Dictionary<WardrobeBodyAnchor, AttachmentPoint>>();
    private readonly Dictionary<WardrobeTabType, Dictionary<WardrobeBodyAnchor, AttachmentPoint>> gameAttachmentLookup = new Dictionary<WardrobeTabType, Dictionary<WardrobeBodyAnchor, AttachmentPoint>>();
    private readonly Dictionary<WardrobeTabType, WardrobeEquippedSet> previewEquippedInstances = new Dictionary<WardrobeTabType, WardrobeEquippedSet>();
    private readonly Dictionary<WardrobeTabType, WardrobeEquippedSet> gameEquippedInstances = new Dictionary<WardrobeTabType, WardrobeEquippedSet>();
    private readonly Dictionary<WardrobeTabType, WardrobeItemView> activeSelections = new Dictionary<WardrobeTabType, WardrobeItemView>();
    private readonly List<WardrobeItemView> registeredItems = new List<WardrobeItemView>();
    private readonly List<WardrobeItemView> runtimeGeneratedItems = new List<WardrobeItemView>();

    private const int InvalidPointerId = -1;   // 追加：無効ポインタID
    private static readonly List<GameObject> WearablePartsBuffer = new List<GameObject>();
    private bool isDraggingPreview;
    private int activePointerId = InvalidPointerId;
    private Vector2 lastPointerPosition;
    private Quaternion previewInitialRotation;
    private bool previewInitialRotationCaptured;

    public WardrobeEquipEvent OnItemEquipped
    {
        get { return onItemEquipped; }
    }

    [Obsolete("Use GetEquippedSet instead. Returns the first equipped instance in the set.")]
    public GameObject GetEquippedInstance(WardrobeTabType category)
    {
        WardrobeEquippedSet set = GetEquippedSet(category);
        return set != null ? set.GetFirstInstance() : null;
    }

    [Obsolete("Use GetGameEquippedSet instead. Returns the first equipped instance in the set.")]
    public GameObject GetGameEquippedInstance(WardrobeTabType category)
    {
        WardrobeEquippedSet set = GetGameEquippedSet(category);
        return set != null ? set.GetFirstInstance() : null;
    }

    public WardrobeEquippedSet GetEquippedSet(WardrobeTabType category)
    {
        return GetEquippedSetInternal(previewEquippedInstances, category);
    }

    public WardrobeEquippedSet GetGameEquippedSet(WardrobeTabType category)
    {
        return GetEquippedSetInternal(gameEquippedInstances, category);
    }

    public WardrobeItemView GetSelectedItem(WardrobeTabType category)
    {
        WardrobeItemView item;
        activeSelections.TryGetValue(category, out item);
        return item;
    }

    private void Reset()
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        if (panelAnimator == null)
        {
            panelAnimator = GetComponent<WardrobePanelAnimator>();
        }
    }

    private void OnValidate()
    {
        InitializeAttachmentAnchors(attachmentPoints);
        InitializeAttachmentAnchors(gameAttachmentPoints);
    }

    private void Awake()
    {
        if (panelAnimator == null)
        {
            panelAnimator = GetComponentInChildren<WardrobePanelAnimator>();
        }

        if (panelRoot == null && panelAnimator != null)
        {
            panelRoot = panelAnimator.gameObject;
        }

        if (openCloseButton != null)
        {
            openCloseButton.onClick.AddListener(TogglePanel);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }

        if (panelAnimator != null)
        {
            panelAnimator.VisibilityChanged.AddListener(OnPanelVisibilityChanged);
        }

        BuildAttachmentLookup();
        BuildGameAttachmentLookup();
        SetupTabs();
        PopulateCatalogItems();

        if (autoRegisterItemsOnAwake)
        {
            RegisterItemViewsInChildren();
        }

        UpdateDescription(null);
        InitializePreviewTarget();
        UpdatePreviewActivation(panelAnimator != null && panelAnimator.IsShown);
        CapturePreviewInitialRotation();
        SetupPreviewEventTrigger();
    }

    private void Start()
    {
        EnsureAnyTabIsActive();
    }

    private void OnDestroy()
    {
        if (openCloseButton != null)
        {
            openCloseButton.onClick.RemoveListener(TogglePanel);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(OnCloseButtonClicked);
        }

        if (panelAnimator != null)
        {
            panelAnimator.VisibilityChanged.RemoveListener(OnPanelVisibilityChanged);
        }

        for (int i = 0; i < categoryTabs.Count && i < toggleHandlers.Count; i++)
        {
            CategoryTab tab = categoryTabs[i];
            UnityAction<bool> handler = toggleHandlers[i];

            if (tab != null && tab.toggle != null && handler != null)
            {
                tab.toggle.onValueChanged.RemoveListener(handler);
            }
        }
    }

    public void ShowPanel(bool instant = false)
    {
        if (panelAnimator != null)
        {
            if (!panelAnimator.IsShown || instant)
            {
                UpdatePreviewActivation(true);
            }

            panelAnimator.Show(instant);
        }
        else if (panelRoot != null)
        {
            panelRoot.SetActive(true);
            UpdatePreviewActivation(true);
        }
    }

    public void HidePanel(bool instant = false)
    {
        if (panelAnimator != null)
        {
            panelAnimator.Hide(instant);
            if (instant)
            {
                UpdatePreviewActivation(false);
            }
        }
        else if (panelRoot != null)
        {
            panelRoot.SetActive(false);
            UpdatePreviewActivation(false);
        }
    }

    private void OnCloseButtonClicked()
    {
        HidePanel();
    }

    public void TogglePanel()
    {
        if (panelAnimator != null)
        {
            if (panelAnimator.IsShown)
            {
                HidePanel();
            }
            else
            {
                ShowPanel();
            }
        }
        else if (panelRoot != null)
        {
            bool activate = !panelRoot.activeSelf;
            panelRoot.SetActive(activate);
            UpdatePreviewActivation(activate);
        }
    }

    public void Equip(WardrobeTabType category, GameObject prefab)
    {
        WardrobeItemView view = FindItemView(category, prefab);
        EquipItem(category, prefab, view);
    }

    public void ClearCategory(WardrobeTabType category)
    {
        WardrobeItemView view = FindItemView(category, null);
        EquipItem(category, null, view);
    }

    public void RegisterRuntimeItem(WardrobeItemView view)
    {
        if (view == null || registeredItems.Contains(view))
        {
            return;
        }

        registeredItems.Add(view);
        view.Initialize(this);
    }

    internal void HandleItemSelected(WardrobeItemView itemView)
    {
        if (itemView == null)
        {
            return;
        }

        EquipItem(itemView.Category, itemView.WearablePrefab, itemView);
    }

    internal void HandleItemDestroyed(WardrobeItemView itemView)
    {
        if (itemView == null)
        {
            return;
        }

        registeredItems.Remove(itemView);
        runtimeGeneratedItems.Remove(itemView);

        WardrobeItemView active;
        if (activeSelections.TryGetValue(itemView.Category, out active) && active == itemView)
        {
            activeSelections.Remove(itemView.Category);
        }
    }

    private void EquipItem(WardrobeTabType category, GameObject prefab, WardrobeItemView source)
    {
        WardrobeEquippedSet previewSetForEvent = null;

        WearablePartsBuffer.Clear();
        if (source != null)
        {
            source.CollectWearablePrefabs(WearablePartsBuffer);
        }

        if (WearablePartsBuffer.Count == 0 && prefab != null)
        {
            WearablePartsBuffer.Add(prefab);
        }

        if (WearablePartsBuffer.Count == 0)
        {
            RemoveEquippedInstances(previewEquippedInstances, category, WardrobeBodyAnchor.None);
            RemoveEquippedInstances(gameEquippedInstances, category, WardrobeBodyAnchor.None);
            previewEquippedInstances.Remove(category);
            gameEquippedInstances.Remove(category);
        }
        else
        {
            WardrobeBodyAnchor anchor = ResolveAnchorForItem(category, source);

            RemoveEquippedInstances(previewEquippedInstances, category, anchor);
            RemoveEquippedInstances(gameEquippedInstances, category, anchor);

            WardrobeEquippedSet newPreviewSet = null;
            WardrobeEquippedSet newGameSet = null;

            for (int i = 0; i < WearablePartsBuffer.Count; i++)
            {
                GameObject partPrefab = WearablePartsBuffer[i];
                if (partPrefab == null)
                {
                    continue;
                }

                AttachmentPoint previewAttachmentPoint;
                GameObject newPreviewInstance = InstantiateForAttachment(partPrefab, attachmentLookup, category, anchor, "Preview", true, out previewAttachmentPoint);

                if (newPreviewInstance != null)
                {
                    if (newPreviewSet == null)
                    {
                        newPreviewSet = new WardrobeEquippedSet();
                    }

                    WardrobeBodyAnchor resolvedAnchor = previewAttachmentPoint != null ? previewAttachmentPoint.GetResolvedAnchor() : anchor;
                    if (resolvedAnchor == WardrobeBodyAnchor.None)
                    {
                        resolvedAnchor = GetDefaultAnchorForCategory(category);
                    }

                    newPreviewSet.Add(resolvedAnchor, newPreviewInstance);
                }

                AttachmentPoint gameAttachmentPoint;
                GameObject newGameInstance = InstantiateForAttachment(partPrefab, gameAttachmentLookup, category, anchor, "Game", false, out gameAttachmentPoint);

                if (newGameInstance != null)
                {
                    if (newGameSet == null)
                    {
                        newGameSet = new WardrobeEquippedSet();
                    }

                    WardrobeBodyAnchor resolvedAnchor = gameAttachmentPoint != null ? gameAttachmentPoint.GetResolvedAnchor() : anchor;
                    if (resolvedAnchor == WardrobeBodyAnchor.None)
                    {
                        resolvedAnchor = GetDefaultAnchorForCategory(category);
                    }

                    newGameSet.Add(resolvedAnchor, newGameInstance);
                }
            }

            if (newPreviewSet != null && !newPreviewSet.IsEmpty)
            {
                previewEquippedInstances[category] = newPreviewSet;
                previewSetForEvent = newPreviewSet;
            }
            else
            {
                previewEquippedInstances.Remove(category);
            }

            if (newGameSet != null && !newGameSet.IsEmpty)
            {
                gameEquippedInstances[category] = newGameSet;
            }
            else
            {
                gameEquippedInstances.Remove(category);
            }
        }

        previewSetForEvent = GetEquippedSetInternal(previewEquippedInstances, category);
        WearablePartsBuffer.Clear();

        UpdateSelectionState(category, source);
        onItemEquipped.Invoke(category, previewSetForEvent, source);
        UpdateDescription(source);
    }

    private void ApplyLayerRecursively(GameObject target, int layer)
    {
        if (target == null)
        {
            return;
        }

        target.layer = layer;

        Transform targetTransform = target.transform;
        for (int i = 0; i < targetTransform.childCount; i++)
        {
            Transform child = targetTransform.GetChild(i);
            if (child != null)
            {
                ApplyLayerRecursively(child.gameObject, layer);
            }
        }
    }

    private void UpdateSelectionState(WardrobeTabType category, WardrobeItemView selectedView)
    {
        WardrobeItemView currentSelected;
        if (activeSelections.TryGetValue(category, out currentSelected) && currentSelected != null && currentSelected != selectedView)
        {
            currentSelected.SetSelected(false);
        }

        if (selectedView != null)
        {
            selectedView.SetSelected(true);
            activeSelections[category] = selectedView;
        }
        else
        {
            activeSelections.Remove(category);
        }
    }

    private void DestroyInstance(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(instance);
        }
        else
        {
            DestroyImmediate(instance);
        }
    }

    private void BuildAttachmentLookup()
    {
        BuildAttachmentLookupInternal(attachmentLookup, attachmentPoints, previewPlayerRoot);
    }

    private void BuildGameAttachmentLookup()
    {
        BuildAttachmentLookupInternal(gameAttachmentLookup, gameAttachmentPoints, gamePlayerRoot);
    }

    private void BuildAttachmentLookupInternal(Dictionary<WardrobeTabType, Dictionary<WardrobeBodyAnchor, AttachmentPoint>> lookup, List<AttachmentPoint> points, Transform root)
    {
        lookup.Clear();

        if (points == null)
        {
            return;
        }

        for (int i = 0; i < points.Count; i++)
        {
            AttachmentPoint point = points[i];
            if (point == null || point.mountPoint == null)
            {
                continue;
            }

            if (root != null && !point.mountPoint.IsChildOf(root))
            {
                continue;
            }

            point.EnsureAnchorInitialized();

            Dictionary<WardrobeBodyAnchor, AttachmentPoint> categoryLookup;
            if (!lookup.TryGetValue(point.category, out categoryLookup))
            {
                categoryLookup = new Dictionary<WardrobeBodyAnchor, AttachmentPoint>();
                lookup.Add(point.category, categoryLookup);
            }

            WardrobeBodyAnchor resolvedAnchor = point.GetResolvedAnchor();
            if (!categoryLookup.ContainsKey(resolvedAnchor))
            {
                categoryLookup.Add(resolvedAnchor, point);
            }
        }
    }

    private GameObject InstantiateForAttachment(GameObject prefab, Dictionary<WardrobeTabType, Dictionary<WardrobeBodyAnchor, AttachmentPoint>> lookup, WardrobeTabType category, WardrobeBodyAnchor anchor, string attachmentRole, bool logWarnings, out AttachmentPoint usedAttachmentPoint)
    {
        usedAttachmentPoint = null;

        if (prefab == null)
        {
            return null;
        }

        AttachmentPoint attachmentPoint;
        if (!TryGetAttachmentPoint(lookup, category, anchor, out attachmentPoint) || attachmentPoint == null || attachmentPoint.mountPoint == null)
        {
            if (logWarnings)
            {
                WardrobeBodyAnchor resolvedAnchor = anchor != WardrobeBodyAnchor.None ? anchor : GetDefaultAnchorForCategory(category);
                Debug.LogWarningFormat(this, "[WardrobeUIController] {0} attachment point for category '{1}' (anchor '{2}') is not configured.", attachmentRole, category, resolvedAnchor);
            }

            return null;
        }

        usedAttachmentPoint = attachmentPoint;
        return InstantiateForAttachment(prefab, attachmentPoint);
    }

    private GameObject InstantiateForAttachment(GameObject prefab, AttachmentPoint attachmentPoint)
    {
        if (prefab == null || attachmentPoint == null || attachmentPoint.mountPoint == null)
        {
            return null;
        }

        GameObject instance = Instantiate(prefab, attachmentPoint.mountPoint, false);
        if (instance == null)
        {
            return null;
        }

        int mountLayer = attachmentPoint.mountPoint.gameObject.layer;
        if (mountLayer >= 0 && mountLayer < 32)
        {
            ApplyLayerRecursively(instance, mountLayer);
        }

        if (attachmentPoint.resetLocalTransform)
        {
            Transform instanceTransform = instance.transform;
            instanceTransform.localPosition = Vector3.zero;
            instanceTransform.localRotation = Quaternion.identity;
            instanceTransform.localScale = Vector3.one;
        }

        return instance;
    }

    private void RemoveEquippedInstances(Dictionary<WardrobeTabType, WardrobeEquippedSet> lookup, WardrobeTabType category, WardrobeBodyAnchor anchor)
    {
        if (lookup == null)
        {
            return;
        }

        WardrobeEquippedSet set;
        if (!lookup.TryGetValue(category, out set) || set == null)
        {
            return;
        }

        List<GameObject> removedInstances = anchor == WardrobeBodyAnchor.None
            ? set.RemoveAll()
            : set.Remove(anchor);

        if (anchor == WardrobeBodyAnchor.None || set.IsEmpty)
        {
            lookup.Remove(category);
        }

        for (int i = 0; i < removedInstances.Count; i++)
        {
            DestroyInstance(removedInstances[i]);
        }
    }

    private WardrobeEquippedSet GetEquippedSetInternal(Dictionary<WardrobeTabType, WardrobeEquippedSet> lookup, WardrobeTabType category)
    {
        if (lookup == null)
        {
            return null;
        }

        WardrobeEquippedSet set;
        return lookup.TryGetValue(category, out set) ? set : null;
    }

    private static bool TryGetAttachmentPoint(Dictionary<WardrobeTabType, Dictionary<WardrobeBodyAnchor, AttachmentPoint>> lookup, WardrobeTabType category, WardrobeBodyAnchor anchor, out AttachmentPoint attachmentPoint)
    {
        attachmentPoint = null;

        if (lookup == null)
        {
            return false;
        }

        Dictionary<WardrobeBodyAnchor, AttachmentPoint> anchorLookup;
        if (!lookup.TryGetValue(category, out anchorLookup) || anchorLookup == null)
        {
            return false;
        }

        if (anchor != WardrobeBodyAnchor.None && anchorLookup.TryGetValue(anchor, out attachmentPoint) && attachmentPoint != null && attachmentPoint.mountPoint != null)
        {
            return true;
        }

        WardrobeBodyAnchor defaultAnchor = GetDefaultAnchorForCategory(category);
        if (defaultAnchor != WardrobeBodyAnchor.None && anchorLookup.TryGetValue(defaultAnchor, out attachmentPoint) && attachmentPoint != null && attachmentPoint.mountPoint != null)
        {
            return true;
        }

        foreach (KeyValuePair<WardrobeBodyAnchor, AttachmentPoint> pair in anchorLookup)
        {
            AttachmentPoint candidate = pair.Value;
            if (candidate != null && candidate.mountPoint != null)
            {
                attachmentPoint = candidate;
                return true;
            }
        }

        return false;
    }

    private WardrobeBodyAnchor ResolveAnchorForItem(WardrobeTabType category, WardrobeItemView source)
    {
        return GetDefaultAnchorForCategory(category);
    }

    private static WardrobeBodyAnchor GetDefaultAnchorForCategory(WardrobeTabType category)
    {
        switch (category)
        {
            case WardrobeTabType.Hair:
                return WardrobeBodyAnchor.HeadTop;
            case WardrobeTabType.Accessories:
                return WardrobeBodyAnchor.HeadAccessory;
            case WardrobeTabType.Eyewear:
                return WardrobeBodyAnchor.Eyes;
            case WardrobeTabType.Tops:
                return WardrobeBodyAnchor.UpperBody;
            case WardrobeTabType.Pants:
                return WardrobeBodyAnchor.LowerBody;
            case WardrobeTabType.OnePiece:
                return WardrobeBodyAnchor.FullBody;
            case WardrobeTabType.Shoes:
                return WardrobeBodyAnchor.Feet;
            default:
                return WardrobeBodyAnchor.None;
        }
    }

    private void InitializeAttachmentAnchors(List<AttachmentPoint> points)
    {
        if (points == null)
        {
            return;
        }

        for (int i = 0; i < points.Count; i++)
        {
            AttachmentPoint point = points[i];
            if (point == null)
            {
                continue;
            }

            point.EnsureAnchorInitialized();
        }
    }

    private void SetupTabs()
    {
        toggleHandlers.Clear();

        if (tabToggleGroup == null && categoryTabs.Count > 0)
        {
            ToggleGroup group = GetComponentInChildren<ToggleGroup>();
            tabToggleGroup = group;
        }

        for (int i = 0; i < categoryTabs.Count; i++)
        {
            CategoryTab tab = categoryTabs[i];
            UnityAction<bool> handler = null;

            if (tab != null && tab.toggle != null)
            {
                if (tabToggleGroup != null)
                {
                    tab.toggle.group = tabToggleGroup;
                }

                int index = i;
                handler = delegate (bool value) { OnTabToggled(index, value); };
                tab.toggle.onValueChanged.AddListener(handler);
            }

            bool isActive = tab != null && tab.toggle != null && tab.toggle.isOn;
            if (tab != null && tab.content != null)
            {
                tab.content.SetActive(isActive);
            }

            toggleHandlers.Add(handler);
        }
    }

    private void OnTabToggled(int tabIndex, bool isOn)
    {
        if (tabIndex < 0 || tabIndex >= categoryTabs.Count)
        {
            return;
        }

        CategoryTab tab = categoryTabs[tabIndex];
        if (tab == null)
        {
            return;
        }

        if (tab.content != null)
        {
            tab.content.SetActive(isOn);
        }
    }

    private void RegisterItemViewsInChildren()
    {
        registeredItems.Clear();
        WardrobeItemView[] views = GetComponentsInChildren<WardrobeItemView>(true);

        for (int i = 0; i < views.Length; i++)
        {
            WardrobeItemView view = views[i];
            if (view == null)
            {
                continue;
            }

            registeredItems.Add(view);
            view.Initialize(this);
        }
    }

    private void PopulateCatalogItems()
    {
        if (wardrobeCatalog == null || wardrobeItemViewPrefab == null)
        {
            return;
        }

        for (int i = 0; i < runtimeGeneratedItems.Count; i++)
        {
            WardrobeItemView generated = runtimeGeneratedItems[i];
            if (generated == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(generated.gameObject);
            }
            else
            {
                DestroyImmediate(generated.gameObject);
            }
        }

        runtimeGeneratedItems.Clear();

        Dictionary<WardrobeTabType, Transform> parentLookup = new Dictionary<WardrobeTabType, Transform>();
        for (int i = 0; i < categoryTabs.Count; i++)
        {
            CategoryTab tab = categoryTabs[i];
            if (tab == null || tab.content == null)
            {
                continue;
            }

            Transform parentTransform = tab.content.transform;
            if (parentTransform == null)
            {
                continue;
            }

            if (!parentLookup.ContainsKey(tab.category))
            {
                parentLookup.Add(tab.category, parentTransform);
            }
        }

        IReadOnlyList<WardrobeCatalogEntry> entries = wardrobeCatalog.Entries;
        if (entries == null)
        {
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            WardrobeCatalogEntry entry = entries[i];
            Transform parent;
            if (!parentLookup.TryGetValue(entry.TabType, out parent) || parent == null)
            {
                continue;
            }

            WardrobeItemView viewInstance = Instantiate(wardrobeItemViewPrefab, parent);
            if (viewInstance == null)
            {
                continue;
            }

            viewInstance.ApplyCatalogEntry(entry);
            runtimeGeneratedItems.Add(viewInstance);
        }
    }

    private void UpdateDescription(WardrobeItemView view)
    {
        if (descriptionText == null)
        {
            return;
        }

        if (view == null)
        {
            descriptionText.text = string.Empty;
            return;
        }

        descriptionText.text = !string.IsNullOrEmpty(view.DescriptionId)
            ? view.DescriptionId
            : view.DisplayName;
    }

    private void EnsureAnyTabIsActive()
    {
        for (int i = 0; i < categoryTabs.Count; i++)
        {
            CategoryTab tab = categoryTabs[i];
            if (tab != null && tab.toggle != null && tab.toggle.isOn)
            {
                OnTabToggled(i, true);
                return;
            }
        }

        for (int i = 0; i < categoryTabs.Count; i++)
        {
            CategoryTab tab = categoryTabs[i];
            if (tab != null && tab.toggle != null)
            {
                tab.toggle.isOn = true;
                OnTabToggled(i, true);
                break;
            }
        }
    }

    private void InitializePreviewTarget()
    {
        if (previewTargetImage != null && previewCamera != null)
        {
            previewTargetImage.texture = previewCamera.targetTexture;
        }
    }

    private void UpdatePreviewActivation(bool visible)
    {
        if (previewPlayerRoot != null)
        {
            previewPlayerRoot.gameObject.SetActive(visible);
        }

        if (previewCamera != null)
        {
            previewCamera.gameObject.SetActive(visible);
        }

        if (visible)
        {
            InitializePreviewTarget();
            CapturePreviewInitialRotation();
        }
        else
        {
            ResetPreviewInteractionState();
            RestorePreviewRotation();
        }
    }

    private void CapturePreviewInitialRotation()
    {
        if (previewPlayerRoot == null)
        {
            return;
        }

        previewInitialRotation = previewPlayerRoot.rotation;
        previewInitialRotationCaptured = true;
    }

    private void RestorePreviewRotation()
    {
        if (!previewInitialRotationCaptured || previewPlayerRoot == null)
        {
            return;
        }

        previewPlayerRoot.rotation = previewInitialRotation;
    }

    private void ResetPreviewInteractionState()
    {
        isDraggingPreview = false;
        lastPointerPosition = Vector2.zero;
        activePointerId = InvalidPointerId;
    }

    private void SetupPreviewEventTrigger()
    {
        if (previewTargetImage == null)
        {
            return;
        }

        EventTrigger eventTrigger = previewTargetImage.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = previewTargetImage.gameObject.AddComponent<EventTrigger>();
        }

        if (eventTrigger.triggers == null)
        {
            eventTrigger.triggers = new List<EventTrigger.Entry>();
        }

        AddEventTriggerListener(eventTrigger, EventTriggerType.PointerDown, OnPreviewPointerDown);
        AddEventTriggerListener(eventTrigger, EventTriggerType.Drag, OnPreviewDrag);
        AddEventTriggerListener(eventTrigger, EventTriggerType.PointerUp, OnPreviewPointerUp);
        AddEventTriggerListener(eventTrigger, EventTriggerType.PointerExit, OnPreviewPointerExit);
        AddEventTriggerListener(eventTrigger, EventTriggerType.Cancel, OnPreviewPointerCancel);
    }

    private void AddEventTriggerListener(EventTrigger trigger, EventTriggerType eventType, Action<PointerEventData> callback)
    {
        if (trigger == null || callback == null)
        {
            return;
        }

        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = eventType };
        entry.callback.AddListener(data =>
        {
            PointerEventData pointerEventData = data as PointerEventData;
            if (pointerEventData != null)
            {
                callback(pointerEventData);
            }
        });
        trigger.triggers.Add(entry);
    }

    private void OnPreviewPointerDown(PointerEventData eventData)
    {
        if (eventData == null)
        {
            return;
        }

        isDraggingPreview = true;
        activePointerId = eventData.pointerId;
        lastPointerPosition = eventData.position;
    }

    private void OnPreviewPointerUp(PointerEventData eventData)
    {
        if (eventData == null || eventData.pointerId != activePointerId)
        {
            return;
        }

        ResetPreviewInteractionState();
    }

    private void OnPreviewPointerExit(PointerEventData eventData)
    {
        if (eventData != null && eventData.pointerId == activePointerId)
        {
            lastPointerPosition = eventData.position;
        }
    }

    private void OnPreviewPointerCancel(PointerEventData eventData)
    {
        if (eventData == null || eventData.pointerId != activePointerId)
        {
            return;
        }

        ResetPreviewInteractionState();
    }

    private void OnPreviewDrag(PointerEventData eventData)
    {
        if (!isDraggingPreview || previewPlayerRoot == null || eventData == null || eventData.pointerId != activePointerId)
        {
            return;
        }

        Vector2 delta = eventData.position - lastPointerPosition;
        lastPointerPosition = eventData.position;

        // Negate delta.x so the preview rotates opposite to the drag direction.
        float yaw = -delta.x * previewRotateSpeed;
        if (previewUseDeltaTime)
        {
            yaw *= Time.deltaTime;
        }

        previewPlayerRoot.Rotate(0f, yaw, 0f, Space.World);
    }

    private void OnPanelVisibilityChanged(bool visible)
    {
        UpdatePreviewActivation(visible);
    }

    /// <summary>
    /// Subscribes to <see cref="OnItemEquipped"/> and forwards equipment changes to another player rig.
    /// The returned <see cref="IDisposable"/> must be disposed to unsubscribe and clean up instantiated objects.
    /// </summary>
    /// <param name="playerRoot">Optional descriptive root for the forwarded player hierarchy.</param>
    /// <param name="attachmentLookup">Attachment points organized by category and anchor for the target player.</param>
    public IDisposable SubscribeGamePlayer(Transform playerRoot, IDictionary<WardrobeTabType, Dictionary<WardrobeBodyAnchor, AttachmentPoint>> attachmentLookup)
    {
        if (attachmentLookup == null)
        {
            throw new ArgumentNullException(nameof(attachmentLookup));
        }

        return new EquipmentForwardSubscription(this, playerRoot, attachmentLookup);
    }

    public IDisposable SubscribeGamePlayer(Transform playerRoot)
    {
        return SubscribeGamePlayer(playerRoot, gameAttachmentLookup);
    }

    private WardrobeItemView FindItemView(WardrobeTabType category, GameObject prefab)
    {
        for (int i = 0; i < registeredItems.Count; i++)
        {
            WardrobeItemView view = registeredItems[i];
            if (view == null || view.Category != category)
            {
                continue;
            }

            if (prefab == null)
            {
                if (view.IsEmpty)
                {
                    return view;
                }
            }
            else if (view.WearablePrefab == prefab)
            {
                return view;
            }
        }

        return null;
    }

    private sealed class EquipmentForwardSubscription : IDisposable
    {
        private readonly WardrobeUIController controller;
        private readonly Transform playerRoot;
        private readonly Dictionary<WardrobeTabType, Dictionary<WardrobeBodyAnchor, AttachmentPoint>> attachments = new Dictionary<WardrobeTabType, Dictionary<WardrobeBodyAnchor, AttachmentPoint>>();
        private readonly Dictionary<WardrobeTabType, WardrobeEquippedSet> instances = new Dictionary<WardrobeTabType, WardrobeEquippedSet>();
        private readonly UnityAction<WardrobeTabType, WardrobeEquippedSet, WardrobeItemView> handler;
        private bool disposed;

        public EquipmentForwardSubscription(WardrobeUIController controller, Transform playerRoot, IDictionary<WardrobeTabType, Dictionary<WardrobeBodyAnchor, AttachmentPoint>> attachmentLookup)
        {
            this.controller = controller;
            this.playerRoot = playerRoot;

            if (attachmentLookup != null)
            {
                foreach (KeyValuePair<WardrobeTabType, Dictionary<WardrobeBodyAnchor, AttachmentPoint>> categoryPair in attachmentLookup)
                {
                    Dictionary<WardrobeBodyAnchor, AttachmentPoint> anchorLookup = categoryPair.Value;
                    if (anchorLookup == null)
                    {
                        continue;
                    }

                    Dictionary<WardrobeBodyAnchor, AttachmentPoint> validAnchors = null;
                    foreach (KeyValuePair<WardrobeBodyAnchor, AttachmentPoint> anchorPair in anchorLookup)
                    {
                        AttachmentPoint point = anchorPair.Value;
                        if (point == null || point.mountPoint == null)
                        {
                            continue;
                        }

                        if (this.playerRoot != null && !point.mountPoint.IsChildOf(this.playerRoot))
                        {
                            continue;
                        }

                        point.EnsureAnchorInitialized();
                        WardrobeBodyAnchor resolvedAnchor = point.GetResolvedAnchor();

                        if (validAnchors == null)
                        {
                            validAnchors = new Dictionary<WardrobeBodyAnchor, AttachmentPoint>();
                        }

                        if (!validAnchors.ContainsKey(resolvedAnchor))
                        {
                            validAnchors.Add(resolvedAnchor, point);
                        }
                    }

                    if (validAnchors != null && validAnchors.Count > 0)
                    {
                        attachments[categoryPair.Key] = validAnchors;
                    }
                }
            }

            handler = HandleEquipmentChanged;
            controller.onItemEquipped.AddListener(handler);
            SyncExistingEquipment();
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            controller.onItemEquipped.RemoveListener(handler);

            foreach (KeyValuePair<WardrobeTabType, WardrobeEquippedSet> categoryPair in instances)
            {
                WardrobeEquippedSet set = categoryPair.Value;
                if (set == null)
                {
                    continue;
                }

                List<GameObject> removed = set.RemoveAll();
                for (int i = 0; i < removed.Count; i++)
                {
                    controller.DestroyInstance(removed[i]);
                }
            }

            instances.Clear();
            attachments.Clear();
        }

        private void HandleEquipmentChanged(WardrobeTabType category, WardrobeEquippedSet previewSet, WardrobeItemView source)
        {
            Forward(category, source);
        }

        private void Forward(WardrobeTabType category, WardrobeItemView source)
        {
            if (source == null || source.WearablePrefab == null)
            {
                RemoveInstance(category, WardrobeBodyAnchor.None);
                return;
            }

            WardrobeBodyAnchor anchor = controller.ResolveAnchorForItem(category, source);
            RemoveInstance(category, anchor);

            AttachmentPoint attachmentPoint;
            if (!TryGetAttachmentPoint(attachments, category, anchor, out attachmentPoint) || attachmentPoint == null || attachmentPoint.mountPoint == null)
            {
                return;
            }

            GameObject newInstance = controller.InstantiateForAttachment(source.WearablePrefab, attachmentPoint);
            if (newInstance != null)
            {
                StoreInstance(category, attachmentPoint.GetResolvedAnchor(), newInstance);
            }
        }

        private void RemoveInstance(WardrobeTabType category, WardrobeBodyAnchor anchor)
        {
            WardrobeEquippedSet set;
            if (!instances.TryGetValue(category, out set) || set == null)
            {
                return;
            }

            List<GameObject> removed = anchor == WardrobeBodyAnchor.None
                ? set.RemoveAll()
                : set.Remove(anchor);

            if (anchor == WardrobeBodyAnchor.None || set.IsEmpty)
            {
                instances.Remove(category);
            }

            for (int i = 0; i < removed.Count; i++)
            {
                controller.DestroyInstance(removed[i]);
            }
        }

        private void StoreInstance(WardrobeTabType category, WardrobeBodyAnchor anchor, GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            WardrobeEquippedSet set;
            if (!instances.TryGetValue(category, out set) || set == null)
            {
                set = new WardrobeEquippedSet();
                instances[category] = set;
            }

            if (anchor == WardrobeBodyAnchor.None)
            {
                anchor = GetDefaultAnchorForCategory(category);
            }

            set.Add(anchor, instance);
        }

        private void SyncExistingEquipment()
        {
            foreach (KeyValuePair<WardrobeTabType, WardrobeItemView> pair in controller.activeSelections)
            {
                Forward(pair.Key, pair.Value);
            }
        }
    }
}
