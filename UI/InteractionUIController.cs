using System;
using System.Collections.Generic;
using System.Text;
using Interaction;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// インタラクション用の会話 UI を管理します。
/// 対象と CSV から構築されたテキストキューを受け取り、
/// スライドイン/アウトやクリック進行、WASD入力での自動クローズを制御します。
/// </summary>
public class InteractionUIController : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private InteractionSlidePanel slidePanel;

    [Header("Text Elements")]
    [SerializeField] private TextMeshProUGUI speakerLabel;
    [SerializeField] private TextMeshProUGUI lineLabel;

    [Header("Background")]
    [SerializeField] private Image backgroundDimmer;
    [SerializeField] private bool enableBackgroundDimmer = true;

    [Header("Behaviour")]
    [SerializeField] private bool closeOnMovementInput = true;
    [SerializeField] private bool allowClickAdvance = true;

    private readonly Queue<InteractionLine> queuedLines = new();
    private InteractionLine? currentLine;
    private IInteractable currentTarget;
    private bool waitForMovementClose;
    private bool closeOnMovementAfterCompletion;

    /// <summary>
    /// 現在パネルが開いているかどうか。
    /// </summary>
    public bool IsPanelOpen => slidePanel != null && slidePanel.IsOpen;

    public IInteractable CurrentTarget => currentTarget;

    public event Action<IInteractable> InteractionClosed;

    private void Reset()
    {
        slidePanel = GetComponentInChildren<InteractionSlidePanel>();
    }

    private void Update()
    {
        if (!IsPanelOpen)
            return;

        if (waitForMovementClose)
        {
            if (IsMovementInputTriggered())
            {
                CloseInteraction();
            }
            return;
        }

        if (allowClickAdvance && Input.GetMouseButtonDown(0))
        {
            AdvanceDialogue();
        }

        if (closeOnMovementInput && IsMovementInputTriggered())
        {
            CloseInteraction();
        }
    }

    /// <summary>
    /// 指定の対象とセリフキューで会話を開始します。
    /// </summary>
    public void BeginInteraction(IInteractable target, Queue<InteractionLine> lines)
    {
        if (target == null || lines == null || lines.Count == 0)
        {
            CloseInteraction();
            return;
        }

        currentTarget = target;
        queuedLines.Clear();
        foreach (var line in lines)
        {
            queuedLines.Enqueue(line);
        }
        currentLine = null;
        waitForMovementClose = false;

        slidePanel?.SlideIn();
        SetBackgroundDimmer(true);
        DisplayNextLine();
    }

    public void SetCloseOnMovementAfterComplete(bool shouldWait)
    {
        closeOnMovementAfterCompletion = shouldWait;
        if (!shouldWait)
        {
            waitForMovementClose = false;
        }
    }

    /// <summary>
    /// PlayerProximityInteractor 等から呼び出し、対象が切り替わった際に同期を取ります。
    /// </summary>
    public void HandleTargetChanged(IInteractable focusedTarget)
    {
        if (currentTarget == null)
            return;

        if (focusedTarget != currentTarget)
        {
            CloseInteraction();
        }
    }

    /// <summary>
    /// 現在のセリフをログに追加し、次のセリフを表示します。
    /// </summary>
    public void AdvanceDialogue()
    {
        if (!IsPanelOpen)
            return;

        if (waitForMovementClose)
            return;

        DisplayNextLine();
    }

    /// <summary>
    /// 会話を終了し、ログや背景の状態をリセットします。
    /// </summary>
    public void CloseInteraction()
    {
        var closedTarget = currentTarget;
        waitForMovementClose = false;
        closeOnMovementAfterCompletion = false;
        queuedLines.Clear();
        currentTarget = null;

        currentLine = null;

        if (speakerLabel != null)
            speakerLabel.text = string.Empty;
        if (lineLabel != null)
            lineLabel.text = string.Empty;

        slidePanel?.SlideOut();
        SetBackgroundDimmer(false);

        if (closedTarget != null)
        {
            InteractionClosed?.Invoke(closedTarget);
        }
    }

    /// <summary>
    /// CSV テキストアセットから会話キューを構築します。
    /// 1列目: 話者名、2列目: セリフを想定しています。
    /// </summary>
    public Queue<InteractionLine> BuildQueueFromCsv(TextAsset csv, bool skipHeader = true)
    {
        var queue = new Queue<InteractionLine>();
        if (csv == null || string.IsNullOrWhiteSpace(csv.text))
            return queue;

        var lines = csv.text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        bool isHeader = skipHeader;
        foreach (var raw in lines)
        {
            if (isHeader)
            {
                isHeader = false;
                continue;
            }

            var values = ParseCsvLine(raw);
            if (values.Length == 0)
                continue;

            string speaker = values.Length > 0 ? values[0] : string.Empty;
            string body = values.Length > 1 ? values[1] : string.Empty;
            queue.Enqueue(new InteractionLine(speaker, body));
        }

        return queue;
    }

    private void DisplayNextLine()
    {
        if (currentLine.HasValue)
            currentLine = null;

        if (queuedLines.Count == 0)
        {
            if (closeOnMovementAfterCompletion)
            {
                waitForMovementClose = true;
                closeOnMovementAfterCompletion = false;
            }
            else
            {
                CloseInteraction();
            }
            return;
        }

        currentLine = queuedLines.Dequeue();
        if (speakerLabel != null)
        {
            speakerLabel.text = currentLine.Value.Speaker ?? string.Empty;
        }
        if (lineLabel != null)
        {
            lineLabel.text = currentLine.Value.Message ?? string.Empty;
        }
    }

    private void SetBackgroundDimmer(bool shouldEnable)
    {
        if (backgroundDimmer == null)
            return;

        bool active = shouldEnable && enableBackgroundDimmer;
        backgroundDimmer.gameObject.SetActive(active);
    }

    private string[] ParseCsvLine(string line)
    {
        var values = new List<string>();
        if (line == null)
            return values.ToArray();

        StringBuilder currentValue = new StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '\"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                {
                    currentValue.Append('\"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                values.Add(currentValue.ToString().Trim());
                currentValue.Clear();
            }
            else
            {
                currentValue.Append(c);
            }
        }

        values.Add(currentValue.ToString().Trim());
        return values.ToArray();
    }

    private bool IsMovementInputTriggered()
    {
        return Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) ||
               Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D);
    }

    [Serializable]
    public readonly struct InteractionLine
    {
        public readonly string Speaker;
        public readonly string Message;

        public InteractionLine(string speaker, string message)
        {
            Speaker = speaker;
            Message = message;
        }
    }
}
