using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages a set of <see cref="WardrobeItemButton"/> components that belong to the same
/// wardrobe category. Ensures only one button is selected at a time and emits
/// selection change events for external systems to react to.
/// </summary>
public class WardrobeItemSelectionGroup : MonoBehaviour
{
    [System.Serializable]
    public class WardrobeItemSelectedEvent : UnityEvent<WardrobeCategory, string>
    {
    }

    [Header("Configuration")]
    [SerializeField] private WardrobeCategory category;
    [SerializeField] private List<WardrobeItemButton> registeredButtons = new List<WardrobeItemButton>();
    [SerializeField] private WardrobeItemButton defaultButton;

    [Header("Events")]
    [SerializeField] private WardrobeItemSelectedEvent onSelectionChanged = new WardrobeItemSelectedEvent();

    private WardrobeItemButton selectedButton;

    /// <summary>
    /// Gets the wardrobe category represented by this selection group.
    /// </summary>
    public WardrobeCategory Category => category;

    /// <summary>
    /// Gets the button currently selected in this group.
    /// </summary>
    public WardrobeItemButton SelectedButton => selectedButton;

    /// <summary>
    /// Gets the event invoked whenever the selected button changes.
    /// </summary>
    public WardrobeItemSelectedEvent OnSelectionChanged => onSelectionChanged;

    private void Awake()
    {
        RemoveNullButtons();
    }

    private void OnEnable()
    {
        RemoveNullButtons();

        if (selectedButton == null || !registeredButtons.Contains(selectedButton))
        {
            SelectInitialButton();
        }
        else
        {
            UpdateSelection(selectedButton, invokeEvent: false);
        }
    }

    private void OnDisable()
    {
        var buttonsSnapshot = new List<WardrobeItemButton>(registeredButtons);
        foreach (WardrobeItemButton button in buttonsSnapshot)
        {
            button?.SetSelected(false);
        }
    }

    /// <summary>
    /// Registers a <see cref="WardrobeItemButton"/> with this selection group.
    /// </summary>
    public void RegisterButton(WardrobeItemButton button)
    {
        if (button == null || registeredButtons.Contains(button))
        {
            return;
        }

        registeredButtons.Add(button);

        if (selectedButton == null && (defaultButton == null || defaultButton == button))
        {
            SelectInitialButton();
        }
        else
        {
            button.SetSelected(button == selectedButton);
        }
    }

    /// <summary>
    /// Unregisters a <see cref="WardrobeItemButton"/> from this selection group.
    /// </summary>
    public void UnregisterButton(WardrobeItemButton button)
    {
        if (button == null)
        {
            return;
        }

        registeredButtons.Remove(button);

        if (selectedButton == button)
        {
            selectedButton = null;
            SelectInitialButton();
        }
    }

    /// <summary>
    /// Requests that the provided button becomes the selected option.
    /// </summary>
    public void RequestSelection(WardrobeItemButton button)
    {
        if (button == null || !registeredButtons.Contains(button))
        {
            return;
        }

        if (selectedButton == button)
        {
            return;
        }

        UpdateSelection(button, invokeEvent: true);
    }

    /// <summary>
    /// Selects the default button or falls back to the first registered button.
    /// </summary>
    public void SelectInitialButton()
    {
        WardrobeItemButton targetButton = null;

        if (defaultButton != null && registeredButtons.Contains(defaultButton))
        {
            targetButton = defaultButton;
        }
        else if (registeredButtons.Count > 0)
        {
            targetButton = registeredButtons[0];
        }

        if (targetButton != null)
        {
            UpdateSelection(targetButton, invokeEvent: true);
        }
        else
        {
            selectedButton = null;
        }
    }

    /// <summary>
    /// Attempts to select a button within the group by its item identifier.
    /// </summary>
    public void SelectByItemId(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return;
        }

        foreach (WardrobeItemButton button in registeredButtons)
        {
            if (button != null && button.ItemId == itemId)
            {
                if (selectedButton != button)
                {
                    UpdateSelection(button, invokeEvent: true);
                }

                return;
            }
        }
    }

    private void UpdateSelection(WardrobeItemButton button, bool invokeEvent)
    {
        selectedButton = button;

        foreach (WardrobeItemButton registeredButton in registeredButtons)
        {
            bool isActive = registeredButton == button;
            registeredButton?.SetSelected(isActive);
        }

        if (invokeEvent && button != null)
        {
            onSelectionChanged?.Invoke(category, button.ItemId);
        }
    }

    private void RemoveNullButtons()
    {
        registeredButtons.RemoveAll(button => button == null);

        WardrobeItemButton[] childButtons = GetComponentsInChildren<WardrobeItemButton>(includeInactive: true);
        foreach (WardrobeItemButton button in childButtons)
        {
            if (button != null && !registeredButtons.Contains(button))
            {
                registeredButtons.Add(button);
            }
        }
    }
}
