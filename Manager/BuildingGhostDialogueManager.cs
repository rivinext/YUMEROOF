using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class BuildingGhostDialogueManager : MonoBehaviour
{
    public static BuildingGhostDialogueManager Instance { get; private set; }

    [SerializeField] private string dialogueCSVPath = "Data/GhostDialogues";
    [SerializeField] private bool skipIfInstanceExists = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [SerializeField] private bool logDuplicateInEditor = true;
#endif

    private readonly List<DialogueDefinition> definitions = new List<DialogueDefinition>();

    public static BuildingGhostDialogueManager CreateIfNeeded()
    {
        Instance = Instance ?? FindFirstObjectByType<BuildingGhostDialogueManager>(FindObjectsInactive.Include);

        if (Instance == null)
            Instance = new GameObject(nameof(BuildingGhostDialogueManager)).AddComponent<BuildingGhostDialogueManager>();

        return Instance;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (logDuplicateInEditor)
            {
                Debug.LogWarning($"[BuildingGhostDialogueManager] Duplicate instance found in scene '{gameObject.scene.name}'. This instance will be skipped because another instance already exists.");
            }
#endif
            if (!skipIfInstanceExists)
                return;

            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadDefinitions();
    }

    public bool TrySelectDialogue(out IReadOnlyList<DialogueLine> lines)
    {
        if (definitions.Count == 0)
        {
            lines = Array.Empty<DialogueLine>();
            return false;
        }

        var definition = SelectDefinition();
        if (definition != null && definition.lines.Count > 0)
        {
            lines = definition.lines;
            return true;
        }

        lines = Array.Empty<DialogueLine>();
        return false;
    }

    private DialogueDefinition SelectDefinition()
    {
        var snapshot = StatusSnapshot.Create();
        DialogueDefinition best = null;
        DialogueDefinition fallback = null;
        foreach (var def in definitions)
        {
            if (string.IsNullOrWhiteSpace(def.condition) && fallback == null)
            {
                fallback = def;
            }

            if (!def.Matches(snapshot))
                continue;

            if (best == null || def.priority > best.priority)
            {
                best = def;
            }
        }

        return best ?? fallback;
    }

    private void LoadDefinitions()
    {
        definitions.Clear();
        if (string.IsNullOrEmpty(dialogueCSVPath))
        {
            Debug.LogWarning("[BuildingGhostDialogueManager] Dialogue CSV path not configured");
            return;
        }

        TextAsset csv = Resources.Load<TextAsset>(dialogueCSVPath);
        if (csv == null)
        {
            Debug.LogWarning($"[BuildingGhostDialogueManager] CSV not found at {dialogueCSVPath}");
            return;
        }

        var rows = csv.text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        bool isHeader = true;
        foreach (var row in rows)
        {
            if (isHeader)
            {
                isHeader = false;
                continue;
            }

            var cells = ParseCsvLine(row);
            if (cells.Length < 5)
                continue;

            var def = new DialogueDefinition
            {
                id = cells[0],
                priority = ParseInt(cells[1]),
                condition = cells[2],
            };

            for (int i = 3; i + 1 < cells.Length; i += 2)
            {
                string speaker = cells[i];
                string message = cells[i + 1];
                if (string.IsNullOrEmpty(speaker) && string.IsNullOrEmpty(message))
                    continue;
                def.lines.Add(new DialogueLine(speaker, message));
            }

            if (def.lines.Count > 0)
            {
                definitions.Add(def);
            }
        }
    }

    private int ParseInt(string value)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
            return result;
        return 0;
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

    [Serializable]
    public readonly struct DialogueLine
    {
        public readonly string Speaker;
        public readonly string Message;

        public DialogueLine(string speaker, string message)
        {
            Speaker = speaker;
            Message = message;
        }
    }

    private sealed class DialogueDefinition
    {
        public string id;
        public int priority;
        public string condition;
        public readonly List<DialogueLine> lines = new List<DialogueLine>();

        public bool Matches(StatusSnapshot snapshot)
        {
            if (string.IsNullOrWhiteSpace(condition))
                return true;

            var clauses = condition.Split(new[] { "&&" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var clause in clauses)
            {
                if (!snapshot.EvaluateClause(clause.Trim()))
                    return false;
            }

            return true;
        }
    }

    private readonly struct StatusSnapshot
    {
        private static readonly string[] Operators = { "<=", ">=", "!=", "==", "<", ">", "=" };

        public readonly int Money;
        public readonly int Cozy;
        public readonly int Nature;
        public readonly string MilestoneId;

        private StatusSnapshot(int money, int cozy, int nature, string milestoneId)
        {
            Money = money;
            Cozy = cozy;
            Nature = nature;
            MilestoneId = milestoneId ?? string.Empty;
        }

        public static StatusSnapshot Create()
        {
            int money = MoneyManager.Instance != null ? MoneyManager.Instance.CurrentMoney : 0;
            int cozy = EnvironmentStatsManager.Instance != null ? EnvironmentStatsManager.Instance.CozyTotal : 0;
            int nature = EnvironmentStatsManager.Instance != null ? EnvironmentStatsManager.Instance.NatureTotal : 0;
            string milestoneId = MilestoneManager.Instance != null ? MilestoneManager.Instance.CurrentMilestoneID : string.Empty;
            return new StatusSnapshot(money, cozy, nature, milestoneId);
        }

        public bool EvaluateClause(string clause)
        {
            if (string.IsNullOrEmpty(clause))
                return true;

            foreach (var op in Operators)
            {
                int index = clause.IndexOf(op, StringComparison.Ordinal);
                if (index <= 0)
                    continue;

                string key = clause.Substring(0, index).Trim();
                string valueToken = clause.Substring(index + op.Length).Trim();

                if (key.Equals("Milestone", StringComparison.OrdinalIgnoreCase))
                {
                    return CompareStrings(MilestoneId, valueToken, op);
                }

                if (!float.TryParse(valueToken, NumberStyles.Float, CultureInfo.InvariantCulture, out float rhs))
                    return false;

                float lhs = GetNumericValue(key);
                return CompareNumbers(lhs, rhs, op);
            }

            return false;
        }

        private float GetNumericValue(string key)
        {
            switch (key.ToLowerInvariant())
            {
                case "money":
                    return Money;
                case "cozy":
                    return Cozy;
                case "nature":
                    return Nature;
                default:
                    return 0f;
            }
        }

        private bool CompareNumbers(float lhs, float rhs, string op)
        {
            switch (op)
            {
                case "<":
                    return lhs < rhs;
                case "<=":
                    return lhs <= rhs;
                case ">":
                    return lhs > rhs;
                case ">=":
                    return lhs >= rhs;
                case "=":
                case "==":
                    return Mathf.Approximately(lhs, rhs);
                case "!=":
                    return !Mathf.Approximately(lhs, rhs);
                default:
                    return false;
            }
        }

        private bool CompareStrings(string current, string expected, string op)
        {
            switch (op)
            {
                case "=":
                case "==":
                    return string.Equals(current, expected, StringComparison.OrdinalIgnoreCase);
                case "!=":
                    return !string.Equals(current, expected, StringComparison.OrdinalIgnoreCase);
                default:
                    return false;
            }
        }
    }
}
