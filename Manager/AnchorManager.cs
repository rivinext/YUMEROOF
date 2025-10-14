using UnityEngine;

public static class AnchorManager
{
    /// <summary>
    /// Aligns an object so that the given anchor's forward (Z) axis faces the wall normal.
    /// The anchor's forward will point toward the wall, i.e., opposite of the normal.
    /// </summary>
    /// <param name="obj">The root object to align.</param>
    /// <param name="anchor">Anchor transform inside the object.</param>
    /// <param name="wallNormal">Normal of the wall the object is attaching to.</param>
    public static void AlignToWall(GameObject obj, Transform anchor, Vector3 wallNormal)
    {
        if (obj == null) return;

        // Base rotation that points forward opposite to wall normal
        Quaternion wallRotation = Quaternion.LookRotation(-wallNormal);

        if (anchor != null)
        {
            // Adjust by inverse of anchor's local rotation so that anchor's Z axis faces the wall.
            obj.transform.rotation = wallRotation * Quaternion.Inverse(anchor.localRotation);
        }
        else
        {
            obj.transform.rotation = wallRotation;
        }
    }
}
