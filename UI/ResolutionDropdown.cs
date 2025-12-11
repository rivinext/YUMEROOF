using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ResolutionDropdown : MonoBehaviour
{
    [SerializeField] TMP_Dropdown dropdown;
    Resolution[] resolutions;
    RefreshRate currentRefreshRate;

    void Start()
    {
        Resolution[] allResolutions = Screen.resolutions;
        currentRefreshRate = Screen.currentResolution.refreshRateRatio;

        // Target list of common 16:9 resolutions to present.
        (int width, int height)[] targetResolutions = new (int, int)[]
        {
            (3840, 2160),
            (2560, 1440),
            (1920, 1080),
            (1600, 900),
            (1366, 768),
            (1280, 720),
        };

        List<Resolution> filtered = new List<Resolution>();
        foreach (var target in targetResolutions)
        {
            foreach (var res in allResolutions)
            {
                if (res.width == target.width &&
                    res.height == target.height &&
                    res.refreshRateRatio.Equals(currentRefreshRate))
                {
                    filtered.Add(res);
                    break;
                }
            }
        }

        resolutions = filtered.ToArray();

        dropdown.options.Clear();
        List<string> options = new List<string>();
        int currentIndex = 0;
        for (int i = 0; i < resolutions.Length; i++)
        {
            var res = resolutions[i];
            string option = $"{res.width} x {res.height}";
            options.Add(option);
            if (res.width == Screen.currentResolution.width && res.height == Screen.currentResolution.height)
            {
                currentIndex = i;
            }
        }

        dropdown.AddOptions(options);
        dropdown.value = currentIndex;
        dropdown.RefreshShownValue();
        dropdown.onValueChanged.AddListener(ChangeResolution);
    }

    void ChangeResolution(int index)
    {
        Screen.SetResolution(resolutions[index].width, resolutions[index].height, Screen.fullScreenMode, currentRefreshRate);
    }
}
