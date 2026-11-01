using UnityEngine;

/// <summary>Moves a laser forward and removes it after a short lifetime.</summary>
[RequireComponent(typeof(Rigidbody2D))]
public class LaserProjectile : MonoBehaviour
{
    [SerializeField, Min(0f)] private float speed = 12f;
    [SerializeField, Min(0.01f)] private float lifetime = 2f;

    private void Start()
    {
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        body.linearVelocity = (Vector2)transform.up * speed;
        Destroy(gameObject, lifetime);
    }
}
