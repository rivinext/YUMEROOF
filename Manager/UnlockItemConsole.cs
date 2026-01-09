using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnlockItemConsole : MonoBehaviour
{
    [SerializeField]
    private Button checkButton;

    private class UnlockData
    {
        public string itemID;
        public string milestoneID;
        public int cozy;
        public int nature;
        public int days;
    }

    private readonly List<UnlockData> unlockItems = new List<UnlockData>();

    void Awake()
    {
        LoadCsv();
        if (checkButton != null)
        {
            checkButton.onClick.AddListener(OnCheckClicked);
        }
    }

    private void LoadCsv()
    {
        TextAsset csvAsset = Resources.Load<TextAsset>("Data/YUME_ROOF - WhenUnlock");
        if (csvAsset == null)
        {
            Debug.LogError("YUME_ROOF - WhenUnlock.csv not found in Resources/Data");
            return;
        }

        string[] lines = csvAsset.text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            string[] tokens = lines[i].Split(',');
            bool allEmpty = true;
            foreach (string token in tokens)
            {
                if (!string.IsNullOrWhiteSpace(token))
                {
                    allEmpty = false;
                    break;
                }
            }

            if (allEmpty)
            {
                continue;
            }

            if (tokens.Length >= 5)
            {
                UnlockData data = new UnlockData
                {
                    itemID = tokens[0].Trim(),
                    milestoneID = tokens[1].Trim(),
                    cozy = int.TryParse(tokens[2], out int cozyVal) ? cozyVal : 0,
                    nature = int.TryParse(tokens[3], out int natureVal) ? natureVal : 0,
                    days = int.TryParse(tokens[4], out int dayVal) ? dayVal : 0,
                };
                unlockItems.Add(data);
            }
        }
    }

    private int GetMilestoneNumber(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return 0;
        }

        int underscoreIndex = id.LastIndexOf('_');
        if (underscoreIndex >= 0 && underscoreIndex < id.Length - 1)
        {
            string numberPart = id.Substring(underscoreIndex + 1);
            if (int.TryParse(numberPart, out int result))
            {
                return result;
            }
        }

        return 0;
    }

    public void OnCheckClicked()
    {
        bool found = false;
        foreach (string id in GetUnlockableItemIds())
        {
            Debug.Log(id);
            found = true;
        }

        if (!found)
        {
            Debug.Log("No items meet current requirements.");
        }
    }

    /// <summary>
    /// Returns the IDs of items that meet the unlock conditions.
    /// </summary>
    public IEnumerable<string> GetUnlockableItemIds()
    {
        string currentMilestone = MilestoneManager.Instance != null
            ? MilestoneManager.Instance.CurrentMilestoneID
            : string.Empty;
        int currentCozy = EnvironmentStatsManager.Instance != null
            ? EnvironmentStatsManager.Instance.CozyTotal
            : 0;
        int currentNature = EnvironmentStatsManager.Instance != null
            ? EnvironmentStatsManager.Instance.NatureTotal
            : 0;
        int currentDay = GameClock.Day;

        foreach (UnlockData data in unlockItems)
        {
            if (GetMilestoneNumber(data.milestoneID) <= GetMilestoneNumber(currentMilestone) &&
                data.cozy <= currentCozy &&
                data.nature <= currentNature &&
                data.days <= currentDay)
            {
                yield return data.itemID;
            }
        }
    }
}
