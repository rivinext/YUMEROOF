using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "InventoryCardAudioProfile", menuName = "Audio/Inventory Card Audio Profile")]
public class InventoryCardAudioProfile : ScriptableObject
{
    [Header("Clips")]
    public AudioClip hoverClip;
    public AudioClip clickClip;

    [Header("Mixer Settings")]
    public AudioMixerGroup outputMixerGroup;
    public AudioRolloffMode rolloffMode = AudioRolloffMode.Linear;

    [Header("Playback Settings")]
    [Range(0f, 1f)]
    public float baseVolume = 1f;
    [Min(0f)]
    public float pitchRandomization = 0f;
    [Min(0f)]
    public float cooldownSeconds = 0.05f;
}
