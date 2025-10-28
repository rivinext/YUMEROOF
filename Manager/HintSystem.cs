using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HintSystem : MonoBehaviour
{
    public static HintSystem Instance { get; private set; }

    [SerializeField] private string hintCSVPath = "Data/YUME_ROOF - Hints";

    private readonly List<HintData> hints = new List<HintData>();
    private readonly Dictionary<string, (float start, float end)> timeRanges = new();
    private GameClock gameClock;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadHints();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu")
        {
            if (Instance == this) Instance = null;
            Destroy(gameObject);
            return;
        }
        gameClock = FindFirstObjectByType<GameClock>();
    }

    void LoadHints()
    {
        TextAsset csv = Resources.Load<TextAsset>(hintCSVPath);
        if (csv == null)
        {
            Debug.LogWarning($"Hint CSV not found at {hintCSVPath}");
            return;
        }

        var lines = csv.text.Split('\n');
        int i = 1;
        for (; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            var parts = line.Split(',');
            if (parts.Length < 10) continue;
            if (string.IsNullOrEmpty(parts[0])) break;

            HintData h = new HintData
            {
                id = parts[0],
                textID = parts[1],
                trigger = Enum.TryParse(parts[2], out TriggerType t) ? t : TriggerType.GameStart,
                triggerParam = parts[3],
                category = parts[4],
                priority = int.TryParse(parts[5], out int pr) ? pr : 0,
                repeatable = parts[6].Trim().Equals("TRUE", StringComparison.OrdinalIgnoreCase),
                cooldownMinutes = ParseCooldown(parts[7]),
                oncePerDay = parts[8].Trim().Equals("TRUE", StringComparison.OrdinalIgnoreCase),
                validTime = parts[9],
                lastShownTime = -1f,
                lastShownDay = -1,
                shown = false,
            };
            hints.Add(h);
        }

        for (i++; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            var parts = line.Split(',');
            if (parts.Length < 11) continue;
            string name = parts[9];
            string range = parts[10];
            var times = range.Split('-');
            if (times.Length != 2) continue;
            if (TryParseTime(times[0], out float start) && TryParseTime(times[1], out float end))
            {
                timeRanges[name] = (start, end);
            }
        }
    }

    float ParseCooldown(string s)
    {
        if (string.IsNullOrEmpty(s) || s == "0") return 0f;
        if (s.EndsWith("d") && float.TryParse(s.Substring(0, s.Length - 1), out float d))
            return d * 1440f;
        if (s.EndsWith("h") && float.TryParse(s.Substring(0, s.Length - 1), out float h))
            return h * 60f;
        return 0f;
    }

    float CurrentGameMinutes => (GameClock.Day - 1) * 1440f + (gameClock != null ? gameClock.currentMinutes : 0f);

    public HintData RequestHint(TriggerType trigger)
    {
        HintData best = null;
        foreach (var h in hints)
        {
            if (h.trigger != trigger) continue;
            if (!IsValidTime(h.validTime)) continue;
            if (!CheckTriggerCondition(h)) continue;
            if (!CanShow(h)) continue;

            if (best == null || h.priority > best.priority)
            {
                best = h;
            }
        }

        if (best != null)
        {
            best.shown = true;
            best.lastShownDay = GameClock.Day;
            best.lastShownTime = CurrentGameMinutes;
        }
        return best;
    }

    bool CanShow(HintData h)
    {
        if (!h.repeatable && h.shown) return false;
        if (h.oncePerDay && h.lastShownDay == GameClock.Day) return false;
        if (h.cooldownMinutes > 0f && h.lastShownTime >= 0f)
        {
            if (CurrentGameMinutes - h.lastShownTime < h.cooldownMinutes)
                return false;
        }
        return true;
    }

    bool TryParseTime(string s, out float minutes)
    {
        minutes = 0f;
        var parts = s.Split(':');
        if (parts.Length != 2) return false;
        if (!int.TryParse(parts[0], out int h)) return false;
        if (!int.TryParse(parts[1], out int m)) return false;
        minutes = h * 60f + m;
        return true;
    }

    bool IsValidTime(string time)
    {
        if (string.IsNullOrEmpty(time) || time.Equals("Any", StringComparison.OrdinalIgnoreCase)) return true;
        float mins = gameClock != null ? gameClock.currentMinutes : 0f;
        if (timeRanges.TryGetValue(time, out var range))
        {
            if (range.start <= range.end)
                return mins >= range.start && mins < range.end;
            else
                return mins >= range.start || mins < range.end;
        }
        return true;
    }

    bool CheckTriggerCondition(HintData h)
    {
        switch (h.trigger)
        {
            case TriggerType.GameStart:
                return true;
            case TriggerType.TimeOfDay:
                return IsValidTime(h.triggerParam);
            case TriggerType.StatusCheck:
                return EvaluateStatus(h.triggerParam);
            case TriggerType.MilestoneClear:
                return EvaluateMilestone(h.triggerParam);
            default:
                return false;
        }
    }

    bool EvaluateStatus(string param)
    {
        var tokens = param.Split(' ');
        if (tokens.Length != 3) return false;
        string key = tokens[0];
        string op = tokens[1];
        if (!float.TryParse(tokens[2], out float value)) return false;

        float current = 0f;
        switch (key)
        {
            case "Money":
                current = MoneyManager.Instance != null ? MoneyManager.Instance.CurrentMoney : 0f;
                break;
            case "Cozy":
                current = EnvironmentStatsManager.Instance != null ? EnvironmentStatsManager.Instance.CozyTotal : 0f;
                break;
            case "Nature":
                current = EnvironmentStatsManager.Instance != null ? EnvironmentStatsManager.Instance.NatureTotal : 0f;
                break;
            default:
                return false;
        }

        switch (op)
        {
            case "<": return current < value;
            case "<=": return current <= value;
            case ">": return current > value;
            case ">=": return current >= value;
            case "=":
            case "==": return Math.Abs(current - value) < 0.001f;
            case "!=": return Math.Abs(current - value) > 0.001f;
            default: return false;
        }
    }

    bool EvaluateMilestone(string param)
    {
        if (MilestoneManager.Instance == null) return false;
        var parts = param.Split('=');
        if (parts.Length != 2) return false;
        string id = parts[1];
        return MilestoneManager.Instance.CurrentMilestoneID != id;
    }

    [Serializable]
    public class HintData
    {
        public string id;
        public string textID;
        public TriggerType trigger;
        public string triggerParam;
        public string category;
        public int priority;
        public bool repeatable;
        public float cooldownMinutes;
        public bool oncePerDay;
        public string validTime;

        public bool shown;
        public float lastShownTime;
        public int lastShownDay;
    }
}

public enum TriggerType
{
    GameStart,
    StatusCheck,
    TimeOfDay,
    MilestoneClear
}
