using UnityEngine;

public interface IIndependentMaterialColorSaveAccessor
{
    void SaveColor(string slotId, string key, HSVColor color);
    bool TryGetColor(string slotId, string key, out HSVColor color);
}
