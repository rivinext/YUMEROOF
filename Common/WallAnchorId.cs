using UnityEngine;

public class WallAnchorId : MonoBehaviour
{
    [SerializeField] private string id;

    public string Id => id;

    public void SetId(string value)
    {
        id = value;
    }
}
