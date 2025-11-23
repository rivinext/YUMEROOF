using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Billboard component that rotates this object to face a target while optionally
/// locking rotation per-axis. Enable only Rotate Y for a classic upright billboard.
/// </summary>
public class Billboard : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Transform the billboard should face. Left empty defaults to the player if found.")]
    public Transform target;

    [Header("Axis Constraints")]
    [Tooltip("Allow rotation toward the target around the X axis (pitch). Disable for upright-only look.")]
    public bool rotateX = false;
    [Tooltip("Allow rotation toward the target around the Y axis (yaw). Enable this and disable the others for Y-only rotation.")]
    public bool rotateY = true;
    [Tooltip("Allow rotation toward the target around the Z axis (roll). Usually disabled for billboards.")]
    public bool rotateZ = false;

    private bool runtimeReassignAttempted;

    void Awake()
    {
        TryAssignPlayerTarget();
    }

    void OnEnable()
    {
        runtimeReassignAttempted = false;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        TryAssignPlayerTarget();
    }

    void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

#if UNITY_EDITOR
    void Reset()
    {
        TryAssignPlayerTarget();
    }
#endif

    void OnActiveSceneChanged(Scene previousScene, Scene newScene)
    {
        runtimeReassignAttempted = false;
        TryAssignPlayerTarget();
    }

    void LateUpdate()
    {
        if (target == null)
        {
            if (!runtimeReassignAttempted)
            {
                runtimeReassignAttempted = true;
                TryAssignPlayerTarget();
            }

            return;
        }

        Vector3 direction = target.position - transform.position;

        // Constrain pitch by removing vertical difference.
        if (!rotateX)
            direction.y = 0f;
        // Constrain yaw by ignoring horizontal plane difference.
        if (!rotateY)
        {
            direction.x = 0f;
            direction.z = 0f;
        }

        // Preserve roll constraints by choosing the appropriate up vector.
        Vector3 upVector = rotateZ && target != null ? target.up : Vector3.up;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(direction, upVector);
    }

    private void TryAssignPlayerTarget()
    {
        if (target != null)
            return;

        // Prefer explicit player components so the billboard can follow the player automatically.
        PlayerManager playerManager = FindFirstObjectByType<PlayerManager>();
        if (playerManager != null)
        {
            target = playerManager.transform;
            runtimeReassignAttempted = false;
            return;
        }

        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null)
        {
            target = playerController.transform;
            runtimeReassignAttempted = false;
            return;
        }

        GameObject taggedPlayer = GameObject.FindWithTag("Player");
        if (taggedPlayer != null)
        {
            target = taggedPlayer.transform;
            runtimeReassignAttempted = false;
        }
    }
}
