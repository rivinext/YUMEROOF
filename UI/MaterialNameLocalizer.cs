
using UnityEngine;
using TMPro;
using UnityEngine.Localization;

public class MaterialNameLocalizer : MonoBehaviour
{
    public TMP_Text text;
    public LocalizedString stringReference = new LocalizedString();

    void OnEnable()
    {
        stringReference.StringChanged += UpdateText;
    }

    void OnDisable()
    {
        stringReference.StringChanged -= UpdateText;
    }

    void UpdateText(string value)
    {
        if (text != null)
            text.text = value;
    }

    public void SetEntry(string nameID)
    {
        stringReference.TableReference = "ItemNames";
        stringReference.TableEntryReference = nameID;
        stringReference.RefreshString();
    }
}
