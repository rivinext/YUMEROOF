using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ResolutionDropdown : MonoBehaviour
{
    [SerializeField] TMP_Dropdown dropdown;
    Resolution[] resolutions;

    void Start()
    {
        Resolution[] allResolutions = Screen.resolutions;
        var currentRefreshRate = Screen.currentResolution.refreshRateRatio;

        // Only include resolutions that match the current refresh rate and are close to 16:9.
        const float targetAspectRatio = 16f / 9f;
        const float aspectRatioTolerance = 0.01f;

        List<Resolution> filtered = new List<Resolution>();
        foreach (var res in allResolutions)
        {
            float aspectRatio = (float)res.width / res.height;
            if (res.refreshRateRatio.Equals(currentRefreshRate) &&
                Mathf.Abs(aspectRatio - targetAspectRatio) < aspectRatioTolerance)
            {
                filtered.Add(res);
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
        Screen.SetResolution(resolutions[index].width, resolutions[index].height, Screen.fullScreen);
    }
}
