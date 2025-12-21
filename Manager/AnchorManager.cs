using UnityEngine;

public static class AnchorManager
{
    /// <summary>
    /// Aligns an object so that the given anchor's forward (Z) axis faces the wall normal.
    /// The anchor's forward will point toward the wall, i.e., opposite of the normal.
    /// </summary>
    /// <param name="obj">The root object to align.</param>
    /// <param name="anchor">Anchor transform inside the object.</param>
    /// <param name="targetAnchor">Anchor transform on the wall to align to.</param>
    public static void AlignToWall(GameObject obj, Transform anchor, Transform targetAnchor)
    {
        if (obj == null || targetAnchor == null) return;

        Quaternion wallRotation = targetAnchor.rotation;

        if (anchor != null)
        {
            // Adjust by inverse of anchor's local rotation so that the anchor matches the target anchor's pose.
            obj.transform.rotation = wallRotation * Quaternion.Inverse(anchor.localRotation);
        }
        else
        {
            obj.transform.rotation = wallRotation;
        }
    }
}
