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
    private bool manualOverrideActive;
    private bool manualOverrideOn;
    private float lastMinutes;

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
        bool crossedTurnOffTime = (lastMinutes < turnOffTimeMinutes && currentMinutes >= turnOffTimeMinutes)
            || (lastMinutes > currentMinutes
                && (turnOffTimeMinutes >= lastMinutes || turnOffTimeMinutes <= currentMinutes));

        if (crossedTurnOffTime)
        {
            manualOverrideActive = false;
            manualOverrideOn = false;
        }

        bool shouldEnable = ShouldEnableForTime(currentMinutes);

        if (manualOverrideActive)
        {
            shouldEnable = manualOverrideOn;
        }

        targetLight.enabled = shouldEnable;
        if (controlsIntensity)
        {
            targetLight.intensity = shouldEnable ? defaultIntensity : 0f;
        }

        lastMinutes = currentMinutes;
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

    public void ToggleManualOverrideFromInteract()
    {
        if (clock != null && !manualOverrideActive && ShouldEnableForTime(clock.currentMinutes))
        {
            manualOverrideActive = true;
            manualOverrideOn = false;
            return;
        }

        ToggleManualOverride();
    }

    private bool ShouldEnableForTime(float currentMinutes)
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
