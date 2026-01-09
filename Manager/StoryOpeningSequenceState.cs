using System;

public static class StoryOpeningSequenceState
{
    public static bool IsNewStorySession { get; private set; }

    public static event Action<bool> NewStorySessionChanged;

    public static void SetNewStorySession(bool isNewStorySession)
    {
        if (IsNewStorySession == isNewStorySession)
            return;

        IsNewStorySession = isNewStorySession;
        NewStorySessionChanged?.Invoke(IsNewStorySession);
    }

    public static void Reset()
    {
        SetNewStorySession(false);
    }
}
