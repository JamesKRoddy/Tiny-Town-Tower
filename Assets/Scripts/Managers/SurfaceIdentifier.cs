using UnityEngine;

/// <summary>
/// Helper component that can be attached to GameObjects to identify their surface type.
/// Useful for props, buildings, and environmental objects.
/// When a raycast hits this object, the SurfaceDetector will use this to determine the surface type.
/// </summary>
public class SurfaceIdentifier : MonoBehaviour
{
    [Tooltip("The surface type this object represents")]
    public SurfaceType surfaceType = SurfaceType.DEFAULT;
}

