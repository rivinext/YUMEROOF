# Shop Conversation Localization Testing

The shop conversation controller now supports LocalizedString assignments, CSV-based IDs, and explicit fallback dictionaries. Use this checklist to verify localization refreshes when switching locales in the editor:

1. Add the **ShopConversationController** component to a scene object and assign the desired intro/exit lines. You can mix LocalizedStrings (table/entry pairs) and plain IDs that resolve through the CSV or fallback entries.
2. In the Inspector, confirm there are no validation warnings about empty or duplicate keys. Provide unique IDs for CSV/fallback lookups to avoid accidental conflicts.
3. Enter Play Mode and open the shop conversation. Advance to any line so that text is visible.
4. Change the active locale via the **Locale Selector** (e.g., `LocalizationSettings.SelectedLocale`) or the Language dropdown in the UI.
5. Observe that the currently displayed line updates immediately to the new locale. Repeat with several lines, including ones sourced from LocalizedString-only entries and ones that rely on CSV/fallback dictionaries.
6. Exit Play Mode after confirming that locale changes continue to refresh the conversation text consistently.

This flow ensures that both CSV-driven and LocalizedString-driven entries react to runtime locale switches without requiring a scene reload.
