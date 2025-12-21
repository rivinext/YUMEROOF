using UnityEngine;

/// <summary>
/// 壁にのみ配置可能であることを示すマーカーコンポーネント。
/// </summary>
[DisallowMultipleComponent]
public class WallOnly : MonoBehaviour, IWallPlaceable
{
}
