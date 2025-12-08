# DynamicLocalizer Layout Refresh Checklist

Follow this manual flow to verify that UI layouts reflow correctly when switching locales and when forcing a refresh in the editor.

1. In the target scene, add **DynamicLocalizer** to the UI root that owns the localized `TextMeshProUGUI` elements. Assign `Layout Root` to the parent `RectTransform` that should be rebuilt (for example, the container holding vertical or horizontal layout groups). Leave **Force Layout On Locale Change** enabled unless you explicitly need to skip layout rebuilds.
2. Enter Play Mode and open the UI that is driven by `DynamicLocalizer`.
3. Use a locale switcher (e.g., a dropdown bound to `LocalizationSettings.SelectedLocale`) to change languages at least three times, including scripts with noticeably different text lengths (e.g., English, Japanese, German). After each change, verify that wrapping, alignment, and layout spacing update correctly and that no text overlaps.
4. While still in Play Mode, disable and then re-enable the GameObject containing `DynamicLocalizer` to trigger the delayed refresh. Confirm that the layout rebuilds and the text still fits correctly in each locale.
5. With the object selected in the Inspector (either in Play Mode or Edit Mode), run the context menu action **Force Refresh All**. Confirm that text updates to the current locale and the layout is rebuilt (look for spacing or wrapping adjustments that match the chosen language).
6. If `debugMode` is enabled on the component, watch the Console for `[DynamicLocalizer]` messages indicating when layout rebuilds were triggered or skipped. Use these logs to confirm that `Layout Root` is detected (or auto-detected) and that rebuilds occur immediately after locale changes.

Completing this checklist ensures the UI reflows correctly when localized content lengths change and when using the editor shortcut to refresh all localized texts.
