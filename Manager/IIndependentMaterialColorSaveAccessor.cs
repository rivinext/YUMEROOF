public interface IIndependentMaterialColorSaveAccessor
{
    bool TryGetColor(string slotKey, string identifier, out HSVColor color);
    void SaveColor(string slotKey, string identifier, HSVColor color);
    IndependentMaterialColorSaveData GetSaveDataForSlot(string slotKey);
}
