using UnityEngine;

public class SceneDoor : MonoBehaviour, IInteractable
{
    [Header("Door Settings")]
    public string targetSceneName;      // 遷移先シーン名
    public string spawnPointName;       // 遷移先のスポーンポイント名

    [Header("Audio Settings")]
    [SerializeField] private AudioClip transitionSfx;
    [Range(0f, 1f)]
    [SerializeField] private float transitionSfxVolume = 1f;

    public void Interact()
    {
        if (!SceneTransitionManager.Instance.IsTransitioning)
        {
            SceneTransitionManager.Instance.TransitionToScene(
                targetSceneName,
                spawnPointName,
                true,
                transitionSfx,
                transitionSfxVolume);
        }
    }
}
