using System.Collections.Generic;
using UnityEngine;

// Updates a LineRenderer to follow detached rope segments while they fall
// and destroys them after a set lifetime.
public class DetachedRope : MonoBehaviour
{
    private List<RopeSegment> segments;
    private LineRenderer lineRenderer;
    private float lifetime;

    // Called by RopeController immediately after creation.
    public void Initialize(List<RopeSegment> segs, LineRenderer template, float destroyAfter)
    {
        segments = segs;
        lifetime = destroyAfter;

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
                Destroy(seg.gameObject, lifetime);
            }
        }

        // Destroy this container as well after the lifetime
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (lineRenderer == null || segments == null)
            return;

        lineRenderer.positionCount = segments.Count;
        for (int i = 0; i < segments.Count; i++)
        {
            if (segments[i] != null)
            {
                lineRenderer.SetPosition(i, segments[i].transform.position);
            }
        }
    }
}

