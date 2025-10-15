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

    void Awake()
    {
        targetLight = GetComponent<Light>();
        if (targetLight != null)
        {
            defaultIntensity = targetLight.intensity;
        }
        clock = FindObjectOfType<GameClock>();
    }

    void Update()
    {
        if (clock == null || targetLight == null) return;

        float currentMinutes = clock.currentMinutes;
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

        targetLight.enabled = shouldEnable;
        targetLight.intensity = shouldEnable ? defaultIntensity : 0f;
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
