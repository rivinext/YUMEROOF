using UnityEngine;

public class SitTrigger : MonoBehaviour, IInteractable, IInteractionPromptDataProvider
{
    [SerializeField, Tooltip("List of potential seat anchors. The closest valid anchor to the player will be used when sitting.")]
    private Transform[] seatAnchors;

    [SerializeField]
    private Transform seatAnchor;

    [Header("Interaction Prompt")]
    [SerializeField] private Transform promptAnchor;
    [SerializeField] private float promptOffset = 1f;
    [SerializeField] private string promptLocalizationKey = string.Empty;

    private PlayerController player;

    private void Awake()
    {
        if ((seatAnchors == null || seatAnchors.Length == 0) && seatAnchor == null)
        {
            seatAnchors = new[] { transform };
        }

        if (seatAnchor == null)
        {
            seatAnchor = transform;
        }

        if (promptAnchor == null)
            promptAnchor = transform;
    }

    public void Interact()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.GetComponent<PlayerController>();
            }
        }

        if (player == null) return;

        if (!player.IsSitting)
        {
            Vector3 playerPosition = player.transform.position;
            Transform targetAnchor = GetClosestAnchor(playerPosition);
            player.Sit(targetAnchor, GetComponent<Collider>());
        }
        else
        {
            player.StandUp();
        }
    }

    private Transform GetClosestAnchor(Vector3 position)
    {
        Transform closestAnchor = null;
        float closestSqrDistance = float.MaxValue;

        if (seatAnchors != null && seatAnchors.Length > 0)
        {
            foreach (Transform anchor in seatAnchors)
            {
                if (anchor == null) continue;

                float sqrDistance = (anchor.position - position).sqrMagnitude;
                if (sqrDistance < closestSqrDistance)
                {
                    closestSqrDistance = sqrDistance;
                    closestAnchor = anchor;
                }
            }
        }

        if (closestAnchor == null && seatAnchor != null)
        {
            closestAnchor = seatAnchor;
        }

        return closestAnchor != null ? closestAnchor : transform;
    }
    public bool TryGetInteractionPromptData(out InteractionPromptData data)
    {
        var anchor = promptAnchor != null ? promptAnchor : transform;
        data = new InteractionPromptData(anchor, promptOffset, promptLocalizationKey);
        return true;
    }
}
