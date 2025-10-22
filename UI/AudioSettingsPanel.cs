using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 設定パネルのBGM/SFXスライダーとAudioVolumeManagerを連動させるコンポーネント。
/// </summary>
public class AudioSettingsPanel : MonoBehaviour
{
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    private void Awake()
    {
        InitializeSlider(bgmSlider, AudioVolumeManager.BgmVolume);
        InitializeSlider(sfxSlider, AudioVolumeManager.SfxVolume);
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

        AudioVolumeManager.OnBgmVolumeChanged += HandleBgmVolumeChanged;
        AudioVolumeManager.OnSfxVolumeChanged += HandleSfxVolumeChanged;

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

        AudioVolumeManager.OnBgmVolumeChanged -= HandleBgmVolumeChanged;
        AudioVolumeManager.OnSfxVolumeChanged -= HandleSfxVolumeChanged;
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
            bgmSlider.SetValueWithoutNotify(AudioVolumeManager.BgmVolume);
        }

        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(AudioVolumeManager.SfxVolume);
        }
    }

    private void HandleBgmSliderChanged(float value)
    {
        AudioVolumeManager.BgmVolume = value;
    }

    private void HandleSfxSliderChanged(float value)
    {
        AudioVolumeManager.SfxVolume = value;
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
