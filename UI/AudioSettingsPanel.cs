using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 設定パネルのBGM/SFXスライダーとAudioManagerを連動させるコンポーネント。
/// </summary>
public class AudioSettingsPanel : MonoBehaviour
{
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    private void Awake()
    {
        InitializeSlider(bgmSlider, AudioManager.CurrentBgmVolume);
        InitializeSlider(sfxSlider, AudioManager.CurrentSfxVolume);
    }

    private void OnEnable()
    {
        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.AddListener(HandleBgmSliderChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.AddListener(HandleSfxSliderChanged);
        }

        AudioManager.OnBgmVolumeChanged += HandleBgmVolumeChanged;
        AudioManager.OnSfxVolumeChanged += HandleSfxVolumeChanged;

        SyncSliders();
    }

    private void OnDisable()
    {
        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.RemoveListener(HandleBgmSliderChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveListener(HandleSfxSliderChanged);
        }

        AudioManager.OnBgmVolumeChanged -= HandleBgmVolumeChanged;
        AudioManager.OnSfxVolumeChanged -= HandleSfxVolumeChanged;
    }

    private void InitializeSlider(Slider slider, float defaultValue)
    {
        if (slider == null)
        {
            return;
        }

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(Mathf.Clamp01(defaultValue));
    }

    private void SyncSliders()
    {
        if (bgmSlider != null)
        {
            bgmSlider.SetValueWithoutNotify(AudioManager.CurrentBgmVolume);
        }

        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(AudioManager.CurrentSfxVolume);
        }
    }

    private void HandleBgmSliderChanged(float value)
    {
        AudioManager.SetBgmVolume(value);
    }

    private void HandleSfxSliderChanged(float value)
    {
        AudioManager.SetSfxVolume(value);
    }

    private void HandleBgmVolumeChanged(float value)
    {
        if (bgmSlider != null)
        {
            bgmSlider.SetValueWithoutNotify(value);
        }
    }

    private void HandleSfxVolumeChanged(float value)
    {
        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(value);
        }
    }
}
