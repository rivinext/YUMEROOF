using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Helper component that binds UI buttons to specific player emotes.
/// </summary>
public class PlayerEmoteButtonBinder : MonoBehaviour
{
    [System.Serializable]
    private class EmoteButtonEntry
    {
        public Button button;
        public string emoteId;
    }

    [SerializeField] private PlayerEmoteController emoteController;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private List<EmoteButtonEntry> buttonEntries = new List<EmoteButtonEntry>();

    private readonly Dictionary<Button, UnityAction> buttonCallbacks = new Dictionary<Button, UnityAction>();
    private readonly Dictionary<Button, bool> buttonOriginalInteractable = new Dictionary<Button, bool>();
    private readonly Dictionary<Button, bool> temporarilyCachedInteractable = new Dictionary<Button, bool>();
    private bool wasPlayerSitting;
    private bool buttonsTemporarilyDisabled;

    private void Awake()
    {
        SetupButtonEvents();
    }

    private void Update()
    {
        if (playerController == null)
        {
            TryResolvePlayerController();
        }

        bool isSitting = playerController != null && playerController.IsSitting;

        if (isSitting == wasPlayerSitting)
        {
            return;
        }

        wasPlayerSitting = isSitting;

        if (isSitting)
        {
            ApplySittingRestrictions();
        }
        else
        {
            RestoreButtonInteractableState();
            SetupButtonEvents();
        }
    }

    /// <summary>
    /// Creates or refreshes button bindings.
    /// </summary>
    public void SetupButtonEvents()
    {
        TryResolvePlayerController();
        if (emoteController == null)
        {
#if UNITY_2023_1_OR_NEWER
            emoteController = FindFirstObjectByType<PlayerEmoteController>();
#else
            emoteController = FindObjectOfType<PlayerEmoteController>();
#endif
        }

        CleanupButtonCaches();

        foreach (EmoteButtonEntry entry in buttonEntries)
        {
            if (entry == null || entry.button == null)
            {
                continue;
            }

            if (buttonCallbacks.TryGetValue(entry.button, out UnityAction previousAction))
            {
                entry.button.onClick.RemoveListener(previousAction);
            }

            if (emoteController == null || string.IsNullOrEmpty(entry.emoteId))
            {
                buttonCallbacks.Remove(entry.button);
                continue;
            }

            string emoteId = entry.emoteId;
            UnityAction action = () => TriggerEmote(emoteId);
            entry.button.onClick.AddListener(action);
            buttonCallbacks[entry.button] = action;
        }

        if (playerController != null && playerController.IsSitting)
        {
            ApplySittingRestrictions();
        }
        else
        {
            RefreshInteractableCache();
        }
    }

    private void OnDestroy()
    {
        foreach (KeyValuePair<Button, UnityAction> binding in buttonCallbacks)
        {
            if (binding.Key != null)
            {
                binding.Key.onClick.RemoveListener(binding.Value);
            }
        }

        buttonCallbacks.Clear();
    }

    private void TriggerEmote(string emoteId)
    {
        emoteController?.PlayEmote(emoteId);
    }

    private void TryResolvePlayerController()
    {
        if (playerController != null)
        {
            return;
        }

        if (emoteController != null)
        {
            playerController = emoteController.GetComponent<PlayerController>();
        }

        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }
    }

    private void CleanupButtonCaches()
    {
        if (buttonOriginalInteractable.Count == 0)
        {
            return;
        }

        List<Button> keys = new List<Button>(buttonOriginalInteractable.Keys);
        foreach (Button button in keys)
        {
            if (button == null || !IsButtonTracked(button))
            {
                buttonOriginalInteractable.Remove(button);
            }
        }
    }

    private bool IsButtonTracked(Button button)
    {
        foreach (EmoteButtonEntry entry in buttonEntries)
        {
            if (entry != null && entry.button == button)
            {
                return true;
            }
        }

        return false;
    }

    private void RefreshInteractableCache(bool ignoreSitting = false)
    {
        if (!ignoreSitting && playerController != null && playerController.IsSitting)
        {
            return;
        }

        buttonOriginalInteractable.Clear();

        foreach (EmoteButtonEntry entry in buttonEntries)
        {
            if (entry?.button == null)
            {
                continue;
            }

            buttonOriginalInteractable[entry.button] = entry.button.interactable;
        }
    }

    private void ApplySittingRestrictions()
    {
        foreach (EmoteButtonEntry entry in buttonEntries)
        {
            if (entry?.button == null)
            {
                continue;
            }

            if (buttonsTemporarilyDisabled)
            {
                entry.button.interactable = false;
                continue;
            }

            if (!buttonOriginalInteractable.ContainsKey(entry.button))
            {
                buttonOriginalInteractable[entry.button] = entry.button.interactable;
            }

            entry.button.interactable = false;
        }
    }

    private void RestoreButtonInteractableState()
    {
        if (buttonsTemporarilyDisabled)
        {
            return;
        }

        foreach (EmoteButtonEntry entry in buttonEntries)
        {
            if (entry?.button == null)
            {
                continue;
            }

            if (buttonOriginalInteractable.TryGetValue(entry.button, out bool wasInteractable))
            {
                entry.button.interactable = wasInteractable;
            }
            else
            {
                entry.button.interactable = true;
            }
        }
    }

    /// <summary>
    /// Temporarily disables or restores interactivity for all tracked buttons.
    /// </summary>
    /// <param name="disabled">True to disable, false to restore.</param>
    public void SetButtonsTemporarilyDisabled(bool disabled)
    {
        if (buttonsTemporarilyDisabled == disabled)
        {
            return;
        }

        buttonsTemporarilyDisabled = disabled;

        if (disabled)
        {
            RefreshInteractableCache(ignoreSitting: true);
            temporarilyCachedInteractable.Clear();

            foreach (EmoteButtonEntry entry in buttonEntries)
            {
                if (entry?.button == null)
                {
                    continue;
                }

                temporarilyCachedInteractable[entry.button] = entry.button.interactable;
                entry.button.interactable = false;
            }

            return;
        }

        foreach (EmoteButtonEntry entry in buttonEntries)
        {
            if (entry?.button == null)
            {
                continue;
            }

            if (temporarilyCachedInteractable.TryGetValue(entry.button, out bool cachedState))
            {
                entry.button.interactable = cachedState;
            }
            else if (buttonOriginalInteractable.TryGetValue(entry.button, out bool originalState))
            {
                entry.button.interactable = originalState;
            }
            else
            {
                entry.button.interactable = true;
            }
        }

        temporarilyCachedInteractable.Clear();

        if (playerController != null && playerController.IsSitting)
        {
            ApplySittingRestrictions();
        }
    }
}
