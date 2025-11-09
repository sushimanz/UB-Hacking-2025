using UnityEngine;
using System.Collections.Generic;

public class proj : MonoBehaviour
{
    [Header("Projectile Properties")]
    [Tooltip("Collider2D used as the projectile hitbox")]
    public Collider2D projectileHitbox;
    [Tooltip("Maximum lifetime of the projectile in seconds")]
    public float lifetime = 5f;
    [Tooltip("Destroy on hit?")]
    public bool destroyOnHit = true;
    [Tooltip("Speed of the projectile")]
    public float speed = 10f;

    // Attack properties received from the player who spawned this projectile
    private AttackProperties attackProperties;
    // Reference to the player who spawned this projectile (to avoid hitting ourselves)
    private GameObject owner;
    // Track objects hit by this projectile to prevent multiple hits
    private HashSet<GameObject> hitObjects = new HashSet<GameObject>();
    // Direction the projectile is traveling (1 = right, -1 = left)
    private int direction = 1;
    // Lifetime timer
    private float lifetimeTimer = 0f;

    void Start()
    {
        lifetimeTimer = lifetime;
        
        // If no hitbox assigned, try to get one from this GameObject
        if (projectileHitbox == null)
        {
            projectileHitbox = GetComponent<Collider2D>();
        }
    }

    void Update()
    {
        // Move the projectile using transform
        transform.position += new Vector3(direction * speed * Time.deltaTime, 0f, 0f);
        
        // Check for hits each frame
        CheckProjectileHit();
        
        // Update lifetime timer
        lifetimeTimer -= Time.deltaTime;
        if (lifetimeTimer <= 0f)
        {
            Destroy(gameObject);
        }
    }

    // Initialize the projectile with attack data from the player
    public void Initialize(AttackProperties properties, GameObject playerOwner, int travelDirection, float projectileSpeed)
    {
        attackProperties = properties;
        owner = playerOwner;
        direction = travelDirection;
        speed = projectileSpeed;
    }

    // Check for projectile hits (similar to player's CheckAttackHit)
    private void CheckProjectileHit()
    {
        if (projectileHitbox == null)
        {
            return;
        }

        // Get the bounds of the projectile hitbox
        Bounds hitboxBounds = projectileHitbox.bounds;

        // Use OverlapBox to find all colliders in the same area as the projectile
        Collider2D[] results = Physics2D.OverlapBoxAll(hitboxBounds.center, hitboxBounds.size, 0f);
        
        foreach (Collider2D col in results)
        {
            // Don't hit ourselves or our own hitbox
            if (col.gameObject == gameObject || col == projectileHitbox)
            {
                continue;
            }

            // Only hit colliders with the "hurtbox" tag
            if (!col.CompareTag("hurtbox"))
            {
                continue;
            }

            // Don't hit the owner's hurtbox
            if (owner != null)
            {
                if (col.gameObject == owner || 
                    (col.transform.parent != null && col.transform.parent.gameObject == owner))
                {
                    continue;
                }
            }

            // Only hit each object once
            if (hitObjects.Contains(col.gameObject))
            {
                continue;
            }
                
            Debug.Log("Projectile hit: " + col.gameObject.name);
            
            // Add to hit list
            hitObjects.Add(col.gameObject);
            
            // Calculate knockback direction (based on projectile direction)
            Vector2 knockbackDir = new Vector2(direction, 0f).normalized;
            
            // Create hit info and send
            HitInfo hitInfo = new HitInfo(
                attackProperties.damage, 
                knockbackDir, 
                attackProperties.knockbackForce, 
                attackProperties.knockbackUpward, 
                attackProperties.stunDuration, 
                attackProperties.hitstopDuration,
                attackProperties.attackHeight
            );
            
            // Send message to the parent of the hurtbox collider
            if (col.transform.parent != null)
            {
                col.transform.parent.gameObject.SendMessage("OnHit", hitInfo, SendMessageOptions.DontRequireReceiver);
            }
            else
            {
                // If no parent, send to the collider's own GameObject
                col.gameObject.SendMessage("OnHit", hitInfo, SendMessageOptions.DontRequireReceiver);
            }
            
            // Destroy projectile on hit if enabled
            if (destroyOnHit)
            {
                Destroy(gameObject);
                return; // Exit early since we're destroying this object
            }
        }
    }
}
