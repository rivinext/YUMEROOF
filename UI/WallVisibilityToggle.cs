using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class WallVisibilityToggle : MonoBehaviour
{
    private const string PlayerPrefsKey = "wall_visibility_control_enabled";

    [Header("References")]
    [SerializeField] private Toggle toggle;
    [SerializeField] private WallLayerController wallLayerController;

    [Header("Settings")]
    [SerializeField] private bool defaultEnabled = true;

    private bool currentState;
    private bool toggleCallbackRegistered;
    private Coroutine cacheWallControllerRoutine;

    private void Awake()
    {
        CacheToggleReference();
    }

    private void OnEnable()
    {
        CacheToggleReference();

        currentState = LoadState();
        ApplyState(currentState, save: false);
        StartCachingWallControllerReference();
        SceneManager.sceneLoaded += HandleSceneLoaded;
        RegisterToggleCallback();
    }

    private void OnDisable()
    {
        UnregisterToggleCallback();
        SaveState(currentState);
        StopCachingWallControllerReference();
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void CacheToggleReference()
    {
        if (toggle == null)
        {
            toggle = GetComponent<Toggle>();
        }
    }

    private void StartCachingWallControllerReference()
    {
        StopCachingWallControllerReference();
        cacheWallControllerRoutine = StartCoroutine(CacheWallControllerReference());
    }

    private IEnumerator CacheWallControllerReference()
    {
        while (wallLayerController == null)
        {
            wallLayerController = FindFirstObjectByType<WallLayerController>();

            if (wallLayerController == null)
            {
                yield return null;
                continue;
            }

            ApplyState(currentState, save: false);
        }

        cacheWallControllerRoutine = null;
    }

    private void StopCachingWallControllerReference()
    {
        if (cacheWallControllerRoutine != null)
        {
            StopCoroutine(cacheWallControllerRoutine);
            cacheWallControllerRoutine = null;
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        wallLayerController = null;
        StartCachingWallControllerReference();
    }

    private void RegisterToggleCallback()
    {
        if (toggle != null && !toggleCallbackRegistered)
        {
            toggle.onValueChanged.AddListener(HandleToggleValueChanged);
            toggleCallbackRegistered = true;
        }
    }

    private void UnregisterToggleCallback()
    {
        if (toggle != null && toggleCallbackRegistered)
        {
            toggle.onValueChanged.RemoveListener(HandleToggleValueChanged);
            toggleCallbackRegistered = false;
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
