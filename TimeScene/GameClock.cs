using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Simple game clock that tracks minutes within a 24 hour day.
/// </summary>
public class GameClock : MonoBehaviour
{
    public static GameClock Instance { get; private set; }
    // Total minutes passed in the current day (0-1440)
    public float currentMinutes = 6 * 60f;

    // Current day count starting from 1
    public int currentDay = 1;

    // Available time scales
    public float[] timeScales = { 4f, 16f, 32f, 64f };

    // Currently applied scale (0 for pause)
    float currentScale = 1f;

    public event Action<float> OnTimeScaleChanged;

    public event Action<int> OnDayChanged;
    public event Action<int> OnSleepAdvancedDay;

#if UNITY_EDITOR
    [Header("Developer Mode")]
    [SerializeField] private bool developerMode = false;
    [SerializeField, Range(0, 1440)] private int developerMinutes = 360;
    private int lastDeveloperMinutes = -1;
#endif

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu" && scene == SceneManager.GetActiveScene())
        {
            if (Instance == this) Instance = null;
            Destroy(gameObject);
        }
    }

    void Update()
    {
#if UNITY_EDITOR
        if (developerMode)
        {
            if (developerMinutes != lastDeveloperMinutes)
            {
                currentMinutes = Mathf.Clamp(developerMinutes, 0, 1440);
                lastDeveloperMinutes = developerMinutes;
            }
        }
#endif
        currentMinutes += Time.deltaTime * currentScale;

        if (currentMinutes >= 1440f)
        {
            currentMinutes -= 1440f;
            currentDay++;
            OnDayChanged?.Invoke(currentDay);
        }
    }

    /// <summary>
    /// Set the time scale. Use 0 for pause.
    /// </summary>
    public void SetTimeScale(float scale)
    {
        currentScale = scale;
        OnTimeScaleChanged?.Invoke(currentScale);
    }

    /// <summary>
    /// Manually sets the current time in minutes. Useful for developer mode.
    /// </summary>
    /// <param name="minutes">Minutes within the current day (0-1440).</param>
    public void SetTime(int minutes)
    {
        currentMinutes = Mathf.Clamp(minutes, 0, 1440);
    }

    /// <summary>
    /// Returns the current time as a string in 12-hour format rounded up to 5 minutes.
    /// Day starts at 6:00 AM and ends at 8:00 PM.
    /// </summary>
    public string GetFormattedTime()
    {
        int totalMinutes = Mathf.CeilToInt(currentMinutes / 5f) * 5;
        totalMinutes = Mathf.Min(totalMinutes, 1440);
        int hours = totalMinutes / 60;
        int minutes = totalMinutes % 60;

        string ampm = hours >= 12 ? "PM" : "AM";
        int displayHour = hours % 12;
        if (displayHour == 0) displayHour = 12;

        return string.Format("{0}:{1:00} {2}", displayHour, minutes, ampm);
    }

    /// <summary>
    /// Returns the current time normalized to [0,1].
    /// </summary>
    public float NormalizedTime => currentMinutes / 1440f;

    public float CurrentScale => currentScale;

    /// <summary>
    /// Set time to the specified minutes and advance to the next day.
    /// </summary>
    /// <param name="minutes">Minutes to set as the new current time.</param>
    public void SetTimeAndAdvanceDay(int minutes)
    {
        int previousDay = currentDay;
        currentMinutes = minutes;
        currentDay++;
        Debug.Log($"[GameClock] Day advanced from {previousDay} to {currentDay} at minute {minutes}.");
        OnDayChanged?.Invoke(currentDay);
    }

    public void TriggerSleepAdvancedDay()
    {
        Debug.Log($"[GameClock] TriggerSleepAdvancedDay invoked for day {currentDay}.");
        OnSleepAdvancedDay?.Invoke(currentDay);
    }

    public static int Day => Instance != null ? Instance.currentDay : 0;

    [Serializable]
    public class ClockData
    {
        public float currentMinutes;
        public int currentDay;
    }

    public ClockData GetSaveData()
    {
        return new ClockData { currentMinutes = currentMinutes, currentDay = currentDay };
    }

    public void ApplySaveData(ClockData data)
    {
        if (data == null) return;
        currentMinutes = data.currentMinutes;
        currentDay = data.currentDay;
        OnDayChanged?.Invoke(currentDay);
    }
}
