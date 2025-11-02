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
    [SerializeField] private List<EmoteButtonEntry> buttonEntries = new List<EmoteButtonEntry>();

    private readonly Dictionary<Button, UnityAction> buttonCallbacks = new Dictionary<Button, UnityAction>();

    private void Awake()
    {
        SetupButtonEvents();
    }

    /// <summary>
    /// Creates or refreshes button bindings.
    /// </summary>
    public void SetupButtonEvents()
    {
        if (emoteController == null)
        {
#if UNITY_2023_1_OR_NEWER
            emoteController = FindFirstObjectByType<PlayerEmoteController>();
#else
            emoteController = FindObjectOfType<PlayerEmoteController>();
#endif
        }

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
}
