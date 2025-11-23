using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// シーン遷移などで使用するスライドパネルのサンプル実装。
/// 子要素のプレースホルダー画像を初期状態で非表示にし、
/// UISlidePanel を介してスライドイン/アウトを制御します。
/// </summary>
[RequireComponent(typeof(UISlidePanel))]
public class TransitionPanel : MonoBehaviour
{
    [SerializeField] private Image placeholderImage;

    private UISlidePanel slidePanel;

    private void Awake()
    {
        slidePanel = GetComponent<UISlidePanel>();
        if (placeholderImage != null)
            placeholderImage.gameObject.SetActive(false);
    }

    /// <summary>
    /// プレースホルダーを表示しつつパネルを中央へスライドイン。
    /// </summary>
    public void SlideIn()
    {
        if (placeholderImage != null)
            placeholderImage.gameObject.SetActive(true);
        slidePanel.SlideIn();
    }

    /// <summary>
    /// パネルを画面外へスライドアウト。
    /// </summary>
    public void SlideOut()
    {
        slidePanel.SlideOut();
    }
}
