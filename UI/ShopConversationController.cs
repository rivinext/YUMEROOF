using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

/// <summary>
/// Controls the conversation panel that appears before the shop UI opens.
/// Handles dimming, dialogue progression and Buy/Sell/Exit choices.
/// </summary>
public class ShopConversationController : MonoBehaviour
{
    [Serializable]
    private struct DialogueLineReference
    {
        [Tooltip("Localization table key used for CSV lookup and localization table resolution.")]
        public string id;
        [Tooltip("LocalizedString that can resolve directly without CSV/lookup indirection.")]
        public LocalizedString localizedString;

        public bool HasKey => !string.IsNullOrEmpty(id);
        public bool HasLocalizedString => localizedString != null && !localizedString.IsEmpty;
    }

    [Header("UI")]
    [SerializeField] private InteractionSlidePanel conversationPanel;
    [SerializeField] private TextMeshProUGUI speakerLabel;
    [SerializeField] private TextMeshProUGUI dialogueLabel;
    [SerializeField] private Image backgroundDimmer;
    [SerializeField] private GameObject choiceButtonGroup;
    [SerializeField] private ToggleGroup choiceToggleGroup;
    [SerializeField] private Toggle buyToggle;
    [SerializeField] private Toggle sellToggle;
    [SerializeField] private Button exitButton;

    [Header("Conversation")]
    [SerializeField, Tooltip("Localization table that stores the default shop conversation entries.")] private string defaultLocalizationTable = "ShopConversation";
    [SerializeField] private string dialogueCSVPath = "Data/ShopDialogue";
    [SerializeField, Tooltip("Optional TextAsset override for the dialogue CSV. If assigned, this is used instead of Resources.Load.")] private TextAsset dialogueCsvAsset;
    [SerializeField, Tooltip("Ordered intro lines. Each entry can use a LocalizedString or a plain ID for CSV/localization table lookup.")] private List<DialogueLineReference> introLines = new()
    {
        new DialogueLineReference { id = "greeting", localizedString = new LocalizedString("ShopConversation", "greeting") },
        new DialogueLineReference { id = "askPurpose", localizedString = new LocalizedString("ShopConversation", "askPurpose") }
    };
    [SerializeField, Tooltip("Line that should be redisplayed when returning from the shop or when toggling choices.")] private DialogueLineReference choiceTriggerLine = new() { id = "askPurpose", localizedString = new LocalizedString("ShopConversation", "askPurpose") };
    [SerializeField, Tooltip("Ordered exit lines. Each entry can use a LocalizedString or a plain ID for CSV/localization table lookup.")] private List<DialogueLineReference> exitLines = new()
    {
        new DialogueLineReference { id = "farewell", localizedString = new LocalizedString("ShopConversation", "farewell") }
    };

    [Header("Dependencies")]
    [SerializeField] private ShopUIManager shopUIManager;

    private readonly Dictionary<string, string> fallbackLocalizedLines = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> localizedLines = new(StringComparer.OrdinalIgnoreCase);
    private bool localizationLoaded;
    private int introLineIndex = -1;
    private int exitLineIndex = -1;
    private bool awaitingChoice;
    private bool exitSequenceActive;
    private bool shopPanelOpen;
    private bool conversationActive;
    private DialogueLineReference activeLine;
    private bool activeLineAssigned;

    /// <summary>
    /// True when the conversation overlay is currently open.
    /// </summary>
    public bool IsConversationActive => conversationActive;

    void Reset()
    {
        if (conversationPanel == null)
        {
            conversationPanel = GetComponentInChildren<InteractionSlidePanel>();
        }
    }

    void Awake()
    {
        if (conversationPanel == null)
        {
            conversationPanel = GetComponentInChildren<InteractionSlidePanel>();
        }

        EnsureShopManagerReference();

        if (buyToggle != null)
        {
            buyToggle.onValueChanged.AddListener(HandleBuyToggleChanged);
        }

        if (sellToggle != null)
        {
            sellToggle.onValueChanged.AddListener(HandleSellToggleChanged);
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(HandleExitSelected);
        }

        UpdateChoiceButtonsVisibility();
    }

    void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
        SubscribeShopEvents();
    }

    void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
        UnsubscribeShopEvents();
    }

    void Update()
    {
        if (!conversationActive || shopPanelOpen)
            return;

        if (exitSequenceActive)
        {
            if (Input.GetMouseButtonDown(0))
            {
                AdvanceExitSequence();
            }
            return;
        }

        if (!awaitingChoice && Input.GetMouseButtonDown(0))
        {
            AdvanceIntroSequence();
        }
    }

    /// <summary>
    /// Starts the conversation if it is not already open.
    /// </summary>
    public void BeginConversation()
    {
        if (conversationActive)
            return;

        LoadLocalizationIfNeeded();
        conversationActive = true;
        awaitingChoice = false;
        exitSequenceActive = false;
        shopPanelOpen = false;
        introLineIndex = -1;
        exitLineIndex = -1;
        activeLineAssigned = false;
        activeLine = default;
        ResetChoiceToggles();
        PlayerController.SetGlobalInputEnabled(false);
        SetBackgroundDimmer(true);
        conversationPanel?.SlideIn();

        if (introLines != null && introLines.Count > 0)
        {
            DisplayIntroLineAt(0);
        }
        else
        {
            EnterChoiceState();
        }
    }

    private void AdvanceIntroSequence()
    {
        if (introLines == null || introLines.Count == 0)
        {
            EnterChoiceState();
            return;
        }

        int nextIndex = Mathf.Min(introLineIndex + 1, introLines.Count - 1);
        if (nextIndex == introLineIndex)
        {
            EnterChoiceState();
            return;
        }

        DisplayIntroLineAt(nextIndex);
    }

    private void AdvanceExitSequence()
    {
        if (!exitSequenceActive)
            return;

        if (exitLines == null || exitLines.Count == 0)
        {
            EndConversation();
            return;
        }

        int nextIndex = exitLineIndex + 1;
        if (nextIndex >= exitLines.Count)
        {
            EndConversation();
        }
        else
        {
            DisplayExitLineAt(nextIndex);
        }
    }

    private void DisplayIntroLineAt(int index)
    {
        if (introLines == null || index < 0 || index >= introLines.Count)
            return;

        introLineIndex = index;
        DialogueLineReference line = introLines[index];
        ShowLine(line);

        bool isChoiceTrigger = IsChoiceTrigger(line);
        bool isLastLine = introLineIndex >= introLines.Count - 1;
        if ((isChoiceTrigger || isLastLine) && !awaitingChoice)
        {
            EnterChoiceState();
        }
    }

    private void DisplayExitLineAt(int index)
    {
        if (exitLines == null || index < 0 || index >= exitLines.Count)
            return;

        exitLineIndex = index;
        DialogueLineReference line = exitLines[index];
        ShowLine(line);
    }

    private void ShowLine(DialogueLineReference line)
    {
        activeLine = line;
        activeLineAssigned = true;
        string lineText = GetLocalizedLine(line);
        if (dialogueLabel != null)
        {
            dialogueLabel.text = lineText;
        }
        if (speakerLabel != null)
        {
            speakerLabel.text = string.Empty;
        }
    }

    private void EnterChoiceState()
    {
        awaitingChoice = true;
        UpdateChoiceButtonsVisibility();
    }

    private void HandleBuySelected()
    {
        if (!awaitingChoice || shopUIManager == null)
            return;

        bool opened = shopUIManager.OpenBuyPanel(true);
        shopPanelOpen = opened && shopUIManager.IsOpen;
        UpdateChoiceButtonsVisibility();

        if (!shopPanelOpen)
        {
            Debug.LogWarning("[ShopConversationController] Failed to open buy panel. Re-enabling choices.");
        }
    }

    private void HandleSellSelected()
    {
        if (!awaitingChoice || shopUIManager == null)
            return;

        bool opened = shopUIManager.OpenSellPanel(true);
        shopPanelOpen = opened && shopUIManager.IsOpen;
        UpdateChoiceButtonsVisibility();

        if (!shopPanelOpen)
        {
            Debug.LogWarning("[ShopConversationController] Failed to open sell panel. Re-enabling choices.");
        }
    }

    private void HandleExitSelected()
    {
        if (!awaitingChoice)
            return;

        awaitingChoice = false;
        exitSequenceActive = exitLines != null && exitLines.Count > 0;
        UpdateChoiceButtonsVisibility();

        if (exitSequenceActive)
        {
            DisplayExitLineAt(0);
        }
        else
        {
            EndConversation();
        }
    }

    private void HandleShopClosed()
    {
        shopPanelOpen = false;
        if (!conversationActive)
            return;

        if (!exitSequenceActive)
        {
            awaitingChoice = true;
            ShowLine(choiceTriggerLine);
        }
        UpdateChoiceButtonsVisibility();
    }

    private void HandleShopOpened()
    {
        shopPanelOpen = true;
        if (conversationActive)
        {
            UpdateChoiceButtonsVisibility();
        }
    }

    private void EndConversation()
    {
        if (!conversationActive)
            return;

        conversationActive = false;
        awaitingChoice = false;
        exitSequenceActive = false;
        shopPanelOpen = false;
        activeLineAssigned = false;
        UpdateChoiceButtonsVisibility();

        if (shopUIManager != null && shopUIManager.IsOpen)
        {
            shopUIManager.CloseShop();
        }

        SetBackgroundDimmer(false);
        conversationPanel?.SlideOut();
        PlayerController.SetGlobalInputEnabled(true);
    }

    private void UpdateChoiceButtonsVisibility()
    {
        bool shouldShowChoices = awaitingChoice && !exitSequenceActive && conversationActive;
        if (choiceButtonGroup != null)
        {
            choiceButtonGroup.SetActive(shouldShowChoices);
        }

        SetChoiceTogglesInteractable(shouldShowChoices);

        if (shouldShowChoices)
        {
            UpdateChoiceToggleInteractivity();
        }
        else
        {
            ResetChoiceToggles();
        }
    }

    private void HandleBuyToggleChanged(bool isOn)
    {
        UpdateChoiceButtonsVisibility();

        if (isOn)
        {
            HandleBuySelected();
        }
    }

    private void HandleSellToggleChanged(bool isOn)
    {
        UpdateChoiceButtonsVisibility();

        if (isOn)
        {
            HandleSellSelected();
        }
    }

    private void ResetChoiceToggles()
    {
        if (choiceToggleGroup != null)
        {
            bool previousAllowSwitchOff = choiceToggleGroup.allowSwitchOff;
            choiceToggleGroup.allowSwitchOff = true;
            choiceToggleGroup.SetAllTogglesOff();
            choiceToggleGroup.allowSwitchOff = previousAllowSwitchOff;
        }
        else
        {
            if (buyToggle != null)
            {
                buyToggle.SetIsOnWithoutNotify(false);
            }
            if (sellToggle != null)
            {
                sellToggle.SetIsOnWithoutNotify(false);
            }
        }
    }

    private void SetChoiceTogglesInteractable(bool interactable)
    {
        if (buyToggle != null)
        {
            buyToggle.interactable = interactable;
        }
        if (sellToggle != null)
        {
            sellToggle.interactable = interactable;
        }
    }

    private void UpdateChoiceToggleInteractivity()
    {
        bool buySelected = buyToggle != null && buyToggle.isOn;
        bool sellSelected = sellToggle != null && sellToggle.isOn;
        bool hasSelection = buySelected || sellSelected;

        if (buyToggle != null)
        {
            buyToggle.interactable = !hasSelection || !buySelected;
        }

        if (sellToggle != null)
        {
            sellToggle.interactable = !hasSelection || !sellSelected;
        }
    }

    private void SetBackgroundDimmer(bool shouldEnable)
    {
        if (backgroundDimmer == null)
            return;

        backgroundDimmer.gameObject.SetActive(shouldEnable);
    }

    private void LoadLocalizationIfNeeded()
    {
        if (localizationLoaded)
            return;

        localizedLines.Clear();
        fallbackLocalizedLines.Clear();
        localizationLoaded = true;

        TextAsset csv = dialogueCsvAsset;
        if (csv == null && !string.IsNullOrEmpty(dialogueCSVPath))
        {
            csv = Resources.Load<TextAsset>(dialogueCSVPath);
        }

        if (csv == null)
        {
            Debug.LogWarning("[ShopConversationController] Dialogue CSV not found. Using localization tables for resolution.");
        }
        else
        {
            string[] rows = csv.text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (rows.Length == 0)
            {
                BuildFallbackLocalization();
                return;
            }

            string[] header = ParseCsvLine(rows[0]);
            int textColumnIndex = DetermineTextColumnIndex(header);
            for (int i = 1; i < rows.Length; i++)
            {
                string[] cells = ParseCsvLine(rows[i]);
                if (cells.Length <= textColumnIndex || string.IsNullOrEmpty(cells[0]))
                    continue;

                localizedLines[cells[0]] = cells[textColumnIndex];
            }
        }

        BuildFallbackLocalization();
    }

    private int DetermineTextColumnIndex(string[] header)
    {
        if (header == null)
            return 0;
        if (header.Length < 2)
            return Mathf.Max(header.Length - 1, 0);

        Locale currentLocale = LocalizationSettings.SelectedLocale;
        string localeCode = currentLocale != null ? currentLocale.Identifier.Code : null;
        if (!string.IsNullOrEmpty(localeCode))
        {
            for (int i = 1; i < header.Length; i++)
            {
                if (string.Equals(header[i], localeCode, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
        }

        return 1;
    }

    private string GetLocalizedLine(DialogueLineReference line)
    {
        if (line.HasLocalizedString)
        {
            string localizedText = line.localizedString.GetLocalizedString();
            if (!string.IsNullOrEmpty(localizedText) && (!line.HasKey || !string.Equals(localizedText, line.id, StringComparison.Ordinal)))
            {
                return localizedText;
            }
        }

        if (line.HasKey)
        {
            string id = line.id;
            if (fallbackLocalizedLines.TryGetValue(id, out string fallbackText))
            {
                return fallbackText;
            }

            if (localizedLines.TryGetValue(id, out string text))
            {
                return text;
            }

            return id;
        }

        return string.Empty;
    }

    private void BuildFallbackLocalization()
    {
        HashSet<string> requiredKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddKeysFromLines(requiredKeys, introLines);
        AddKeyIfPresent(requiredKeys, choiceTriggerLine);
        AddKeysFromLines(requiredKeys, exitLines);

        foreach (string key in requiredKeys)
        {
            TryAddTableLocalization(key);
        }
    }

    private void TryAddTableLocalization(string key)
    {
        if (string.IsNullOrEmpty(key))
            return;

        if (!string.IsNullOrEmpty(defaultLocalizationTable))
        {
            LocalizedString localizedString = new LocalizedString(defaultLocalizationTable, key);
            string localizedText = localizedString.GetLocalizedString();
            if (!string.IsNullOrEmpty(localizedText) && !string.Equals(localizedText, key, StringComparison.Ordinal))
            {
                fallbackLocalizedLines[key] = localizedText;
                return;
            }
        }

        if (localizedLines.TryGetValue(key, out string csvText))
        {
            fallbackLocalizedLines[key] = csvText;
        }
    }

    private void HandleLocaleChanged(Locale locale)
    {
        localizationLoaded = false;
        LoadLocalizationIfNeeded();

        RefreshLocalizedStrings();

        if (activeLineAssigned)
        {
            ShowLine(activeLine);
        }
    }

    private void RefreshLocalizedStrings()
    {
        RefreshLocalizedString(choiceTriggerLine.localizedString);
        RefreshLocalizedStringCollection(introLines);
        RefreshLocalizedStringCollection(exitLines);
    }

    private void RefreshLocalizedString(LocalizedString localizedString)
    {
        if (localizedString != null && !localizedString.IsEmpty)
        {
            localizedString.RefreshString();
        }
    }

    private void RefreshLocalizedStringCollection(IEnumerable<DialogueLineReference> lines)
    {
        if (lines == null)
            return;

        foreach (DialogueLineReference line in lines)
        {
            RefreshLocalizedString(line.localizedString);
        }
    }

    private string[] ParseCsvLine(string line)
    {
        List<string> values = new List<string>();
        if (line == null)
            return values.ToArray();

        bool inQuotes = false;
        var current = new System.Text.StringBuilder();
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '\"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                {
                    current.Append('\"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                values.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        values.Add(current.ToString().Trim());
        return values.ToArray();
    }

    private void SubscribeShopEvents()
    {
        EnsureShopManagerReference();
        if (shopUIManager == null)
            return;

        shopUIManager.ShopOpened -= HandleShopOpened;
        shopUIManager.ShopClosed -= HandleShopClosed;
        shopUIManager.ShopOpened += HandleShopOpened;
        shopUIManager.ShopClosed += HandleShopClosed;
    }

    private void UnsubscribeShopEvents()
    {
        if (shopUIManager == null)
            return;

        shopUIManager.ShopOpened -= HandleShopOpened;
        shopUIManager.ShopClosed -= HandleShopClosed;
    }

    void EnsureShopManagerReference()
    {
        if (shopUIManager == null)
        {
            shopUIManager = FindFirstObjectByType<ShopUIManager>();
        }
    }

    private void AddKeysFromLines(HashSet<string> keys, IEnumerable<DialogueLineReference> lines)
    {
        if (lines == null)
            return;

        foreach (DialogueLineReference line in lines)
        {
            AddKeyIfPresent(keys, line);
        }
    }

    private void AddKeyIfPresent(HashSet<string> keys, DialogueLineReference line)
    {
        if (!string.IsNullOrEmpty(line.id))
        {
            keys.Add(line.id);
        }
    }

    private bool IsChoiceTrigger(DialogueLineReference line)
    {
        if (!choiceTriggerLine.HasKey || !line.HasKey)
            return false;

        return string.Equals(choiceTriggerLine.id, line.id, StringComparison.OrdinalIgnoreCase);
    }

    void OnValidate()
    {
        ValidateDialogueKeys();
    }

    private void ValidateDialogueKeys()
    {
        ValidateLines(introLines, "Intro line");
        ValidateLines(exitLines, "Exit line");

        if (choiceTriggerLine.HasKey && HasDuplicateKey(choiceTriggerLine.id, introLines.Concat(exitLines)))
        {
            Debug.LogWarning("[ShopConversationController] Choice trigger key duplicates another line. Verify the intended flow.", this);
        }
    }

    private void ValidateLines(IEnumerable<DialogueLineReference> lines, string labelPrefix)
    {
        if (lines == null)
            return;

        HashSet<string> seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int index = 0;
        foreach (DialogueLineReference line in lines)
        {
            if (!line.HasKey && !line.HasLocalizedString)
            {
                Debug.LogWarning($"[ShopConversationController] {labelPrefix} at index {index} has an empty key. CSV/localization table lookup will be skipped.", this);
            }
            else if (line.HasKey && !seenKeys.Add(line.id))
            {
                Debug.LogWarning($"[ShopConversationController] {labelPrefix} key '{line.id}' is duplicated. Only the first occurrence will be used for localization lookup.", this);
            }

            index++;
        }
    }

    private bool HasDuplicateKey(string key, IEnumerable<DialogueLineReference> lines)
    {
        if (string.IsNullOrEmpty(key) || lines == null)
            return false;

        return lines.Any(line => line.HasKey && string.Equals(line.id, key, StringComparison.OrdinalIgnoreCase));
    }
}
