using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Represents an individual wardrobe tab button that can be selected in the UI.
/// Handles click interactions, selection state, and associated content visibility.
/// </summary>
[RequireComponent(typeof(Button))]
public class WardrobeTabButton : MonoBehaviour, IPointerClickHandler
{
    [Header("Tab Configuration")]
    [SerializeField] private WardrobeTabGroup tabGroup;
    [SerializeField] private WardrobeCategory category;
    [SerializeField] private GameObject contentRoot;

    [Header("Visuals")]
    [SerializeField] private Graphic targetGraphic;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.white;

    private bool isSelected;

    /// <summary>
    /// Gets the category represented by this tab.
    /// </summary>
    public WardrobeCategory Category => category;

    /// <summary>
    /// Gets the content GameObject associated with this tab.
    /// </summary>
    public GameObject ContentRoot => contentRoot;

    /// <summary>
    /// Gets a value indicating whether this tab is currently selected.
    /// </summary>
    public bool IsSelected => isSelected;

    private void Reset()
    {
        if (tabGroup == null)
        {
            tabGroup = GetComponentInParent<WardrobeTabGroup>();
        }

        if (targetGraphic == null)
        {
            targetGraphic = GetComponent<Graphic>();
        }
    }

    private void Awake()
    {
        EnsureReferences();
    }

    private void OnEnable()
    {
        EnsureReferences();
        RegisterToGroup();
        ApplySelectionVisual();
        SetContentActive(isSelected);
    }

    private void OnDisable()
    {
        UnregisterFromGroup();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        tabGroup?.RequestTabSelection(this);
    }

    internal void SetSelected(bool selected)
    {
        isSelected = selected;
        ApplySelectionVisual();
    }

    internal void SetContentActive(bool active)
    {
        if (contentRoot != null && contentRoot.activeSelf != active)
        {
            contentRoot.SetActive(active);
        }
    }

    private void EnsureReferences()
    {
        if (tabGroup == null)
        {
            tabGroup = GetComponentInParent<WardrobeTabGroup>();
        }

        if (targetGraphic == null)
        {
            targetGraphic = GetComponent<Graphic>();
        }
    }

    private void RegisterToGroup()
    {
        tabGroup?.RegisterTab(this);
    }

    private void UnregisterFromGroup()
    {
        tabGroup?.UnregisterTab(this);
    }

    private void ApplySelectionVisual()
    {
        if (targetGraphic != null)
        {
            targetGraphic.color = isSelected ? selectedColor : normalColor;
        }
    }
}
