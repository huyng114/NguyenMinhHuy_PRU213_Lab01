using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Reads arrow keys or WASD and moves the spaceship using 2D physics.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 5f;
    [SerializeField, Min(0f)] private float edgePadding = 0.1f;

    [Header("Shooting")]
    [SerializeField] private LaserProjectile laserPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField, Min(0.01f)] private float fireInterval = 0.2f;

    private Rigidbody2D body;
    private Camera gameplayCamera;
    private SpriteRenderer shipRenderer;
    private Vector2 moveInput;
    private float nextFireTime;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        gameplayCamera = Camera.main;
        shipRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        moveInput = Vector2.zero;
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed) moveInput.x -= 1f;
        if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed) moveInput.x += 1f;
        if (keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed) moveInput.y -= 1f;
        if (keyboard.upArrowKey.isPressed || keyboard.wKey.isPressed) moveInput.y += 1f;

        // Keep diagonal movement at the same speed as horizontal movement.
        moveInput = moveInput.normalized;

        if (keyboard.spaceKey.isPressed && Time.time >= nextFireTime)
        {
            Shoot();
        }
    }

    private void Shoot()
    {
        if (laserPrefab == null || firePoint == null) return;

        Instantiate(laserPrefab, firePoint.position, firePoint.rotation);
        if (GameManager.Instance != null) GameManager.Instance.PlayLaserSound();
        nextFireTime = Time.time + fireInterval;
    }

    private void FixedUpdate()
    {
        Vector2 nextPosition = body.position + moveInput * moveSpeed * Time.fixedDeltaTime;
        body.MovePosition(ClampToCamera(nextPosition));
    }

    private Vector2 ClampToCamera(Vector2 position)
    {
        if (gameplayCamera == null) return position;

        // The Gameplay camera faces the XY plane, with no rotation.
        float depth = Vector3.Dot(transform.position - gameplayCamera.transform.position,
            gameplayCamera.transform.forward);
        Vector3 bottomLeft = gameplayCamera.ViewportToWorldPoint(new Vector3(0f, 0f, depth));
        Vector3 topRight = gameplayCamera.ViewportToWorldPoint(new Vector3(1f, 1f, depth));

        // Include the sprite size and pivot offset so the whole ship stays visible.
        Vector2 halfSize = shipRenderer != null
            ? (Vector2)shipRenderer.bounds.extents : Vector2.zero;
        Vector2 centerOffset = shipRenderer != null
            ? (Vector2)(shipRenderer.bounds.center - transform.position) : Vector2.zero;

        float minX = bottomLeft.x + halfSize.x + edgePadding - centerOffset.x;
        float maxX = topRight.x - halfSize.x - edgePadding - centerOffset.x;
        float minY = bottomLeft.y + halfSize.y + edgePadding - centerOffset.y;
        float maxY = topRight.y - halfSize.y - edgePadding - centerOffset.y;

        position.x = minX <= maxX ? Mathf.Clamp(position.x, minX, maxX) : (minX + maxX) * 0.5f;
        position.y = minY <= maxY ? Mathf.Clamp(position.y, minY, maxY) : (minY + maxY) * 0.5f;
        return position;
    }

    private void OnDisable()
    {
        moveInput = Vector2.zero;
    }
}
