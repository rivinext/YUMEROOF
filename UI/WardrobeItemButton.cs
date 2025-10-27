using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Represents an individual selectable wardrobe item button within a category.
/// Responsible for informing the parent selection group when clicked and for
/// updating its own visual selection state.
/// </summary>
[RequireComponent(typeof(Button))]
public class WardrobeItemButton : MonoBehaviour, IPointerClickHandler
{
    [Header("Configuration")]
    [SerializeField] private WardrobeItemSelectionGroup selectionGroup;
    [SerializeField] private string itemId;

    [Header("Visuals")]
    [SerializeField] private Graphic targetGraphic;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.white;

    private bool isSelected;

    /// <summary>
    /// Gets the identifier for this wardrobe item.
    /// </summary>
    public string ItemId => itemId;

    /// <summary>
    /// Gets a value indicating whether this button is currently selected.
    /// </summary>
    public bool IsSelected => isSelected;

    private void Reset()
    {
        if (selectionGroup == null)
        {
            selectionGroup = GetComponentInParent<WardrobeItemSelectionGroup>();
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
    }

    private void OnDisable()
    {
        UnregisterFromGroup();
    }

    /// <inheritdoc />
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        selectionGroup?.RequestSelection(this);
    }

    internal void SetSelected(bool selected)
    {
        isSelected = selected;
        ApplySelectionVisual();
    }

    private void EnsureReferences()
    {
        if (selectionGroup == null)
        {
            selectionGroup = GetComponentInParent<WardrobeItemSelectionGroup>();
        }

        if (targetGraphic == null)
        {
            targetGraphic = GetComponent<Graphic>();
        }
    }

    private void RegisterToGroup()
    {
        selectionGroup?.RegisterButton(this);
    }

    private void UnregisterFromGroup()
    {
        selectionGroup?.UnregisterButton(this);
    }

    private void ApplySelectionVisual()
    {
        if (targetGraphic != null)
        {
            targetGraphic.color = isSelected ? selectedColor : normalColor;
        }
    }
}
