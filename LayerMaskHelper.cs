using UnityEngine;

public static class LayerMaskHelper
{
    private static int invisibleWallMask = -1;

    private static int InvisibleWallMask
    {
        get
        {
            if (invisibleWallMask == -1)
            {
                invisibleWallMask = LayerMask.GetMask("InvisibleWall");
            }

            return invisibleWallMask;
        }
    }

    public static int ExcludeInvisibleWall(int mask)
    {
        int maskToExclude = InvisibleWallMask;
        if (maskToExclude == 0)
        {
            return mask;
        }

        return mask & ~maskToExclude;
    }

    public static int ExcludeInvisibleWall(LayerMask mask)
    {
        return ExcludeInvisibleWall(mask.value);
    }
}
