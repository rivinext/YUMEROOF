using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class WardrobePartTag : MonoBehaviour
{
    [SerializeField] private string partName = string.Empty;
    [SerializeField, HideInInspector] private string lastAutoAssignedPartName = string.Empty;
    [SerializeField, HideInInspector] private bool hasManualOverride = false;

    private static readonly Dictionary<string, string> AllowedPartNames = BuildAllowedPartNames();

    public string PartName
    {
        get => partName;
        set => partName = value;
    }

    private void Reset()
    {
        AutoAssignPartName();
    }

    private void OnValidate()
    {
        AutoAssignPartName();
    }

    private void AutoAssignPartName()
    {
        string extractedPartName;
        if (!TryExtractPartNameFromGameObject(out extractedPartName))
        {
            if (string.IsNullOrEmpty(partName))
            {
                hasManualOverride = false;
                lastAutoAssignedPartName = string.Empty;
            }
            return;
        }

        if (string.IsNullOrEmpty(partName))
        {
            partName = extractedPartName;
            lastAutoAssignedPartName = partName;
            hasManualOverride = false;
            return;
        }

        if (hasManualOverride)
        {
            return;
        }

        if (string.Equals(partName, extractedPartName, StringComparison.OrdinalIgnoreCase))
        {
            partName = extractedPartName;
            lastAutoAssignedPartName = partName;
            hasManualOverride = false;
            return;
        }

        if (string.Equals(partName, lastAutoAssignedPartName, StringComparison.OrdinalIgnoreCase))
        {
            partName = extractedPartName;
            lastAutoAssignedPartName = partName;
            hasManualOverride = false;
            return;
        }

        hasManualOverride = true;
        lastAutoAssignedPartName = partName;
    }

    private bool TryExtractPartNameFromGameObject(out string extractedPartName)
    {
        extractedPartName = string.Empty;

        if (gameObject == null)
        {
            return false;
        }

        string name = gameObject.name;
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        string normalized = NormalizeName(name);
        if (string.IsNullOrEmpty(normalized))
        {
            return false;
        }

        string allowedName;
        if (!AllowedPartNames.TryGetValue(normalized, out allowedName))
        {
            return false;
        }

        extractedPartName = allowedName;
        return true;
    }

    private static Dictionary<string, string> BuildAllowedPartNames()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        Array values = Enum.GetValues(typeof(WardrobeTabType));
        for (int i = 0; i < values.Length; i++)
        {
            string name = values.GetValue(i)?.ToString();
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            result[name] = name;
        }

        return result;
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return string.Empty;
        }

        name = name.Trim();

        int dotIndex = name.LastIndexOf('.');
        if (dotIndex >= 0 && dotIndex < name.Length - 1)
        {
            bool numericSuffix = true;
            for (int i = dotIndex + 1; i < name.Length; i++)
            {
                if (!char.IsDigit(name[i]))
                {
                    numericSuffix = false;
                    break;
                }
            }

            if (numericSuffix)
            {
                name = name.Substring(0, dotIndex);
            }
        }

        return name;
    }
}
