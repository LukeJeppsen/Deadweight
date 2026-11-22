using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [Header("Movement Points")]
    [SerializeField] private Transform[] waypoints; // Drop your empty gameobject targets here
    [SerializeField] private float speed = 4f;
    [SerializeField] private float waitTime = 1f;    // How long to pause at the top/bottom

    private int currentWaypointIndex = 0;
    private float cooldownTimer;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // Ensure the platform starts exactly at the first waypoint position
        if (waypoints.Length > 0)
        {
            transform.position = waypoints[0].position;
        }
    }

    void FixedUpdate()
    {
        if (waypoints.Length == 0) return;

        // Handle the waiting pause timer at each stop
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = Vector2.zero; // Make sure it sits perfectly still
            return;
        }

        MoveTowardsTarget();
    }

    private void MoveTowardsTarget()
    {
        Transform target = waypoints[currentWaypointIndex];

        // Calculate the direction vector toward our target position
        Vector2 targetPosition = target.position;
        Vector2 currentPosition = rb.position;
        Vector2 direction = (targetPosition - currentPosition).normalized;

        // Move using Rigidbody physics instead of transform.position for smoother player tracking
        float step = speed * Time.fixedDeltaTime;
        Vector2 newPosition = Vector2.MoveTowards(currentPosition, targetPosition, step);
        rb.MovePosition(newPosition);

        // Check if we are close enough to the waypoint to switch targets
        if (Vector2.Distance(rb.position, targetPosition) < 0.05f)
        {
            // Lock onto exact position to prevent drift
            rb.position = targetPosition;

            // Cycle to the next waypoint index (loops back to 0 at the end)
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;

            // Start the waiting pause
            cooldownTimer = waitTime;
        }
    }

    // --- "Sticky Player" Logic ---
    // --- "Sticky Player" Logic ---
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(transform);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        // Fix: Only alter the hierarchy if both objects are active in the scene!
        // This prevents the error during scene shutdown or object destruction.
        if (gameObject.activeInHierarchy && collision.gameObject.activeInHierarchy)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                collision.transform.SetParent(null);
            }
        }
    }

    // Safety Net: If the platform is destroyed or disabled while the player is standing on it
    private void OnDisable()
    {
        // If the game is shutting down, don't try to unparent anything
        if (!gameObject.scene.isLoaded) return;

        // Find the player in the scene safely
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null && player.transform.parent == transform)
        {
            player.transform.SetParent(null);
        }
    }
}