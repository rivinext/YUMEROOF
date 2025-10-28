using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
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
    [SerializeField] private Transform previewPlayerRoot;
    [SerializeField] private Camera previewCamera;
    [SerializeField] private RawImage previewTargetImage;
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
    private class AttachmentPoint
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
    private readonly Dictionary<WardrobeTabType, GameObject> equippedInstances = new Dictionary<WardrobeTabType, GameObject>();
    private readonly Dictionary<WardrobeTabType, WardrobeItemView> activeSelections = new Dictionary<WardrobeTabType, WardrobeItemView>();
    private readonly List<WardrobeItemView> registeredItems = new List<WardrobeItemView>();
    private readonly List<WardrobeItemView> runtimeGeneratedItems = new List<WardrobeItemView>();

    public WardrobeEquipEvent OnItemEquipped
    {
        get { return onItemEquipped; }
    }

    public GameObject GetEquippedInstance(WardrobeTabType category)
    {
        GameObject instance;
        equippedInstances.TryGetValue(category, out instance);
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
        SetupTabs();
        PopulateCatalogItems();

        if (autoRegisterItemsOnAwake)
        {
            RegisterItemViewsInChildren();
        }

        UpdateDescription(null);
        InitializePreviewTarget();
        UpdatePreviewActivation(panelAnimator != null && panelAnimator.IsShown);
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
        if (equippedInstances.TryGetValue(category, out currentInstance) && currentInstance != null)
        {
            DestroyInstance(currentInstance);
        }

        GameObject newInstance = null;

        if (prefab != null)
        {
            AttachmentPoint attachmentPoint;
            if (!attachmentLookup.TryGetValue(category, out attachmentPoint) || attachmentPoint == null || attachmentPoint.mountPoint == null)
            {
                Debug.LogWarningFormat(this, "[WardrobeUIController] Attachment point for category '{0}' is not configured.", category);
            }
            else
            {
                newInstance = Instantiate(prefab, attachmentPoint.mountPoint, false);
                if (newInstance != null && attachmentPoint.mountPoint != null)
                {
                    int mountLayer = attachmentPoint.mountPoint.gameObject.layer;
                    if (mountLayer >= 0 && mountLayer < 32)
                    {
                        ApplyLayerRecursively(newInstance, mountLayer);
                    }
                }
                if (attachmentPoint.resetLocalTransform && newInstance != null)
                {
                    Transform instanceTransform = newInstance.transform;
                    instanceTransform.localPosition = Vector3.zero;
                    instanceTransform.localRotation = Quaternion.identity;
                    instanceTransform.localScale = Vector3.one;
                }
            }
        }

        equippedInstances[category] = newInstance;
        UpdateSelectionState(category, source);
        onItemEquipped.Invoke(category, newInstance, source);
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
        attachmentLookup.Clear();

        for (int i = 0; i < attachmentPoints.Count; i++)
        {
            AttachmentPoint point = attachmentPoints[i];
            if (point == null || point.mountPoint == null)
            {
                continue;
            }

            if (!attachmentLookup.ContainsKey(point.category))
            {
                attachmentLookup.Add(point.category, point);
            }
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
        }
    }

    private void OnPanelVisibilityChanged(bool visible)
    {
        UpdatePreviewActivation(visible);
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
}
