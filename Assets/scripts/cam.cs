using UnityEngine;

public class cam : MonoBehaviour
{
    [Header("Targets")]
    [Tooltip("First player to track")]
    public Transform player1;
    [Tooltip("Second player to track")]
    public Transform player2;

    [Header("Camera Settings")]
    [Tooltip("How smoothly the camera follows")]
    public float smoothTime = 0.3f;
    [Tooltip("Offset from the center point")]
    public Vector3 offset = new Vector3(0f, 0f, -10f);
    [Tooltip("Minimum distance to maintain between camera and players")]
    public float minZoom = 3f;
    [Tooltip("Maximum distance the camera can zoom out")]
    public float maxZoom = 10f;
    [Tooltip("Padding around players when calculating zoom")]
    public float zoomPadding = 1f;

    [Header("Camera Bounds")]
    [Tooltip("Enable camera bounds to restrict camera movement")]
    public bool useBounds = true;
    [Tooltip("Minimum X position the camera can move to")]
    public float minX = -10f;
    [Tooltip("Maximum X position the camera can move to")]
    public float maxX = 10f;
    [Tooltip("Minimum Y position the camera can move to")]
    public float minY = -5f;
    [Tooltip("Maximum Y position the camera can move to")]
    public float maxY = 5f;

    private Vector3 velocity = Vector3.zero;
    private Camera camd;
    private float zoomVelocity = 0f;

    void Start()
    {
        camd = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (player1 == null || player2 == null)
            return;

        // Calculate the center point between both players (X only)
        float centerX = (player1.position.x + player2.position.x) / 2f;

        // Calculate desired position with offset
        Vector3 desiredPosition = transform.position;
        desiredPosition.x = centerX + offset.x;

        // Calculate distance between players (horizontal only)
        float distance = Mathf.Abs(player1.position.x - player2.position.x);

        // Calculate target orthographic size based on distance
        float targetZoom = Mathf.Clamp(distance + zoomPadding, minZoom, maxZoom);
        
        // Smoothly adjust the orthographic size for zooming
        if (camd != null && camd.orthographic)
        {
            camd.orthographicSize = Mathf.SmoothDamp(camd.orthographicSize, targetZoom, ref zoomVelocity, smoothTime);
        }

        // Keep Z position constant for 2D
        desiredPosition.z = offset.z;

        // Apply bounds to keep camera within specified area
        if (useBounds)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
        }

        // Smoothly move camera using SmoothDamp for butter-smooth movement
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
    }
}
