using UnityEngine;

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

    public void Cut()
    {
        if (joint != null)
        {
            Destroy(joint);
        }
    }
}
