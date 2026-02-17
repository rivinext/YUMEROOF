using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GraphicsSettingsController : MonoBehaviour
{
    private const string TargetFpsKey = "graphics.targetFps";
    private const string VSyncCountKey = "graphics.vsyncCount";
    private const int DefaultTargetFps = 60;
    private const int DefaultVSyncCount = 1;

    [Header("UI References")]
    [SerializeField] private TMP_Dropdown targetFpsDropdown;
    [SerializeField] private Toggle vSyncToggle;
    [SerializeField] private TMP_Text vSyncHintText;

    private readonly List<int> targetFpsValues = new List<int> { 30, 60, 90, 120, -1 };

    private void Start()
    {
        InitializeTargetFpsDropdown();
        ApplySavedSettings();
        RegisterEvents();
        UpdateVSyncHintText();
    }

    private void OnDestroy()
    {
        if (targetFpsDropdown != null)
        {
            targetFpsDropdown.onValueChanged.RemoveListener(OnTargetFpsDropdownValueChanged);
        }

        if (vSyncToggle != null)
        {
            vSyncToggle.onValueChanged.RemoveListener(OnVSyncToggleValueChanged);
        }
    }

    private void InitializeTargetFpsDropdown()
    {
        if (targetFpsDropdown == null)
        {
            return;
        }

        targetFpsDropdown.options.Clear();
        targetFpsDropdown.AddOptions(new List<string>
        {
            "30",
            "60",
            "90",
            "120",
            "無制限"
        });
    }

    private void ApplySavedSettings()
    {
        int savedTargetFps = PlayerPrefs.GetInt(TargetFpsKey, DefaultTargetFps);
        int savedVSyncCount = PlayerPrefs.GetInt(VSyncCountKey, DefaultVSyncCount);

        ApplyTargetFps(savedTargetFps);
        ApplyVSyncCount(savedVSyncCount);

        if (targetFpsDropdown != null)
        {
            int targetFpsIndex = targetFpsValues.IndexOf(savedTargetFps);
            targetFpsDropdown.SetValueWithoutNotify(targetFpsIndex >= 0 ? targetFpsIndex : targetFpsValues.IndexOf(DefaultTargetFps));
            targetFpsDropdown.RefreshShownValue();
        }

        if (vSyncToggle != null)
        {
            vSyncToggle.SetValueWithoutNotify(savedVSyncCount > 0);
        }
    }

    private void RegisterEvents()
    {
        if (targetFpsDropdown != null)
        {
            targetFpsDropdown.onValueChanged.AddListener(OnTargetFpsDropdownValueChanged);
        }

        if (vSyncToggle != null)
        {
            vSyncToggle.onValueChanged.AddListener(OnVSyncToggleValueChanged);
        }
    }

    private void OnTargetFpsDropdownValueChanged(int index)
    {
        if (index < 0 || index >= targetFpsValues.Count)
        {
            return;
        }

        int targetFps = targetFpsValues[index];
        ApplyTargetFps(targetFps);
        PlayerPrefs.SetInt(TargetFpsKey, targetFps);
        PlayerPrefs.Save();
    }

    private void OnVSyncToggleValueChanged(bool isOn)
    {
        int vSyncCount = isOn ? 1 : 0;
        ApplyVSyncCount(vSyncCount);
        PlayerPrefs.SetInt(VSyncCountKey, vSyncCount);
        PlayerPrefs.Save();
        UpdateVSyncHintText();
    }

    private static void ApplyTargetFps(int targetFps)
    {
        Application.targetFrameRate = targetFps;
    }

    private static void ApplyVSyncCount(int vSyncCount)
    {
        QualitySettings.vSyncCount = Mathf.Clamp(vSyncCount, 0, 1);
    }

    private void UpdateVSyncHintText()
    {
        if (vSyncHintText == null)
        {
            return;
        }

        vSyncHintText.text = "※VSync ON時はFPS上限が期待通り効かないことがあります。";
    }
}
