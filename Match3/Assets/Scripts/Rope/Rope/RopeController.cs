using UnityEngine;
using System.Collections.Generic;

public class RopeController : MonoBehaviour
{
    public Transform startPoint;

    public Transform endPoint;

    public GameObject segmentPrefab;
    
    public int segmentCount = 15;
    public float segmentLength = 0.2f;
    public LayerMask ropeLayer;

    private readonly List<RopeSegment> segments = new();
    private LineRenderer lineRenderer;
    private DistanceJoint2D endJoint;

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

        List<RopeSegment> bottom = segments.GetRange(index, segments.Count - index);

        segments.RemoveRange(index, segments.Count - index);

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = segments.Count + 1;
        }

        seg.Cut();

        bool keepAnchor = false;
        if (endJoint != null)
        {
            RopeSegment endSeg = endJoint.GetComponent<RopeSegment>();
            if (bottom.Contains(endSeg))
            {
                keepAnchor = true;
                endJoint = null;
            }
        }

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