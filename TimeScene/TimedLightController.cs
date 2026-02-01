using UnityEngine;

/// <summary>
/// Enables or disables a Light component based on the in-game time.
/// </summary>
[RequireComponent(typeof(Light))]
public class TimedLightController : MonoBehaviour
{
    [SerializeField, Range(0, 1440)] private int turnOnTimeMinutes;
    [SerializeField, Range(0, 1440)] private int turnOffTimeMinutes;

    private Light targetLight;
    private GameClock clock;
    private float defaultIntensity;
    private DayNightLighting dayNightLighting;
    private bool controlsIntensity = true;
    private float lastMinutes = -1f;
    private ManualOverrideState manualOverride = ManualOverrideState.None;

    private enum ManualOverrideState
    {
        None,
        ForceOn,
        ForceOff
    }

    void Awake()
    {
        targetLight = GetComponent<Light>();
        dayNightLighting = GetComponent<DayNightLighting>();
        controlsIntensity = dayNightLighting == null;
        if (targetLight != null)
        {
            if (controlsIntensity)
            {
                defaultIntensity = targetLight.intensity;
            }
        }
        clock = FindFirstObjectByType<GameClock>();
    }

    void Update()
    {
        if (clock == null || targetLight == null) return;

        float currentMinutes = clock.currentMinutes;
        HandleManualOverrideWindows(currentMinutes);
        bool shouldEnable;

        if (turnOnTimeMinutes < turnOffTimeMinutes)
        {
            shouldEnable = currentMinutes >= turnOnTimeMinutes && currentMinutes < turnOffTimeMinutes;
        }
        else if (turnOnTimeMinutes > turnOffTimeMinutes)
        {
            shouldEnable = currentMinutes >= turnOnTimeMinutes || currentMinutes < turnOffTimeMinutes;
        }
        else
        {
            shouldEnable = false;
        }

        if (manualOverride == ManualOverrideState.ForceOn)
        {
            shouldEnable = true;
        }
        else if (manualOverride == ManualOverrideState.ForceOff)
        {
            shouldEnable = false;
        }

        targetLight.enabled = shouldEnable;
        if (controlsIntensity)
        {
            targetLight.intensity = shouldEnable ? defaultIntensity : 0f;
        }

        lastMinutes = currentMinutes;
    }

    public void ToggleManualOverride()
    {
        if (clock == null) return;

        bool autoShouldEnable = IsAutoOn(clock.currentMinutes);
        manualOverride = autoShouldEnable ? ManualOverrideState.ForceOff : ManualOverrideState.ForceOn;
    }

    public void SetManualOverride(bool forceOn)
    {
        manualOverride = forceOn ? ManualOverrideState.ForceOn : ManualOverrideState.ForceOff;
    }

    public void ClearManualOverride()
    {
        manualOverride = ManualOverrideState.None;
    }

    private bool IsAutoOn(float currentMinutes)
    {
        if (turnOnTimeMinutes < turnOffTimeMinutes)
        {
            return currentMinutes >= turnOnTimeMinutes && currentMinutes < turnOffTimeMinutes;
        }

        if (turnOnTimeMinutes > turnOffTimeMinutes)
        {
            return currentMinutes >= turnOnTimeMinutes || currentMinutes < turnOffTimeMinutes;
        }

        return false;
    }

    private void HandleManualOverrideWindows(float currentMinutes)
    {
        if (lastMinutes < 0f)
        {
            lastMinutes = currentMinutes;
            return;
        }

        bool wrappedDay = lastMinutes > currentMinutes;
        if (manualOverride == ManualOverrideState.ForceOn)
        {
            if (HasCrossedTime(lastMinutes, currentMinutes, turnOffTimeMinutes, wrappedDay))
            {
                manualOverride = ManualOverrideState.None;
            }
        }
        else if (manualOverride == ManualOverrideState.ForceOff)
        {
            if (HasCrossedTime(lastMinutes, currentMinutes, turnOnTimeMinutes, wrappedDay))
            {
                manualOverride = ManualOverrideState.None;
            }
        }
    }

    private static bool HasCrossedTime(float previousMinutes, float currentMinutes, int targetMinutes, bool wrappedDay)
    {
        if (!wrappedDay)
        {
            return previousMinutes < targetMinutes && currentMinutes >= targetMinutes;
        }

        return previousMinutes < targetMinutes || currentMinutes >= targetMinutes;
    }

    public string TurnOnTime
    {
        get => MinutesToTimeString(turnOnTimeMinutes);
        set => turnOnTimeMinutes = ParseTimeString(value);
    }

    public string TurnOffTime
    {
        get => MinutesToTimeString(turnOffTimeMinutes);
        set => turnOffTimeMinutes = ParseTimeString(value);
    }

    public void SetManualOverride(bool on)
    {
        manualOverrideActive = true;
        manualOverrideOn = on;
    }

    public void ToggleManualOverride()
    {
        manualOverrideActive = true;
        manualOverrideOn = !manualOverrideOn;
    }

    private int ParseTimeString(string time)
    {
        if (string.IsNullOrEmpty(time)) return 0;
        var parts = time.Split(':');
        int hours = 0;
        int minutes = 0;
        if (parts.Length > 0) int.TryParse(parts[0], out hours);
        if (parts.Length > 1) int.TryParse(parts[1], out minutes);
        return Mathf.Clamp(hours * 60 + minutes, 0, 1440);
    }

    private string MinutesToTimeString(int minutes)
    {
        int hours = minutes / 60;
        int mins = minutes % 60;
        return string.Format("{0:00}:{1:00}", hours, mins);
    }
}
