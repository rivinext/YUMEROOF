using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class PlayerEmergencyReturnButton : MonoBehaviour
{
    [SerializeField]
    private string overrideSpawnPointName;

    [SerializeField]
    private float idleCrossFade = 0.1f;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(HandleButtonClicked);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleButtonClicked);
        }
    }

    private void HandleButtonClicked()
    {
        var manager = SceneTransitionManager.Instance;
        bool success = manager != null && manager.ForceReturnPlayerToSpawn(overrideSpawnPointName, idleCrossFade);
        if (!success)
        {
            Debug.LogWarning("Player emergency return failed.", this);
        }
    }
}
