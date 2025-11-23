using UnityEngine;

public class GhostFloatAnimator : MonoBehaviour
{
    [SerializeField]
    private float amplitude = 0.1f;

    [SerializeField]
    private float speed = 1f;

    private float initialY;

    private void Start()
    {
        initialY = transform.position.y;
    }

    private void Update()
    {
        var position = transform.position;
        position.y = initialY + amplitude * Mathf.Sin(Time.time * speed);
        transform.position = position;
    }
}
