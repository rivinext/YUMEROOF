using UnityEngine;
using UnityEngine.Rendering.Universal;

public class SilhouetteToggleController : MonoBehaviour
{
    // Inspector から PlayerHidden(Render Objects) をドラッグして割り当てる
    [SerializeField] private ScriptableRendererFeature playerHiddenFeature;

    // UI Toggle などから呼ぶ用
    public void SetSilhouetteEnabled(bool enabled)
    {
        if (playerHiddenFeature != null)
        {
            playerHiddenFeature.SetActive(enabled);
        }
    }
}
