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

    private readonly List<RopeSegment> segments = new();
    private LineRenderer lineRenderer;

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

        // Attach end object to last segment if provided
        if (endPoint != null)
        {
            DistanceJoint2D endJoint = endPoint.gameObject.AddComponent<DistanceJoint2D>();
            endJoint.autoConfigureDistance = false;
            endJoint.distance = segmentLength;
            endJoint.connectedBody = segments[^1].GetComponent<Rigidbody2D>();
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
        if (seg != null)
        {
            int index = segments.IndexOf(seg);
            if (index != -1)
            {
                // detach the sliced segment and everything below it
                for (int i = segments.Count - 1; i >= index; i--)
                {
                    segments[i].Cut();
                    segments.RemoveAt(i);
                }

                // shrink the line renderer to the remaining segments
                if (lineRenderer != null)
                {
                    lineRenderer.positionCount = segments.Count + 1;
                }
            }
            else
            {
                // fallback if segment isn't tracked
                seg.Cut();
            }
        }
    }
}
