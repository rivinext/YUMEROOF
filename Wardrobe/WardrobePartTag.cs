using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class WardrobePartTag : MonoBehaviour
{
    [SerializeField] private string partName = string.Empty;

    public string PartName
    {
        get => partName;
        set => partName = value;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        partName = WardrobePartNameUtility.NormalizePartName(gameObject != null ? gameObject.name : string.Empty);
    }
#endif
}

public static class WardrobePartNameUtility
{
    private static readonly string[] s_allowedPartNames =
    {
        "Accessories",
        "Hair",
        "Head",
        "Neck",
        "Chest",
        "Back",
        "Hip",
        "UpperArm.L",
        "UpperArm.R",
        "LowerArm.L",
        "LowerArm.R",
        "Hand.L",
        "Hand.R",
        "UpperLeg.L",
        "UpperLeg.R",
        "LowerLeg.L",
        "LowerLeg.R",
        "Foot.L",
        "Foot.R"
    };

    private static readonly Dictionary<string, string> s_allowedPartLookup = BuildLookup();

    public static IReadOnlyList<string> AllowedPartNames => s_allowedPartNames;

    public static bool TryGetCanonicalName(string rawName, out string canonicalName)
    {
        canonicalName = string.Empty;

        if (string.IsNullOrEmpty(rawName))
        {
            return false;
        }

        string candidate = Normalize(rawName);
        if (string.IsNullOrEmpty(candidate))
        {
            return false;
        }

        while (!string.IsNullOrEmpty(candidate))
        {
            if (s_allowedPartLookup.TryGetValue(candidate, out canonicalName))
            {
                return true;
            }

            int lastDotIndex = candidate.LastIndexOf('.');
            if (lastDotIndex < 0)
            {
                break;
            }

            candidate = candidate.Substring(0, lastDotIndex).TrimEnd();
        }

        canonicalName = string.Empty;
        return false;
    }

    public static string NormalizePartName(string rawName)
    {
        string canonicalName;
        return TryGetCanonicalName(rawName, out canonicalName) ? canonicalName : string.Empty;
    }

    private static Dictionary<string, string> BuildLookup()
    {
        Dictionary<string, string> lookup = new Dictionary<string, string>(s_allowedPartNames.Length, StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < s_allowedPartNames.Length; i++)
        {
            string name = s_allowedPartNames[i];
            if (!lookup.ContainsKey(name))
            {
                lookup.Add(name, name);
            }
        }

        return lookup;
    }

    private static string Normalize(string rawName)
    {
        string trimmed = rawName.Trim();

        const string PrefixToken = "Part_";
        const string BracketToken = "[Part]";

        if (trimmed.StartsWith(PrefixToken, StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed.Substring(PrefixToken.Length).Trim();
        }

        int bracketIndex = trimmed.IndexOf(BracketToken, StringComparison.OrdinalIgnoreCase);
        if (bracketIndex >= 0)
        {
            trimmed = trimmed.Substring(bracketIndex + BracketToken.Length).Trim();
        }

        return trimmed;
    }
}
