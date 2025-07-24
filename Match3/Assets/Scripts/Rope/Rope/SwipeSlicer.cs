using UnityEngine;

// Detects swipe gestures on mobile (touch) and mouse input in the editor
// and performs linecasts to detect rope colliders.  When a rope collider is
// hit the owning RopeController is notified to cut the rope.
// Attach this script to a persistent GameObject (e.g. a manager).  Set
// ropeLayerMask to the layer containing your rope segments.  This avoids
// slicing unrelated objects.  Optionally enable DebugDraw to visualise
// swipe traces during development.
public class SwipeSlicer : MonoBehaviour
{
    [Tooltip("Layer mask used to detect rope colliders during slicing.")]
    public LayerMask ropeLayerMask;

    [Tooltip("Draw the swipe lines in the Scene view for debugging.")]
    public bool debugDraw = false;

    private Vector2 lastScreenPos;
    private bool slicing;

    private void Update()
    {
        // Handle touch input on mobile
        // if (Input.touchCount > 0)
        // {
        //     Touch touch = Input.GetTouch(0);
        //     switch (touch.phase)
        //     {
        //         case TouchPhase.Began:
        //             slicing = true;
        //             lastScreenPos = touch.position;
        //             break;
        //         case TouchPhase.Moved:
        //             if (slicing)
        //             {
        //                 TrySlice(lastScreenPos, touch.position);
        //                 lastScreenPos = touch.position;
        //             }
        //             break;
        //         case TouchPhase.Ended:
        //         case TouchPhase.Canceled:
        //             slicing = false;
        //             break;
        //     }
        // }

        // Allow mouse input in the editor for testing
        if (Input.GetMouseButtonDown(0))
        {
            slicing = true;
            lastScreenPos = Input.mousePosition;
        }
        else if (Input.GetMouseButton(0) && slicing)
        {
            Vector2 currentPos = Input.mousePosition;
            TrySlice(lastScreenPos, currentPos);
            lastScreenPos = currentPos;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            slicing = false;
        }
    }

    // Perform a linecast between two screen positions and cut any rope colliders hit.
    private void TrySlice(Vector2 screenStart, Vector2 screenEnd)
    {
        float zDistance = Mathf.Abs(Camera.main.transform.position.z - 0f); 

        if (Camera.main == null)
        {
            Debug.Log("Camera Not found!");
            return;
        }
        Vector3 worldStart = Camera.main.ScreenToWorldPoint(
            new Vector3(screenStart.x, screenStart.y, zDistance)
        );
        Vector3 worldEnd = Camera.main.ScreenToWorldPoint(
            new Vector3(screenEnd.x,   screenEnd.y,   zDistance)
        );
        RaycastHit2D hit = Physics2D.Linecast(worldStart, worldEnd,ropeLayerMask);
        if (debugDraw)
        {
            Debug.DrawLine(worldStart, worldEnd, Color.red, 0.5f);
        }
        if (hit.collider != null)
        {
            RopeController rope = hit.collider.GetComponentInParent<RopeController>();
            if (rope != null)
            {
                rope.CutRopeAt(hit.collider.gameObject);
            }
        }
    }
}
