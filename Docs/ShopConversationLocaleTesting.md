# Shop Conversation Localization Testing

The shop conversation controller now supports LocalizedString assignments and CSV-based IDs. Use this checklist to verify localization refreshes when switching locales in the editor:

1. Add the **ShopConversationController** component to a scene object and assign the desired intro/exit lines. You can mix LocalizedStrings (table/entry pairs) and plain IDs that resolve through the CSV or the localization table.
2. In the Inspector, confirm there are no validation warnings about empty or duplicate keys. Provide unique IDs for CSV/localization lookups to avoid accidental conflicts.
3. Enter Play Mode and open the shop conversation. Advance to any line so that text is visible.
4. Change the active locale via the **Locale Selector** (e.g., `LocalizationSettings.SelectedLocale`) or the Language dropdown in the UI.
5. Observe that the currently displayed line updates immediately to the new locale. Repeat with several lines, including ones sourced from LocalizedString-only entries and ones that rely on CSV lookups.
6. Exit Play Mode after confirming that locale changes continue to refresh the conversation text consistently.

## CSV-free operation

* When you rely solely on localization tables, unresolved keys fall back to showing the key itself (for example, `greeting` or `farewell`).
* Keep all translated strings for shop conversations inside the dedicated localization table (e.g., `ShopConversation`) rather than hard-coded fallbacks.
* Optionally provide a CSV for bulk editing, but if you omit it the controller will only use Localization Table entries to render text.

This flow ensures that both CSV-driven and LocalizedString-driven entries react to runtime locale switches without requiring a scene reload.
