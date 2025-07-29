using UnityEngine;
using System.Collections.Generic;

// This component builds and draws a simple 2D rope between two points using
// DistanceJoint2D constraints.  Each rope segment is a prefab containing
// a Rigidbody2D and a DistanceJoint2D (configured as a single joint to the
// previous segment).  A LineRenderer on the same GameObject renders the
// rope visually.  When a segment's joint is destroyed (e.g. by a slice),
// all segments below will detach and fall under physics.
//
// Usage:
//   1. Create an empty GameObject and add a LineRenderer component.
//   2. Attach this RopeController script to the GameObject.
//   3. Assign the startPoint (anchor) and optional endPoint (payload) transforms.
//   4. Create a segment prefab containing a Rigidbody2D, a CircleCollider2D (or
//      other collider) and a DistanceJoint2D.  The DistanceJoint2D should
//      initially have no connected body; the script configures it at runtime.
//   5. Set segmentCount and segmentLength to control rope length.
//   6. Optionally assign a layer to the segment colliders and set ropeLayer
//      on this component.  When slicing (see SwipeSlicer.cs), only colliders
//      on ropeLayer will be detected.
public class RopeController : MonoBehaviour
{
    [Tooltip("The fixed anchor point at the top of the rope.")]
    public Transform startPoint;

    [Tooltip("Optional end object to attach to the last rope segment.")]
    public Transform endPoint;

    [Tooltip("Prefab used for each rope segment.  Requires a Rigidbody2D, a collider and a DistanceJoint2D.")]
    public GameObject segmentPrefab;

    [Tooltip("Number of segments in the rope.")]
    public int segmentCount = 15;

    [Tooltip("Distance between rope segment centres.")]
    public float segmentLength = 0.2f;

    [Tooltip("Layer mask used by the slicer to detect rope colliders.")]
    public LayerMask ropeLayer;

    [Header("Connect To Another Rope")]
    [Tooltip("Optional rope whose segment will act as this rope's start anchor.")]
    public RopeController startSegmentRope;
    [Tooltip("Segment index on startSegmentRope to attach to.")]
    public int startSegmentIndex;

    [Tooltip("Optional rope whose segment will act as this rope's end anchor.")]
    public RopeController endSegmentRope;
    [Tooltip("Segment index on endSegmentRope to attach to.")]
    public int endSegmentIndex;

    private readonly List<RopeSegment> segments = new();
    private LineRenderer lineRenderer;
    private DistanceJoint2D endJoint;

    // Allow other scripts to query segments for dynamic connections
    public RopeSegment GetSegment(int index)
    {
        if (index < 0 || index >= segments.Count)
            return null;
        return segments[index];
    }

    // Attach the rope's starting joint to a different anchor at runtime.
    public void AttachStartAnchor(Transform newAnchor)
    {
        if (segments.Count == 0 || newAnchor == null)
            return;
        startPoint = newAnchor;
        DistanceJoint2D joint = segments[0].GetComponent<DistanceJoint2D>();
        Rigidbody2D rb = newAnchor.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            joint.connectedBody = rb;
            joint.connectedAnchor = Vector2.zero;
        }
        else
        {
            joint.connectedBody = null;
            joint.connectedAnchor = newAnchor.position;
        }
    }

    // Attach or move the end joint to a new anchor at runtime.
    public void AttachEndAnchor(Transform newAnchor)
    {
        if (segments.Count == 0)
            return;

        if (endJoint == null)
        {
            endJoint = segments[^1].gameObject.AddComponent<DistanceJoint2D>();
            endJoint.autoConfigureDistance = false;
            endJoint.distance = segmentLength;
        }

        endPoint = newAnchor;

        if (newAnchor != null)
        {
            Rigidbody2D rb = newAnchor.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                endJoint.connectedBody = rb;
                endJoint.connectedAnchor = Vector2.zero;
            }
            else
            {
                endJoint.connectedBody = null;
                endJoint.connectedAnchor = newAnchor.position;
            }
        }
        else
        {
            Destroy(endJoint);
            endJoint = null;
        }
    }

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            Debug.LogError("RopeController requires a LineRenderer component on the same GameObject.");
        }
    }

    private void Start()
    {
        if (segmentPrefab == null || startPoint == null)
        {
            Debug.LogError("RopeController is missing required references.");
            return;
        }
        BuildRope();
        StartCoroutine(ApplySegmentConnections());
    }

    private System.Collections.IEnumerator ApplySegmentConnections()
    {
        // wait one frame so other ropes can finish building
        yield return null;

        if (startSegmentRope != null)
        {
            RopeSegment seg = startSegmentRope.GetSegment(startSegmentIndex);
            if (seg != null)
            {
                AttachStartAnchor(seg.transform);
            }
        }

        if (endSegmentRope != null)
        {
            RopeSegment seg = endSegmentRope.GetSegment(endSegmentIndex);
            if (seg != null)
            {
                AttachEndAnchor(seg.transform);
            }
        }
    }

    private void Update()
    {
        DrawRope();
    }

    // Build the rope by instantiating segment prefabs and configuring their joints.
    private void BuildRope()
    {
        Vector2 segmentPosition = startPoint.position;
        Rigidbody2D previousBody = startPoint.GetComponent<Rigidbody2D>();
        for (int i = 0; i < segmentCount; i++)
        {
            GameObject seg = Instantiate(segmentPrefab, segmentPosition, Quaternion.identity, transform);
            RopeSegment ropeSeg = seg.GetComponent<RopeSegment>();
            if (ropeSeg == null)
            {
                ropeSeg = seg.AddComponent<RopeSegment>();
            }
            segments.Add(ropeSeg);
            Rigidbody2D rb = seg.GetComponent<Rigidbody2D>();
            DistanceJoint2D joint = seg.GetComponent<DistanceJoint2D>();
            if (joint == null)
            {
                joint = seg.AddComponent<DistanceJoint2D>();
            }
            joint.autoConfigureDistance = false;
            joint.distance = segmentLength;
            if (previousBody != null)
            {
                joint.connectedBody = previousBody;
            }
            else
            {
                joint.connectedAnchor = startPoint.position;
            }

            // assign rope layer to the collider for slicing detection
            if (ropeLayer.value != 0)
            {
                Collider2D col = seg.GetComponent<Collider2D>();
                if (col != null)
                {
                    int layerIndex = (int)Mathf.Log(ropeLayer.value, 2);
                    seg.layer = layerIndex;
                }
            }

            segmentPosition.y -= segmentLength;
            previousBody = rb;
        }

        // Attach the last segment to the end point if provided.  The joint is
        // placed on the last segment so the end point itself does not require
        // a Rigidbody2D (e.g. when it is just a static anchor).
        if (endPoint != null)
        {
            endJoint = segments[^1].gameObject.AddComponent<DistanceJoint2D>();
            endJoint.autoConfigureDistance = false;
            endJoint.distance = segmentLength;

            Rigidbody2D anchorRb = endPoint.GetComponent<Rigidbody2D>();
            if (anchorRb != null)
            {
                endJoint.connectedBody = anchorRb;
            }
            else
            {
                endJoint.connectedBody = null;
                endJoint.connectedAnchor = endPoint.position;
            }
        }
    }

    // Update the LineRenderer positions based on current segment positions.
    private void DrawRope()
    {
        if (lineRenderer == null || startPoint == null)
            return;
        int count = segments.Count + 1;
        if (lineRenderer.positionCount != count)
        {
            lineRenderer.positionCount = count;
        }
        lineRenderer.SetPosition(0, startPoint.position);
        for (int i = 0; i < segments.Count; i++)
        {
            lineRenderer.SetPosition(i + 1, segments[i].transform.position);
        }
    }

    // Called by SwipeSlicer when a rope collider is sliced.  Finds the corresponding
    // segment and cuts it by destroying its joint.  This detaches the sliced
    // segment and all segments below from the anchor.
    public void CutRopeAt(GameObject hitSegment)
    {
        RopeSegment seg = hitSegment.GetComponent<RopeSegment>();
        if (seg == null)
            return;

        int index = segments.IndexOf(seg);
        if (index == -1)
        {
            seg.Cut();
            return;
        }

        // bottom part including the cut segment
        List<RopeSegment> bottom = segments.GetRange(index, segments.Count - index);

        // remove them from this rope
        segments.RemoveRange(index, segments.Count - index);

        // shrink the line renderer to the remaining segments
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = segments.Count + 1;
        }

        // cut only the first segment so the chain stays intact
        seg.Cut();

        bool keepAnchor = false;
        if (endJoint != null)
        {
            RopeSegment endSeg = endJoint.GetComponent<RopeSegment>();
            if (bottom.Contains(endSeg))
            {
                // piece stays attached to the end object
                keepAnchor = true;
                endJoint = null;
            }
        }

        // create a temporary object to render the detached piece
        if (bottom.Count > 0)
        {
            GameObject temp = new("DetachedRope");
            DetachedRope dr = temp.AddComponent<DetachedRope>();
            temp.AddComponent<LineRenderer>();
            float lifetime = keepAnchor ? -1f : 5f;
            dr.Initialize(bottom, lineRenderer, lifetime, keepAnchor ? endPoint : null);
        }
    }
}
