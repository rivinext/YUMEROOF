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
    [SerializeField] private List<AttachmentPoint> gameAttachmentPoints = new List<AttachmentPoint>();
    [SerializeField] private Transform gamePlayerRoot;
    [SerializeField] private WardrobePreviewController previewController;
    [SerializeField] private bool autoRegisterItemsOnAwake = true;
    [SerializeField] private WardrobeCatalog wardrobeCatalog;
    [SerializeField] private WardrobeItemView wardrobeItemViewPrefab;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private AudioClip toggleButtonClip;
    [SerializeField] private AudioClip closeButtonClip;
    [SerializeField] private AudioSource toggleButtonAudioSource;
    [SerializeField] private AudioSource closeButtonAudioSource;

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
        public string partName = string.Empty;
        public Transform mountPoint;
        public bool resetLocalTransform = true;
    }

    private sealed class EquippedInstanceSet
    {
        private readonly Dictionary<string, GameObject> instances = new Dictionary<string, GameObject>();

        public bool IsEmpty => instances.Count == 0;

        public void Add(string partName, GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            instances[NormalizePartName(partName)] = instance;
        }

        public bool TryGet(string partName, out GameObject instance)
        {
            return instances.TryGetValue(NormalizePartName(partName), out instance);
        }

        public GameObject GetDefault()
        {
            GameObject instance;
            if (instances.TryGetValue(string.Empty, out instance) && instance != null)
            {
                return instance;
            }

            foreach (KeyValuePair<string, GameObject> pair in instances)
            {
                if (pair.Value != null)
                {
                    return pair.Value;
                }
            }

            return null;
        }

        public void DestroyAll(Action<GameObject> destroyer)
        {
            foreach (KeyValuePair<string, GameObject> pair in instances)
            {
                if (pair.Value != null)
                {
                    destroyer?.Invoke(pair.Value);
                }
            }

            instances.Clear();
        }
    }

    [Serializable]
    public class WardrobeEquipEvent : UnityEvent<WardrobeTabType, GameObject, WardrobeItemView> { }

    [SerializeField] private WardrobeEquipEvent onItemEquipped = new WardrobeEquipEvent();

    private readonly List<UnityAction<bool>> toggleHandlers = new List<UnityAction<bool>>();
    private readonly Dictionary<WardrobeTabType, Dictionary<string, AttachmentPoint>> attachmentLookup = new Dictionary<WardrobeTabType, Dictionary<string, AttachmentPoint>>();
    private readonly Dictionary<WardrobeTabType, Dictionary<string, AttachmentPoint>> gameAttachmentLookup = new Dictionary<WardrobeTabType, Dictionary<string, AttachmentPoint>>();
    private readonly Dictionary<WardrobeTabType, EquippedInstanceSet> previewEquippedInstances = new Dictionary<WardrobeTabType, EquippedInstanceSet>();
    private readonly Dictionary<WardrobeTabType, EquippedInstanceSet> gameEquippedInstances = new Dictionary<WardrobeTabType, EquippedInstanceSet>();
    private readonly Dictionary<WardrobeTabType, WardrobeItemView> activeSelections = new Dictionary<WardrobeTabType, WardrobeItemView>();
    private readonly List<WardrobeItemView> registeredItems = new List<WardrobeItemView>();
    private readonly List<WardrobeItemView> runtimeGeneratedItems = new List<WardrobeItemView>();

    private bool hasLoadedSelections;

    public bool HasLoadedSelections => hasLoadedSelections || activeSelections.Count > 0;

    public WardrobeEquipEvent OnItemEquipped
    {
        get { return onItemEquipped; }
    }

    public GameObject GetEquippedInstance(WardrobeTabType category)
    {
        EquippedInstanceSet instanceSet;
        if (previewEquippedInstances.TryGetValue(category, out instanceSet) && instanceSet != null)
        {
            return instanceSet.GetDefault();
        }

        return null;
    }

    public GameObject GetGameEquippedInstance(WardrobeTabType category)
    {
        EquippedInstanceSet instanceSet;
        if (gameEquippedInstances.TryGetValue(category, out instanceSet) && instanceSet != null)
        {
            return instanceSet.GetDefault();
        }

        return null;
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

        UIPanelExclusionManager.Instance?.Register(this);

        BuildAttachmentLookup();
        BuildGameAttachmentLookup();
        SetupTabs();
        PopulateCatalogItems();

        if (autoRegisterItemsOnAwake)
        {
            RegisterItemViewsInChildren();
        }

        UpdateDescription(null);
        if (previewController != null)
        {
            previewController.InitializePreviewTarget();
            previewController.CapturePreviewInitialZoom();
            previewController.UpdatePreviewActivation(panelAnimator != null && panelAnimator.IsShown);
            previewController.CapturePreviewInitialRotation();
            previewController.SetupPreviewEventTrigger();
        }

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

    public bool IsShown
    {
        get
        {
            if (panelAnimator != null)
            {
                return panelAnimator.IsShown;
            }

            return panelRoot != null && panelRoot.activeSelf;
        }
    }

    public bool IsOpen => IsShown;

    public void ShowPanel(bool instant = false)
    {
        if (panelAnimator != null)
        {
            bool wasInactive = panelRoot != null && !panelRoot.activeInHierarchy;
            if (wasInactive)
            {
                panelRoot.SetActive(true);
            }

            bool forceInstant = instant || !panelAnimator.isActiveAndEnabled || !panelAnimator.gameObject.activeInHierarchy || wasInactive;

            if (!panelAnimator.IsShown || forceInstant)
            {
                UpdatePreviewActivation(true);
            }

            UIPanelExclusionManager.Instance?.NotifyOpened(this);
            panelAnimator.Show(forceInstant);
        }
        else if (panelRoot != null)
        {
            UIPanelExclusionManager.Instance?.NotifyOpened(this);
            panelRoot.SetActive(true);
            UpdatePreviewActivation(true);
        }
    }

    public void HidePanel(bool instant = false)
    {
        if (panelAnimator != null)
        {
            bool forceInstant = instant || !panelAnimator.isActiveAndEnabled || !panelAnimator.gameObject.activeInHierarchy;

            panelAnimator.Hide(forceInstant);
            if (forceInstant)
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
        PlayButtonSfx(closeButtonClip, closeButtonAudioSource);
        HidePanel();
    }

    public void TogglePanel()
    {
        PlayButtonSfx(toggleButtonClip, toggleButtonAudioSource);

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

    private void PlayButtonSfx(AudioClip clip, AudioSource preferredSource)
    {
        if (clip == null)
        {
            return;
        }

        AudioSource source = preferredSource;
        if (source == null)
        {
            source = toggleButtonAudioSource;
        }

        if (source == null)
        {
            return;
        }

        source.PlayOneShot(clip, AudioManager.CurrentSfxVolume);
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
        EquippedInstanceSet currentSet;
        if (previewEquippedInstances.TryGetValue(category, out currentSet) && currentSet != null)
        {
            currentSet.DestroyAll(DestroyInstance);
        }
        previewEquippedInstances.Remove(category);

        if (gameEquippedInstances.TryGetValue(category, out currentSet) && currentSet != null)
        {
            currentSet.DestroyAll(DestroyInstance);
        }
        gameEquippedInstances.Remove(category);

        EquippedInstanceSet newPreviewSet = InstantiatePartSet(prefab, source, attachmentLookup, category, "Preview", true);
        EquippedInstanceSet newGameSet = InstantiatePartSet(prefab, source, gameAttachmentLookup, category, "Game", false);

        GameObject newPreviewInstance = null;

        if (newPreviewSet != null && !newPreviewSet.IsEmpty)
        {
            previewEquippedInstances[category] = newPreviewSet;
            newPreviewInstance = newPreviewSet.GetDefault();
        }

        if (newGameSet != null && !newGameSet.IsEmpty)
        {
            gameEquippedInstances[category] = newGameSet;
        }

        UpdateSelectionState(category, source);
        onItemEquipped.Invoke(category, newPreviewInstance, source);
        UpdateDescription(source);
        hasLoadedSelections = true;
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
        Transform previewRoot = previewController != null ? previewController.PreviewPlayerRoot : null;
        BuildAttachmentLookupInternal(attachmentLookup, attachmentPoints, previewRoot);
    }

    private void BuildGameAttachmentLookup()
    {
        BuildAttachmentLookupInternal(gameAttachmentLookup, gameAttachmentPoints, gamePlayerRoot);
    }

    private void BuildAttachmentLookupInternal(Dictionary<WardrobeTabType, Dictionary<string, AttachmentPoint>> lookup, List<AttachmentPoint> points, Transform root)
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

            Dictionary<string, AttachmentPoint> perCategory;
            if (!lookup.TryGetValue(point.category, out perCategory))
            {
                perCategory = new Dictionary<string, AttachmentPoint>();
                lookup.Add(point.category, perCategory);
            }

            string partKey = NormalizePartName(point.partName);
            if (!perCategory.ContainsKey(partKey))
            {
                perCategory.Add(partKey, point);
            }
        }
    }

    private static string NormalizePartName(string partName)
    {
        return WardrobePartNameUtility.NormalizePartName(partName);
    }

    private AttachmentPoint ResolveAttachment(Dictionary<WardrobeTabType, Dictionary<string, AttachmentPoint>> lookup, WardrobeTabType category, string partName)
    {
        Dictionary<string, AttachmentPoint> perCategory;
        if (!lookup.TryGetValue(category, out perCategory) || perCategory == null)
        {
            return null;
        }

        AttachmentPoint attachmentPoint;
        string normalizedPartName = NormalizePartName(partName);
        if (perCategory.TryGetValue(normalizedPartName, out attachmentPoint) && attachmentPoint != null && attachmentPoint.mountPoint != null)
        {
            return attachmentPoint;
        }

        if (perCategory.TryGetValue(string.Empty, out attachmentPoint) && attachmentPoint != null && attachmentPoint.mountPoint != null)
        {
            return attachmentPoint;
        }

        foreach (KeyValuePair<string, AttachmentPoint> pair in perCategory)
        {
            if (pair.Value != null && pair.Value.mountPoint != null)
            {
                return pair.Value;
            }
        }

        return null;
    }

    private struct ExtractedPart
    {
        public string PartName;
        public GameObject Prefab;
    }

    private static readonly List<ExtractedPart> s_extractedPartsBuffer = new List<ExtractedPart>();
    private static readonly Dictionary<string, ExtractedPart> s_extractedPartsLookup = new Dictionary<string, ExtractedPart>();

    private static bool TryResolvePartNameFromTransform(Transform transform, out string partName)
    {
        partName = string.Empty;
        if (transform == null)
        {
            return false;
        }

        string name = transform.name;
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        const string PrefixToken = "Part_";
        const string BracketToken = "[Part]";

        if (name.StartsWith(PrefixToken, StringComparison.OrdinalIgnoreCase))
        {
            partName = name.Substring(PrefixToken.Length).Trim();
        }
        else
        {
            int bracketIndex = name.IndexOf(BracketToken, StringComparison.OrdinalIgnoreCase);
            if (bracketIndex >= 0)
            {
                partName = name.Substring(bracketIndex + BracketToken.Length).Trim();
            }
        }

        if (string.IsNullOrEmpty(partName))
        {
            return false;
        }

        string canonicalName = WardrobePartNameUtility.NormalizePartName(partName);
        if (string.IsNullOrEmpty(canonicalName))
        {
            partName = string.Empty;
            return false;
        }

        partName = canonicalName;
        return true;
    }

    private static IReadOnlyList<ExtractedPart> ExtractFallbackParts(GameObject fallbackPrefab)
    {
        s_extractedPartsBuffer.Clear();
        s_extractedPartsLookup.Clear();

        if (fallbackPrefab == null)
        {
            return s_extractedPartsBuffer;
        }

        WardrobePartTag[] tags = fallbackPrefab.GetComponentsInChildren<WardrobePartTag>(true);
        for (int i = 0; i < tags.Length; i++)
        {
            WardrobePartTag tag = tags[i];
            if (tag == null)
            {
                continue;
            }

            Transform tagTransform = tag.transform;
            if (tagTransform == null)
            {
                continue;
            }

            string partName = tag.PartName;
            if (string.IsNullOrEmpty(partName))
            {
                partName = tagTransform.name;
            }

            partName = NormalizePartName(partName);
            if (string.IsNullOrEmpty(partName))
            {
                continue;
            }
            if (s_extractedPartsLookup.ContainsKey(partName))
            {
                continue;
            }

            ExtractedPart part = new ExtractedPart
            {
                PartName = partName,
                Prefab = tagTransform.gameObject,
            };

            s_extractedPartsLookup.Add(partName, part);
            s_extractedPartsBuffer.Add(part);
        }

        if (s_extractedPartsBuffer.Count == 0)
        {
            Transform[] transforms = fallbackPrefab.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform child = transforms[i];
                if (child == null || child == fallbackPrefab.transform)
                {
                    continue;
                }

                string partName;
                if (!TryResolvePartNameFromTransform(child, out partName))
                {
                    continue;
                }

                partName = NormalizePartName(partName);
                if (string.IsNullOrEmpty(partName))
                {
                    continue;
                }
                if (s_extractedPartsLookup.ContainsKey(partName))
                {
                    continue;
                }

                ExtractedPart part = new ExtractedPart
                {
                    PartName = partName,
                    Prefab = child.gameObject,
                };

                s_extractedPartsLookup.Add(partName, part);
                s_extractedPartsBuffer.Add(part);
            }
        }

        return s_extractedPartsBuffer;
    }

    private EquippedInstanceSet InstantiatePartSet(GameObject fallbackPrefab, WardrobeItemView source, Dictionary<WardrobeTabType, Dictionary<string, AttachmentPoint>> lookup, WardrobeTabType category, string attachmentRole, bool logWarnings)
    {
        EquippedInstanceSet instanceSet = null;
        bool attemptedPartInstantiation = false;
        bool instantiatedAny = false;

        if (source != null && source.HasPartPrefabs)
        {
            attemptedPartInstantiation = true;
            IReadOnlyList<WardrobeItemView.WearablePart> parts = source.PartPrefabs;
            for (int i = 0; i < parts.Count; i++)
            {
                WardrobeItemView.WearablePart part = parts[i];
                if (part == null || part.Prefab == null)
                {
                    continue;
                }

                string partName = part.PartName;
                AttachmentPoint attachmentPoint = ResolveAttachment(lookup, category, partName);
                if (attachmentPoint == null)
                {
                    if (logWarnings)
                    {
                        string label = string.IsNullOrEmpty(partName) ? "<default>" : partName;
                        Debug.LogWarningFormat(this, "[WardrobeUIController] {0} attachment point for category '{1}' part '{2}' is not configured.", attachmentRole, category, label);
                    }

                    continue;
                }

                GameObject instance = InstantiateForAttachment(part.Prefab, attachmentPoint);
                if (instance == null)
                {
                    continue;
                }

                if (instanceSet == null)
                {
                    instanceSet = new EquippedInstanceSet();
                }

                instanceSet.Add(partName, instance);
                instantiatedAny = true;
            }
        }

        if (!attemptedPartInstantiation && fallbackPrefab != null && source != null && !source.HasPartPrefabs)
        {
            IReadOnlyList<ExtractedPart> extractedParts = ExtractFallbackParts(fallbackPrefab);
            if (extractedParts != null && extractedParts.Count > 0)
            {
                attemptedPartInstantiation = true;

                for (int i = 0; i < extractedParts.Count; i++)
                {
                    ExtractedPart extracted = extractedParts[i];
                    if (extracted.Prefab == null)
                    {
                        continue;
                    }

                    string partName = extracted.PartName;
                    AttachmentPoint attachmentPoint = ResolveAttachment(lookup, category, partName);
                    if (attachmentPoint == null)
                    {
                        if (logWarnings)
                        {
                            string label = string.IsNullOrEmpty(partName) ? "<default>" : partName;
                            Debug.LogWarningFormat(this, "[WardrobeUIController] {0} attachment point for category '{1}' part '{2}' is not configured.", attachmentRole, category, label);
                        }

                        continue;
                    }

                    GameObject instance = InstantiateForAttachment(extracted.Prefab, attachmentPoint);
                    if (instance == null)
                    {
                        continue;
                    }

                    if (instanceSet == null)
                    {
                        instanceSet = new EquippedInstanceSet();
                    }

                    instanceSet.Add(partName, instance);
                    instantiatedAny = true;
                }
            }
        }

        if ((!attemptedPartInstantiation || !instantiatedAny) && fallbackPrefab != null)
        {
            AttachmentPoint attachmentPoint = ResolveAttachment(lookup, category, string.Empty);
            if (attachmentPoint == null)
            {
                if (logWarnings)
                {
                    Debug.LogWarningFormat(this, "[WardrobeUIController] {0} attachment point for category '{1}' is not configured.", attachmentRole, category);
                }

                return instanceSet;
            }

            GameObject instance = InstantiateForAttachment(fallbackPrefab, attachmentPoint);
            if (instance != null)
            {
                if (instanceSet == null)
                {
                    instanceSet = new EquippedInstanceSet();
                }

                instanceSet.Add(string.Empty, instance);
                instantiatedAny = true;
            }
        }

        return instantiatedAny ? instanceSet : null;
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

    private void UpdatePreviewActivation(bool visible)
    {
        if (previewController != null)
        {
            previewController.UpdatePreviewActivation(visible);
        }
    }

    private static IEnumerable<WardrobeTabType> EnumerateCategories()
    {
        return (WardrobeTabType[])Enum.GetValues(typeof(WardrobeTabType));
    }

    public void CollectSelectionEntries(List<WardrobeSelectionEntry> target)
    {
        if (target == null)
        {
            return;
        }

        target.Clear();

        foreach (WardrobeTabType category in EnumerateCategories())
        {
            WardrobeItemView selected = GetSelectedItem(category);
            string itemId = selected != null && !selected.IsEmpty ? selected.ItemId : string.Empty;

            target.Add(new WardrobeSelectionEntry
            {
                category = category,
                itemId = itemId,
            });
        }
    }

    public void ApplySelectionEntries(IEnumerable<WardrobeSelectionEntry> entries)
    {
        if (entries == null)
        {
            return;
        }

        HashSet<WardrobeTabType> usedCategories = new HashSet<WardrobeTabType>();

        foreach (WardrobeSelectionEntry entry in entries)
        {
            usedCategories.Add(entry.category);

            if (string.IsNullOrEmpty(entry.itemId))
            {
                ClearCategory(entry.category);
                continue;
            }

            WardrobeItemView itemView = FindItemViewByItemId(entry.category, entry.itemId);
            if (itemView == null)
            {
                ClearCategory(entry.category);
                continue;
            }

            EquipItem(entry.category, itemView.WearablePrefab, itemView);
        }

        foreach (WardrobeTabType category in EnumerateCategories())
        {
            if (!usedCategories.Contains(category))
            {
                ClearCategory(category);
            }
        }

        hasLoadedSelections = true;
    }

    private WardrobeItemView FindItemViewByItemId(WardrobeTabType category, string targetItemId)
    {
        if (string.IsNullOrEmpty(targetItemId))
        {
            return null;
        }

        for (int i = 0; i < registeredItems.Count; i++)
        {
            WardrobeItemView view = registeredItems[i];
            if (view == null || view.Category != category)
            {
                continue;
            }

            string itemId = view.ItemId;
            if (!string.IsNullOrEmpty(itemId) && string.Equals(itemId, targetItemId, StringComparison.Ordinal))
            {
                return view;
            }
        }

        for (int i = 0; i < runtimeGeneratedItems.Count; i++)
        {
            WardrobeItemView view = runtimeGeneratedItems[i];
            if (view == null || view.Category != category)
            {
                continue;
            }

            string itemId = view.ItemId;
            if (!string.IsNullOrEmpty(itemId) && string.Equals(itemId, targetItemId, StringComparison.Ordinal))
            {
                return view;
            }
        }

        return null;
    }

    private void OnPanelVisibilityChanged(bool visible)
    {
        UpdatePreviewActivation(visible);
        PlayerController.SetGlobalInputEnabled(!visible);
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
        private readonly Dictionary<WardrobeTabType, Dictionary<string, AttachmentPoint>> attachments = new Dictionary<WardrobeTabType, Dictionary<string, AttachmentPoint>>();
        private readonly Dictionary<WardrobeTabType, EquippedInstanceSet> instances = new Dictionary<WardrobeTabType, EquippedInstanceSet>();
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

                Dictionary<string, AttachmentPoint> perCategory;
                if (!attachments.TryGetValue(point.category, out perCategory))
                {
                    perCategory = new Dictionary<string, AttachmentPoint>();
                    attachments.Add(point.category, perCategory);
                }

                string partKey = NormalizePartName(point.partName);
                if (!perCategory.ContainsKey(partKey))
                {
                    perCategory.Add(partKey, point);
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

            foreach (KeyValuePair<WardrobeTabType, EquippedInstanceSet> pair in instances)
            {
                if (pair.Value != null)
                {
                    pair.Value.DestroyAll(controller.DestroyInstance);
                }
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
            EquippedInstanceSet currentSet;
            if (instances.TryGetValue(category, out currentSet) && currentSet != null)
            {
                currentSet.DestroyAll(controller.DestroyInstance);
            }
            instances.Remove(category);

            GameObject fallbackPrefab = source != null ? source.WearablePrefab : null;
            EquippedInstanceSet newSet = controller.InstantiatePartSet(fallbackPrefab, source, attachments, category, "Forward", false);
            if (newSet == null || newSet.IsEmpty)
            {
                return;
            }

            instances[category] = newSet;
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
