using UnityEngine;

/// <summary>
/// Holds shared configuration for displaying an interaction prompt.
/// </summary>
public struct InteractionPromptData
{
    public Transform Anchor { get; }
    public float HeightOffset { get; }
    public string LocalizationKey { get; }

    public bool IsValid => Anchor != null;

    public InteractionPromptData(Transform anchor, float heightOffset = 1f, string localizationKey = "")
    {
        Anchor = anchor;
        HeightOffset = heightOffset;
        LocalizationKey = localizationKey ?? string.Empty;
    }
}

/// <summary>
/// Implemented by components that can supply prompt data to the shared prompt controller.
/// </summary>
public interface IInteractionPromptDataProvider
{
    bool TryGetInteractionPromptData(out InteractionPromptData data);
}
