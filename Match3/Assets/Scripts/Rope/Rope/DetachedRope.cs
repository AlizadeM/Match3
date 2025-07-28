using System.Collections.Generic;
using UnityEngine;

// Updates a LineRenderer to follow detached rope segments while they fall
// and destroys them after a set lifetime.
public class DetachedRope : MonoBehaviour
{
    private List<RopeSegment> segments;
    private LineRenderer lineRenderer;
    private float lifetime;
    private Transform endAnchor;
    private DistanceJoint2D endJoint;
    private bool destructionScheduled;

    private void UpdateLine()
    {
        if (lineRenderer == null || segments == null)
            return;
        int count = segments.Count + (endAnchor != null ? 1 : 0);
        lineRenderer.positionCount = count;
        for (int i = 0; i < segments.Count; i++)
        {
            if (segments[i] != null)
                lineRenderer.SetPosition(i, segments[i].transform.position);
        }
        if (endAnchor != null)
            lineRenderer.SetPosition(count - 1, endAnchor.position);
    }
    private void ScheduleDestruction(float time)
    {
        if (destructionScheduled || time <= 0f)
            return;
        destructionScheduled = true;
        foreach (var seg in segments)
        {
            if (seg != null)
                Destroy(seg.gameObject, time);
        }
        Destroy(gameObject, time);
    }

    // Called by RopeController immediately after creation.
    public void Initialize(List<RopeSegment> segs, LineRenderer template, float destroyAfter, Transform anchor = null)
    {
        segments = segs;
        lifetime = destroyAfter;
        endAnchor = anchor;
        if (endAnchor != null)
        {
            Rigidbody2D anchorRb = endAnchor.GetComponent<Rigidbody2D>();
            foreach (var s in segs)
            {
                foreach (var j in s.GetComponents<DistanceJoint2D>())
                {
                    if (anchorRb != null && j.connectedBody == anchorRb)
                    {
                        endJoint = j;
                        break;
                    }
                    if (anchorRb == null && j.connectedBody == null)
                    {
                        endJoint = j;
                        break;
                    }
                }
                if (endJoint != null)
                    break;
            }
        }

        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        if (template != null)
        {
            lineRenderer.widthMultiplier = template.widthMultiplier;
            lineRenderer.material = template.material;
            lineRenderer.numCapVertices = template.numCapVertices;
            lineRenderer.numCornerVertices = template.numCornerVertices;
            lineRenderer.colorGradient = template.colorGradient;
            lineRenderer.textureMode = template.textureMode;
        }

        foreach (var seg in segments)
        {
            if (seg != null)
            {
                seg.transform.SetParent(transform, true);
            }
        }

        UpdateLine();
        ScheduleDestruction(lifetime);
    }

    private void Update()
    {
        UpdateLine();
    }

    // Handle further cuts on this detached rope in the same way as RopeController.
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
            lineRenderer.positionCount = segments.Count + (endAnchor != null ? 1 : 0);
        }

        UpdateLine();

        seg.Cut();

        bool keepAnchor = false;
        if (endJoint != null)
        {
            RopeSegment anchorSeg = endJoint.GetComponent<RopeSegment>();
            if (bottom.Contains(anchorSeg))
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
            float life = keepAnchor ? -1f : lifetime;
            dr.Initialize(bottom, lineRenderer, life, keepAnchor ? endAnchor : null);
        }

        if (keepAnchor)
        {
            // anchor moves to the newly spawned detached piece
            endAnchor = null;
            if (lineRenderer != null)
            {
                lineRenderer.positionCount = segments.Count;
            }
            lifetime = 5f;
            ScheduleDestruction(lifetime);
        }

        if (segments.Count == 0)
        {
            Destroy(gameObject);
        }
        else
        {
            UpdateLine();
        }
    }
}

