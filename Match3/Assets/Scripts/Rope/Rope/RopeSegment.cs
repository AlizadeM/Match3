using UnityEngine;

// Represents a single segment in a rope.  Each segment should have a
// Rigidbody2D and a DistanceJoint2D connecting it to the segment above.
// When Cut() is called the joint is destroyed, causing this segment and
// any segments below to detach from the rope.  Additional behaviours
// (e.g. playing a sound, spawning particles) can be added here.
public class RopeSegment : MonoBehaviour
{
    private DistanceJoint2D joint;

    private void Awake()
    {
        joint = GetComponent<DistanceJoint2D>();
        if (joint == null)
        {
            joint = gameObject.AddComponent<DistanceJoint2D>();
        }
    }

    // Detach this segment from the rope by destroying its joint.
    public void Cut()
    {
        if (joint != null)
        {
            Destroy(joint);
        }
    }
}
