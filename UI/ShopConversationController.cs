using System;
using System.Collections.Generic;
using System.Text;
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
    [Header("UI")]
    [SerializeField] private InteractionSlidePanel conversationPanel;
    [SerializeField] private TextMeshProUGUI speakerLabel;
    [SerializeField] private TextMeshProUGUI dialogueLabel;
    [SerializeField] private Image backgroundDimmer;
    [SerializeField] private GameObject choiceButtonGroup;
    [SerializeField] private Button buyButton;
    [SerializeField] private Button sellButton;
    [SerializeField] private Button exitButton;

    [Header("Conversation")]
    [SerializeField] private string dialogueCSVPath = "Data/ShopDialogue";
    [SerializeField] private List<string> introLineIds = new() { "greeting", "askPurpose" };
    [SerializeField] private string choiceTriggerLineId = "askPurpose";
    [SerializeField] private List<string> exitLineIds = new() { "farewell" };

    [Header("Dependencies")]
    [SerializeField] private ShopUIManager shopUIManager;

    private readonly Dictionary<string, string> localizedLines = new(StringComparer.OrdinalIgnoreCase);
    private bool localizationLoaded;
    private int introLineIndex = -1;
    private int exitLineIndex = -1;
    private bool awaitingChoice;
    private bool exitSequenceActive;
    private bool shopPanelOpen;
    private bool conversationActive;
    private string activeLineId;

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

        if (buyButton != null)
        {
            buyButton.onClick.AddListener(HandleBuySelected);
        }

        if (sellButton != null)
        {
            sellButton.onClick.AddListener(HandleSellSelected);
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
        activeLineId = null;
        PlayerController.SetGlobalInputEnabled(false);
        SetBackgroundDimmer(true);
        conversationPanel?.SlideIn();

        if (introLineIds != null && introLineIds.Count > 0)
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
        if (introLineIds == null || introLineIds.Count == 0)
        {
            EnterChoiceState();
            return;
        }

        int nextIndex = Mathf.Min(introLineIndex + 1, introLineIds.Count - 1);
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

        if (exitLineIds == null || exitLineIds.Count == 0)
        {
            EndConversation();
            return;
        }

        int nextIndex = exitLineIndex + 1;
        if (nextIndex >= exitLineIds.Count)
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
        if (introLineIds == null || index < 0 || index >= introLineIds.Count)
            return;

        introLineIndex = index;
        string id = introLineIds[index];
        ShowLine(id);

        bool isChoiceTrigger = !string.IsNullOrEmpty(choiceTriggerLineId) &&
                               string.Equals(id, choiceTriggerLineId, StringComparison.OrdinalIgnoreCase);
        bool isLastLine = introLineIndex >= introLineIds.Count - 1;
        if ((isChoiceTrigger || isLastLine) && !awaitingChoice)
        {
            EnterChoiceState();
        }
    }

    private void DisplayExitLineAt(int index)
    {
        if (exitLineIds == null || index < 0 || index >= exitLineIds.Count)
            return;

        exitLineIndex = index;
        string id = exitLineIds[index];
        ShowLine(id);
    }

    private void ShowLine(string id)
    {
        activeLineId = id;
        string lineText = GetLocalizedLine(id);
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

        shopPanelOpen = true;
        UpdateChoiceButtonsVisibility();
        shopUIManager.OpenBuyPanel(true);
    }

    private void HandleSellSelected()
    {
        if (!awaitingChoice || shopUIManager == null)
            return;

        shopPanelOpen = true;
        UpdateChoiceButtonsVisibility();
        shopUIManager.OpenSellPanel(true);
    }

    private void HandleExitSelected()
    {
        if (!awaitingChoice)
            return;

        awaitingChoice = false;
        exitSequenceActive = exitLineIds != null && exitLineIds.Count > 0;
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
            if (!string.IsNullOrEmpty(choiceTriggerLineId))
            {
                ShowLine(choiceTriggerLineId);
            }
        }
        UpdateChoiceButtonsVisibility();
    }

    private void EndConversation()
    {
        if (!conversationActive)
            return;

        conversationActive = false;
        awaitingChoice = false;
        exitSequenceActive = false;
        shopPanelOpen = false;
        activeLineId = null;
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
        if (choiceButtonGroup == null)
            return;

        bool visible = awaitingChoice && !shopPanelOpen && !exitSequenceActive && conversationActive;
        choiceButtonGroup.SetActive(visible);
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
        localizationLoaded = true;

        if (string.IsNullOrEmpty(dialogueCSVPath))
        {
            Debug.LogWarning("[ShopConversationController] Dialogue CSV path is empty.");
            return;
        }

        TextAsset csv = Resources.Load<TextAsset>(dialogueCSVPath);
        if (csv == null)
        {
            Debug.LogWarning($"[ShopConversationController] CSV not found at {dialogueCSVPath}");
            return;
        }

        string[] rows = csv.text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        if (rows.Length == 0)
            return;

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

    private string GetLocalizedLine(string id)
    {
        if (string.IsNullOrEmpty(id))
            return string.Empty;

        if (localizedLines.TryGetValue(id, out string text))
        {
            return text;
        }

        return id;
    }

    private void HandleLocaleChanged(Locale locale)
    {
        localizationLoaded = false;
        LoadLocalizationIfNeeded();

        if (!string.IsNullOrEmpty(activeLineId))
        {
            ShowLine(activeLineId);
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

        shopUIManager.ShopClosed -= HandleShopClosed;
        shopUIManager.ShopClosed += HandleShopClosed;
    }

    private void UnsubscribeShopEvents()
    {
        if (shopUIManager == null)
            return;

        shopUIManager.ShopClosed -= HandleShopClosed;
    }

    void EnsureShopManagerReference()
    {
        if (shopUIManager == null)
        {
            shopUIManager = FindFirstObjectByType<ShopUIManager>();
        }
    }
}
