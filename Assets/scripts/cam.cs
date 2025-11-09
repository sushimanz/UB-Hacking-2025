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
    public float minZoom = 5f;
    [Tooltip("Maximum distance the camera can zoom out")]
    public float maxZoom = 15f;
    [Tooltip("Padding around players when calculating zoom")]
    public float zoomPadding = 2f;

    private Vector3 velocity = Vector3.zero;

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

        // Adjust camera Z position based on distance (for 2D games, this zooms the camera)
        float targetZoom = Mathf.Clamp(distance + zoomPadding, minZoom, maxZoom);
        desiredPosition.z = -targetZoom;

        // Smoothly move camera using SmoothDamp for butter-smooth movement
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
    }
}
