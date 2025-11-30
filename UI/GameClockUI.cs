using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.EventSystems;

public class GameClockUI : MonoBehaviour
{
    public GameClock clock;
    public TMP_Text timeText;
    public TMP_Text dayText;
    public ToggleGroup timeScaleToggleGroup;
    public Toggle pauseToggle;
    public Toggle scale1Toggle;
    public Toggle scale2Toggle;
    public Toggle scale4Toggle;
    public Toggle scale6Toggle;

    private Toggle _activeToggle;
    private GameClock _currentClock;

    void SetActiveToggle(Toggle activeToggle)
    {
        Toggle[] toggles = { pauseToggle, scale1Toggle, scale2Toggle, scale4Toggle, scale6Toggle };

        foreach (var toggle in toggles)
        {
            if (toggle == null)
                continue;

            if (!toggle.interactable)
                toggle.interactable = true;
        }

        _activeToggle = activeToggle;

        if (_activeToggle != null && !_activeToggle.isOn)
            _activeToggle.isOn = true;

        var eventSystem = EventSystem.current;
        if (eventSystem != null && ShouldUpdateSelection(eventSystem))
        {
            eventSystem.SetSelectedGameObject(null);

            if (_activeToggle != null)
            {
                eventSystem.SetSelectedGameObject(_activeToggle.gameObject);
                _activeToggle.Select();
            }
        }
    }

    bool ShouldUpdateSelection(EventSystem eventSystem)
    {
        var selectedGameObject = eventSystem.currentSelectedGameObject;
        if (selectedGameObject == null)
            return true;

        return selectedGameObject.transform.IsChildOf(transform);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        StartCoroutine(AssignClockWhenAvailable());
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (_currentClock != null)
        {
            _currentClock.OnDayChanged -= UpdateDayText;
            _currentClock.OnTimeScaleChanged -= HandleTimeScaleChanged;
        }
    }

    void Update()
    {
        if (_currentClock != null && timeText != null)
            timeText.text = _currentClock.GetFormattedTime();
    }

    void UpdateDayText(int day)
    {
        if (dayText != null)
            dayText.text = day.ToString();
    }

    void InitializeClock()
    {
        if (clock == null)
            return;

        if (_currentClock != null)
        {
            _currentClock.OnDayChanged -= UpdateDayText;
            _currentClock.OnTimeScaleChanged -= HandleTimeScaleChanged;
        }

        _currentClock = clock;
        _currentClock.OnTimeScaleChanged += HandleTimeScaleChanged;

        Toggle[] toggles = { pauseToggle, scale1Toggle, scale2Toggle, scale4Toggle, scale6Toggle };

        foreach (var toggle in toggles)
        {
            if (toggle == null)
                continue;

            toggle.onValueChanged.RemoveAllListeners();
            if (timeScaleToggleGroup != null)
                toggle.group = timeScaleToggleGroup;
        }

        if (pauseToggle != null)
            pauseToggle.onValueChanged.AddListener(isOn =>
            {
                if (!isOn || _currentClock == null)
                    return;
                _currentClock.SetTimeScale(0f);
                SetActiveToggle(pauseToggle);
            });
        if (scale1Toggle != null)
            scale1Toggle.onValueChanged.AddListener(isOn =>
            {
                if (!isOn || _currentClock == null)
                    return;
                _currentClock.SetTimeScale(_currentClock.timeScales[0]);
                SetActiveToggle(scale1Toggle);
            });
        if (scale2Toggle != null)
            scale2Toggle.onValueChanged.AddListener(isOn =>
            {
                if (!isOn || _currentClock == null)
                    return;
                _currentClock.SetTimeScale(_currentClock.timeScales[1]);
                SetActiveToggle(scale2Toggle);
            });
        if (scale4Toggle != null)
            scale4Toggle.onValueChanged.AddListener(isOn =>
            {
                if (!isOn || _currentClock == null)
                    return;
                _currentClock.SetTimeScale(_currentClock.timeScales[2]);
                SetActiveToggle(scale4Toggle);
            });
        if (scale6Toggle != null)
            scale6Toggle.onValueChanged.AddListener(isOn =>
            {
                if (!isOn || _currentClock == null)
                    return;
                _currentClock.SetTimeScale(_currentClock.timeScales[3]);
                SetActiveToggle(scale6Toggle);
            });

        UpdateDayText(_currentClock.currentDay);
        _currentClock.OnDayChanged += UpdateDayText;

        _currentClock.SetTimeScale(_currentClock.timeScales[0]);
        SetActiveToggle(scale1Toggle);
        HandleTimeScaleChanged(_currentClock.CurrentScale);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(AssignClockWhenAvailable());
    }

    IEnumerator AssignClockWhenAvailable()
    {
        if (clock != null)
        {
            InitializeClock();
            yield break;
        }

        while (clock == null)
        {
            clock = GameClock.Instance ?? FindFirstObjectByType<GameClock>(FindObjectsInactive.Include);
            if (clock == null)
                yield return null;
        }

        InitializeClock();
    }

    void HandleTimeScaleChanged(float scale)
    {
        Toggle targetToggle = null;

        if (Mathf.Approximately(scale, 0f))
        {
            targetToggle = pauseToggle;
        }
        else if (_currentClock != null)
        {
            var scales = _currentClock.timeScales;
            if (scales != null && scales.Length > 0)
            {
                Toggle[] scaleToggles = { scale1Toggle, scale2Toggle, scale4Toggle, scale6Toggle };
                int maxIndex = Mathf.Min(scales.Length, scaleToggles.Length);

                for (int i = 0; i < maxIndex; i++)
                {
                    if (Mathf.Approximately(scale, scales[i]))
                    {
                        targetToggle = scaleToggles[i];
                        break;
                    }
                }

                if (targetToggle == null && scaleToggles[0] != null && Mathf.Approximately(scale, 1f))
                    targetToggle = scaleToggles[0];
            }
        }

        if (targetToggle != null)
            SetActiveToggle(targetToggle);
    }
}
