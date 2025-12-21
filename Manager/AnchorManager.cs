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

        AlignToWall(obj, anchor, targetAnchor.rotation);
    }

    /// <summary>
    /// Aligns an object using the given reference rotation, avoiding deriving rotation from the normal.
    /// </summary>
    /// <param name="obj">The root object to align.</param>
    /// <param name="anchor">Anchor transform inside the object.</param>
    /// <param name="referenceRotation">Rotation representing the wall basis (forward, up, right).</param>
    public static void AlignToWall(GameObject obj, Transform anchor, Quaternion referenceRotation)
    {
        if (obj == null) return;

        Quaternion targetRotation = referenceRotation;

        if (anchor != null)
        {
            // Adjust by inverse of anchor's local rotation so that the anchor matches the target anchor's pose.
            obj.transform.rotation = targetRotation * Quaternion.Inverse(anchor.localRotation);
        }
        else
        {
            obj.transform.rotation = targetRotation;
        }
    }

    /// <summary>
    /// Aligns an object using the given wall normal and the desired wall rotation, avoiding deriving rotation from the normal.
    /// </summary>
    /// <param name="obj">The root object to align.</param>
    /// <param name="anchor">Anchor transform inside the object.</param>
    /// <param name="wallNormal">The outward normal of the wall.</param>
    /// <param name="wallRotation">Rotation representing the wall basis (forward, up, right).</param>
    public static void AlignToWall(GameObject obj, Transform anchor, Vector3 wallNormal, Quaternion wallRotation)
    {
        if (obj == null) return;

        Quaternion targetRotation = wallRotation;

        if (targetRotation == Quaternion.identity && wallNormal != Vector3.zero)
        {
            targetRotation = Quaternion.LookRotation(-wallNormal);
        }

        AlignToWall(obj, anchor, targetRotation);
    }
}
