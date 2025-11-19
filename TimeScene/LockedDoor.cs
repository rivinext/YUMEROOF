using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// シーン遷移を行わず、ロック音のみを再生するドアコンポーネント。
/// SceneDoor と同様に AudioManager の音量通知に追従します。
/// </summary>
public class LockedDoor : MonoBehaviour, IInteractable
{
    [Header("Audio Settings")]
    [SerializeField] private AudioClip lockedSfx;
    [Range(0f, 1f)]
    [SerializeField] private float lockedSfxVolume = 1f;
    [Tooltip("同じロック音を連打した際のクールダウン時間 (秒)。0 で無効。")]
    [SerializeField] private float sfxCooldown = 0.4f;

    [Header("UI Settings")]
    [SerializeField] private InteractionUIController interactionUIController;
    [SerializeField] private string speakerName;
    [TextArea]
    [SerializeField] private string lockedMessage;
    [Tooltip("インタラクション終了後に移動入力で自動的に閉じる場合は有効化。")]
    [SerializeField] private bool closeOnMovementAfterMessage = true;

    private AudioSource audioSource;
    private float nextPlayableTime;

    private void OnEnable()
    {
        SetupAudioSource();
        AudioManager.OnSfxVolumeChanged += HandleSfxVolumeChanged;
        HandleSfxVolumeChanged(AudioManager.CurrentSfxVolume);
    }

    private void OnDisable()
    {
        AudioManager.OnSfxVolumeChanged -= HandleSfxVolumeChanged;
    }

    private void SetupAudioSource()
    {
        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    private void HandleSfxVolumeChanged(float volume)
    {
        if (audioSource == null)
        {
            return;
        }

        audioSource.volume = Mathf.Clamp01(volume);
    }

    public void Interact()
    {
        TryPlayLockedSfx();
        ShowLockedMessage();
    }

    private void TryPlayLockedSfx()
    {
        if (lockedSfx == null || audioSource == null)
        {
            return;
        }

        if (sfxCooldown > 0f && Time.time < nextPlayableTime)
        {
            return;
        }

        audioSource.PlayOneShot(lockedSfx, lockedSfxVolume * AudioManager.CurrentSfxVolume);
        nextPlayableTime = Time.time + Mathf.Max(0f, sfxCooldown);
    }

    private void ShowLockedMessage()
    {
        if (interactionUIController == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(lockedMessage))
        {
            return;
        }

        var queue = new Queue<InteractionUIController.InteractionLine>();
        queue.Enqueue(new InteractionUIController.InteractionLine(speakerName, lockedMessage));

        interactionUIController.BeginInteraction(this, queue);
        interactionUIController.SetCloseOnMovementAfterComplete(closeOnMovementAfterMessage);
    }
}
