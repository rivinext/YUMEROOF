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
    public Button pauseButton;
    public Button scale1Button;
    public Button scale2Button;
    public Button scale4Button;
    public Button scale6Button;

    private Button _activeButton;
    private GameClock _currentClock;
    private bool _buttonsInitialized;
    private bool _timeScaleInitialized;

    void SetActiveButton(Button activeButton)
    {
        Button[] buttons = { pauseButton, scale1Button, scale2Button, scale4Button, scale6Button };

        foreach (var button in buttons)
        {
            if (button == null)
                continue;

            if (!button.interactable)
                button.interactable = true;
        }

        _activeButton = activeButton;

        var eventSystem = EventSystem.current;
        if (eventSystem != null && ShouldUpdateSelection(eventSystem))
        {
            eventSystem.SetSelectedGameObject(null);

            if (_activeButton != null)
            {
                eventSystem.SetSelectedGameObject(_activeButton.gameObject);
                _activeButton.Select();
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

        if (!_buttonsInitialized)
        {
            if (pauseButton != null)
                pauseButton.onClick.AddListener(() =>
                {
                    if (_currentClock == null)
                        return;
                    _currentClock.SetTimeScale(0f);
                    SetActiveButton(pauseButton);
                });
            if (scale1Button != null)
                scale1Button.onClick.AddListener(() =>
                {
                    if (_currentClock == null)
                        return;
                    _currentClock.SetTimeScale(_currentClock.timeScales[0]);
                    SetActiveButton(scale1Button);
                });
            if (scale2Button != null)
                scale2Button.onClick.AddListener(() =>
                {
                    if (_currentClock == null)
                        return;
                    _currentClock.SetTimeScale(_currentClock.timeScales[1]);
                    SetActiveButton(scale2Button);
                });
            if (scale4Button != null)
                scale4Button.onClick.AddListener(() =>
                {
                    if (_currentClock == null)
                        return;
                    _currentClock.SetTimeScale(_currentClock.timeScales[2]);
                    SetActiveButton(scale4Button);
                });
            if (scale6Button != null)
                scale6Button.onClick.AddListener(() =>
                {
                    if (_currentClock == null)
                        return;
                    _currentClock.SetTimeScale(_currentClock.timeScales[3]);
                    SetActiveButton(scale6Button);
                });

            _buttonsInitialized = true;
        }

        UpdateDayText(_currentClock.currentDay);
        _currentClock.OnDayChanged += UpdateDayText;

        if (!_timeScaleInitialized)
        {
            _currentClock.SetTimeScale(_currentClock.timeScales[0]);
            SetActiveButton(scale1Button);
            _timeScaleInitialized = true;
        }
        else if (_activeButton != null)
        {
            SetActiveButton(_activeButton);
        }

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
        Button targetButton = null;

        if (Mathf.Approximately(scale, 0f))
        {
            targetButton = pauseButton;
        }
        else if (_currentClock != null)
        {
            var scales = _currentClock.timeScales;
            if (scales != null && scales.Length > 0)
            {
                Button[] scaleButtons = { scale1Button, scale2Button, scale4Button, scale6Button };
                int maxIndex = Mathf.Min(scales.Length, scaleButtons.Length);

                for (int i = 0; i < maxIndex; i++)
                {
                    if (Mathf.Approximately(scale, scales[i]))
                    {
                        targetButton = scaleButtons[i];
                        break;
                    }
                }

                if (targetButton == null && scaleButtons[0] != null && Mathf.Approximately(scale, 1f))
                    targetButton = scaleButtons[0];
            }
        }

        if (targetButton != null)
            SetActiveButton(targetButton);
    }
}
