using UnityEngine;

[DisallowMultipleComponent]
public sealed class WardrobePartTag : MonoBehaviour
{
    [SerializeField] private string partName = string.Empty;

    public string PartName
    {
        get => partName;
        set => partName = value;
    }
}
