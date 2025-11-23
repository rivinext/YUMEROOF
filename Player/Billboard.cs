using UnityEngine;

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

    void Awake()
    {
        TryAssignPlayerTarget();
    }

#if UNITY_EDITOR
    void Reset()
    {
        TryAssignPlayerTarget();
    }
#endif

    void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 direction = target.position - transform.position;

        if (!rotateX)
            direction.x = 0f;
        if (!rotateY)
            direction.y = 0f;
        if (!rotateZ)
            direction.z = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
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
            return;
        }

        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null)
        {
            target = playerController.transform;
            return;
        }

        GameObject taggedPlayer = GameObject.FindWithTag("Player");
        if (taggedPlayer != null)
            target = taggedPlayer.transform;
    }
}
