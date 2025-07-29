using UnityEngine;

// Helper component to connect one rope to a specific segment of another rope.
// Add this script to any GameObject and assign the ropes in the inspector.
public class RopeConnector : MonoBehaviour
{
    [Tooltip("Rope that will be re-anchored at runtime.")]
    public RopeController ropeToAttach;

    [Tooltip("Rope providing the segment anchor.")]
    public RopeController targetRope;

    [Tooltip("Index of the segment on the target rope to use as the anchor.")]
    public int targetSegmentIndex;

    [Tooltip("Connect to the start of ropeToAttach when true, otherwise the end.")]
    public bool attachToStart = true;

    private void Start()
    {
        if (ropeToAttach == null || targetRope == null)
            return;

        RopeSegment seg = targetRope.GetSegment(targetSegmentIndex);
        if (seg == null)
            return;

        if (attachToStart)
        {
            ropeToAttach.AttachStartAnchor(seg.transform);
        }
        else
        {
            ropeToAttach.AttachEndAnchor(seg.transform);
        }
    }
}
