using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Game/Dev Item Injector Settings")]
public class DevItemInjectorSettings : MonoBehaviour
{
    [SerializeField] private bool enableInjection = true;
    [SerializeField] private bool verboseLogging = true;
    [SerializeField] private List<DevItemInjector.DevEntry> furnitureItems = new List<DevItemInjector.DevEntry>();
    [SerializeField] private List<DevItemInjector.DevEntry> materialItems = new List<DevItemInjector.DevEntry>();

    public bool EnableInjection => enableInjection;
    public bool VerboseLogging => verboseLogging;
    public IReadOnlyList<DevItemInjector.DevEntry> FurnitureItems => furnitureItems;
    public IReadOnlyList<DevItemInjector.DevEntry> MaterialItems => materialItems;
}
