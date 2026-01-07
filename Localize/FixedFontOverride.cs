using TMPro;
using UnityEngine;

/// <summary>
/// 固定フォントを指定して再適用するためのコンポーネント。
/// GlobalFontManagerの自動更新から対象を除外したい場合に使用する。
/// </summary>
public class FixedFontOverride : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset fixedFont;
    [SerializeField] private bool includeChildren = false;

    private void OnEnable()
    {
        ApplyFixedFont();
    }

    private void OnValidate()
    {
        ApplyFixedFont();
    }

    private void ApplyFixedFont()
    {
        if (fixedFont == null)
        {
            return;
        }

        if (includeChildren)
        {
            TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in texts)
            {
                text.font = fixedFont;
            }
            return;
        }

        TextMeshProUGUI targetText = GetComponent<TextMeshProUGUI>();
        if (targetText != null)
        {
            targetText.font = fixedFont;
        }
    }
}
