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

    void ApplyTimeScale(Toggle toggle, float timeScale)
    {
        if (_currentClock == null || toggle == null)
            return;

        _currentClock.SetTimeScale(timeScale);
        SetActiveToggle(toggle);
    }

    void SetupToggle(Toggle toggle, float timeScale)
    {
        if (toggle == null)
            return;

        toggle.onValueChanged.AddListener(isOn =>
        {
            if (!isOn)
                return;

            ApplyTimeScale(toggle, timeScale);
        });

        var pointerHandler = toggle.GetComponent<GameClockToggleHandler>() ?? toggle.gameObject.AddComponent<GameClockToggleHandler>();
        pointerHandler.Initialize(this, toggle, timeScale);
    }

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
            eventSystem.SetSelectedGameObject(_activeToggle.gameObject);
            _activeToggle.Select();
        }
    }

    bool ShouldUpdateSelection(EventSystem eventSystem)
    {
        var selectedGameObject = eventSystem.currentSelectedGameObject;
        if (_activeToggle == null)
            return false;

        if (IsFocusedInputField(selectedGameObject))
            return false;

        if (selectedGameObject == null)
            return true;

        if (!selectedGameObject.transform.IsChildOf(transform))
            return false;

        if (selectedGameObject == _activeToggle.gameObject)
            return false;

        return false;
    }

    bool IsFocusedInputField(GameObject selectedGameObject)
    {
        if (selectedGameObject == null)
            return false;

        var inputField = selectedGameObject.GetComponent<TMP_InputField>();
        return inputField != null && inputField.isFocused;
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
        EnsureActiveToggleState();

        if (_currentClock != null && timeText != null)
            timeText.text = _currentClock.GetFormattedTime();
    }

    void EnsureActiveToggleState()
    {
        if (_activeToggle == null)
            return;

        if (!_activeToggle.isOn)
            _activeToggle.isOn = true;

        var eventSystem = EventSystem.current;
        if (eventSystem == null || !ShouldUpdateSelection(eventSystem))
            return;

        eventSystem.SetSelectedGameObject(_activeToggle.gameObject);
        _activeToggle.Select();
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

            var navigation = toggle.navigation;
            navigation.mode = Navigation.Mode.None;
            toggle.navigation = navigation;
        }

        if (timeScaleToggleGroup != null)
            timeScaleToggleGroup.allowSwitchOff = false;

        if (_currentClock != null)
        {
            SetupToggle(pauseToggle, 0f);
            SetupToggle(scale1Toggle, _currentClock.timeScales[0]);
            SetupToggle(scale2Toggle, _currentClock.timeScales[1]);
            SetupToggle(scale4Toggle, _currentClock.timeScales[2]);
            SetupToggle(scale6Toggle, _currentClock.timeScales[3]);
        }

        UpdateDayText(_currentClock.currentDay);
        _currentClock.OnDayChanged += UpdateDayText;

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

    class GameClockToggleHandler : MonoBehaviour, IPointerDownHandler, IPointerClickHandler, ISubmitHandler, ISelectHandler
    {
        Toggle _toggle;
        GameClockUI _gameClockUI;
        float _timeScale;

        public void Initialize(GameClockUI gameClockUI, Toggle toggle, float timeScale)
        {
            _gameClockUI = gameClockUI;
            _toggle = toggle;
            _timeScale = timeScale;
        }

        bool CanHandle()
        {
            if (_toggle == null || _gameClockUI == null)
                return false;

            var eventSystem = EventSystem.current;
            if (eventSystem == null)
                return false;

            return true;
        }

        void HandleToggleActivation()
        {
            if (!CanHandle())
                return;

            if (!_toggle.isOn)
                _toggle.isOn = true;

            _gameClockUI.ApplyTimeScale(_toggle, _timeScale);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            HandleToggleActivation();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            HandleToggleActivation();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            HandleToggleActivation();
        }

        public void OnSelect(BaseEventData eventData)
        {
            HandleToggleActivation();
        }
    }
}
