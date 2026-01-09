using UnityEngine;
using UnityEngine.UI;

public class PanelToggleSwitcher : MonoBehaviour
{
    [Header("トグル")]
    public Toggle toggleA;
    public Toggle toggleB;
    public Toggle toggleC;

    [Header("対応するパネル")]
    public GameObject panelA;
    public GameObject panelB;
    public GameObject panelC;

    void Start()
    {
        // 初期状態（Aを表示）など
        ShowPanelA();

        // トグルのイベント登録
        toggleA.onValueChanged.AddListener(isOn => {
            if (isOn) ShowPanelA();
        });
        toggleB.onValueChanged.AddListener(isOn => {
            if (isOn) ShowPanelB();
        });
        toggleC.onValueChanged.AddListener(isOn => {
            if (isOn) ShowPanelC();
        });
    }

    void ShowPanelA()
    {
        panelA.SetActive(true);
        panelB.SetActive(false);
        panelC.SetActive(false);
    }

    void ShowPanelB()
    {
        panelA.SetActive(false);
        panelB.SetActive(true);
        panelC.SetActive(false);
    }

    void ShowPanelC()
    {
        panelA.SetActive(false);
        panelB.SetActive(false);
        panelC.SetActive(true);
    }
}
