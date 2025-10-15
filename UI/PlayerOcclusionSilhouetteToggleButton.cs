using Player;
using UnityEngine;
using UnityEngine.UI;

public class PlayerOcclusionSilhouetteToggleButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button toggleButton;
    [SerializeField] private PlayerOcclusionSilhouette occlusionSilhouette;

    [Header("Settings")]
    [SerializeField] private bool startEnabled = true;

    private bool isSilhouetteEnabled;

    private void Awake()
    {
        CacheButtonReference();
        CacheSilhouetteReference();
        InitializeState();
    }

    private void OnEnable()
    {
        RegisterButtonCallback();
        ApplyState();
    }

    private void OnDisable()
    {
        UnregisterButtonCallback();
    }

    private void CacheButtonReference()
    {
        if (toggleButton == null)
        {
            toggleButton = GetComponent<Button>();
        }
    }

    private void CacheSilhouetteReference()
    {
        if (occlusionSilhouette == null)
        {
            occlusionSilhouette = FindObjectOfType<PlayerOcclusionSilhouette>();
        }
    }

    private void InitializeState()
    {
        isSilhouetteEnabled = occlusionSilhouette == null ? startEnabled : occlusionSilhouette.enabled;
        if (!startEnabled && occlusionSilhouette != null)
        {
            isSilhouetteEnabled = false;
            occlusionSilhouette.enabled = false;
        }
    }

    private void RegisterButtonCallback()
    {
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(HandleToggleButtonClicked);
        }
    }

    private void UnregisterButtonCallback()
    {
        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveListener(HandleToggleButtonClicked);
        }
    }

    private void HandleToggleButtonClicked()
    {
        isSilhouetteEnabled = !isSilhouetteEnabled;
        ApplyState();
    }

    private void ApplyState()
    {
        if (occlusionSilhouette == null)
        {
            return;
        }

        occlusionSilhouette.forceSilhouette = false;
        occlusionSilhouette.enabled = isSilhouetteEnabled;
    }

    public void SetSilhouetteEnabled(bool enabled)
    {
        isSilhouetteEnabled = enabled;
        ApplyState();
    }
}
