using UnityEngine;
using TMPro;

public class EnvironmentStatsDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text cozyText;
    [SerializeField] private TMP_Text natureText;

    void Start()
    {
        if (EnvironmentStatsManager.Instance != null)
        {
            EnvironmentStatsManager.Instance.OnStatsChanged += UpdateDisplay;
            UpdateDisplay(EnvironmentStatsManager.Instance.CozyTotal,
                          EnvironmentStatsManager.Instance.NatureTotal);
        }
    }

    void OnDestroy()
    {
        if (EnvironmentStatsManager.Instance != null)
        {
            EnvironmentStatsManager.Instance.OnStatsChanged -= UpdateDisplay;
        }
    }

    void UpdateDisplay(int cozy, int nature)
    {
        if (cozyText != null)
        {
            cozyText.text = $"Cozy: {cozy}";
        }
        if (natureText != null)
        {
            natureText.text = $"Nature: {nature}";
        }
    }
}
