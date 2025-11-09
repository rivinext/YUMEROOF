using UnityEngine;
using UnityEngine.UI;

public class WallVisibilityToggle : MonoBehaviour
{
    private const string PlayerPrefsKey = "wall_visibility_control_enabled";

    [Header("References")]
    [SerializeField] private Toggle toggle;
    [SerializeField] private WallLayerController wallLayerController;

    [Header("Settings")]
    [SerializeField] private bool defaultEnabled = true;

    private bool currentState;

    private void Awake()
    {
        CacheToggleReference();
    }

    private void OnEnable()
    {
        CacheToggleReference();
        CacheWallControllerReference();

        currentState = LoadState();
        ApplyState(currentState, save: false);
        RegisterToggleCallback();
    }

    private void OnDisable()
    {
        UnregisterToggleCallback();
        SaveState(currentState);
    }

    private void CacheToggleReference()
    {
        if (toggle == null)
        {
            toggle = GetComponent<Toggle>();
        }
    }

    private void CacheWallControllerReference()
    {
        if (wallLayerController == null)
        {
            wallLayerController = FindFirstObjectByType<WallLayerController>();
        }
    }

    private void RegisterToggleCallback()
    {
        if (toggle != null)
        {
            toggle.onValueChanged.AddListener(HandleToggleValueChanged);
        }
    }

    private void UnregisterToggleCallback()
    {
        if (toggle != null)
        {
            toggle.onValueChanged.RemoveListener(HandleToggleValueChanged);
        }
    }

    private void HandleToggleValueChanged(bool isOn)
    {
        ApplyState(isOn, save: true);
    }

    private void ApplyState(bool enabled, bool save)
    {
        currentState = enabled;

        if (toggle != null)
        {
            toggle.SetIsOnWithoutNotify(enabled);
        }

        if (wallLayerController != null)
        {
            wallLayerController.VisibilityControlEnabled = enabled;
        }

        if (save)
        {
            SaveState(enabled);
        }
    }

    private bool LoadState()
    {
        if (PlayerPrefs.HasKey(PlayerPrefsKey))
        {
            return PlayerPrefs.GetInt(PlayerPrefsKey) == 1;
        }

        if (wallLayerController != null)
        {
            return wallLayerController.VisibilityControlEnabled;
        }

        return defaultEnabled;
    }

    private void SaveState(bool enabled)
    {
        PlayerPrefs.SetInt(PlayerPrefsKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }
}
