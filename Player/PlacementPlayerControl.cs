using UnityEngine;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(Rigidbody))]
public class PlacementPlayerControl : MonoBehaviour
{
    private PlayerController playerController;
    private Rigidbody playerRigidbody;

    void Awake()
    {
        CacheComponents();
    }

    void Reset()
    {
        CacheComponents();
    }

    private void CacheComponents()
    {
        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        if (playerRigidbody == null)
            playerRigidbody = GetComponent<Rigidbody>();
    }

    public void DisableControl()
    {
        if (playerController == null || playerRigidbody == null)
            CacheComponents();

        if (playerController != null)
            playerController.enabled = false;

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.isKinematic = true;
        }
    }

    public void EnableControl()
    {
        if (playerController == null || playerRigidbody == null)
            CacheComponents();

        if (playerController != null)
            playerController.enabled = true;

        if (playerRigidbody != null)
            playerRigidbody.isKinematic = false;
    }
}
