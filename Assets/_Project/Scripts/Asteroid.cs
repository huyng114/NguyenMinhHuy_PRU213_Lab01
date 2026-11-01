using UnityEngine;

/// <summary>Drifts in a random direction and breaks when hit by a laser.</summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Asteroid : MonoBehaviour
{
    [SerializeField, Min(0f)] private float minSpeed = 1f;
    [SerializeField, Min(0f)] private float maxSpeed = 3f;
    [SerializeField, Min(0f)] private float rotationSpeed = 60f;
    [SerializeField, Min(0.01f)] private float lifetime = 15f;

    private bool destroyed;

    private void Start()
    {
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        float angle = Random.Range(0f, Mathf.PI * 2f);
        Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        body.linearVelocity = direction * Random.Range(minSpeed, Mathf.Max(minSpeed, maxSpeed));
        body.angularVelocity = Random.Range(-rotationSpeed, rotationSpeed);
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (destroyed) return;

        LaserProjectile laser = other.GetComponentInParent<LaserProjectile>();
        if (laser == null) return;

        destroyed = true;
        if (GameManager.Instance != null) GameManager.Instance.PlayExplosionSound();
        // Disable the projectile immediately so it cannot hit another asteroid this frame.
        Collider2D[] laserColliders = laser.GetComponentsInChildren<Collider2D>();
        foreach (Collider2D laserCollider in laserColliders)
            laserCollider.enabled = false;

        Destroy(laser.gameObject);
        Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (destroyed || collision.gameObject.GetComponentInParent<PlayerController>() == null) return;
        if (GameManager.Instance != null) GameManager.Instance.GameOver();
    }
}
