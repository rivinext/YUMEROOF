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
        int currentRefreshRate = Screen.currentResolution.refreshRate;

        List<Resolution> filtered = new List<Resolution>();
        foreach (var res in allResolutions)
        {
            if (res.refreshRate == currentRefreshRate)
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
