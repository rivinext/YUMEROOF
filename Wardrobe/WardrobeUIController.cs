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
        public Transform mountPoint;
        public bool resetLocalTransform = true;
    }

    [Serializable]
    public class WardrobeEquipEvent : UnityEvent<WardrobeTabType, GameObject, WardrobeItemView> { }

    [SerializeField] private WardrobeEquipEvent onItemEquipped = new WardrobeEquipEvent();

    private readonly List<UnityAction<bool>> toggleHandlers = new List<UnityAction<bool>>();
    private readonly Dictionary<WardrobeTabType, AttachmentPoint> attachmentLookup = new Dictionary<WardrobeTabType, AttachmentPoint>();
    private readonly Dictionary<WardrobeTabType, AttachmentPoint> gameAttachmentLookup = new Dictionary<WardrobeTabType, AttachmentPoint>();
    private readonly Dictionary<WardrobeTabType, GameObject> previewEquippedInstances = new Dictionary<WardrobeTabType, GameObject>();
    private readonly Dictionary<WardrobeTabType, GameObject> gameEquippedInstances = new Dictionary<WardrobeTabType, GameObject>();
    private readonly Dictionary<WardrobeTabType, WardrobeItemView> activeSelections = new Dictionary<WardrobeTabType, WardrobeItemView>();
    private readonly List<WardrobeItemView> registeredItems = new List<WardrobeItemView>();
    private readonly List<WardrobeItemView> runtimeGeneratedItems = new List<WardrobeItemView>();

    private const int InvalidPointerId = -1;   // 追加：無効ポインタID
    private bool isDraggingPreview;
    private int activePointerId = InvalidPointerId;
    private Vector2 lastPointerPosition;
    private Quaternion previewInitialRotation;
    private bool previewInitialRotationCaptured;

    public WardrobeEquipEvent OnItemEquipped
    {
        get { return onItemEquipped; }
    }

    public GameObject GetEquippedInstance(WardrobeTabType category)
    {
        GameObject instance;
        previewEquippedInstances.TryGetValue(category, out instance);
        return instance;
    }

    public GameObject GetGameEquippedInstance(WardrobeTabType category)
    {
        GameObject instance;
        gameEquippedInstances.TryGetValue(category, out instance);
        return instance;
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
        GameObject currentInstance;
        if (previewEquippedInstances.TryGetValue(category, out currentInstance) && currentInstance != null)
        {
            DestroyInstance(currentInstance);
        }
        previewEquippedInstances.Remove(category);

        if (gameEquippedInstances.TryGetValue(category, out currentInstance) && currentInstance != null)
        {
            DestroyInstance(currentInstance);
        }
        gameEquippedInstances.Remove(category);

        GameObject newPreviewInstance = null;
        GameObject newGameInstance = null;

        if (prefab != null)
        {
            newPreviewInstance = InstantiateForAttachment(prefab, attachmentLookup, category, "Preview", true);
            newGameInstance = InstantiateForAttachment(prefab, gameAttachmentLookup, category, "Game", false);
        }

        if (newPreviewInstance != null)
        {
            previewEquippedInstances[category] = newPreviewInstance;
        }

        if (newGameInstance != null)
        {
            gameEquippedInstances[category] = newGameInstance;
        }

        UpdateSelectionState(category, source);
        onItemEquipped.Invoke(category, newPreviewInstance, source);
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

    private void BuildAttachmentLookupInternal(Dictionary<WardrobeTabType, AttachmentPoint> lookup, List<AttachmentPoint> points, Transform root)
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

            if (!lookup.ContainsKey(point.category))
            {
                lookup.Add(point.category, point);
            }
        }
    }

    private GameObject InstantiateForAttachment(GameObject prefab, Dictionary<WardrobeTabType, AttachmentPoint> lookup, WardrobeTabType category, string attachmentRole, bool logWarnings)
    {
        if (prefab == null)
        {
            return null;
        }

        AttachmentPoint attachmentPoint;
        if (!lookup.TryGetValue(category, out attachmentPoint) || attachmentPoint == null || attachmentPoint.mountPoint == null)
        {
            if (logWarnings)
            {
                Debug.LogWarningFormat(this, "[WardrobeUIController] {0} attachment point for category '{1}' is not configured.", attachmentRole, category);
            }

            return null;
        }

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
    /// <param name="attachmentPoints">Attachment points that define where each wardrobe category should be mounted.</param>
    public IDisposable SubscribeGamePlayer(Transform playerRoot, IEnumerable<AttachmentPoint> attachmentPoints)
    {
        if (attachmentPoints == null)
        {
            throw new ArgumentNullException(nameof(attachmentPoints));
        }

        return new EquipmentForwardSubscription(this, playerRoot, attachmentPoints);
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
        private readonly Dictionary<WardrobeTabType, AttachmentPoint> attachments = new Dictionary<WardrobeTabType, AttachmentPoint>();
        private readonly Dictionary<WardrobeTabType, GameObject> instances = new Dictionary<WardrobeTabType, GameObject>();
        private readonly UnityAction<WardrobeTabType, GameObject, WardrobeItemView> handler;
        private bool disposed;

        public EquipmentForwardSubscription(WardrobeUIController controller, Transform playerRoot, IEnumerable<AttachmentPoint> attachmentPoints)
        {
            this.controller = controller;
            this.playerRoot = playerRoot;

            foreach (AttachmentPoint point in attachmentPoints)
            {
                if (point == null || point.mountPoint == null)
                {
                    continue;
                }

                if (this.playerRoot != null && point.mountPoint != null && !point.mountPoint.IsChildOf(this.playerRoot))
                {
                    continue;
                }

                if (!attachments.ContainsKey(point.category))
                {
                    attachments.Add(point.category, point);
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

            foreach (KeyValuePair<WardrobeTabType, GameObject> pair in instances)
            {
                controller.DestroyInstance(pair.Value);
            }

            instances.Clear();
            attachments.Clear();
        }

        private void HandleEquipmentChanged(WardrobeTabType category, GameObject previewInstance, WardrobeItemView source)
        {
            Forward(category, source);
        }

        private void Forward(WardrobeTabType category, WardrobeItemView source)
        {
            GameObject currentInstance;
            if (instances.TryGetValue(category, out currentInstance) && currentInstance != null)
            {
                controller.DestroyInstance(currentInstance);
            }
            instances.Remove(category);

            if (source == null || source.WearablePrefab == null)
            {
                return;
            }

            AttachmentPoint attachmentPoint;
            if (!attachments.TryGetValue(category, out attachmentPoint) || attachmentPoint == null || attachmentPoint.mountPoint == null)
            {
                return;
            }

            GameObject newInstance = controller.InstantiateForAttachment(source.WearablePrefab, attachmentPoint);
            if (newInstance != null)
            {
                instances[category] = newInstance;
            }
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
