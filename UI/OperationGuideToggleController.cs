using UnityEngine;
using UnityEngine.UI;

public class OperationGuideToggleController : MonoBehaviour
{
    private const string PlayerPrefsKey = "UI.OperationGuide.Enabled";

    [SerializeField] private Toggle operationGuideToggle;
    [SerializeField] private FurniturePlacementTutorialAnimator furniturePlacementTutorialAnimator;
    [SerializeField] private GameObject[] operationGuideCanvases;
    [SerializeField] private bool defaultEnabled = true;

    private bool isInitialized;
    private bool toggleCallbackRegistered;

    private void Awake()
    {
        CacheToggleReference();
    }

    private void OnEnable()
    {
        CacheToggleReference();
        InitializeToggle();
        RegisterToggleCallback();
    }

    private void Start()
    {
        if (!isInitialized)
        {
            InitializeToggle();
            RegisterToggleCallback();
        }
    }

    private void OnDisable()
    {
        UnregisterToggleCallback();
    }

    private void CacheToggleReference()
    {
        if (operationGuideToggle == null)
        {
            operationGuideToggle = GetComponent<Toggle>();
        }
    }

    private void RegisterToggleCallback()
    {
        if (operationGuideToggle != null && !toggleCallbackRegistered)
        {
            operationGuideToggle.onValueChanged.AddListener(HandleToggleValueChanged);
            toggleCallbackRegistered = true;
        }
    }

    private void UnregisterToggleCallback()
    {
        if (operationGuideToggle != null && toggleCallbackRegistered)
        {
            operationGuideToggle.onValueChanged.RemoveListener(HandleToggleValueChanged);
            toggleCallbackRegistered = false;
        }
    }

    private void InitializeToggle()
    {
        bool isOn = LoadState();
        isInitialized = true;
        ApplyState(isOn, save: false);
    }

    private void HandleToggleValueChanged(bool isOn)
    {
        ApplyState(isOn, save: true);
    }

    private void ApplyState(bool isOn, bool save)
    {
        if (operationGuideToggle != null)
        {
            operationGuideToggle.SetIsOnWithoutNotify(isOn);
        }

        if (furniturePlacementTutorialAnimator != null)
        {
            furniturePlacementTutorialAnimator.gameObject.SetActive(isOn);

            if (isOn)
            {
                furniturePlacementTutorialAnimator.Play();
            }
            else
            {
                furniturePlacementTutorialAnimator.Stop();
            }
        }

        if (operationGuideCanvases != null)
        {
            foreach (GameObject canvas in operationGuideCanvases)
            {
                if (canvas == null)
                {
                    continue;
                }

                canvas.SetActive(isOn);
            }
        }

        if (save)
        {
            SaveState(isOn);
        }
    }

    private bool LoadState()
    {
        if (PlayerPrefs.HasKey(PlayerPrefsKey))
        {
            return PlayerPrefs.GetInt(PlayerPrefsKey) == 1;
        }

        return defaultEnabled;
    }

    private void SaveState(bool isOn)
    {
        PlayerPrefs.SetInt(PlayerPrefsKey, isOn ? 1 : 0);
        PlayerPrefs.Save();
    }
}
