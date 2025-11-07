using UnityEngine;
using UnityEngine.Localization;

public class SitTrigger : MonoBehaviour, IInteractable, IInteractableBillboardPromptSource
{
    [SerializeField, Tooltip("List of potential seat anchors. The closest valid anchor to the player will be used when sitting.")]
    private Transform[] seatAnchors;

    [SerializeField]
    private Transform seatAnchor;

    [Header("Interaction UI")]
    public GameObject interactionPrompt;

    [SerializeField]
    private InteractableBillboardPrompt prompt;

    [SerializeField]
    private Transform promptAnchor;

    [SerializeField]
    private LocalizedString interactionMessage = new LocalizedString();

    [SerializeField, TextArea]
    private string interactionMessageFallback;

    [SerializeField]
    private Sprite interactionIcon;

    [SerializeField]
    private bool useCustomPromptOffset;

    [SerializeField]
    private Vector3 promptWorldOffset = Vector3.zero;

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
        {
            promptAnchor = seatAnchor != null ? seatAnchor : transform;
        }
    }

    private void Start()
    {
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }

    public bool TryGetPromptRequest(out InteractableBillboardPromptRequest request)
    {
        if (prompt != null)
        {
            request = new InteractableBillboardPromptRequest
            {
                Prompt = prompt,
                Interactable = this,
                Anchor = promptAnchor != null ? promptAnchor : transform,
                WorldOffset = promptWorldOffset,
                LocalizedMessage = interactionMessage,
                FallbackMessage = interactionMessageFallback,
                Icon = interactionIcon,
                HasCustomOffset = useCustomPromptOffset,
                HasFallbackMessage = !string.IsNullOrEmpty(interactionMessageFallback)
            };
            return true;
        }

        request = default;
        return false;
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
}
